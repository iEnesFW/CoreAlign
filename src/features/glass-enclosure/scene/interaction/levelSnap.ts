import { stickyDimensionMm } from './planSnap';
import type { SceneState } from '../../model/project.types';

const LEVEL_SNAP_TOLERANCE_MM = 60;

export const collectHeightLevels = (scene: SceneState, excludeId?: string): number[] => {
  const levels = new Set<number>([0]);
  for (const wall of scene.walls ?? []) {
    if (wall.id === excludeId) continue;
    levels.add(Math.round(wall.heightMm));
    if (wall.heightEndMm !== null && wall.heightEndMm !== undefined)
      levels.add(Math.round(wall.heightEndMm));
  }
  for (const run of scene.runs) {
    if (run.id === excludeId) continue;
    const base = run.geomZ ?? 0;
    levels.add(Math.round(base));
    levels.add(Math.round(base + run.heightMm));
  }
  for (const slab of scene.slabs ?? []) {
    if (slab.id === excludeId) continue;
    levels.add(Math.round(slab.elevationMm));
    levels.add(Math.round(slab.elevationMm + slab.thicknessMm));
  }
  return [...levels];
};

export const snapToLevels = (
  value: number,
  levels: number[],
  toleranceMm = LEVEL_SNAP_TOLERANCE_MM,
): number => {
  let best: number | null = null;
  let bestDist = toleranceMm;
  for (const level of levels) {
    const dist = Math.abs(value - level);
    if (dist <= bestDist) {
      bestDist = dist;
      best = level;
    }
  }
  return best ?? stickyDimensionMm(value);
};
