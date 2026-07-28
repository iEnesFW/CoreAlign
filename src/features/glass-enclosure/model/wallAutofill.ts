import {
  RUN_PLAN_THICKNESS_MM,
  buildPlanFootprint,
  buildRunFootprint,
  penetratesAny,
} from '../scene/interaction/planCollision';
import type { PlanFootprint } from '../scene/interaction/planCollision';
import { arcPointAt } from './arcGeometry';
import { resolveWallArc, resolveWallHoles } from './wallHoleGeometry';
import type { WallHoleSkip } from './wallHoleGeometry';
import type { PanelShapeKind, SceneRunState, SceneWallState } from './project.types';

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

const ENDPOINT_TOLERANCE_MM = 150;
export const MIN_EDGE_MM = 300;

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

export interface WallFillPlan {
  edges: OpenEdge[];
  skipped: WallHoleSkip[];
}

// WHY: the hole set comes from resolveWallHoles — the SAME decisions the wall builder makes when it
// carves the body. Re-deriving it here (raw record, no clamp, no fits/overlap gate) is what produced
// glass that did not match the hole: 11 mm too tall on a full-height window, 5 mm too wide against a
// wall end, and whole panes for holes the wall never cut.
export const computeWallFillPlan = (
  walls: SceneWallState[],
  existingRuns: SceneRunState[] = [],
): WallFillPlan => {
  // Skip a hole that an existing glass run already covers, so re-running autofill is idempotent
  // (no stacked duplicate panels).
  const runFootprints = existingRuns.map((r) => buildRunFootprint(r, 0, 0, r.rotationDeg));
  const edges: OpenEdge[] = [];
  const skipped: WallHoleSkip[] = [];
  // WHY: 'approximated' is an advisory about a hole the resolver GLAZED. If the plan then refuses
  // that same hole, the advisory would claim a fill that never happened and double-count the hole.
  const refused = new Set<string>();
  const refuse = (hole: { source: string; id: string }, reason: WallHoleSkip['reason']) => {
    refused.add(`${hole.source}:${hole.id}`);
    skipped.push({ source: hole.source as WallHoleSkip['source'], id: hole.id, reason });
  };
  for (const wall of walls) {
    const resolved = resolveWallHoles(wall);
    skipped.push(...resolved.skipped);
    const radians = (wall.rotationDeg * Math.PI) / 180;
    const cos = Math.cos(radians);
    const sin = Math.sin(radians);
    // The hole's bottom is measured from the wall's own base, so a raised wall lifts the fill panel
    // by the wall's geomZ on top of the local sill height.
    const wallBaseZ = wall.geomZ ?? 0;
    // ARC wall: hole offsets are DEVELOPED arc-length u, and rotationDeg is the ROLLED start tangent
    // — walking origin + u·dir(rotationDeg) leaves the wall immediately (~0.3·R off at the mid-face
    // of a 90° arc, the reported "detached glass"). The fill must be a SUB-ARC of the wall: same
    // radius, sweep = uWidth/radius, origin ON the arc, rotation = the tangent at the hole's start.
    const resolvedArcWall = resolveWallArc(wall);
    for (const hole of resolved.holes) {
      if (hole.uWidthMm < MIN_EDGE_MM || hole.zHeightMm < MIN_EDGE_MM) {
        refuse(hole, 'tooSmall');
        continue;
      }
      const geomZ = Math.round(wallBaseZ + hole.zBottomMm);
      // WHY: round the TOP against the same grid as the base, not the height on its own. Rounding
      // base and height independently let the pane's top edge land up to 0.75 mm off the carved
      // hole's top (measured on a polygon feature hole) — a hairline z-fighting seam.
      const heightMm = Math.round(wallBaseZ + hole.zBottomMm + hole.zHeightMm) - geomZ;
      if (resolvedArcWall) {
        const phi0 = hole.uStartMm / resolvedArcWall.radiusMm;
        const subSweepRad = hole.uWidthMm / resolvedArcWall.radiusMm;
        const start = arcPointAt(resolvedArcWall.radiusMm, resolvedArcWall.direction, phi0);
        const originX = Math.round(wall.originX + start.x * cos - start.z * sin);
        const originY = Math.round(wall.originY + start.x * sin + start.z * cos);
        const rotationDeg =
          Math.round((wall.rotationDeg + resolvedArcWall.direction * phi0 * (180 / Math.PI)) * 10) /
          10;
        // WHY: measure on the EXACT resolved radius (the band the wall actually draws), round only
        // what is stored — geomArcRadiusMm is persisted as an integer.
        const subChordMm = Math.round(2 * resolvedArcWall.radiusMm * Math.sin(subSweepRad / 2));
        // WHY: 0.01° (matches the backend numeric(5,2) so it survives the refetch) — a coarser 0.1°
        // drifted the panel's DEVELOPED width ~2-3mm from the hole, the visible gap on a shaped fill.
        const subSweepDeg =
          Math.round(resolvedArcWall.direction * subSweepRad * (180 / Math.PI) * 100) / 100;
        // Idempotency footprint from the REAL sub-arc band (a straight capsule along the phantom
        // tangent would neither match the bent run it created nor the hole it should cover).
        const pseudoRun: SceneRunState = {
          id: 'opening-edge',
          orderIndex: 0,
          label: '',
          lengthMm: subChordMm,
          heightMm,
          originX,
          originY,
          rotationDeg,
          profileSystemId: '',
          colorId: null,
          hasTopDrip: false,
          hasBottomThreshold: false,
          geomZ,
          geomArcRadiusMm: Math.round(resolvedArcWall.radiusMm),
          geomArcSweepDeg: subSweepDeg,
          panels: [],
        };
        if (penetratesAny(buildRunFootprint(pseudoRun, 0, 0, rotationDeg), runFootprints)) {
          refuse(hole, 'alreadyFilled');
          continue;
        }
        edges.push({
          originX,
          originY,
          rotationDeg,
          lengthMm: subChordMm,
          heightMm,
          geomZ,
          geomArcRadiusMm: Math.round(resolvedArcWall.radiusMm),
          geomArcSweepDeg: subSweepDeg,
          arcGlassBent: true,
          shapeKind: hole.shape?.shapeKind ?? null,
          shapePointsJson: hole.shape?.shapePointsJson ?? null,
        });
        continue;
      }
      const originX = Math.round(wall.originX + hole.uStartMm * cos);
      const originY = Math.round(wall.originY + hole.uStartMm * sin);
      const lengthMm = Math.round(hole.uWidthMm);
      const footprint: PlanFootprint = buildPlanFootprint(
        'opening-edge',
        originX,
        originY,
        lengthMm,
        wall.rotationDeg,
        RUN_PLAN_THICKNESS_MM / 2,
        geomZ,
        geomZ + heightMm,
      );
      if (penetratesAny(footprint, runFootprints)) {
        refuse(hole, 'alreadyFilled');
        continue;
      }
      edges.push({
        originX,
        originY,
        rotationDeg: wall.rotationDeg,
        lengthMm,
        heightMm,
        geomZ,
        shapeKind: hole.shape?.shapeKind ?? null,
        shapePointsJson: hole.shape?.shapePointsJson ?? null,
      });
    }
  }
  return {
    edges,
    skipped: skipped.filter(
      (s) => s.reason !== 'approximated' || !refused.has(`${s.source}:${s.id}`),
    ),
  };
};

export const computeOpeningEdges = (
  walls: SceneWallState[],
  existingRuns: SceneRunState[] = [],
): OpenEdge[] => computeWallFillPlan(walls, existingRuns).edges;
