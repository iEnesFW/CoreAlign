import type { PlanMoveDelta } from './planSnap';

export interface PlanFootprint {
  ownerId: string;
  x1: number;
  y1: number;
  x2: number;
  y2: number;
  halfWidthMm: number;
  zMinMm: number;
  zMaxMm: number;
  // When set, collision uses this true plan outline (world mm) instead of the
  // spine +/- halfWidth rectangle, so curved/polygon objects collide by shape.
  polygon?: Vec[];
}

const DEG2RAD = Math.PI / 180;
const CONTACT_EPS_MM = 1;
const CLAMP_ITERATIONS = 14;

export const normalizePlanAngleDeg = (deg: number) => ((deg % 360) + 360) % 360;

export const buildPlanFootprint = (
  ownerId: string,
  startX: number,
  startY: number,
  lengthMm: number,
  rotationDeg: number,
  halfWidthMm: number,
  zMinMm: number,
  zMaxMm: number,
): PlanFootprint => {
  const rad = rotationDeg * DEG2RAD;
  const cos = Math.cos(rad);
  const sin = Math.sin(rad);
  return {
    ownerId,
    x1: startX,
    y1: startY,
    x2: startX + lengthMm * cos,
    y2: startY + lengthMm * sin,
    halfWidthMm,
    zMinMm,
    zMaxMm,
  };
};

interface Vec {
  x: number;
  y: number;
}

// Footprint whose collision shape is an arbitrary plan outline (world mm),
// used for curved runs and pen-drawn polygon surfaces.
export const buildPolygonFootprint = (
  ownerId: string,
  polygon: Vec[],
  zMinMm: number,
  zMaxMm: number,
): PlanFootprint => {
  let minX = Infinity;
  let minY = Infinity;
  let maxX = -Infinity;
  let maxY = -Infinity;
  for (const p of polygon) {
    if (p.x < minX) minX = p.x;
    if (p.y < minY) minY = p.y;
    if (p.x > maxX) maxX = p.x;
    if (p.y > maxY) maxY = p.y;
  }
  return {
    ownerId,
    x1: minX,
    y1: minY,
    x2: maxX,
    y2: maxY,
    halfWidthMm: 0,
    zMinMm,
    zMaxMm,
    polygon,
  };
};

// The 4 corners of the oriented rectangle (full centerline +/- halfWidth).
const footprintCorners = (f: PlanFootprint): Vec[] => {
  let dx = f.x2 - f.x1;
  let dy = f.y2 - f.y1;
  const len = Math.hypot(dx, dy);
  if (len < 1e-6) {
    dx = 1;
    dy = 0;
  } else {
    dx /= len;
    dy /= len;
  }
  const nx = -dy * f.halfWidthMm;
  const ny = dx * f.halfWidthMm;
  return [
    { x: f.x1 + nx, y: f.y1 + ny },
    { x: f.x2 + nx, y: f.y2 + ny },
    { x: f.x2 - nx, y: f.y2 - ny },
    { x: f.x1 - nx, y: f.y1 - ny },
  ];
};

const projectOverlap = (a: Vec[], b: Vec[], axisX: number, axisY: number): number => {
  let aMin = Infinity;
  let aMax = -Infinity;
  let bMin = Infinity;
  let bMax = -Infinity;
  for (const p of a) {
    const v = p.x * axisX + p.y * axisY;
    if (v < aMin) aMin = v;
    if (v > aMax) aMax = v;
  }
  for (const p of b) {
    const v = p.x * axisX + p.y * axisY;
    if (v < bMin) bMin = v;
    if (v > bMax) bMax = v;
  }
  return Math.min(aMax, bMax) - Math.max(aMin, bMin);
};

// Largest overlap extent of the intersection region across both rectangles'
// edge axes (SAT). Returns 0 when separated. A genuine corner butt-joint is
// small on every axis, so the max stays within a half width; a deep slice is
// large on the axis it cuts along, so the max exposes it even when the SAT
// minimum collapses to a thin sliver.
const obbOverlapExtent = (ca: Vec[], cb: Vec[]): number => {
  const axes: Vec[] = [];
  for (const corners of [ca, cb]) {
    for (let i = 0; i < 2; i += 1) {
      const ax = corners[i + 1].x - corners[i].x;
      const ay = corners[i + 1].y - corners[i].y;
      const len = Math.hypot(ax, ay);
      if (len < 1e-6) continue;
      axes.push({ x: -ay / len, y: ax / len });
    }
  }
  let maxOverlap = 0;
  for (const axis of axes) {
    const overlap = projectOverlap(ca, cb, axis.x, axis.y);
    if (overlap <= 0) return 0;
    if (overlap > maxOverlap) maxOverlap = overlap;
  }
  return maxOverlap;
};

const footprintOutline = (f: PlanFootprint): Vec[] => f.polygon ?? footprintCorners(f);

interface Aabb {
  minX: number;
  minY: number;
  maxX: number;
  maxY: number;
}

