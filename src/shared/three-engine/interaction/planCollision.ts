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
  polygon?: Vec[];
}

const DEG2RAD = Math.PI / 180;
const CONTACT_EPS_MM = 1;
const CLAMP_ITERATIONS = 14;
// How much plan overlap counts as a "joint" (corner butt / flush mount) rather than penetration.
// min(halfWidth) lets thin walls meet at a corner, but for THICK bodies (a 1-2m box, halfWidth
// 500-1000) it let them interpenetrate by half their size. Cap it: walls (halfWidth ≤ cap, i.e.
// thickness ≤ 300mm) still corner-join; a thick box can only overlap by the cap before it blocks.
const JOINT_TOLERANCE_MAX_MM = 150;
const jointToleranceMm = (a: PlanFootprint, b: PlanFootprint): number =>
  Math.min(a.halfWidthMm, b.halfWidthMm, JOINT_TOLERANCE_MAX_MM) + CONTACT_EPS_MM;

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

export const buildPolygonFootprint = (
  ownerId: string,
  polygon: Vec[],
  zMinMm: number,
  zMaxMm: number,
  bandMm = 0,
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
    halfWidthMm: bandMm,
    zMinMm,
    zMaxMm,
    polygon,
  };
};

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
    // WHY: two boxes that butt exactly should separate on the shared-edge normal (overlap 0), but at
    // arbitrary rotations cos/sin leave a ~±1e-12 residue; a tiny POSITIVE residue skipped the `<= 0`
    // early-exit and returned the large shared-edge overlap → a flush butt read as a collision. Treat
    // a sub-epsilon overlap (< CONTACT_EPS_MM) as separated.
    if (overlap <= CONTACT_EPS_MM) return 0;
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

const polygonsOverlap = (a: Vec[], b: Vec[]): boolean => {
  for (let i = 0; i < a.length; i += 1) {
    const a1 = a[i];
    const a2 = a[(i + 1) % a.length];
    for (let j = 0; j < b.length; j += 1) {
      if (segmentsCross(a1, a2, b[j], b[(j + 1) % b.length])) return true;
    }
  }
  for (const p of a) if (pointInPolygon(p.x, p.y, b)) return true;
  for (const p of b) if (pointInPolygon(p.x, p.y, a)) return true;
  return false;
};

const distPointToSegmentMm = (p: Vec, a: Vec, b: Vec): number => {
  const vx = b.x - a.x;
  const vy = b.y - a.y;
  const lenSq = vx * vx + vy * vy;
  const t =
    lenSq === 0 ? 0 : Math.min(1, Math.max(0, ((p.x - a.x) * vx + (p.y - a.y) * vy) / lenSq));
  return Math.hypot(a.x + t * vx - p.x, a.y + t * vy - p.y);
};

const pointDepthInPolygonMm = (p: Vec, poly: Vec[]): number => {
  if (!pointInPolygon(p.x, p.y, poly)) return 0;
  let min = Infinity;
  for (let i = 0, j = poly.length - 1; i < poly.length; j = i, i += 1) {
    const d = distPointToSegmentMm(p, poly[j], poly[i]);
    if (d < min) min = d;
  }
  return Number.isFinite(min) ? min : 0;
};

// Vertex-containment depth is structurally CAPPED at the contained body's inradius (its
// halfWidth), so the OBB joint tolerance (min halfWidth + 1) would let two equal-width bands
// overlap along their WHOLE length without ever registering. Polygon (arc/curved) bodies
// therefore get a small CONTACT-scale tolerance instead: enough for a snapped joint's grid and
// rounding overlap (grid step 5mm), nothing more.
const POLYGON_CONTACT_TOLERANCE_MM = 6;
const polygonToleranceMm = (a: PlanFootprint, b: PlanFootprint): number =>
  Math.min(jointToleranceMm(a, b), POLYGON_CONTACT_TOLERANCE_MM);

