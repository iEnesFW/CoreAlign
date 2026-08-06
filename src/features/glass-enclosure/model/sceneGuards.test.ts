import { describe, expect, it } from 'vitest';
import {
  BODY_FLOOR_MM,
  blockedByLock,
  clampPanelPatch,
  clampRunPatch,
  clampSlabPatch,
  clampSurfacePatch,
  clampWallPatch,
} from './sceneGuards';
import type { SceneRunState, SceneWallState } from './project.types';

/**
 * The guard layer is the ONE writer every editor funnels through, so these tests pin the invariant
 * that used to be re-implemented (or forgotten) per panel: a body cannot be committed below its
 * physical floor, a locked body cannot be edited except to unlock it, and an opening cannot be left
 * larger than the wall that now carries it.
 */

const wall = (over: Partial<SceneWallState> = {}) =>
  ({
    id: 'w1',
    label: 'W',
    originX: 0,
    originY: 0,
    rotationDeg: 0,
    lengthMm: 4000,
    heightMm: 2600,
    thicknessMm: 200,
    openings: [],
    features: [],
    ...over,
  }) as unknown as SceneWallState;

const run = (over: Partial<SceneRunState> = {}) =>
  ({
    id: 'r1',
    label: 'R',
    lengthMm: 3000,
    heightMm: 2200,
    originX: 0,
    originY: 0,
    rotationDeg: 0,
    panels: [],
    ...over,
  }) as unknown as SceneRunState;

describe('dimension floors', () => {
  it('refuses a wall thinner, shorter or lower than its floor', () => {
    const patch = clampWallPatch(wall(), { lengthMm: -500, heightMm: 0, thicknessMm: -30 });
    expect(patch.lengthMm).toBe(BODY_FLOOR_MM.wallLength);
    expect(patch.heightMm).toBe(BODY_FLOOR_MM.wallHeight);
    expect(patch.thicknessMm).toBe(BODY_FLOOR_MM.wallThickness);
  });

  it('leaves a legitimate wall value alone (only rounding it)', () => {
    const patch = clampWallPatch(wall(), { lengthMm: 3200.4, thicknessMm: 120 });
    expect(patch.lengthMm).toBe(3200);
    expect(patch.thicknessMm).toBe(120);
  });

  it('refuses a degenerate slab', () => {
    const patch = clampSlabPatch({ lengthMm: -500, depthMm: 0, thicknessMm: -30 });
    expect(patch.lengthMm).toBe(BODY_FLOOR_MM.slabPlan);
    expect(patch.depthMm).toBe(BODY_FLOOR_MM.slabPlan);
    expect(patch.thicknessMm).toBe(BODY_FLOOR_MM.slabThickness);
  });

  it('refuses an inside-out surface', () => {
    expect(clampSurfacePatch({ thicknessMm: -12 }).thicknessMm).toBe(
      BODY_FLOOR_MM.surfaceThickness,
    );
  });

  it('refuses a zero-height run', () => {
    const patch = clampRunPatch(run(), { heightMm: 0, lengthMm: 10 });
    expect(patch.heightMm).toBe(BODY_FLOOR_MM.runHeight);
    expect(patch.lengthMm).toBe(BODY_FLOOR_MM.runLength);
  });

  // Shortening a run used to leave a taller panel override behind, and the server reads
  // `panel.HeightMm ?? run.HeightMm` for the net area, the cut list AND the nesting blank — so a
  // 2200 mm pane was ordered and cut for a 1200 mm run. Same rule as the wall/opening re-fit.
  it('re-fits a taller panel override when the run is shortened', () => {
    const tall = run({
      heightMm: 2400,
      panels: [
        { id: 'p1', widthMm: 900, heightMm: 2200 },
        { id: 'p2', widthMm: 900, heightMm: 1000 },
        { id: 'p3', widthMm: 900 },
      ],
    } as Partial<SceneRunState>);

    const patch = clampRunPatch(tall, { heightMm: 1200 });

    expect(patch.panels?.[0].heightMm).toBe(1200);
    // A shorter override and an absent one are both left alone.
    expect(patch.panels?.[1].heightMm).toBe(1000);
    expect(patch.panels?.[2].heightMm).toBeUndefined();
  });

  it('leaves the panels alone when the patch does not change the run height', () => {
    const tall = run({
      heightMm: 2400,
      panels: [{ id: 'p1', widthMm: 900, heightMm: 2200 }],
    } as Partial<SceneRunState>);
    expect(clampRunPatch(tall, { originX: 500 }).panels).toBeUndefined();
  });

  it("re-fits the PATCH's own panel list, not the stored one", () => {
    const tall = run({
      heightMm: 2400,
      panels: [{ id: 'p1', widthMm: 900, heightMm: 2200 }],
    } as Partial<SceneRunState>);

    const patch = clampRunPatch(tall, {
      heightMm: 1200,
      panels: [{ id: 'p9', widthMm: 400, heightMm: 2000 }] as SceneRunState['panels'],
    });

    expect(patch.panels).toHaveLength(1);
    expect(patch.panels?.[0].id).toBe('p9');
    expect(patch.panels?.[0].heightMm).toBe(1200);
  });

  it('caps a panel height override at the run height instead of stretching the glass', () => {
    expect(clampPanelPatch(run({ heightMm: 2200 }), { heightMm: 99999 }).heightMm).toBe(2200);
    expect(clampPanelPatch(run(), { heightMm: -50 }).heightMm).toBe(BODY_FLOOR_MM.panelHeight);
    expect(clampPanelPatch(run(), { heightMm: 1800 }).heightMm).toBe(1800);
  });

  it('does not invent values for properties the patch does not carry', () => {
    expect(clampWallPatch(wall(), { colorHex: '#123456' })).toEqual({ colorHex: '#123456' });
    expect(clampPanelPatch(run(), { hasHandle: true })).toEqual({ hasHandle: true });
  });
});