const outlineAabb = (pts: Vec[]): Aabb => {
  let minX = Infinity;
  let minY = Infinity;
  let maxX = -Infinity;
  let maxY = -Infinity;
  for (const p of pts) {
    if (p.x < minX) minX = p.x;
    if (p.y < minY) minY = p.y;
    if (p.x > maxX) maxX = p.x;
    if (p.y > maxY) maxY = p.y;
  }
  return { minX, minY, maxX, maxY };
};

const aabbSeparated = (a: PlanFootprint, b: PlanFootprint): boolean => {
  const ba = outlineAabb(footprintOutline(a));
  const bb = outlineAabb(footprintOutline(b));
  return ba.maxX < bb.minX || bb.maxX < ba.minX || ba.maxY < bb.minY || bb.maxY < ba.minY;
};

const pointInPolygon = (px: number, py: number, poly: Vec[]): boolean => {
  let inside = false;
  for (let i = 0, j = poly.length - 1; i < poly.length; j = i, i += 1) {
    const xi = poly[i].x;
    const yi = poly[i].y;
    const xj = poly[j].x;
    const yj = poly[j].y;
    if (yi > py !== yj > py && px < ((xj - xi) * (py - yi)) / (yj - yi) + xi) inside = !inside;
  }
  return inside;
};

const segmentsCross = (a1: Vec, a2: Vec, b1: Vec, b2: Vec): boolean => {
  const dir = (p: Vec, q: Vec, r: Vec) => (q.x - p.x) * (r.y - p.y) - (q.y - p.y) * (r.x - p.x);
  const d1 = dir(b1, b2, a1);
  const d2 = dir(b1, b2, a2);
  const d3 = dir(a1, a2, b1);
  const d4 = dir(a1, a2, b2);
  return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
};

// True if two simple plan polygons overlap with area (a shared edge or vertex
// touch is not an overlap). Handles non-convex outlines, unlike the SAT path.
const polygonsOverlap = (a: Vec[], b: Vec[]): boolean => {
  for (let i = 0; i < a.length; i += 1) {
    const a1 = a[i];
    const a2 = a[(i + 1) % a.length];
    for (let j = 0; j < b.length; j += 1) {
      if (segmentsCross(a1, a2, b[j], b[(j + 1) % b.length])) return true;
    }
  }
  // Containment with no edge crossing: probe every vertex, so an overlap is not
  // missed when one polygon's first corner happens to land on the other's edge.
  for (const p of a) if (pointInPolygon(p.x, p.y, b)) return true;
  for (const p of b) if (pointInPolygon(p.x, p.y, a)) return true;
  return false;
};

const footprintsPenetrate = (a: PlanFootprint, b: PlanFootprint) => {
  if (a.zMaxMm <= b.zMinMm + CONTACT_EPS_MM || b.zMaxMm <= a.zMinMm + CONTACT_EPS_MM) return false;
  if (a.polygon || b.polygon) {
    if (aabbSeparated(a, b)) return false;
    return polygonsOverlap(footprintOutline(a), footprintOutline(b));
  }
  const extent = obbOverlapExtent(footprintCorners(a), footprintCorners(b));
  // Allow a true corner butt-joint (a small square bounded by the thinner half
  // width) but block any overlap that runs deeper along either body axis.
  const jointTolerance = Math.min(a.halfWidthMm, b.halfWidthMm) + CONTACT_EPS_MM;
  return extent > jointTolerance;
};

// 2D (z-agnostic) overlap test, used by the stacking law to decide which
// supports a moved object rests on.
const footprintsOverlapXY = (a: PlanFootprint, b: PlanFootprint): boolean => {
  if (aabbSeparated(a, b)) return false;
  if (a.polygon || b.polygon) return polygonsOverlap(footprintOutline(a), footprintOutline(b));
  return obbOverlapExtent(footprintCorners(a), footprintCorners(b)) > CONTACT_EPS_MM;
};

// Elevation (mm) a moved footprint rests at: the highest top among supports it
// overlaps in plan, or the given fallback when it rests on nothing.
export const restElevationMm = (
  moved: PlanFootprint,
  supports: PlanFootprint[],
  fallbackMm: number,
): number => {
  let top = fallbackMm;
  for (const s of supports) {
    if (s.ownerId === moved.ownerId) continue;
    if (s.zMaxMm > top && footprintsOverlapXY(moved, s)) top = s.zMaxMm;
  }
  return top;
};

export type PlanFootprintSet = PlanFootprint | PlanFootprint[];

export const penetratesAny = (moved: PlanFootprintSet, obstacles: PlanFootprint[]) => {
  const footprints = Array.isArray(moved) ? moved : [moved];
  return footprints.some((footprint) =>
    obstacles.some((o) => o.ownerId !== footprint.ownerId && footprintsPenetrate(footprint, o)),
  );
};

