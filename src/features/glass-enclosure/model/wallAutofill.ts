import {
  RUN_PLAN_THICKNESS_MM,
  buildPlanFootprint,
  buildRunFootprint,
  penetratesAny,
} from '../scene/interaction/planCollision';
import type { PlanFootprint } from '../scene/interaction/planCollision';
import { arcPointAt, isRealArc, resolveArc } from './arcGeometry';
import { featureOutlineMm } from './wallFeatureGeometry';
import { serializePanelPolygonPoints } from './panelPolygon';
import type {
  PanelShapeKind,
  SceneRunState,
  SceneWallFeature,
  SceneWallState,
} from './project.types';

export interface OpenEdge {
  originX: number;
  originY: number;
  rotationDeg: number;
  lengthMm: number;
  heightMm?: number;
  geomZ?: number;
  geomArcRadiusMm?: number;
  geomArcSweepDeg?: number;
  arcGlassBent?: boolean;
  shapeKind?: PanelShapeKind | null;
  shapePointsJson?: string | null;
}

// Map a shaped wall hole (the feature silhouette) onto a glass PANEL shape so the
// fill glass matches the hole instead of being a rectangle that overflows the wall.
const featurePanelShape = (
  feature: SceneWallFeature,
): { shapeKind: PanelShapeKind; shapePointsJson: string | null } | null => {
  if (feature.shape === 'rect') return null;
  if (feature.shape === 'circle' || feature.shape === 'ellipse') {
    return { shapeKind: 'ellipse', shapePointsJson: null };
  }
  const outline = featureOutlineMm({
    shape: feature.shape,
    offsetMm: feature.offsetMm,
    centerZMm: feature.centerZMm,
    widthMm: feature.widthMm,
    heightMm: feature.heightMm,
    sides: feature.sides,
    points: feature.points,
  });
  // Feature outline is absolute (around offset/centerZ); a panel polygon is local,
  // bottom-centred, y-up — shift x to centre, z (+up) to [0, height].
  const hh = feature.heightMm / 2;
  const pts = outline.map((p) => ({
    x: Math.round(p.x - feature.offsetMm),
    y: Math.round(p.z - feature.centerZMm + hh),
  }));
  return { shapeKind: 'polygon', shapePointsJson: serializePanelPolygonPoints(pts) };
};

const ENDPOINT_TOLERANCE_MM = 150;
const MIN_EDGE_MM = 300;

interface Endpoint {
  x: number;
  y: number;
  wallId: string;
}

const wallEndpoints = (wall: SceneWallState): [Endpoint, Endpoint] => {
  const radians = (wall.rotationDeg * Math.PI) / 180;
  return [
    { x: wall.originX, y: wall.originY, wallId: wall.id },
    {
      x: wall.originX + wall.lengthMm * Math.cos(radians),
      y: wall.originY + wall.lengthMm * Math.sin(radians),
      wallId: wall.id,
    },
  ];
};

const distance = (a: Endpoint, b: Endpoint) => Math.hypot(a.x - b.x, a.y - b.y);

export const computeOpenEdges = (walls: SceneWallState[]): OpenEdge[] => {
  if (walls.length < 2) return [];
  const endpoints = walls.flatMap(wallEndpoints);
  const free = endpoints.filter(
    (point, index) =>
      !endpoints.some(
        (other, otherIndex) =>
          otherIndex !== index &&
          other.wallId !== point.wallId &&
          distance(point, other) <= ENDPOINT_TOLERANCE_MM,
      ),
  );

  const edges: OpenEdge[] = [];
  const used = new Set<number>();
  for (let i = 0; i < free.length; i += 1) {
    if (used.has(i)) continue;
    let best = -1;
    let bestDistance = Number.POSITIVE_INFINITY;
    for (let j = i + 1; j < free.length; j += 1) {
      if (used.has(j) || free[j].wallId === free[i].wallId) continue;
      const d = distance(free[i], free[j]);
      if (d < bestDistance) {
        bestDistance = d;
        best = j;
      }
    }
    if (best === -1 || bestDistance < MIN_EDGE_MM) continue;
    used.add(i);
    used.add(best);
    const a = free[i];
    const b = free[best];
    edges.push({
      originX: Math.round(a.x),
      originY: Math.round(a.y),
      rotationDeg: Math.round((Math.atan2(b.y - a.y, b.x - a.x) * 180) / Math.PI),
      lengthMm: Math.round(bestDistance),
    });
  }
  return edges;
};

export const DEFAULT_PANEL_TARGET_MM = 600;
export const MAX_AUTOFILL_PANELS = 20;
export const SERVER_PANEL_CAP = 50;

export const suggestedPanelCount = (lengthMm: number) =>
  Math.max(1, Math.min(MAX_AUTOFILL_PANELS, Math.ceil(lengthMm / DEFAULT_PANEL_TARGET_MM)));

export const panelCountForWidth = (lengthMm: number, maxPanelWidthMm?: number): number => {
  if (maxPanelWidthMm && maxPanelWidthMm > 0) {
    return Math.max(1, Math.min(SERVER_PANEL_CAP, Math.ceil(lengthMm / maxPanelWidthMm)));
  }
  return Math.max(1, Math.min(MAX_AUTOFILL_PANELS, Math.ceil(lengthMm / DEFAULT_PANEL_TARGET_MM)));
};