describe('lock', () => {
  it('refuses every edit to a locked body', () => {
    expect(blockedByLock({ locked: true }, { lengthMm: 1000 })).toBe(true);
    expect(blockedByLock({ locked: true }, { colorHex: '#fff' })).toBe(true);
  });

  it('still allows the body to be unlocked', () => {
    expect(blockedByLock({ locked: true }, { locked: false })).toBe(false);
  });

  it('leaves unlocked bodies alone', () => {
    expect(blockedByLock({ locked: false }, { lengthMm: 1000 })).toBe(false);
    expect(blockedByLock(undefined, { lengthMm: 1000 })).toBe(false);
  });

  it('refuses a mixed patch that would sneak geometry in with the unlock', () => {
    expect(blockedByLock({ locked: true }, { locked: false, lengthMm: 9999 })).toBe(true);
  });
});

describe('openings follow the wall they live in', () => {
  const withWindow = wall({
    openings: [
      { id: 'o1', kind: 'window', offsetMm: 2000, widthMm: 2000, heightMm: 1400, sillMm: 900 },
    ],
  } as unknown as Partial<SceneWallState>);

  it('re-fits an opening when the wall is shortened under it', () => {
    const patch = clampWallPatch(withWindow, { lengthMm: 600 });
    const opening = patch.openings?.[0];
    expect(opening).toBeDefined();
    // 600 mm wall minus the 20 mm edge margin on each side.
    expect(opening!.widthMm).toBeLessThanOrEqual(560);
    expect(opening!.offsetMm - opening!.widthMm / 2).toBeGreaterThanOrEqual(0);
    expect(opening!.offsetMm + opening!.widthMm / 2).toBeLessThanOrEqual(600);
  });

  it('re-fits an opening when the wall is lowered under it', () => {
    const patch = clampWallPatch(withWindow, { heightMm: 1200 });
    const opening = patch.openings![0];
    expect(opening.heightMm + opening.sillMm).toBeLessThanOrEqual(1200);
  });

  it('does not touch the openings when only appearance changes', () => {
    expect(clampWallPatch(withWindow, { colorHex: '#abcdef' }).openings).toBeUndefined();
  });

  it('leaves an opening that still fits exactly as authored', () => {
    const patch = clampWallPatch(withWindow, { lengthMm: 4000 });
    expect(patch.openings![0]).toMatchObject({ widthMm: 2000, offsetMm: 2000, heightMm: 1400 });
  });
});

