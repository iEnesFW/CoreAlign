import {
  buildRunFootprint,
  buildSlabFootprint,
  buildWallFootprint,
  footprintsOverlapXY,
  type PlanFootprint,
} from '../scene/interaction/planCollision';
import { resolveAttachedRunIds } from './wallAttachment';
import type { SceneState } from './project.types';

// How close an object's base must be to the floor's top surface to count as resting on it.
const REST_TOLERANCE_MM = 60;

export interface FloorFollow {
  deltaZMm: number;
  wallIds: string[];
  runIds: string[];
  roofSlabIds: string[];
}

// When a FLOOR slab's top surface (elevation + thickness) moves, the walls / runs / roofs RESTING on
// that surface follow by the same ΔZ so they stay on top. Attachment is GEOMETRIC: an object's base
// sits at the floor's old top AND its plan footprint overlaps the floor. Objects merely LINKED to a
// mover — a run bonded to a wall, a panel/hole on a run/wall — are NOT moved here: a bonded run rides
// the wall cascade (moved once), and panels/holes/hardware inherit their parent's Z, so nothing is
// double-shifted and a non-resting linked object never drifts. Pure: returns the ids to move; the
// caller applies ΔZ (walls/runs on geomZ, roofs on elevationMm).
export const computeFloorFollow = (
  scene: SceneState,
  slabId: string,
  newElevationMm: number,
  newThicknessMm: number,
): FloorFollow | null => {
  const slab = (scene.slabs ?? []).find((s) => s.id === slabId);
  if (!slab || slab.kind !== 'floor') return null;
  const oldTopMm = slab.elevationMm + slab.thicknessMm;
  const newTopMm = newElevationMm + newThicknessMm;
  const deltaZMm = Math.round(newTopMm - oldTopMm);
  if (deltaZMm === 0) return null;

  const floorFp = buildSlabFootprint(slab, 0, 0, slab.rotationDeg);
  const restsOnFloor = (baseZMm: number, fp: PlanFootprint) =>
    Math.abs(baseZMm - oldTopMm) <= REST_TOLERANCE_MM && footprintsOverlapXY(fp, floorFp);

  const walls = scene.walls ?? [];
  const runs = scene.runs;

  const wallIds: string[] = [];
  for (const wall of walls) {
    if (restsOnFloor(wall.geomZ ?? 0, buildWallFootprint(wall, 0, 0, wall.rotationDeg))) {
      wallIds.push(wall.id);
    }
  }

  const runIds = new Set<string>();
  for (const wallId of wallIds) {
    const wall = walls.find((w) => w.id === wallId);
    if (!wall) continue;
    for (const runId of resolveAttachedRunIds(wall, runs)) runIds.add(runId);
  }
  for (const run of runs) {
    if (runIds.has(run.id)) continue;
    if (restsOnFloor(run.geomZ ?? 0, buildRunFootprint(run, 0, 0, run.rotationDeg))) {
      runIds.add(run.id);
    }
  }

  const raisedWallTops = wallIds
    .map((wallId) => walls.find((w) => w.id === wallId))
    .filter((wall): wall is (typeof walls)[number] => Boolean(wall))
    .map((wall) => ({
      topMm: (wall.geomZ ?? 0) + Math.max(wall.heightMm, wall.heightEndMm ?? wall.heightMm),
      fp: buildWallFootprint(wall, 0, 0, wall.rotationDeg),
    }));
  const roofSlabIds: string[] = [];
  for (const other of scene.slabs ?? []) {
    if (other.id === slabId || other.kind !== 'roof') continue;
    const roofFp = buildSlabFootprint(other, 0, 0, other.rotationDeg);
    const restsOnRaisedWall = raisedWallTops.some(
      (wt) =>
        Math.abs(other.elevationMm - wt.topMm) <= REST_TOLERANCE_MM &&
        footprintsOverlapXY(roofFp, wt.fp),
    );
    if (restsOnRaisedWall) roofSlabIds.push(other.id);
  }

  return { deltaZMm, wallIds, runIds: [...runIds], roofSlabIds };
};
