export interface BarrelPoint {
  x: number;
  y: number;
}

// Symmetric circular barrel profile across the span [0, lengthMm]: y = 0 at both
// eaves, peak y = riseMm at the centre. Returns segments+1 points left→right (mm).
export const barrelArcProfilePoints = (
  lengthMm: number,
  riseMm: number,
  segments = 24,
): BarrelPoint[] => {
  const length = Math.max(1, lengthMm);
  const rise = Math.max(0, riseMm);
  if (rise <= 0) {
    return [
      { x: 0, y: 0 },
      { x: length, y: 0 },
    ];
  }
  // WHY: circular segment radius from chord (length) + sagitta (rise).
  const radius = rise / 2 + (length * length) / (8 * rise);
  const cx = length / 2;
  const cy = rise - radius;
  const n = 2 * Math.max(1, Math.round(segments / 2)); // even → a sample lands on the ridge
  const points: BarrelPoint[] = [];
  for (let i = 0; i <= n; i += 1) {
    const x = (length * i) / n;
    const dx = x - cx;
    const y = cy + Math.sqrt(Math.max(0, radius * radius - dx * dx));
    points.push({ x, y: Math.max(0, y) });
  }
  return points;
};