describe('a patch that REPLACES the openings wins over the re-clamp', () => {
  const withWindow = wall({
    openings: [
      { id: 'o1', kind: 'window', offsetMm: 2000, widthMm: 2000, heightMm: 1400, sillMm: 900 },
    ],
  } as unknown as Partial<SceneWallState>);

  it('keeps an explicit empty list when the same patch curves the wall', () => {
    // The bow handle converts a wall to an arc and clears its openings in ONE patch — a curved
    // band cannot carve them. Re-clamping from the OLD wall silently restored them.
    const patch = clampWallPatch(withWindow, {
      geomArcRadiusMm: 2500,
      geomArcSweepDeg: 60,
      rotationDeg: -30,
      openings: [],
    } as Partial<SceneWallState>);
    expect(patch.openings).toEqual([]);
  });

  it('clamps the openings the caller sent, not the ones already on the wall', () => {
    const patch = clampWallPatch(withWindow, {
      lengthMm: 600,
      openings: [
        { id: 'o2', kind: 'door', offsetMm: 300, widthMm: 2000, heightMm: 2100, sillMm: 0 },
      ],
    } as unknown as Partial<SceneWallState>);
    expect(patch.openings).toHaveLength(1);
    expect(patch.openings![0].id).toBe('o2');
    expect(patch.openings![0].widthMm).toBeLessThanOrEqual(560);
  });

  it('still re-clamps from the wall when the patch does not touch openings', () => {
    const patch = clampWallPatch(withWindow, { lengthMm: 600 });
    expect(patch.openings).toHaveLength(1);
    expect(patch.openings![0].id).toBe('o1');
    expect(patch.openings![0].widthMm).toBeLessThanOrEqual(560);
  });
});

describe('a stored shape follows the box that shrinks under it', () => {
  const boxJson = (halfW: number, h: number) =>
    JSON.stringify([
      { x: -halfW, y: 0 },
      { x: halfW, y: 0 },
      { x: halfW, y: h },
      { x: -halfW, y: h },
    ]);
  const shapedPanel = (over: Record<string, unknown> = {}) =>
    ({
      id: 'p1',
      panelIndex: 0,
      widthMm: 1000,
      shapeKind: 'polygon',
      shapePointsJson: boxJson(500, 2200),
      ...over,
    }) as unknown as SceneRunState['panels'][number];

  it('a width-only panel patch re-clamps the outline into the new box', () => {
    // Without this, the persist ships the NEW width with the OLD silhouette and the server-side
    // box validator refuses the whole panel update (client/server split-brain).
    const p = shapedPanel();
    const patch = clampPanelPatch(run({ panels: [p] }), { widthMm: 400 }, p);
    expect(patch.shapePointsJson).toBeDefined();
    const points = JSON.parse(patch.shapePointsJson ?? '[]') as { x: number }[];
    for (const pt of points) expect(Math.abs(pt.x)).toBeLessThanOrEqual(200);
  });

  it('a growing box leaves the patch shape-free — nothing to re-fit', () => {
    const p = shapedPanel();
    const patch = clampPanelPatch(run({ panels: [p] }), { widthMm: 2000 }, p);
    expect('shapePointsJson' in patch).toBe(false);
  });

  it('a shape that collapses under the clamp falls back to a rectangle', () => {
    // An outline living entirely at the right edge collapses to a line when the box narrows —
    // the pane drops its shape (rect over-estimates area: the safe direction) instead of
    // persisting uncuttable glass.
    const sliver = shapedPanel({
      shapePointsJson: JSON.stringify([
        { x: 400, y: 0 },
        { x: 500, y: 0 },
        { x: 500, y: 2200 },
        { x: 400, y: 2200 },
      ]),
    });
    const patch = clampPanelPatch(run({ panels: [sliver] }), { widthMm: 400 }, sliver);
    expect(patch.shapeKind).toBeNull();
    expect(patch.shapePointsJson).toBeNull();
  });

  it('a run height shrink re-fits the outline of a pane that INHERITS the height', () => {
    const p = shapedPanel();
    const patch = clampRunPatch(run({ panels: [p] }), { heightMm: 1200 });
    expect(patch.panels).toBeDefined();
    const points = JSON.parse(patch.panels![0].shapePointsJson ?? '[]') as { y: number }[];
    for (const pt of points) expect(pt.y).toBeLessThanOrEqual(1200);
  });
});
