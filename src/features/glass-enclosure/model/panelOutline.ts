import type { PanelShapeKind, PanelTopShape } from './project.types';

export interface PanelOutlineSpec {
  widthMm: number;
  heightMm: number;
  topShape?: PanelTopShape | null;
  topRightHeightMm?: number | null;
  archRiseMm?: number | null;
  shapeKind?: PanelShapeKind | null;
  points?: PanelPoint[] | null;
}

export interface PanelPoint {
  x: number;
  y: number;
}

const ARCH_SEGMENTS = 16;
const ELLIPSE_SEGMENTS = 48;

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
