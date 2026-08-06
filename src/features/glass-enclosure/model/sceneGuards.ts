import { developedLengthMm } from './arcGeometry';
import { normalizePanelOutlineJson, refitPanelShape } from './panelShapeOutline';
import { notifyPanelOutlineRejected } from './panelOutlineFeedback';
import type {
  ScenePanelState,
  SceneRunState,
  SceneSlabState,
  SceneSurfaceState,
  SceneWallOpening,
  SceneWallState,
} from './project.types';

/**
 * The ONE place body dimensions are floored and locked bodies are protected.
 *
 * WHY a single layer instead of a check per editor: the same property had up to four different
 * floors reachable from the same screen (wall thickness: inspector 50, transform toolbar 10, drag
 * gizmo 50, raw store write unbounded), and several properties had none at all — a panel height of
 * 99999 stretched the glass to 100 m, a slab could be committed at -500 mm length, and a body
 * marked "locked" was still editable from every panel because only the 3D builders honoured the
 * flag. Editors come and go; the store is the single writer every one of them funnels through, so
 * the invariant belongs here. The floors are the ones already proven in the drag gizmos and
 * inspectors, not new numbers.
 */
export const BODY_FLOOR_MM = {
  wallLength: 100,
  wallHeight: 100,
  wallThickness: 50,
  slabPlan: 100,
  slabThickness: 50,
  surfaceThickness: 20,
  runLength: 100,
  runHeight: 100,
  panelWidth: 100,
  panelHeight: 100,
} as const;

const OPENING_MIN_MM = 100;
const OPENING_EDGE_MM = 20;

const floorMm = (value: number, floor: number) => Math.max(floor, Math.round(value));

/**
 * Fit an opening inside the wall that carries it. Exported because the same rule has to run both
 * when the opening is edited and when the WALL changes shape underneath it.
 */
export const clampWallOpening = (
  wall: SceneWallState,
  opening: SceneWallOpening,
): SceneWallOpening => {
  const faceLen = developedLengthMm(wall.lengthMm, wall.geomArcRadiusMm, wall.geomArcSweepDeg);
  const topLimit = Math.max(1, Math.min(wall.heightMm, wall.heightEndMm ?? wall.heightMm));
  const widthCap = Math.max(OPENING_MIN_MM, faceLen - 2 * OPENING_EDGE_MM);
  const widthMm = Math.max(OPENING_MIN_MM, Math.min(opening.widthMm, widthCap));
  const halfW = widthMm / 2;
  const offsetMm = Math.min(Math.max(opening.offsetMm, halfW), Math.max(halfW, faceLen - halfW));
  const heightMm = Math.max(OPENING_MIN_MM, Math.min(opening.heightMm, topLimit));
  const sillMm = Math.min(Math.max(0, opening.sillMm), Math.max(0, topLimit - heightMm));
  return {
    ...opening,
    offsetMm: Math.round(offsetMm),
    widthMm: Math.round(widthMm),
    sillMm: Math.round(sillMm),
    heightMm: Math.round(heightMm),
  };
};

/**
 * Is this write allowed to touch a locked body?
 *
 * A locked body still has to be UNLOCKABLE, so a patch that only flips `locked` always passes.
 * Anything else is refused, which is what "locked" has meant in the 3D scene all along.
 */
export const blockedByLock = (
  body: { locked?: boolean | null } | undefined,
  patch: Record<string, unknown>,
): boolean => {
  if (!body?.locked) return false;
  return Object.keys(patch).some((key) => key !== 'locked');
};

// WHY deletion needs its own gate: blockedByLock inspects a PATCH, and a delete carries none —
// so every remove* action wrote straight through and a locked body could be erased from the
// layer list, the context menu or a multi-delete. The lock is meant to survive exactly that.
export const blockedByLockOnDelete = (body: { locked?: boolean | null } | undefined): boolean =>
  Boolean(body?.locked);

/**
 * Every id in the scene that carries the lock, for the BULK paths.
 *
 * WHY: the single-body setters all gate on `blockedByLock`, but the group move, the wall move with
 * its attached glass, the Alt-stack, the rotate, the multi-delete and the grouping button all write
 * through `applyScenePatch`, which sees no guard at all. So the lock only ever held against a
 * DIRECT edit: put a locked pane in a multi-selection, or drag the unlocked wall its glass is bonded
 * to, and it moved anyway. These paths filter their id sets through this and report the rejection.
 */
