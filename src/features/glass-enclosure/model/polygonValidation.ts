export interface Point2D {
  x: number;
  y: number;
}

const segmentsCross = (a1: Point2D, a2: Point2D, b1: Point2D, b2: Point2D): boolean => {
  const dir = (p: Point2D, q: Point2D, r: Point2D) =>
    (q.x - p.x) * (r.y - p.y) - (q.y - p.y) * (r.x - p.x);
  const d1 = dir(b1, b2, a1);
  const d2 = dir(b1, b2, a2);
  const d3 = dir(a1, a2, b1);
  const d4 = dir(a1, a2, b2);
  return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
};

export const polygonSelfIntersects = (pts: Point2D[]): boolean => {
  const n = pts.length;
  if (n < 4) return false;
  for (let i = 0; i < n; i += 1) {
    const a1 = pts[i];
    const a2 = pts[(i + 1) % n];
    for (let j = i + 1; j < n; j += 1) {
      if ((i + 1) % n === j || (j + 1) % n === i) continue;
      if (segmentsCross(a1, a2, pts[j], pts[(j + 1) % n])) return true;
    }
  }
  return false;
};
