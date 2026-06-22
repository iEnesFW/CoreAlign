import type { PanelPoint } from './panelOutline';

// Parse a free-polygon panel outline from its persisted JSON.
// Points are panel-local, bottom-centred, y-up: x in [-w/2, w/2], y in [0, h] (mm).
// Returns null for anything that is not a usable polygon (≥ 3 finite points).
export const parsePanelPolygonPoints = (json?: string | null): PanelPoint[] | null => {
  if (!json) return null;
  try {
    const raw = JSON.parse(json);
    if (!Array.isArray(raw)) return null;
    const pts = raw
      .filter(
        (p): p is PanelPoint =>
          Boolean(p) &&
          typeof p.x === 'number' &&
          typeof p.y === 'number' &&
          Number.isFinite(p.x) &&
          Number.isFinite(p.y),
      )
      .map((p) => ({ x: p.x, y: p.y }));
    return pts.length >= 3 ? pts : null;
  } catch {
    return null;
  }
};

export const serializePanelPolygonPoints = (points: PanelPoint[]): string =>
  JSON.stringify(points.map((p) => ({ x: Math.round(p.x), y: Math.round(p.y) })));

// Shoelace area (always positive) for the net glass silhouette.
export const panelPolygonAreaMm2 = (points: PanelPoint[]): number => {
  let sum = 0;
  for (let i = 0; i < points.length; i += 1) {
    const a = points[i];
    const b = points[(i + 1) % points.length];
    sum += a.x * b.y - b.x * a.y;
  }
  return Math.abs(sum) / 2;
};

// A regular n-gon inscribed in the panel's width × height box, panel-local
// (bottom-centred, y-up), first vertex at top-centre. sides >= 3.
export const presetPolygonPoints = (
  sides: number,
  widthMm: number,
  heightMm: number,
): PanelPoint[] => {
  const n = Math.max(3, Math.round(sides));
  const rx = widthMm / 2;
  const ry = heightMm / 2;
  const cy = ry;
  const points: PanelPoint[] = [];
  for (let i = 0; i < n; i += 1) {
    const angle = Math.PI / 2 + (i / n) * Math.PI * 2;
    points.push({ x: Math.round(rx * Math.cos(angle)), y: Math.round(cy + ry * Math.sin(angle)) });
  }
  return points;
};
