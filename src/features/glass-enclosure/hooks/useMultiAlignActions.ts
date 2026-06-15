import { useTranslation } from 'react-i18next';
import { queueToast } from '@/shared/api/toastQueue';
import { distributePanelWidths, useDesignerStore } from '../model/designerStore';
import {
  alignTargetCenter,
  alignTargetEndpoints,
  alignTargetHeightMm,
  alignTargetId,
  alignTargetLengthMm,
} from '../model/multiAlign';
import {
  buildRunFootprint,
  buildSlabFootprint,
  buildWallFootprint,
  penetratesAny,
} from '../scene/interaction/planCollision';
import { useRunEntityActions } from './useDesignerEntityActions';
import type { AlignTarget } from '../model/multiAlign';
import type { PlanFootprint } from '../scene/interaction/planCollision';
import type {
  SceneRunState,
  SceneSlabState,
  SceneState,
  SceneWallState,
} from '../model/project.types';

const MIN_WALL_HEIGHT_MM = 100;

type PatchOp =
  | { kind: 'run'; id: string; patch: Partial<SceneRunState> }
  | { kind: 'wall'; id: string; patch: Partial<SceneWallState> }
  | { kind: 'slab'; id: string; patch: Partial<SceneSlabState> };

const patchedRun = (run: SceneRunState, patch: Partial<SceneRunState>): SceneRunState => {
  const merged = { ...run, ...patch };
  if (patch.lengthMm !== undefined && patch.lengthMm !== run.lengthMm) {
    return { ...merged, panels: distributePanelWidths(merged.panels, merged.lengthMm) };
  }
  return merged;
};

const applyOps = (scene: SceneState, ops: PatchOp[]): SceneState => {
  const runOps = new Map(ops.filter((op) => op.kind === 'run').map((op) => [op.id, op.patch]));
  const wallOps = new Map(ops.filter((op) => op.kind === 'wall').map((op) => [op.id, op.patch]));
  const slabOps = new Map(ops.filter((op) => op.kind === 'slab').map((op) => [op.id, op.patch]));
  return {
    ...scene,
    runs: scene.runs.map((run) => {
      const patch = runOps.get(run.id);
      return patch ? patchedRun(run, patch as Partial<SceneRunState>) : run;
    }),
    walls: (scene.walls ?? []).map((wall) => {
      const patch = wallOps.get(wall.id);
      return patch ? { ...wall, ...(patch as Partial<SceneWallState>) } : wall;
    }),
    slabs: (scene.slabs ?? []).map((slab) => {
      const patch = slabOps.get(slab.id);
      return patch ? { ...slab, ...(patch as Partial<SceneSlabState>) } : slab;
    }),
  };
};

const targetFootprint = (target: AlignTarget, dxMm: number, dyMm: number): PlanFootprint => {
  if (target.kind === 'run') {
    return buildRunFootprint(target.run, dxMm, dyMm, target.run.rotationDeg);
  }
  if (target.kind === 'wall') {
    return buildWallFootprint(target.wall, dxMm, dyMm, target.wall.rotationDeg);
  }
  return buildSlabFootprint(target.slab, dxMm, dyMm, target.slab.rotationDeg);
};

const moveOp = (target: AlignTarget, dxMm: number, dyMm: number): PatchOp => {
  const id = alignTargetId(target);
  const origin =
    target.kind === 'run' ? target.run : target.kind === 'wall' ? target.wall : target.slab;
  return {
    kind: target.kind,
    id,
    patch: {
      originX: Math.round(origin.originX + dxMm),
      originY: Math.round(origin.originY + dyMm),
    },
  };
};

