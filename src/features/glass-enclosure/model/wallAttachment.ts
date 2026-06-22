import { arcEndLocal } from './arcGeometry';
import type { SceneRunState, SceneWallState } from './project.types';

const ATTACH_BAND_MM = 80;

const runEndPoint = (run: SceneRunState): { x: number; y: number } => {
  const rad = (run.rotationDeg * Math.PI) / 180;
  const cos = Math.cos(rad);
  const sin = Math.sin(rad);
  if (run.geomArcRadiusMm && run.geomArcRadiusMm > 0) {
    const e = arcEndLocal(run.lengthMm, run.geomArcRadiusMm, run.geomArcSweepDeg ?? 1);
    return {
      x: run.originX + e.xMm * cos - e.yMm * sin,
      y: run.originY + e.xMm * sin + e.yMm * cos,
    };
  }
  return { x: run.originX + run.lengthMm * cos, y: run.originY + run.lengthMm * sin };
};

const toLocal = (wall: SceneWallState, x: number, y: number) => {
  const radians = (wall.rotationDeg * Math.PI) / 180;
  const dx = x - wall.originX;
  const dy = y - wall.originY;
  return {
    along: dx * Math.cos(radians) + dy * Math.sin(radians),
    across: -dx * Math.sin(radians) + dy * Math.cos(radians),
  };
};

const pointAttached = (wall: SceneWallState, x: number, y: number) => {
  const local = toLocal(wall, x, y);
  const band = wall.thicknessMm / 2 + ATTACH_BAND_MM;
  return (
    local.along >= -ATTACH_BAND_MM &&
    local.along <= wall.lengthMm + ATTACH_BAND_MM &&
    Math.abs(local.across) <= band
  );
};

export const findAttachedRunIds = (wall: SceneWallState, runs: SceneRunState[]): string[] => {
  const attached: string[] = [];
  for (const run of runs) {
    const end = runEndPoint(run);
    if (pointAttached(wall, run.originX, run.originY) && pointAttached(wall, end.x, end.y)) {
      attached.push(run.id);
    }
  }
  return attached;
};

export const findAttachedWallIds = (run: SceneRunState, walls: SceneWallState[]): string[] => {
  const end = runEndPoint(run);
  const attached: string[] = [];
  for (const wall of walls) {
    if (pointAttached(wall, run.originX, run.originY) && pointAttached(wall, end.x, end.y)) {
      attached.push(wall.id);
    }
  }
  return attached;
};
