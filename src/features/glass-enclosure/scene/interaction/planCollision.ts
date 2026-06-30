import { buildPlanFootprint, buildPolygonFootprint } from '@/shared/three-engine';
import { resolveArc } from '../../model/arcGeometry';
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
  isFloating,
  normalizePlanAngleDeg,
  penetratesAny,
  firstPenetratingOwner,
  footprintsPenetrate,
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

const ARC_FOOTPRINT_STEP_RAD = 0.25;

const buildArcWallFootprint = (
  wall: SceneWallState,
  dxMm: number,
  dyMm: number,
  rotationDeg: number,
): PlanFootprint => {
  const zMin = wall.geomZ ?? 0;
  const zMax = zMin + Math.max(wall.heightMm, wall.heightEndMm ?? wall.heightMm);
  // CHORD-INVARIANT: render straight from the stored (radius, sweep); the ends are fixed.
  const resolved = resolveArc(wall.geomArcRadiusMm ?? 0, wall.geomArcSweepDeg ?? 1);
  const radius = resolved.radiusMm;
  const direction = resolved.direction;
  const sweep = resolved.sweepRad;
  const half = wall.thicknessMm / 2;
  const steps = Math.max(6, Math.ceil(sweep / ARC_FOOTPRINT_STEP_RAD));
  const rad = rotationDeg * DEG2RAD;
  const cosR = Math.cos(rad);
  const sinR = Math.sin(rad);
  const toWorld = (lx: number, ly: number) => ({
    x: wall.originX + dxMm + lx * cosR - ly * sinR,
    y: wall.originY + dyMm + lx * sinR + ly * cosR,
  });
  const outer: { x: number; y: number }[] = [];
  const inner: { x: number; y: number }[] = [];
  for (let i = 0; i <= steps; i += 1) {
    const phi = (sweep * i) / steps;
    const px = radius * Math.sin(phi);
    const py = direction * radius * (1 - Math.cos(phi));
    const tangent = Math.atan2(direction * Math.sin(phi), Math.cos(phi));
    const nx = -Math.sin(tangent);
    const ny = Math.cos(tangent);
    outer.push(toWorld(px + nx * half, py + ny * half));
    inner.push(toWorld(px - nx * half, py - ny * half));
  }
  return buildPolygonFootprint(wall.id, [...outer, ...inner.reverse()], zMin, zMax, half);
};

export const buildWallFootprint = (
  wall: SceneWallState,
  dxMm: number,
  dyMm: number,
  rotationDeg: number,
): PlanFootprint => {
  if (wall.geomArcRadiusMm && wall.geomArcRadiusMm > 0) {
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
  // CHORD-INVARIANT: render straight from the stored (radius, sweep); the ends are fixed.
  const resolved = resolveArc(run.geomArcRadiusMm ?? 0, run.geomArcSweepDeg ?? 1);
  const radius = resolved.radiusMm;
  const direction = resolved.direction;
  const sweep = resolved.sweepRad;
  const half = RUN_PLAN_THICKNESS_MM / 2;
  const steps = Math.max(6, Math.ceil(sweep / ARC_FOOTPRINT_STEP_RAD));
  const rad = rotationDeg * DEG2RAD;
  const cosR = Math.cos(rad);
  const sinR = Math.sin(rad);
  const toWorld = (lx: number, ly: number) => ({
    x: run.originX + dxMm + lx * cosR - ly * sinR,
    y: run.originY + dyMm + lx * sinR + ly * cosR,
  });
  const outer: { x: number; y: number }[] = [];
  const inner: { x: number; y: number }[] = [];
  for (let i = 0; i <= steps; i += 1) {
    const phi = (sweep * i) / steps;
    const px = radius * Math.sin(phi);
    const py = direction * radius * (1 - Math.cos(phi));
    const tangent = Math.atan2(direction * Math.sin(phi), Math.cos(phi));
    const nx = -Math.sin(tangent);
    const ny = Math.cos(tangent);
    outer.push(toWorld(px + nx * half, py + ny * half));
    inner.push(toWorld(px - nx * half, py - ny * half));
  }
  return buildPolygonFootprint(
    run.id,
    [...outer, ...inner.reverse()],
    zMin,
    zMin + run.heightMm,
    half,
  );
};

export const buildRunFootprint = (
  run: SceneRunState,
  dxMm: number,
  dyMm: number,
  rotationDeg: number,
): PlanFootprint => {
  if (run.geomArcRadiusMm && run.geomArcRadiusMm > 0) {
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