// Approximate polygon-vs-polygon penetration depth via mutual vertex containment (a corner butt /
// flush joint shows up as a vertex a few mm inside the other body — the exact measure the joint
// tolerance exists for). Crossing edges WITHOUT any contained vertex (a plus-sign overlap) is a
// genuinely deep intersection, reported as Infinity.
const polygonPenetrationDepthMm = (a: Vec[], b: Vec[]): number => {
  if (!polygonsOverlap(a, b)) return 0;
  let depth = 0;
  for (const p of a) {
    const d = pointDepthInPolygonMm(p, b);
    if (d > depth) depth = d;
  }
  for (const p of b) {
    const d = pointDepthInPolygonMm(p, a);
    if (d > depth) depth = d;
  }
  return depth > 0 ? depth : Infinity;
};

export const footprintsPenetrate = (a: PlanFootprint, b: PlanFootprint) => {
  if (a.zMaxMm <= b.zMinMm + CONTACT_EPS_MM || b.zMaxMm <= a.zMinMm + CONTACT_EPS_MM) return false;
  if (a.polygon || b.polygon) {
    if (aabbSeparated(a, b)) return false;
    // A contact-scale tolerance — without it an arc body could NEVER corner-join or butt flush
    // (snap engaged, then the zero-tolerance collision slid the body away).
    return (
      polygonPenetrationDepthMm(footprintOutline(a), footprintOutline(b)) > polygonToleranceMm(a, b)
    );
  }
  const extent = obbOverlapExtent(footprintCorners(a), footprintCorners(b));
  return extent > jointToleranceMm(a, b);
};

export const footprintsOverlapXY = (a: PlanFootprint, b: PlanFootprint): boolean => {
  if (aabbSeparated(a, b)) return false;
  if (a.polygon || b.polygon) return polygonsOverlap(footprintOutline(a), footprintOutline(b));
  return obbOverlapExtent(footprintCorners(a), footprintCorners(b)) > CONTACT_EPS_MM;
};

/**
 * The top of the highest support this body OVERLAPS, at any height — deliberate Alt-stacking.
 *
 * WHY there is no "must be below me" guard here, unlike {@link supportTopBelowMm}: holding Alt is
 * the user explicitly asking to climb ONTO something, which is usually taller than where the body
 * currently sits. Gravity must not use this.
 */
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

// Top of the tallest support whose footprint CONTAINS the given plan point (the dragged object's
// centre), else the fallback. Used as the precise "the object is clearly ON TOP of this" test for
// auto-stacking — dragging merely beside/against something does not contain the centre, so it
// stays lateral (the eager "any overlap" trigger was the annoyance).
export const SUPPORT_TOLERANCE_MM = 5;

/**
 * The top of the highest thing this body can actually rest ON — the one resolver gravity uses.
 *
 * Three rules, and every one of them was a bug when it was missing:
 *  - a support ABOVE our base is something we stand BESIDE, not on. Without this a roof overhead,
 *    or a wall standing on the floor we are dragging, was reported as "what I rest on" and the body
 *    teleported up to it.
 *  - a body never supports itself.
 *  - support is decided by plan OVERLAP, not by a single probe point, so "partly over the floor"
 *    already counts — a centre-point probe cannot see a floor the body is only half onto, and the
 *    lateral slide then pins it at the floor's edge forever.
 *
 * Deliberate Alt-stacking is NOT this: there the user is explicitly asking to climb onto something
 * taller, which is what {@link restElevationMm} does.
 */
export const supportTopBelowMm = (
  moved: PlanFootprint,
  supports: PlanFootprint[],
  baseMm: number,
  groundMm = 0,
  toleranceMm = SUPPORT_TOLERANCE_MM,
): number => {
  let top = groundMm;
  for (const s of supports) {
    if (s.ownerId === moved.ownerId) continue;
    if (s.zMaxMm > baseMm + toleranceMm) continue;
    if (s.zMaxMm <= top) continue;
    if (!footprintsOverlapXY(moved, s)) continue;
    top = s.zMaxMm;
  }
  return top;
};