export const lockedBodyIds = (scene: {
  walls?: { id: string; locked?: boolean | null }[] | null;
  runs?: { id: string; locked?: boolean | null }[] | null;
  slabs?: { id: string; locked?: boolean | null }[] | null;
  surfaces?: { id: string; locked?: boolean | null }[] | null;
}): Set<string> => {
  const out = new Set<string>();
  for (const group of [scene.walls, scene.runs, scene.slabs, scene.surfaces]) {
    for (const body of group ?? []) if (body.locked) out.add(body.id);
  }
  return out;
};

/** Remove the locked members from a moving/deleting set; `blocked` drives the toast. */
export const dropLockedIds = (
  ids: Iterable<string>,
  locked: Set<string>,
): { ids: Set<string>; blocked: boolean } => {
  const out = new Set<string>();
  let blocked = false;
  for (const id of ids) {
    if (locked.has(id)) blocked = true;
    else out.add(id);
  }
  return { ids: out, blocked };
};

const WALL_SHAPE_KEYS = [
  'lengthMm',
  'heightMm',
  'heightEndMm',
  'geomArcRadiusMm',
  'geomArcSweepDeg',
] as const;

export const clampWallPatch = (
  wall: SceneWallState,
  patch: Partial<SceneWallState>,
): Partial<SceneWallState> => {
  const next: Partial<SceneWallState> = { ...patch };
  if (typeof next.lengthMm === 'number')
    next.lengthMm = floorMm(next.lengthMm, BODY_FLOOR_MM.wallLength);
  if (typeof next.heightMm === 'number')
    next.heightMm = floorMm(next.heightMm, BODY_FLOOR_MM.wallHeight);
  if (typeof next.heightEndMm === 'number')
    next.heightEndMm = floorMm(next.heightEndMm, BODY_FLOOR_MM.wallHeight);
  if (typeof next.thicknessMm === 'number')
    next.thicknessMm = floorMm(next.thicknessMm, BODY_FLOOR_MM.wallThickness);

  // WHY re-clamp the openings here: shortening a wall left its openings at the old span, so a
  // 600 mm wall kept a 2000 mm window and rendered as an empty frame. The opening rule already
  // existed but only ran when the OPENING was edited, never when the wall changed under it.
  // WHY the patch's own openings win: re-clamping from `wall.openings` unconditionally OVERWROTE
  // whatever the caller sent — so the bow handle's arc conversion, which clears openings in the
  // same patch (a curved band cannot carve them), had its `openings: []` silently restored and the
  // wall kept holes it never cuts. Clamp what is BEING written, not what was there.
  const shapeChanged = WALL_SHAPE_KEYS.some((key) => next[key] !== undefined);
  const sourceOpenings = next.openings ?? wall.openings ?? [];
  if (shapeChanged && sourceOpenings.length > 0) {
    const reshaped: SceneWallState = { ...wall, ...next };
    next.openings = sourceOpenings.map((opening) => clampWallOpening(reshaped, opening));
  }
  return next;
};

export const clampSlabPatch = (patch: Partial<SceneSlabState>): Partial<SceneSlabState> => {
  const next: Partial<SceneSlabState> = { ...patch };
  if (typeof next.lengthMm === 'number')
    next.lengthMm = floorMm(next.lengthMm, BODY_FLOOR_MM.slabPlan);
  if (typeof next.depthMm === 'number')
    next.depthMm = floorMm(next.depthMm, BODY_FLOOR_MM.slabPlan);
  if (typeof next.thicknessMm === 'number')
    next.thicknessMm = floorMm(next.thicknessMm, BODY_FLOOR_MM.slabThickness);
  return next;
};

export const clampSurfacePatch = (
  patch: Partial<SceneSurfaceState>,
): Partial<SceneSurfaceState> => {
  const next: Partial<SceneSurfaceState> = { ...patch };
  if (typeof next.thicknessMm === 'number')
    next.thicknessMm = floorMm(next.thicknessMm, BODY_FLOOR_MM.surfaceThickness);
  return next;
};

