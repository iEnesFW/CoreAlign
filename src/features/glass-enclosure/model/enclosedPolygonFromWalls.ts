import { bowArcPlanPoints, bowFromArc, isRealArc, radiusFromChordSweep } from './arcGeometry';
import { bodyEndLocalMm } from '../geometry/curvature';
import { polygonSelfIntersects, type Point2D } from './polygonValidation';
import type { SceneSurfacePoint, SceneWallState } from './project.types';

const SNAP_MM = 60;
const ARC_SEGMENTS = 24;

const rotate = (x: number, y: number, deg: number): Point2D => {
  const r = (deg * Math.PI) / 180;
  const c = Math.cos(r);
  const s = Math.sin(r);
  return { x: x * c - y * s, y: x * s + y * c };
};

const isBentWall = (wall: SceneWallState): boolean =>
  typeof wall.bendAngleDeg === 'number' &&
  Math.abs(wall.bendAngleDeg) > 0.01 &&
  typeof wall.bendAtMm === 'number' &&
  wall.bendAtMm > 0;

const wallStart = (wall: SceneWallState): Point2D => ({ x: wall.originX, y: wall.originY });

const localEnd = (wall: SceneWallState): Point2D => {
  if (isRealArc(wall.geomArcRadiusMm, wall.geomArcSweepDeg)) {
    const e = bodyEndLocalMm(wall);
    return { x: e.xMm, y: e.yMm };
  }
  if (isBentWall(wall)) {
    const leg1 = Math.min(wall.bendAtMm ?? 0, wall.lengthMm);
    const leg2 = Math.max(0, wall.lengthMm - leg1);
    const turn = ((wall.bendAngleDeg ?? 0) * Math.PI) / 180;
    return { x: leg1 + leg2 * Math.cos(turn), y: leg2 * Math.sin(turn) };
  }
  return { x: wall.lengthMm, y: 0 };
};

const wallEnd = (wall: SceneWallState): Point2D => {
  const local = localEnd(wall);
  const world = rotate(local.x, local.y, wall.rotationDeg);
  return { x: wall.originX + world.x, y: wall.originY + world.y };
};

const wallPolyline = (wall: SceneWallState, start: Point2D, end: Point2D): Point2D[] => {
  if (isRealArc(wall.geomArcRadiusMm, wall.geomArcSweepDeg)) {
    const chord = Math.hypot(end.x - start.x, end.y - start.y) || 1;
    const radius = radiusFromChordSweep(chord, wall.geomArcRadiusMm, wall.geomArcSweepDeg);
    const sagitta = bowFromArc(chord, radius, wall.geomArcSweepDeg ?? 0);
    return bowArcPlanPoints(start.x, start.y, end.x, end.y, sagitta, ARC_SEGMENTS);
  }
  if (isBentWall(wall)) {
    const leg1 = Math.min(wall.bendAtMm ?? 0, wall.lengthMm);
    const bendLocal = rotate(leg1, 0, wall.rotationDeg);
    return [start, { x: wall.originX + bendLocal.x, y: wall.originY + bendLocal.y }, end];
  }
  return [start, end];
};

export const enclosedPolygonFromWalls = (walls: SceneWallState[]): SceneSurfacePoint[] | null => {
  const usable = walls.filter((w) => w.lengthMm > 0);
  if (usable.length < 3) return null;

  const vertices: Point2D[] = [];
  const vertexOf = (p: Point2D): number => {
    for (let i = 0; i < vertices.length; i += 1) {
      if (Math.hypot(vertices[i].x - p.x, vertices[i].y - p.y) <= SNAP_MM) return i;
    }
    vertices.push({ x: p.x, y: p.y });
    return vertices.length - 1;
  };

  const segs = usable.map((wall) => {
    const start = wallStart(wall);
    const end = wallEnd(wall);
    return { a: vertexOf(start), b: vertexOf(end), poly: wallPolyline(wall, start, end) };
  });

  if (segs.some((seg) => seg.a === seg.b)) return null;

  const adj: number[][] = vertices.map(() => []);
  segs.forEach((seg, i) => {
    adj[seg.a].push(i);
    adj[seg.b].push(i);
  });

  // WHY: a clean single enclosing ring needs EXACTLY two walls at every corner — degree 1 = open
  // chain, degree >= 3 = T-junction/branch; both are ambiguous so the caller falls back to a bbox.
  if (adj.some((list) => list.length !== 2)) return null;

  const used = new Set<number>();
  const ordered: { seg: number; forward: boolean }[] = [];
  let curSeg = 0;
  let entry = segs[0].a;
  for (let step = 0; step < segs.length; step += 1) {
    if (used.has(curSeg)) return null;
    used.add(curSeg);
    const seg = segs[curSeg];
    const forward = seg.a === entry;
    const exit = forward ? seg.b : seg.a;
    ordered.push({ seg: curSeg, forward });
    const next = adj[exit].find((i) => i !== curSeg);
    if (next === undefined) return null;
    curSeg = next;
    entry = exit;
  }
  if (used.size !== segs.length || curSeg !== 0 || entry !== segs[0].a) return null;

  const pts: Point2D[] = [];
  for (const { seg, forward } of ordered) {
    const poly = forward ? segs[seg].poly : [...segs[seg].poly].reverse();
    for (let i = 0; i < poly.length - 1; i += 1) {
      pts.push({ x: Math.round(poly[i].x), y: Math.round(poly[i].y) });
    }
  }
  if (pts.length < 3) return null;
  if (polygonSelfIntersects(pts)) return null;
  return pts;
};
