export interface ArcPoint {
  x: number;
  y: number;
}

const MAX_ARC_SEGMENTS = 96;
const MIN_BULGE_MM = 10;
// 20mm chords keep a shift-drawn arc visually smooth after the cutter sweep (50mm read as facets).
const SEGMENT_ARC_LEN_MM = 20;

export const chordBulgeMm = (a: ArcPoint, b: ArcPoint, m: ArcPoint): number => {
  const dx = b.x - a.x;
  const dy = b.y - a.y;
  const len = Math.hypot(dx, dy);
  if (len < 1e-6) return 0;
  return ((m.x - a.x) * -dy + (m.y - a.y) * dx) / len;
};

// Radius + swept angle of the arc the pen would draw, for a live readout. Mirrors the
// chord→radius math in tessellateArc; a sagitta past the radius means the major (reflex) arc.
export const arcMetricsFromBulge = (
  a: ArcPoint,
  b: ArcPoint,
  bulgeMm: number,
): { radiusMm: number; angleDeg: number } => {
  const chord = Math.hypot(b.x - a.x, b.y - a.y);
  const s = Math.abs(bulgeMm);
  if (chord < 1e-6 || s < MIN_BULGE_MM) return { radiusMm: 0, angleDeg: 0 };
  const radius = (chord * chord) / 4 / s / 2 + s / 2;
  const half = Math.asin(Math.min(1, chord / 2 / radius));
  const angleRad = s > radius ? 2 * (Math.PI - half) : 2 * half;
  return { radiusMm: radius, angleDeg: (angleRad * 180) / Math.PI };
};

export const tessellateArc = (a: ArcPoint, b: ArcPoint, bulgeMm: number): ArcPoint[] => {
  const dx = b.x - a.x;
  const dy = b.y - a.y;
  const chord = Math.hypot(dx, dy);
  if (chord < 1e-6 || Math.abs(bulgeMm) < MIN_BULGE_MM) return [b];
  const s = bulgeMm;
  const radius = (chord * chord) / 4 / Math.abs(s) / 2 + Math.abs(s) / 2;
  const tx = dx / chord;
  const ty = dy / chord;
  const nx = -ty * Math.sign(s);
  const ny = tx * Math.sign(s);
  const midX = (a.x + b.x) / 2;
  const midY = (a.y + b.y) / 2;
  const cx = midX - nx * (radius - Math.abs(s));
  const cy = midY - ny * (radius - Math.abs(s));
  const a0 = Math.atan2(a.y - cy, a.x - cx);
  const wrap = (d: number): number => {
    let v = d;
    while (v <= -Math.PI) v += 2 * Math.PI;
    while (v > Math.PI) v -= 2 * Math.PI;
    return v;
  };
  const apexAngle = Math.atan2(ny, nx);
  const delta = wrap(apexAngle - a0) + wrap(Math.atan2(b.y - cy, b.x - cx) - apexAngle);
  const arcLen = Math.abs(delta) * radius;
  const segments = Math.max(2, Math.min(MAX_ARC_SEGMENTS, Math.ceil(arcLen / SEGMENT_ARC_LEN_MM)));
  const pts: ArcPoint[] = [];
  for (let i = 1; i <= segments; i += 1) {
    const ang = a0 + (delta * i) / segments;
    pts.push({ x: cx + radius * Math.cos(ang), y: cy + radius * Math.sin(ang) });
  }
  return pts;
};
