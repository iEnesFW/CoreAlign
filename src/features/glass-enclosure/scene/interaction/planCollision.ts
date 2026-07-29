import { buildPlanFootprint, buildPolygonFootprint } from '@/shared/three-engine';
import { isRealArc } from '../../model/arcGeometry';
import { arcBandOutlineMm } from '../../model/bandOutline';
import { curvedSlabPlanOutlineMm } from '../builders/curvedSlabGeometry';
import type { PlanFootprint } from '@/shared/three-engine';
import type {
  SceneRunState,
  SceneSlabState,
  SceneSurfaceState,
  SceneWallState,
} from '../../model/project.types';

export {
  buildPlanFootprint,
  buildPolygonFootprint,
  restElevationMm,
  restElevationAtPointMm,
  restsOnSupportAtMm,
  isFloating,
  normalizePlanAngleDeg,
  penetratesAny,
  firstPenetratingOwner,
  footprintsPenetrate,
  footprintsOverlapXY,
  clampPlanMove,
  clampPlanMoveNoDeepen,
  slidePlanMove,
  clampPlanStretch,
  clampPlanRotation,
  type PlanFootprint,
  type PlanFootprintSet,
} from '@/shared/three-engine';

const DEG2RAD = Math.PI / 180;

export const RUN_PLAN_THICKNESS_MM = 50;

const buildArcWallFootprint = (
  wall: SceneWallState,
  dxMm: number,
  dyMm: number,
  rotationDeg: number,
): PlanFootprint => {
  const zMin = wall.geomZ ?? 0;
  const zMax = zMin + Math.max(wall.heightMm, wall.heightEndMm ?? wall.heightMm);
  const half = wall.thicknessMm / 2;
  const outline = arcBandOutlineMm(
    wall,
    wall.originX + dxMm,
    wall.originY + dyMm,
    rotationDeg,
    half,
  );
  return buildPolygonFootprint(wall.id, outline, zMin, zMax, half);
};

export const buildWallFootprint = (
  wall: SceneWallState,
  dxMm: number,
  dyMm: number,
  rotationDeg: number,
): PlanFootprint => {
  // isRealArc, NOT radius-only: a legacy "half-arc" row (radius set, sweep null/0) RENDERS straight
  // — a radius-only gate gave it a ~1° phantom stub footprint while the body showed full length.
  if (isRealArc(wall.geomArcRadiusMm, wall.geomArcSweepDeg)) {
    return buildArcWallFootprint(wall, dxMm, dyMm, rotationDeg);
  }
  const zMin = wall.geomZ ?? 0;
  return buildPlanFootprint(
    wall.id,
    wall.originX + dxMm,
    wall.originY + dyMm,
    wall.lengthMm,
    rotationDeg,
    wall.thicknessMm / 2,
    zMin,
    zMin + Math.max(wall.heightMm, wall.heightEndMm ?? wall.heightMm),
  );
};

const buildArcRunFootprint = (
  run: SceneRunState,
  dxMm: number,
  dyMm: number,
  rotationDeg: number,
): PlanFootprint => {
  const zMin = run.geomZ ?? 0;
  const half = RUN_PLAN_THICKNESS_MM / 2;
  const outline = arcBandOutlineMm(run, run.originX + dxMm, run.originY + dyMm, rotationDeg, half);
  return buildPolygonFootprint(run.id, outline, zMin, zMin + run.heightMm, half);
};

export const buildRunFootprint = (
  run: SceneRunState,
  dxMm: number,
  dyMm: number,
  rotationDeg: number,
): PlanFootprint => {
  if (isRealArc(run.geomArcRadiusMm, run.geomArcSweepDeg)) {
    return buildArcRunFootprint(run, dxMm, dyMm, rotationDeg);
  }
  const zMin = run.geomZ ?? 0;
  return buildPlanFootprint(
    run.id,
    run.originX + dxMm,
    run.originY + dyMm,
    run.lengthMm,
    rotationDeg,
    RUN_PLAN_THICKNESS_MM / 2,
    zMin,
    zMin + run.heightMm,
  );
};

export const buildSlabFootprint = (
  slab: SceneSlabState,
  dxMm: number,
  dyMm: number,
  rotationDeg: number,
): PlanFootprint => {
  const rad = rotationDeg * DEG2RAD;
  // A plan-curved slab's body bows OUTSIDE the flat rect (apex by the sagitta, back edge fanning
  // past the ends) — collide/snap/stack against the real band, not a phantom rectangle. Sampled
  // from the SAME plan columns the mesh is built from.
  if (isRealArc(slab.geomArcRadiusMm, slab.geomArcSweepDeg)) {
    const cosR = Math.cos(rad);
    const sinR = Math.sin(rad);
    const outline = curvedSlabPlanOutlineMm(
      slab.lengthMm,
      slab.depthMm,
      slab.geomArcRadiusMm ?? 0,
      slab.geomArcSweepDeg ?? 1,
      slab.slabArcAxis ?? 'length',
    ).map((p) => ({
      x: slab.originX + dxMm + p.x * cosR - p.z * sinR,
      y: slab.originY + dyMm + p.x * sinR + p.z * cosR,
    }));
    return buildPolygonFootprint(
      slab.id,
      outline,
      slab.elevationMm,
      slab.elevationMm + slab.thicknessMm,
      Math.min(slab.lengthMm, slab.depthMm) / 2,
    );
  }
  return buildPlanFootprint(
    slab.id,
    slab.originX + dxMm - (Math.sin(rad) * slab.depthMm) / 2,
    slab.originY + dyMm + (Math.cos(rad) * slab.depthMm) / 2,
    slab.lengthMm,
    rotationDeg,
    slab.depthMm / 2,
    slab.elevationMm,
    slab.elevationMm + slab.thicknessMm,
  );
};

export const buildSurfaceFootprint = (
  surface: SceneSurfaceState,
  dxMm = 0,
  dyMm = 0,
): PlanFootprint =>
  buildPolygonFootprint(
    surface.id,
    surface.points.map((p) => ({ x: p.x + dxMm, y: p.y + dyMm })),
    surface.elevationMm,
    surface.elevationMm + surface.thicknessMm,
  );
