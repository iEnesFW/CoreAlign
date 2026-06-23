import type { CornerRadiiMm, PanelShapeKind, PanelTopShape } from './project.types';

export interface PanelOutlineSpec {
  widthMm: number;
  heightMm: number;
  topShape?: PanelTopShape | null;
  topRightHeightMm?: number | null;
  archRiseMm?: number | null;
  shapeKind?: PanelShapeKind | null;
  points?: PanelPoint[] | null;
  cornerRadiiMm?: CornerRadiiMm | null;
}

export interface PanelPoint {
  x: number;
  y: number;
}

const ARCH_SEGMENTS = 16;
const ELLIPSE_SEGMENTS = 48;
const CORNER_ARC_SEGMENTS = 10;
const MIN_FILLET_MM = 0.5;

const hasCornerRadii = (r?: CornerRadiiMm | null): boolean =>
  Boolean(r && ((r.tl ?? 0) > 0 || (r.tr ?? 0) > 0 || (r.bl ?? 0) > 0 || (r.br ?? 0) > 0));

// Round each corner of a quad to match the glass silhouette, sampling the same
// pIn → corner → pOut quadratic that filletedShapeMm draws so the glass face and the
// wrapping frame band (both consume this outline) hug an identical curve.
const filletedOutlinePoints = (corners: PanelPoint[], radiiMm: number[]): PanelPoint[] => {
  const n = corners.length;
  const out: PanelPoint[] = [];
  for (let i = 0; i < n; i += 1) {
    const p = corners[i];
    const prev = corners[(i + n - 1) % n];
    const next = corners[(i + 1) % n];
    const inLen = Math.hypot(p.x - prev.x, p.y - prev.y);
    const outLen = Math.hypot(next.x - p.x, next.y - p.y);
    const rad = Math.min(
      Math.max(0, radiiMm[i] ?? 0),
      Math.max(0, inLen / 2 - MIN_FILLET_MM),
      Math.max(0, outLen / 2 - MIN_FILLET_MM),
    );
    if (rad <= MIN_FILLET_MM || inLen === 0 || outLen === 0) {
      out.push({ x: p.x, y: p.y });
      continue;
    }
    const inX = (p.x - prev.x) / inLen;
    const inY = (p.y - prev.y) / inLen;
    const outX = (next.x - p.x) / outLen;
    const outY = (next.y - p.y) / outLen;
    const pInX = p.x - inX * rad;
    const pInY = p.y - inY * rad;
    const pOutX = p.x + outX * rad;
    const pOutY = p.y + outY * rad;
    for (let s = 0; s <= CORNER_ARC_SEGMENTS; s += 1) {
      const t = s / CORNER_ARC_SEGMENTS;
      const mt = 1 - t;
      out.push({
        x: mt * mt * pInX + 2 * mt * t * p.x + t * t * pOutX,
        y: mt * mt * pInY + 2 * mt * t * p.y + t * t * pOutY,
      });
    }
  }
  return out;
};

export const panelOutlinePointsMm = (spec: PanelOutlineSpec): PanelPoint[] => {
  const w = Math.max(1, spec.widthMm);
  const x0 = -w / 2;
  const x1 = w / 2;

  if (spec.shapeKind === 'polygon' && spec.points && spec.points.length >= 3) {
    // Scale the authored polygon so its bounding box fills the panel cell (w × h).
    // This makes the glass track panel resizes and fill the cell — no stale gap when
    // the panel is widened/narrowed after the shape was drawn.
    const pts = spec.points;
    let minX = Infinity;
    let maxX = -Infinity;
    let minY = Infinity;
    let maxY = -Infinity;
    for (const p of pts) {
      if (p.x < minX) minX = p.x;
      if (p.x > maxX) maxX = p.x;
      if (p.y < minY) minY = p.y;
      if (p.y > maxY) maxY = p.y;
    }
    const bw = maxX - minX;
    const bh = maxY - minY;
    if (bw <= 0 || bh <= 0) return pts;
    const hM = Math.max(1, spec.heightMm);
    const sx = w / bw;
    const sy = hM / bh;
    const cxp = (minX + maxX) / 2;
    return pts.map((p) => ({ x: (p.x - cxp) * sx, y: (p.y - minY) * sy }));
  }

  if (spec.shapeKind === 'ellipse') {
    const rx = w / 2;
    const ry = Math.max(1, spec.heightMm) / 2;
    const pts: PanelPoint[] = [];
    for (let i = 0; i < ELLIPSE_SEGMENTS; i += 1) {
      const a = (i / ELLIPSE_SEGMENTS) * Math.PI * 2;
      pts.push({ x: rx * Math.cos(a), y: ry + ry * Math.sin(a) });
    }
    return pts;
  }
  const hL = Math.max(1, spec.heightMm);
  const shape = spec.topShape ?? 'flat';
  const hR = shape === 'flat' ? hL : Math.max(1, spec.topRightHeightMm ?? hL);

  const pts: PanelPoint[] = [
    { x: x0, y: 0 },
    { x: x1, y: 0 },
    { x: x1, y: hR },
  ];

  if (shape === 'arched') {
    const rise = Math.max(0, spec.archRiseMm ?? 0);
    if (rise > 0) {
      for (let i = 1; i < ARCH_SEGMENTS; i += 1) {
        const t = i / ARCH_SEGMENTS;
        const x = x1 + (x0 - x1) * t;
        const headY = hR + (hL - hR) * t;
        const y = headY + rise * Math.sin(Math.PI * t);
        pts.push({ x, y });
      }
    }
  }

  pts.push({ x: x0, y: hL });

  // Round the corners of a plain (flat / raked) quad to match the rounded glass; the arch
  // curve owns the top edge so it is left sharp at the springline. pts order is bl, br, tr, tl.
  if (shape !== 'arched' && pts.length === 4 && hasCornerRadii(spec.cornerRadiiMm)) {
    const r = spec.cornerRadiiMm ?? {};
    return filletedOutlinePoints(pts, [r.bl ?? 0, r.br ?? 0, r.tr ?? 0, r.tl ?? 0]);
  }
  return pts;
};

export const panelIsShaped = (spec: {
  topShape?: PanelTopShape | null;
  archRiseMm?: number | null;
  cornerRadiiMm?: { tl?: number; tr?: number; bl?: number; br?: number } | null;
  shapeKind?: PanelShapeKind | null;
}): boolean => {
  if (spec.shapeKind) return true;
  const shape = spec.topShape ?? 'flat';
  if (shape === 'raked') return true;
  if (shape === 'arched' && (spec.archRiseMm ?? 0) > 0) return true;
  const r = spec.cornerRadiiMm;
  return Boolean(r && (r.tl || r.tr || r.bl || r.br));
};