export const clampRunPatch = (
  run: SceneRunState,
  patch: Partial<SceneRunState>,
): Partial<SceneRunState> => {
  const next: Partial<SceneRunState> = { ...patch };
  if (typeof next.lengthMm === 'number')
    next.lengthMm = floorMm(next.lengthMm, BODY_FLOOR_MM.runLength);
  if (typeof next.heightMm === 'number')
    next.heightMm = floorMm(next.heightMm, BODY_FLOOR_MM.runHeight);

  // WHY re-fit the panel overrides here (the wall/opening rule, one body over): clampPanelPatch caps
  // an override at the run height when the PANEL is edited, but nothing re-ran when the RUN shrank
  // under it. A 2200 mm override left on a run cut down to 1200 mm is not a rendering glitch — the
  // server reads `panel.HeightMm ?? run.HeightMm` for the net area, the cut list and the nesting
  // blank, so a 2200 mm pane gets ORDERED and cut for a 1200 mm run.
  // The patch's own list wins over the stored one, exactly as clampWallPatch does for openings.
  if (typeof next.heightMm === 'number') {
    const cap = next.heightMm;
    const source = next.panels ?? run.panels ?? [];
    if (source.some((panel) => typeof panel.heightMm === 'number' && panel.heightMm > cap)) {
      next.panels = source.map((panel) =>
        typeof panel.heightMm === 'number' && panel.heightMm > cap
          ? { ...panel, heightMm: cap }
          : panel,
      );
    }
    // A height change also changes the box a shaped pane's outline must fit — panes WITHOUT an
    // override inherit the run height, so the override cap above does not cover them.
    if (cap !== run.heightMm) {
      const base = next.panels ?? source;
      let changed = false;
      const refitted = base.map((panel) => {
        const refit = refitPanelShape(panel, panel.widthMm, panel.heightMm ?? cap);
        if (!refit) return panel;
        changed = true;
        if (refit.rejection) notifyPanelOutlineRejected(refit.rejection);
        return { ...panel, shapeKind: refit.shapeKind, shapePointsJson: refit.shapePointsJson };
      });
      if (changed) next.panels = refitted;
    }
  }
  return next;
};

export const clampPanelPatch = (
  run: SceneRunState,
  patch: Partial<ScenePanelState>,
  panel?: ScenePanelState,
): Partial<ScenePanelState> => {
  const next: Partial<ScenePanelState> = { ...patch };
  if (typeof next.widthMm === 'number')
    next.widthMm = floorMm(next.widthMm, BODY_FLOOR_MM.panelWidth);
  // WHY capped at the run height: a panel height override is a SHORTER pane inside the run (a
  // transom or a stepped top), never a taller one. Unbounded, 99999 stretched the glass 100 m into
  // the sky and a negative value produced an inside-out pane.
  if (typeof next.heightMm === 'number') {
    const cap = Math.max(BODY_FLOOR_MM.panelHeight, Math.round(run.heightMm));
    next.heightMm = Math.min(cap, floorMm(next.heightMm, BODY_FLOOR_MM.panelHeight));
  }

  // A SHAPED pane's outline is the silhouette the BOM prices, the cut list orders and the nester
  // places — so it passes the same gate no matter which editor produced it. A rejected outline
  // leaves the stored shape untouched rather than writing a pane that cannot be cut.
  if (next.shapePointsJson !== undefined && next.shapePointsJson !== null) {
    const widthMm = next.widthMm ?? panel?.widthMm ?? run.lengthMm;
    const heightMm = next.heightMm ?? panel?.heightMm ?? run.heightMm;
    const outline = normalizePanelOutlineJson(next.shapePointsJson, widthMm, heightMm);
    if (outline.json === null) {
      notifyPanelOutlineRejected(outline.rejection);
      delete next.shapePointsJson;
      delete next.shapeKind;
    } else {
      next.shapePointsJson = outline.json;
    }
  } else if (
    panel &&
    next.shapePointsJson === undefined &&
    (typeof next.widthMm === 'number' || typeof next.heightMm === 'number')
  ) {
    // A dimension-only patch changes the box under a stored shape — re-clamp the outline into it,
    // or the persist of the new width/height is refused server-side against the stale silhouette.
    const refit = refitPanelShape(
      panel,
      next.widthMm ?? panel.widthMm,
      next.heightMm ?? panel.heightMm ?? run.heightMm,
    );
    if (refit) {
      if (refit.rejection) notifyPanelOutlineRejected(refit.rejection);
      next.shapeKind = refit.shapeKind;
      next.shapePointsJson = refit.shapePointsJson;
    }
  }
  return next;
};