export const computeOpeningEdges = (
  walls: SceneWallState[],
  existingRuns: SceneRunState[] = [],
): OpenEdge[] => {
  // Skip an opening/hole that an existing glass run already covers, so re-running
  // autofill is idempotent (no stacked duplicate panels).
  const runFootprints = existingRuns.map((r) => buildRunFootprint(r, 0, 0, r.rotationDeg));
  const edges: OpenEdge[] = [];
  for (const wall of walls) {
    const radians = (wall.rotationDeg * Math.PI) / 180;
    const cos = Math.cos(radians);
    const sin = Math.sin(radians);
    // The opening's sill is measured from the wall's own base, so a raised wall
    // lifts the fill panel by the wall's geomZ on top of the local sill height.
    const wallBaseZ = wall.geomZ ?? 0;
    // ARC wall: feature/opening offsets are DEVELOPED arc-length u, and rotationDeg is the ROLLED
    // start tangent — walking origin + u·dir(rotationDeg) leaves the wall immediately (~0.3·R off
    // at the mid-face of a 90° arc, the reported "detached glass"). The fill must be a SUB-ARC of
    // the wall: same radius, sweep = uWidth/radius, origin ON the arc, rotation = the tangent at
    // the hole's start (which reparametrizes the remaining arc identically).
    const resolvedArcWall = isRealArc(wall.geomArcRadiusMm, wall.geomArcSweepDeg)
      ? resolveArc(wall.geomArcRadiusMm ?? 0, wall.geomArcSweepDeg ?? 1)
      : null;
    const pushEdge = (
      startMm: number,
      widthMm: number,
      sillMm: number,
      heightMm: number,
      shape?: { shapeKind: PanelShapeKind; shapePointsJson: string | null } | null,
    ) => {
      if (widthMm < MIN_EDGE_MM || heightMm < MIN_EDGE_MM) return;
      const geomZ = Math.round(wallBaseZ + sillMm);
      if (resolvedArcWall) {
        const u0 = Math.max(0, Math.min(startMm, resolvedArcWall.arcLengthMm));
        const uWidth = Math.min(widthMm, resolvedArcWall.arcLengthMm - u0);
        if (uWidth < MIN_EDGE_MM) return;
        const phi0 = u0 / resolvedArcWall.radiusMm;
        const subSweepRad = uWidth / resolvedArcWall.radiusMm;
        const start = arcPointAt(resolvedArcWall.radiusMm, resolvedArcWall.direction, phi0);
        const originX = Math.round(wall.originX + start.x * cos - start.z * sin);
        const originY = Math.round(wall.originY + start.x * sin + start.z * cos);
        const rotationDeg =
          Math.round((wall.rotationDeg + resolvedArcWall.direction * phi0 * (180 / Math.PI)) * 10) /
          10;
        const subChordMm = Math.round(2 * resolvedArcWall.radiusMm * Math.sin(subSweepRad / 2));
        const subSweepDeg =
          Math.round(resolvedArcWall.direction * subSweepRad * (180 / Math.PI) * 10) / 10;
        // Idempotency footprint from the REAL sub-arc band (a straight capsule along the phantom
        // tangent would neither match the bent run it created nor the hole it should cover).
        const pseudoRun: SceneRunState = {
          id: 'opening-edge',
          orderIndex: 0,
          label: '',
          lengthMm: subChordMm,
          heightMm: Math.round(heightMm),
          originX,
          originY,
          rotationDeg,
          profileSystemId: '',
          colorId: null,
          hasTopDrip: false,
          hasBottomThreshold: false,
          geomZ,
          geomArcRadiusMm: resolvedArcWall.radiusMm,
          geomArcSweepDeg: subSweepDeg,
          panels: [],
        };
        if (penetratesAny(buildRunFootprint(pseudoRun, 0, 0, rotationDeg), runFootprints)) return;
        edges.push({
          originX,
          originY,
          rotationDeg,
          lengthMm: subChordMm,
          heightMm: Math.round(heightMm),
          geomZ,
          geomArcRadiusMm: resolvedArcWall.radiusMm,
          geomArcSweepDeg: subSweepDeg,
          arcGlassBent: true,
          shapeKind: shape?.shapeKind ?? null,
          shapePointsJson: shape?.shapePointsJson ?? null,
        });
        return;
      }
      const originX = Math.round(wall.originX + startMm * cos);
      const originY = Math.round(wall.originY + startMm * sin);
      const footprint: PlanFootprint = buildPlanFootprint(
        'opening-edge',
        originX,
        originY,
        Math.round(widthMm),
        wall.rotationDeg,
        RUN_PLAN_THICKNESS_MM / 2,
        geomZ,
        geomZ + Math.round(heightMm),
      );
      if (penetratesAny(footprint, runFootprints)) return;
      edges.push({
        originX,
        originY,
        rotationDeg: wall.rotationDeg,
        lengthMm: Math.round(widthMm),
        heightMm: Math.round(heightMm),
        geomZ,
        shapeKind: shape?.shapeKind ?? null,
        shapePointsJson: shape?.shapePointsJson ?? null,
      });
    };
    for (const opening of wall.openings ?? []) {
      pushEdge(
        opening.offsetMm - opening.widthMm / 2,
        opening.widthMm,
        opening.sillMm,
        opening.heightMm,
      );
    }
    for (const feature of wall.features ?? []) {
      const throughHole =
        feature.mode === 'hole' ||
        (feature.mode === 'recess' && feature.depthMm >= wall.thicknessMm - 5);
      if (!throughHole) continue;
      pushEdge(
        feature.offsetMm - feature.widthMm / 2,
        feature.widthMm,
        feature.centerZMm - feature.heightMm / 2,
        feature.heightMm,
        featurePanelShape(feature),
      );
    }
  }
  return edges;
};
