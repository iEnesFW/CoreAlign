import { buildWallFootprint, type PlanFootprint } from '../scene/interaction/planCollision';
import { enclosedPolygonFromWalls } from './enclosedPolygonFromWalls';
import type { SceneSlabState, SceneSurfaceState, SceneWallState } from './project.types';

const ROOF_THICKNESS_MM = 150;
const MIN_WALLS_FOR_ROOF = 3;

// The plan-outline points of a wall footprint: the polygon for an arc/bent wall, or the four
// rectangle corners derived from the centreline + half-thickness for a straight one.
const footprintPoints = (f: PlanFootprint): { x: number; y: number }[] => {
  if (f.polygon) return f.polygon;
  const dx = f.x2 - f.x1;
  const dy = f.y2 - f.y1;
  const len = Math.hypot(dx, dy) || 1;
  const nx = (-dy / len) * f.halfWidthMm;
  const ny = (dx / len) * f.halfWidthMm;
  return [
    { x: f.x1 + nx, y: f.y1 + ny },
    { x: f.x1 - nx, y: f.y1 - ny },
    { x: f.x2 + nx, y: f.y2 + ny },
    { x: f.x2 - nx, y: f.y2 - ny },
  ];
};

const wallTopMm = (wall: SceneWallState) =>
  (wall.geomZ ?? 0) + Math.max(wall.heightMm, wall.heightEndMm ?? wall.heightMm);

// A flat rectangular roof covering the bounding box of the selected walls, resting on the tallest
// wall's top. Arc/bent walls are covered too — their footprint polygon feeds the bbox. Returns the
// slab geometry (no id); the caller adds an id and inserts it. Null for fewer than three walls or a
// degenerate area. (A polygon-exact roof that hugs a non-rectangular plan is a later refinement.)
export const computeRoofOverWalls = (
  walls: SceneWallState[],
): Omit<SceneSlabState, 'id'> | null => {
  if (walls.length < MIN_WALLS_FOR_ROOF) return null;
  let minX = Infinity;
  let minY = Infinity;
  let maxX = -Infinity;
  let maxY = -Infinity;
  for (const wall of walls) {
    for (const point of footprintPoints(buildWallFootprint(wall, 0, 0, wall.rotationDeg))) {
      if (point.x < minX) minX = point.x;
      if (point.x > maxX) maxX = point.x;
      if (point.y < minY) minY = point.y;
      if (point.y > maxY) maxY = point.y;
    }
  }
  const lengthMm = Math.round(maxX - minX);
  const depthMm = Math.round(maxY - minY);
  if (lengthMm < 1 || depthMm < 1) return null;
  return {
    kind: 'roof',
    originX: Math.round(minX),
    originY: Math.round(minY),
    rotationDeg: 0,
    lengthMm,
    depthMm,
    thicknessMm: ROOF_THICKNESS_MM,
    elevationMm: Math.round(Math.max(...walls.map(wallTopMm))),
    colorHex: null,
    features: [],
  };
};

export const computeRoofSurfaceOverWalls = (
  walls: SceneWallState[],
): Omit<SceneSurfaceState, 'id'> | null => {
  const polygon = enclosedPolygonFromWalls(walls);
  if (!polygon || polygon.length < 3) return null;
  return {
    kind: 'roof',
    points: polygon,
    elevationMm: Math.round(Math.max(...walls.map(wallTopMm))),
    thicknessMm: ROOF_THICKNESS_MM,
    colorHex: null,
  };
};