export const clampPlanMove = (
  footprintAt: (dxMm: number, dyMm: number) => PlanFootprintSet,
  obstacles: PlanFootprint[],
  dxMm: number,
  dyMm: number,
): PlanMoveDelta => {
  if (!penetratesAny(footprintAt(dxMm, dyMm), obstacles)) return { dxMm, dyMm };
  if (penetratesAny(footprintAt(0, 0), obstacles)) return { dxMm, dyMm };
  let lo = 0;
  let hi = 1;
  for (let i = 0; i < CLAMP_ITERATIONS; i += 1) {
    const mid = (lo + hi) / 2;
    if (penetratesAny(footprintAt(dxMm * mid, dyMm * mid), obstacles)) hi = mid;
    else lo = mid;
  }
  return { dxMm: Math.round(dxMm * lo), dyMm: Math.round(dyMm * lo) };
};

const SWEEP_MIN_STEPS = 24;
const SWEEP_MAX_STEPS = 4096;

// Smallest obstacle "thickness" band along the sweep, so the step length stays
// below it and a thin obstacle can never be straddled between two samples.
const minObstacleBandMm = (obstacles: PlanFootprint[]): number => {
  let min = Number.POSITIVE_INFINITY;
  for (const o of obstacles) min = Math.min(min, o.halfWidthMm);
  return Number.isFinite(min) ? Math.max(2, min) : 2;
};

// When the destination is clear but the straight path crosses an obstacle, stop
// the moved object flush at the near face instead of letting it teleport behind.
const sweptBoundary = (
  footprintAt: (dxMm: number, dyMm: number) => PlanFootprintSet,
  obstacles: PlanFootprint[],
  dxMm: number,
  dyMm: number,
): PlanMoveDelta | null => {
  const pathLen = Math.hypot(dxMm, dyMm);
  // Resolve sweep step below the thinnest obstacle band so we cannot tunnel.
  const steps = Math.min(
    SWEEP_MAX_STEPS,
    Math.max(SWEEP_MIN_STEPS, Math.ceil(pathLen / minObstacleBandMm(obstacles))),
  );
  let lastClear = 0;
  for (let i = 1; i <= steps; i += 1) {
    const t = i / steps;
    if (penetratesAny(footprintAt(dxMm * t, dyMm * t), obstacles)) {
      let lo = lastClear;
      let hi = t;
      for (let k = 0; k < CLAMP_ITERATIONS; k += 1) {
        const mid = (lo + hi) / 2;
        if (penetratesAny(footprintAt(dxMm * mid, dyMm * mid), obstacles)) hi = mid;
        else lo = mid;
      }
      return { dxMm: Math.round(dxMm * lo), dyMm: Math.round(dyMm * lo) };
    }
    lastClear = t;
  }
  return null;
};

export const slidePlanMove = (
  footprintAt: (dxMm: number, dyMm: number) => PlanFootprintSet,
  obstacles: PlanFootprint[],
  dxMm: number,
  dyMm: number,
): PlanMoveDelta => {
  if (penetratesAny(footprintAt(0, 0), obstacles)) return { dxMm, dyMm };
  if (!penetratesAny(footprintAt(dxMm, dyMm), obstacles)) {
    return sweptBoundary(footprintAt, obstacles, dxMm, dyMm) ?? { dxMm, dyMm };
  }
  const alongX = clampPlanMove(footprintAt, obstacles, dxMm, 0);
  const alongY = clampPlanMove(footprintAt, obstacles, 0, dyMm);
  if (!penetratesAny(footprintAt(alongX.dxMm, alongY.dyMm), obstacles)) {
    return { dxMm: alongX.dxMm, dyMm: alongY.dyMm };
  }
  return clampPlanMove(footprintAt, obstacles, dxMm, dyMm);
};

export const clampPlanStretch = (
  footprintAt: (deltaMm: number) => PlanFootprintSet,
  obstacles: PlanFootprint[],
  deltaMm: number,
): number => {
  if (!penetratesAny(footprintAt(deltaMm), obstacles)) return deltaMm;
  if (penetratesAny(footprintAt(0), obstacles)) return deltaMm;
  let lo = 0;
  let hi = 1;
  for (let i = 0; i < CLAMP_ITERATIONS; i += 1) {
    const mid = (lo + hi) / 2;
    if (penetratesAny(footprintAt(deltaMm * mid), obstacles)) hi = mid;
    else lo = mid;
  }
  return Math.round(deltaMm * lo);
};

export const clampPlanRotation = (
  footprintAt: (deg: number) => PlanFootprintSet,
  obstacles: PlanFootprint[],
  fromDeg: number,
  toDeg: number,
): number => {
  if (!penetratesAny(footprintAt(toDeg), obstacles)) return toDeg;
  if (penetratesAny(footprintAt(fromDeg), obstacles)) return toDeg;
  let lo = 0;
  let hi = 1;
  for (let i = 0; i < CLAMP_ITERATIONS; i += 1) {
    const mid = (lo + hi) / 2;
    if (penetratesAny(footprintAt(fromDeg + (toDeg - fromDeg) * mid), obstacles)) hi = mid;
    else lo = mid;
  }
  return Math.round(fromDeg + (toDeg - fromDeg) * lo);
};
