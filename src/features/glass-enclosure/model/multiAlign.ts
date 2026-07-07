import { arcEndLocal, isRealArc, resolveArc } from './arcGeometry';
import type { SceneRunState, SceneSlabState, SceneWallState } from './project.types';

export type AlignTarget =
  | { kind: 'run'; run: SceneRunState }
  | { kind: 'wall'; wall: SceneWallState }
  | { kind: 'slab'; slab: SceneSlabState };

export interface PlanXY {
  x: number;
  y: number;
}

const DEG2RAD = Math.PI / 180;

// The far end of a run/wall. For a real arc, rotationDeg is the ROLLED START TANGENT (not the
// chord direction), so `origin + length·dir(rotationDeg)` is the phantom straight end — the real
// end is arcEndLocal rotated into world (same transform wallAttachment/multiAutofill use). Aligning
// or end-to-end merging arc parts off the phantom end drifts them by ~0.3·R.
const lineEndXY = (line: SceneRunState | SceneWallState): PlanXY => {
  const rad = line.rotationDeg * DEG2RAD;
  if (isRealArc(line.geomArcRadiusMm, line.geomArcSweepDeg)) {
    const resolved = resolveArc(line.geomArcRadiusMm ?? 0, line.geomArcSweepDeg ?? 1);
    const e = arcEndLocal(resolved.radiusMm, line.geomArcSweepDeg ?? 1);
    return {
      x: line.originX + e.xMm * Math.cos(rad) - e.yMm * Math.sin(rad),
      y: line.originY + e.xMm * Math.sin(rad) + e.yMm * Math.cos(rad),
    };
  }
  return {
    x: line.originX + line.lengthMm * Math.cos(rad),
    y: line.originY + line.lengthMm * Math.sin(rad),
  };
};

export const alignTargetId = (target: AlignTarget): string => {
  if (target.kind === 'run') return target.run.id;
  if (target.kind === 'wall') return target.wall.id;
  return target.slab.id;
};

export const alignTargetCenter = (target: AlignTarget): PlanXY => {
  if (target.kind === 'slab') {
    const slab = target.slab;
    const rad = slab.rotationDeg * DEG2RAD;
    const cos = Math.cos(rad);
    const sin = Math.sin(rad);
    return {
      x: slab.originX + (slab.lengthMm / 2) * cos - (slab.depthMm / 2) * sin,
      y: slab.originY + (slab.lengthMm / 2) * sin + (slab.depthMm / 2) * cos,
    };
  }
  const line = target.kind === 'run' ? target.run : target.wall;
  const end = lineEndXY(line);
  return {
    x: (line.originX + end.x) / 2,
    y: (line.originY + end.y) / 2,
  };
};

export const alignTargetEndpoints = (
  target: AlignTarget,
): { start: PlanXY; end: PlanXY } | null => {
  if (target.kind === 'slab') return null;
  const line = target.kind === 'run' ? target.run : target.wall;
  return {
    start: { x: line.originX, y: line.originY },
    end: lineEndXY(line),
  };
};

export const alignTargetHeightMm = (target: AlignTarget): number | null => {
  if (target.kind === 'run') return target.run.heightMm;
  if (target.kind === 'wall') return target.wall.heightMm;
  return null;
};

export const alignTargetLengthMm = (target: AlignTarget): number | null => {
  if (target.kind === 'run') return target.run.lengthMm;
  if (target.kind === 'wall') return target.wall.lengthMm;
  return null;
};