export const restsOnSupportAtMm = (
  moved: PlanFootprint,
  supports: PlanFootprint[],
  baseMm: number,
  toleranceMm: number,
  groundMm = 0,
): boolean => {
  if (Math.abs(baseMm - groundMm) <= toleranceMm) return true;
  for (const s of supports) {
    if (s.ownerId === moved.ownerId) continue;
    // WHY: a support whose top is ABOVE our base is something we stand BESIDE, not on — a floor
    // slab overlaps every wall standing on it in plan, and taking that wall's top would report
    // the floor as "not resting".
    if (s.zMaxMm > baseMm + toleranceMm) continue;
    if (Math.abs(s.zMaxMm - baseMm) > toleranceMm) continue;
    if (footprintsOverlapXY(moved, s)) return true;
  }
  return false;
};

export const isFloating = (
  moved: PlanFootprint,
  supports: PlanFootprint[],
  gapMm: number,
  groundMm = 0,
): boolean => {
  if (moved.zMinMm <= groundMm + gapMm) return false;
  let topBelow = groundMm;
  for (const s of supports) {
    if (s.ownerId === moved.ownerId) continue;
    if (!footprintsOverlapXY(moved, s)) continue;
    if (s.zMaxMm < moved.zMinMm - gapMm) continue;
    const top = Math.min(s.zMaxMm, moved.zMinMm);
    if (top > topBelow) topBelow = top;
  }
  return moved.zMinMm - topBelow > gapMm;
};

export type PlanFootprintSet = PlanFootprint | PlanFootprint[];

export const penetratesAny = (moved: PlanFootprintSet, obstacles: PlanFootprint[]) => {
  const footprints = Array.isArray(moved) ? moved : [moved];
  return footprints.some((footprint) =>
    obstacles.some((o) => o.ownerId !== footprint.ownerId && footprintsPenetrate(footprint, o)),
  );
};

