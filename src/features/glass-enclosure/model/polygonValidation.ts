export interface Point2D {
  x: number;
  y: number;
}

const orient = (o: Point2D, a: Point2D, b: Point2D) =>
  (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

const onCollinearSegment = (a: Point2D, b: Point2D, p: Point2D) =>
  Math.min(a.x, b.x) <= p.x &&
  p.x <= Math.max(a.x, b.x) &&
  Math.min(a.y, b.y) <= p.y &&
  p.y <= Math.max(a.y, b.y);

// WHY: strict-crossing alone misses a loop that passes exactly through one of its own vertices
// (orientation 0) — that still pinches the contour and breaks earcut; test collinear-touch too.
const segmentsCross = (p1: Point2D, p2: Point2D, p3: Point2D, p4: Point2D): boolean => {
  const d1 = orient(p3, p4, p1);
  const d2 = orient(p3, p4, p2);
  const d3 = orient(p1, p2, p3);
  const d4 = orient(p1, p2, p4);
  if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0))) {
    return true;
  }
  if (d1 === 0 && onCollinearSegment(p3, p4, p1)) return true;
  if (d2 === 0 && onCollinearSegment(p3, p4, p2)) return true;
  if (d3 === 0 && onCollinearSegment(p1, p2, p3)) return true;
  return d4 === 0 && onCollinearSegment(p1, p2, p4);
};

export const polygonSelfIntersects = (pts: Point2D[]): boolean => {
  const n = pts.length;
  if (n < 4) return false;
  for (let i = 0; i < n; i += 1) {
    const a1 = pts[i];
    const a2 = pts[(i + 1) % n];
    for (let j = i + 2; j < n; j += 1) {
      if (i === 0 && j === n - 1) continue;
      if (segmentsCross(a1, a2, pts[j], pts[(j + 1) % n])) return true;
    }
  }
  return false;
};

export const polygonSignedAreaMm2 = (pts: readonly Point2D[]): number => {
  const n = pts.length;
  if (n < 3) return 0;
  let area = 0;
  for (let i = 0; i < n; i += 1) {
    const a = pts[i];
    const b = pts[(i + 1) % n];
    area += a.x * b.y - b.x * a.y;
  }
  return area / 2;
};

export const polygonAreaMm2 = (pts: readonly Point2D[]): number =>
  Math.abs(polygonSignedAreaMm2(pts));

export const polygonAreaM2 = (pts: readonly Point2D[]): number => polygonAreaMm2(pts) / 1_000_000;
