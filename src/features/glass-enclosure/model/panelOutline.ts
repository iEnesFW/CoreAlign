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
    return spec.points;
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
