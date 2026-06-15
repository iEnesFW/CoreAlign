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
  const rad = line.rotationDeg * DEG2RAD;
  return {
    x: line.originX + (line.lengthMm / 2) * Math.cos(rad),
    y: line.originY + (line.lengthMm / 2) * Math.sin(rad),
  };
};

export const alignTargetEndpoints = (
  target: AlignTarget,
): { start: PlanXY; end: PlanXY } | null => {
  if (target.kind === 'slab') return null;
  const line = target.kind === 'run' ? target.run : target.wall;
  const rad = line.rotationDeg * DEG2RAD;
  return {
    start: { x: line.originX, y: line.originY },
    end: {
      x: line.originX + line.lengthMm * Math.cos(rad),
      y: line.originY + line.lengthMm * Math.sin(rad),
    },
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
