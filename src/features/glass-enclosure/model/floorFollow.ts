import {
  buildRunFootprint,
  buildSlabFootprint,
  buildWallFootprint,
  footprintsOverlapXY,
  normalizePlanAngleDeg,
  type PlanFootprint,
} from '../scene/interaction/planCollision';
import { resolveAttachedRunIds } from './wallAttachment';
import type { SceneSlabState, SceneState } from './project.types';

// How close an object's base must be to the floor's top surface to count as resting on it.
const REST_TOLERANCE_MM = 60;

export interface FloorFollow {
  deltaZMm: number;
  deltaXMm: number;
  deltaYMm: number;
  sweepDeg: number;
  pivotXMm: number;
  pivotYMm: number;
  wallIds: string[];
  runIds: string[];
  roofSlabIds: string[];
}

export interface FloorPose {
  elevationMm: number;
  thicknessMm: number;
  originX: number;
  originY: number;
  rotationDeg: number;
}

const rotateAbout = (
  x: number,
  y: number,
  pivotX: number,
  pivotY: number,
  sweepDeg: number,
): { x: number; y: number } => {
  if (sweepDeg === 0) return { x, y };
  const rad = (sweepDeg * Math.PI) / 180;
  const cos = Math.cos(rad);
  const sin = Math.sin(rad);
  const dx = x - pivotX;
  const dy = y - pivotY;
  return { x: pivotX + dx * cos - dy * sin, y: pivotY + dx * sin + dy * cos };
};

// The rotation pivot the slab itself turns about: the centre of its plan rectangle, in world mm.
const slabCentreMm = (slab: SceneSlabState): { x: number; y: number } => {
  const rad = (slab.rotationDeg * Math.PI) / 180;
  const dirX = Math.cos(rad);
  const dirY = Math.sin(rad);
  return {
    x: slab.originX + (slab.lengthMm / 2) * dirX - (slab.depthMm / 2) * dirY,
    y: slab.originY + (slab.lengthMm / 2) * dirY + (slab.depthMm / 2) * dirX,
  };
};

/**
 * When a FLOOR slab moves, everything RESTING on it rides along.
 *
 * The riders are resolved against the floor's OLD pose — base within tolerance of the old top AND
 * plan footprints overlapping — and then the same transform (ΔZ, ΔX/ΔY, and the rotation about the
 * slab's own pivot) is applied to them. Objects merely LINKED to a mover — a run bonded to a wall,
 * a panel/hole on a run/wall — are not listed separately: a bonded run rides the wall cascade
 * (moved once) and panels/holes/hardware inherit their parent's pose, so nothing is double-shifted.
 *
 * WHY the lateral half exists at all: this used to be purely vertical, and the caller only invoked
 * it for elevation/thickness edits. Sliding a floor sideways therefore left every wall, pane and
 * roof standing where they were — the floor moved out from under the scene — and gravity then
 * dropped them, permanently, whenever the floor's top sat above grade.
 *
 * Pure: returns the ids plus the transform; the caller writes it.
 */
export const computeFloorFollow = (
  scene: SceneState,
  slabId: string,
  next: FloorPose,
): FloorFollow | null => {
  const slab = (scene.slabs ?? []).find((s) => s.id === slabId);
  if (!slab || slab.kind !== 'floor') return null;

  const oldTopMm = slab.elevationMm + slab.thicknessMm;
  const deltaZMm = Math.round(next.elevationMm + next.thicknessMm - oldTopMm);
  const sweepDeg = normalizePlanAngleDeg(next.rotationDeg - slab.rotationDeg);
  const pivot = slabCentreMm(slab);
  // A rotation turns the body about its own centre, so the origin the caller writes already encodes
  // it; the riders must orbit that SAME pivot. Whatever translation survives on top of that is the
  // slide (a pure rotate leaves it at zero, a pure slide leaves the sweep at zero).
  const rotatedOrigin = rotateAbout(slab.originX, slab.originY, pivot.x, pivot.y, sweepDeg);
  const deltaXMm = Math.round(next.originX - rotatedOrigin.x);
  const deltaYMm = Math.round(next.originY - rotatedOrigin.y);
  if (deltaZMm === 0 && deltaXMm === 0 && deltaYMm === 0 && sweepDeg === 0) return null;

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

  return {
    deltaZMm,
    deltaXMm,
    deltaYMm,
    sweepDeg,
    pivotXMm: pivot.x,
    pivotYMm: pivot.y,
    wallIds,
    runIds: [...runIds],
    roofSlabIds,
  };
};

/** Apply the follow transform to one rider's plan pose. */
export const followRiderPose = <
  T extends { originX: number; originY: number; rotationDeg: number },
>(
  body: T,
  follow: FloorFollow,
): { originX: number; originY: number; rotationDeg: number } => {
  const rotated = rotateAbout(
    body.originX,
    body.originY,
    follow.pivotXMm,
    follow.pivotYMm,
    follow.sweepDeg,
  );
  return {
    originX: Math.round(rotated.x + follow.deltaXMm),
    originY: Math.round(rotated.y + follow.deltaYMm),
    rotationDeg:
      follow.sweepDeg === 0
        ? body.rotationDeg
        : normalizePlanAngleDeg(body.rotationDeg + follow.sweepDeg),
  };
};