export const useMultiAlignActions = () => {
  const { t } = useTranslation();
  const { persistRun } = useRunEntityActions();

  const collectTargets = (): AlignTarget[] => {
    const state = useDesignerStore.getState();
    return state.multiSelection.order
      .map((ref): AlignTarget | null => {
        if (ref.kind === 'run') {
          const run = state.scene.runs.find((r) => r.id === ref.id);
          return run ? { kind: 'run', run } : null;
        }
        if (ref.kind === 'wall') {
          const wall = (state.scene.walls ?? []).find((w) => w.id === ref.id);
          return wall ? { kind: 'wall', wall } : null;
        }
        const slab = (state.scene.slabs ?? []).find((s) => s.id === ref.id);
        return slab ? { kind: 'slab', slab } : null;
      })
      .filter((target): target is AlignTarget => target !== null);
  };

  const allFootprints = (): PlanFootprint[] => {
    const scene = useDesignerStore.getState().scene;
    return [
      ...(scene.walls ?? []).map((w) => buildWallFootprint(w, 0, 0, w.rotationDeg)),
      ...scene.runs.map((r) => buildRunFootprint(r, 0, 0, r.rotationDeg)),
      ...(scene.slabs ?? []).map((s) => buildSlabFootprint(s, 0, 0, s.rotationDeg)),
    ];
  };

  const commitOps = (ops: PatchOp[]) => {
    if (ops.length === 0) return;
    const state = useDesignerStore.getState();
    state.applyScenePatch((scene) => applyOps(scene, ops));
    for (const op of ops) {
      if (op.kind !== 'run') continue;
      const fresh = useDesignerStore.getState().scene.runs.find((r) => r.id === op.id);
      if (fresh) void persistRun(fresh);
    }
  };

  const notifySkipped = (skipped: number) => {
    if (skipped === 0) return;
    queueToast({
      dedupeKey: 'glass-align-skipped',
      variant: 'warning',
      description: t('GlassEnclosure.Designer.MultiSelect.SkippedCollision', {
        defaultValue: '{{count}} öğe çakışma nedeniyle atlandı.',
        count: skipped,
      }),
    });
  };

  const notifyNeedTwo = () =>
    queueToast({
      dedupeKey: 'glass-align-need-two',
      variant: 'info',
      description: t('GlassEnclosure.Designer.MultiSelect.NeedTwo', {
        defaultValue: 'Bu işlem için en az iki öğe seçin.',
      }),
    });

  const alignCenters = (axis: 'x' | 'y') => {
    const targets = collectTargets();
    if (targets.length < 2) {
      notifyNeedTwo();
      return;
    }
    const movingIds = new Set(targets.slice(1).map(alignTargetId));
    const obstacles = allFootprints().filter((f) => !movingIds.has(f.ownerId));
    const anchor = alignTargetCenter(targets[0]);
    const ops: PatchOp[] = [];
    let skipped = 0;
    for (const target of targets.slice(1)) {
      const center = alignTargetCenter(target);
      const dxMm = axis === 'x' ? anchor.x - center.x : 0;
      const dyMm = axis === 'y' ? anchor.y - center.y : 0;
      if (Math.round(dxMm) === 0 && Math.round(dyMm) === 0) continue;
      const candidate = targetFootprint(target, dxMm, dyMm);
      if (penetratesAny(candidate, obstacles)) {
        skipped += 1;
        continue;
      }
      obstacles.push(candidate);
      ops.push(moveOp(target, dxMm, dyMm));
    }
    commitOps(ops);
    notifySkipped(skipped);
  };

  const distributeEvenly = (axis: 'x' | 'y') => {
    const targets = collectTargets();
    if (targets.length < 3) {
      notifyNeedTwo();
      return;
    }
    const withCenter = targets.map((target) => ({ target, center: alignTargetCenter(target) }));
    withCenter.sort((a, b) => (axis === 'x' ? a.center.x - b.center.x : a.center.y - b.center.y));
    const first = withCenter[0].center;
    const last = withCenter[withCenter.length - 1].center;
    const step = (axis === 'x' ? last.x - first.x : last.y - first.y) / (withCenter.length - 1);
    const movingIds = new Set(withCenter.slice(1, -1).map((w) => alignTargetId(w.target)));
    const obstacles = allFootprints().filter((f) => !movingIds.has(f.ownerId));
    const ops: PatchOp[] = [];
    let skipped = 0;
    for (let i = 1; i < withCenter.length - 1; i += 1) {
      const { target, center } = withCenter[i];
      const targetCoord = (axis === 'x' ? first.x : first.y) + step * i;
      const dxMm = axis === 'x' ? targetCoord - center.x : 0;
      const dyMm = axis === 'y' ? targetCoord - center.y : 0;
      if (Math.round(dxMm) === 0 && Math.round(dyMm) === 0) continue;
      const candidate = targetFootprint(target, dxMm, dyMm);
      if (penetratesAny(candidate, obstacles)) {
        skipped += 1;
        continue;
      }
      obstacles.push(candidate);
      ops.push(moveOp(target, dxMm, dyMm));
    }
    commitOps(ops);
    notifySkipped(skipped);
  };

  const joinEndToEnd = () => {
    const targets = collectTargets().filter((target) => target.kind !== 'slab');
    if (targets.length < 2) {
      notifyNeedTwo();
      return;
    }
    const movingIds = new Set(targets.slice(1).map(alignTargetId));
    const obstacles = allFootprints().filter((f) => !movingIds.has(f.ownerId));
    const ops: PatchOp[] = [];
    let skipped = 0;
    let cursor = alignTargetEndpoints(targets[0])?.end;
    for (const target of targets.slice(1)) {
      const endpoints = alignTargetEndpoints(target);
      if (!endpoints || !cursor) continue;
      const dxMm = cursor.x - endpoints.start.x;
      const dyMm = cursor.y - endpoints.start.y;
      const candidate = targetFootprint(target, dxMm, dyMm);
      if (penetratesAny(candidate, obstacles)) {
        skipped += 1;
        continue;
      }
      obstacles.push(candidate);
      ops.push(moveOp(target, dxMm, dyMm));
      cursor = { x: endpoints.end.x + dxMm, y: endpoints.end.y + dyMm };
    }
    commitOps(ops);
    notifySkipped(skipped);
  };

  const equalizeHeights = () => {
    const targets = collectTargets().filter((target) => target.kind !== 'slab');
    if (targets.length < 2) {
      notifyNeedTwo();
      return;
    }
    const anchorHeight = Math.max(MIN_WALL_HEIGHT_MM, alignTargetHeightMm(targets[0]) ?? 0);
    const footprints = allFootprints();
    const ops: PatchOp[] = [];
    let skipped = 0;
    for (const target of targets.slice(1)) {
      const id = alignTargetId(target);
      const obstacles = footprints.filter((f) => f.ownerId !== id);
      if (target.kind === 'run') {
        const candidate: SceneRunState = { ...target.run, heightMm: anchorHeight };
        if (penetratesAny(buildRunFootprint(candidate, 0, 0, candidate.rotationDeg), obstacles)) {
          skipped += 1;
          continue;
        }
        ops.push({ kind: 'run', id, patch: { heightMm: anchorHeight } });
        continue;
      }
      if (target.kind !== 'wall') continue;
      const wall = target.wall;
      const heightEnd = wall.heightEndMm ?? null;
      const candidate: SceneWallState = {
        ...wall,
        heightMm: anchorHeight,
        heightEndMm:
          heightEnd === null
            ? null
            : Math.max(MIN_WALL_HEIGHT_MM, heightEnd + (anchorHeight - wall.heightMm)),
      };
      if (penetratesAny(buildWallFootprint(candidate, 0, 0, candidate.rotationDeg), obstacles)) {
        skipped += 1;
        continue;
      }
      ops.push({
        kind: 'wall',
        id,
        patch: { heightMm: candidate.heightMm, heightEndMm: candidate.heightEndMm },
      });
    }
    commitOps(ops);
    notifySkipped(skipped);
  };

  const equalizeLengths = () => {
    const targets = collectTargets().filter((target) => target.kind !== 'slab');
    if (targets.length < 2) {
      notifyNeedTwo();
      return;
    }
    const anchorLength = alignTargetLengthMm(targets[0]);
    if (anchorLength === null || anchorLength < MIN_WALL_HEIGHT_MM) return;
    const footprints = allFootprints();
    const ops: PatchOp[] = [];
    let skipped = 0;
    for (const target of targets.slice(1)) {
      const id = alignTargetId(target);
      const obstacles = footprints.filter((f) => f.ownerId !== id);
      if (target.kind === 'run') {
        const candidate: SceneRunState = { ...target.run, lengthMm: anchorLength };
        if (penetratesAny(buildRunFootprint(candidate, 0, 0, candidate.rotationDeg), obstacles)) {
          skipped += 1;
          continue;
        }
        ops.push({ kind: 'run', id, patch: { lengthMm: anchorLength } });
        continue;
      }
      if (target.kind !== 'wall') continue;
      const candidate: SceneWallState = { ...target.wall, lengthMm: anchorLength };
      if (penetratesAny(buildWallFootprint(candidate, 0, 0, candidate.rotationDeg), obstacles)) {
        skipped += 1;
        continue;
      }
      ops.push({ kind: 'wall', id, patch: { lengthMm: anchorLength } });
    }
    commitOps(ops);
    notifySkipped(skipped);
  };

  return { alignCenters, distributeEvenly, joinEndToEnd, equalizeHeights, equalizeLengths };
};