export const firstPenetratingOwner = (
  moved: PlanFootprintSet,
  obstacles: PlanFootprint[],
): string | null => {
  const footprints = Array.isArray(moved) ? moved : [moved];
  for (const footprint of footprints) {
    for (const o of obstacles) {
      if (o.ownerId !== footprint.ownerId && footprintsPenetrate(footprint, o)) return o.ownerId;
    }
  }
  return null;
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

const minObstacleBandMm = (obstacles: PlanFootprint[]): number => {
  let min = Number.POSITIVE_INFINITY;
  for (const o of obstacles) min = Math.min(min, o.halfWidthMm);
  return Number.isFinite(min) ? Math.max(2, min) : 2;
};

const sweptBoundary = (
  footprintAt: (dxMm: number, dyMm: number) => PlanFootprintSet,
  obstacles: PlanFootprint[],
  dxMm: number,
  dyMm: number,
): PlanMoveDelta | null => {
  const pathLen = Math.hypot(dxMm, dyMm);
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

const NO_DEEPEN_EPS_MM = 1;

// True SAT penetration depth = the MIN overlap across separating axes (smallest push to
// separate). obbOverlapExtent above returns the MAX overlap (a separation test), which stays
// constant when a long-thin box slides deeper along its long axis — so it cannot detect
// deepening. This MIN form can.
const obbPenetrationDepth = (ca: Vec[], cb: Vec[]): number => {
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
  let minOverlap = Infinity;
  for (const axis of axes) {
    const overlap = projectOverlap(ca, cb, axis.x, axis.y);
    if (overlap <= 0) return 0;
    if (overlap < minOverlap) minOverlap = overlap;
  }
  return Number.isFinite(minOverlap) ? minOverlap : 0;
};

// Penetration DEPTH (mm) of a into b beyond the contact tolerance, with the same Z-gate
// footprintsPenetrate uses. 0 means clear, touching, or vertically separated. Lets a move be
// judged by how MUCH it overlaps, not just whether it overlaps.
const penetrationDepthMm = (a: PlanFootprint, b: PlanFootprint): number => {
  if (a.zMaxMm <= b.zMinMm + CONTACT_EPS_MM || b.zMaxMm <= a.zMinMm + CONTACT_EPS_MM) return 0;
  if (a.polygon || b.polygon) {
    if (aabbSeparated(a, b)) return 0;
    const depth = polygonPenetrationDepthMm(footprintOutline(a), footprintOutline(b));
    const jointTolerance = polygonToleranceMm(a, b);
    if (depth <= jointTolerance) return 0;
    if (Number.isFinite(depth)) return depth - jointTolerance;
    // Crossing-without-containment: magnitude from the AABB overlap (finite, monotonic in travel).
    const ba = outlineAabb(footprintOutline(a));
    const bb = outlineAabb(footprintOutline(b));
    const ox = Math.min(ba.maxX, bb.maxX) - Math.max(ba.minX, bb.minX);
    const oy = Math.min(ba.maxY, bb.maxY) - Math.max(ba.minY, bb.minY);
    return Math.max(0, Math.min(ox, oy));
  }
  const depth = obbPenetrationDepth(footprintCorners(a), footprintCorners(b));
  const jointTolerance = jointToleranceMm(a, b);
  return depth > jointTolerance ? depth - jointTolerance : 0;
};

const maxDepthAgainst = (footprints: PlanFootprint[], o: PlanFootprint): number => {
  let depth = 0;
  for (const fp of footprints) {
    if (o.ownerId === fp.ownerId) continue;
    const d = penetrationDepthMm(fp, o);
    if (d > depth) depth = d;
  }
  return depth;
};

// Clamp a move so it never INCREASES penetration with any obstacle beyond where it started.
// Sliding free of an existing overlap (depth shrinks) and parallel travel (depth constant) are
// allowed; pushing deeper into something already touched — or into a fresh body — is blocked.
// This is the hard "objects never interpenetrate further" guarantee.
export const clampPlanMoveNoDeepen = (
  footprintAt: (dxMm: number, dyMm: number) => PlanFootprintSet,
  obstacles: PlanFootprint[],
  dxMm: number,
  dyMm: number,
): PlanMoveDelta => {
  if (obstacles.length === 0) return { dxMm, dyMm };
  const reachable = narrowToSweptPath(footprintAt, obstacles, dxMm, dyMm);
  if (reachable.length === 0) return { dxMm, dyMm };
  const toArr = (s: PlanFootprintSet) => (Array.isArray(s) ? s : [s]);
  const startFps = toArr(footprintAt(0, 0));
  const baseline = reachable.map((o) => maxDepthAgainst(startFps, o));
  const ok = (frac: number): boolean => {
    const fps = toArr(footprintAt(dxMm * frac, dyMm * frac));
    for (let i = 0; i < reachable.length; i += 1) {
      if (maxDepthAgainst(fps, reachable[i]) > baseline[i] + NO_DEEPEN_EPS_MM) return false;
    }
    return true;
  };
  if (ok(1)) return { dxMm, dyMm };
  const pathLen = Math.hypot(dxMm, dyMm);
  const steps = Math.min(
    SWEEP_MAX_STEPS,
    Math.max(SWEEP_MIN_STEPS, Math.ceil(pathLen / minObstacleBandMm(reachable))),
  );
  let lastOk = 0;
  for (let i = 1; i <= steps; i += 1) {
    const t = i / steps;
    if (!ok(t)) {
      let lo = lastOk;
      let hi = t;
      for (let k = 0; k < CLAMP_ITERATIONS; k += 1) {
        const mid = (lo + hi) / 2;
        if (ok(mid)) lo = mid;
        else hi = mid;
      }
      return { dxMm: Math.round(dxMm * lo), dyMm: Math.round(dyMm * lo) };
    }
    lastOk = t;
  }
  return { dxMm, dyMm };
};

/**
 * Obstacles the moving body could POSSIBLY touch anywhere along this move.
 *
 * WHY: both sweep loops re-test every obstacle at every sub-step, and the step count is driven by
 * the thinnest obstacle IN THE WHOLE SCENE — so one 25 mm glass run made a 3 m drag cost 120
 * sub-steps even when every body was kilometres away, and the cost grew with how far the user had
 * already dragged (the delta is measured from drag start). Filtering to the swept bounding box
 * first is conservative: a body whose box does not meet the swept box cannot be hit at any point
 * along the path, so nothing that could collide is dropped.
 */
const narrowToSweptPath = (
  footprintAt: (dxMm: number, dyMm: number) => PlanFootprintSet,
  obstacles: PlanFootprint[],
  dxMm: number,
  dyMm: number,
): PlanFootprint[] => {
  if (obstacles.length === 0) return obstacles;
  const toArr = (s: PlanFootprintSet) => (Array.isArray(s) ? s : [s]);
  const boxes = [...toArr(footprintAt(0, 0)), ...toArr(footprintAt(dxMm, dyMm))].map((f) =>
    outlineAabb(footprintOutline(f)),
  );
  let minX = Infinity;
  let minY = Infinity;
  let maxX = -Infinity;
  let maxY = -Infinity;
  for (const b of boxes) {
    if (b.minX < minX) minX = b.minX;
    if (b.minY < minY) minY = b.minY;
    if (b.maxX > maxX) maxX = b.maxX;
    if (b.maxY > maxY) maxY = b.maxY;
  }
  // The joint tolerance is the largest slack any narrow-phase test can allow, so pad by it.
  const pad = JOINT_TOLERANCE_MAX_MM + CONTACT_EPS_MM;
  return obstacles.filter((o) => {
    const b = outlineAabb(footprintOutline(o));
    return !(
      b.maxX < minX - pad ||
      b.minX > maxX + pad ||
      b.maxY < minY - pad ||
      b.minY > maxY + pad
    );
  });
};

export const slidePlanMove = (
  footprintAt: (dxMm: number, dyMm: number) => PlanFootprintSet,
  obstacles: PlanFootprint[],
  dxMm: number,
  dyMm: number,
): PlanMoveDelta => {
  // If the body already overlaps something at the start, exclude only those obstacles
  // so it can slide free of an existing overlap — but KEEP every other obstacle active
  // so it still can't tunnel through a fresh object. (Previously a start-overlap freed
  // the whole move, which let a flush/overlapping body pass straight through others.)
  const reachable = narrowToSweptPath(footprintAt, obstacles, dxMm, dyMm);
  if (reachable.length === 0) return { dxMm, dyMm };
  const startFp = footprintAt(0, 0);
  const active = penetratesAny(startFp, reachable)
    ? reachable.filter((o) => !penetratesAny(startFp, [o]))
    : reachable;
  let result: PlanMoveDelta;
  if (!penetratesAny(footprintAt(dxMm, dyMm), active)) {
    result = sweptBoundary(footprintAt, active, dxMm, dyMm) ?? { dxMm, dyMm };
  } else {
    const alongX = clampPlanMove(footprintAt, active, dxMm, 0);
    const alongY = clampPlanMove(footprintAt, active, 0, dyMm);
    if (!penetratesAny(footprintAt(alongX.dxMm, alongY.dyMm), active)) {
      result = { dxMm: alongX.dxMm, dyMm: alongY.dyMm };
    } else {
      result = clampPlanMove(footprintAt, active, dxMm, dyMm);
    }
  }
  // Final hard gate against the FULL obstacle set: the `active` filter only stops the body
  // being trapped by an overlap it starts inside — this stops it being pushed any DEEPER
  // into that same overlap (slide-out still allowed because depth only shrinks).
  return clampPlanMoveNoDeepen(footprintAt, reachable, result.dxMm, result.dyMm);
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
