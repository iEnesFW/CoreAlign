import { buildPlanFootprint, buildPolygonFootprint } from '@/shared/three-engine';
import { isRealArc } from '../../model/arcGeometry';
import { arcBandOutlineMm } from '../../model/bandOutline';
import { curvedSlabPlanOutlineMm } from '../builders/curvedSlabGeometry';
import { bowedPolygonOutline, edgeArcOutline, hasEdgeArc } from '../../model/edgeArcOutline';
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
  stackableSupports,
  supportTopBelowMm,
  liftToClearMm,
  SUPPORT_TOLERANCE_MM,
  WALKABLE_STEP_UP_MM,
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

/**
 * How far a slab's real body rises above its flat top: the ridge of a barrel or pitched roof.
 *
 * WHY the footprint has to know: the mesh extrudes that profile from y=0 up to rise+thickness, but
 * the footprint claimed `elevation .. elevation + thickness`, so a body dropped or Alt-stacked onto
 * an 800 mm ridge buried itself 800 mm inside it and nothing reported a collision. The box is
 * CONSERVATIVE (the ridge is only that high mid-span) — that is the safe direction: it lifts a body
 * slightly too much near the eaves instead of letting it pass through the roof.
 */
export const slabRiseMm = (slab: SceneSlabState): number =>
  Math.max(0, slab.arcRiseMm ?? 0, slab.pitchRiseMm ?? 0);

export const buildSlabFootprint = (
  slab: SceneSlabState,
  dxMm: number,
  dyMm: number,
  rotationDeg: number,
): PlanFootprint => {
  const rad = rotationDeg * DEG2RAD;
  const topMm = slab.elevationMm + slabRiseMm(slab) + slab.thicknessMm;
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
    return {
      ...buildPolygonFootprint(
        slab.id,
        outline,
        slab.elevationMm,
        topMm,
        Math.min(slab.lengthMm, slab.depthMm) / 2,
      ),
      walkable: slab.kind === 'floor',
    };
  }
  // A single-edge arc bows one rect edge OUTSIDE the plan rectangle, and the mesh is built from
  // exactly this outline (SlabObject's hasEdgeArc branch) — collide against the real silhouette.
  // An INWARD bow matters just as much: a rectangle would over-claim and veto legitimate moves.
  if (hasEdgeArc(slab.geomEdgeArc)) {
    const cosR = Math.cos(rad);
    const sinR = Math.sin(rad);
    const bowed = edgeArcOutline(slab.lengthMm, slab.depthMm, slab.geomEdgeArc ?? {});
    if (bowed.length >= 3) {
      return {
        ...buildPolygonFootprint(
          slab.id,
          bowed.map((p) => ({
            x: slab.originX + dxMm + p.x * cosR - p.y * sinR,
            y: slab.originY + dyMm + p.x * sinR + p.y * cosR,
          })),
          slab.elevationMm,
          topMm,
          Math.min(slab.lengthMm, slab.depthMm) / 2,
        ),
        walkable: slab.kind === 'floor',
      };
    }
  }
  return {
    ...buildPlanFootprint(
      slab.id,
      slab.originX + dxMm - (Math.sin(rad) * slab.depthMm) / 2,
      slab.originY + dyMm + (Math.cos(rad) * slab.depthMm) / 2,
      slab.lengthMm,
      rotationDeg,
      slab.depthMm / 2,
      slab.elevationMm,
      topMm,
    ),
    walkable: slab.kind === 'floor',
  };
};

export const buildSurfaceFootprint = (
  surface: SceneSurfaceState,
  dxMm = 0,
  dyMm = 0,
): PlanFootprint => ({
  ...buildPolygonFootprint(
    surface.id,
    // The mesh (and the DXF) is built from the BOWED outline — a raw-vertex footprint let a body
    // bowed 600 mm outward pass straight through that slice, and an inward bow claim space it
    // does not occupy. bowedPolygonOutline returns the raw points when there are no arcs.
    bowedPolygonOutline(surface.points, surface.edgeArcs ?? null).map((p) => ({
      x: p.x + dxMm,
      y: p.y + dyMm,
    })),
    surface.elevationMm,
    surface.elevationMm + surface.thicknessMm,
  ),
  walkable: surface.kind === 'floor',
});
