import type { SceneRunState, SceneWallState } from './project.types';

const ATTACH_BAND_MM = 80;

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
    const radians = (run.rotationDeg * Math.PI) / 180;
    const endX = run.originX + run.lengthMm * Math.cos(radians);
    const endY = run.originY + run.lengthMm * Math.sin(radians);
    if (pointAttached(wall, run.originX, run.originY) && pointAttached(wall, endX, endY)) {
      attached.push(run.id);
    }
  }
  return attached;
};
