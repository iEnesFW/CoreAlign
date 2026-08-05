import { stickyDimensionMm } from './planSnap';
import type { SceneState } from '../../model/project.types';

const LEVEL_SNAP_TOLERANCE_MM = 60;

/**
 * Every ABSOLUTE Z the scene actually has something at — the magnet targets for an elevation drag.
 *
 * WHY the wall branch was wrong: it added `heightMm`, a RELATIVE size, into a list of absolute
 * levels and ignored `geomZ` entirely. A 2600 wall standing on a 400 podium tops out at 3000, and
 * the list held neither 3000 nor its base 400 — so the roof handle had no magnet where the wall
 * really is, AND it had a phantom one at 2600 where nothing exists. The run branch already did it
 * correctly (base and base+height); this is the same two lines, one body over. Surfaces were
 * missing altogether, so a pen-drawn deck offered no level at all.
 */
export const collectHeightLevels = (scene: SceneState, excludeId?: string): number[] => {
  const levels = new Set<number>([0]);
  for (const wall of scene.walls ?? []) {
    if (wall.id === excludeId) continue;
    const base = wall.geomZ ?? 0;
    levels.add(Math.round(base));
    levels.add(Math.round(base + wall.heightMm));
    if (wall.heightEndMm !== null && wall.heightEndMm !== undefined)
      levels.add(Math.round(base + wall.heightEndMm));
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
  for (const surface of scene.surfaces ?? []) {
    if (surface.id === excludeId) continue;
    levels.add(Math.round(surface.elevationMm));
    levels.add(Math.round(surface.elevationMm + surface.thicknessMm));
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
