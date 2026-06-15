import { buildPlanFootprint, buildPolygonFootprint } from '@/shared/three-engine';
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
  normalizePlanAngleDeg,
  penetratesAny,
  clampPlanMove,
  slidePlanMove,
  clampPlanStretch,
  clampPlanRotation,
  type PlanFootprint,
  type PlanFootprintSet,
} from '@/shared/three-engine';

const DEG2RAD = Math.PI / 180;

export const RUN_PLAN_THICKNESS_MM = 50;

export const buildWallFootprint = (
  wall: SceneWallState,
  dxMm: number,
  dyMm: number,
  rotationDeg: number,
): PlanFootprint =>
  buildPlanFootprint(
    wall.id,
    wall.originX + dxMm,
    wall.originY + dyMm,
    wall.lengthMm,
    rotationDeg,
    wall.thicknessMm / 2,
    0,
    Math.max(wall.heightMm, wall.heightEndMm ?? wall.heightMm),
  );

export const buildRunFootprint = (
  run: SceneRunState,
  dxMm: number,
  dyMm: number,
  rotationDeg: number,
): PlanFootprint => {
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
