import { BufferGeometry, Float32BufferAttribute } from 'three';

const STEP_RAD = 0.06;
const MIN_COLS = 6;

// Bend a flat slab (length × depth × thickness, one-sided from its origin corner) SYMMETRICALLY into
// a single-curvature arc along the chosen PLAN axis, built directly in the slab's LOCAL frame
// (X = length, Z = depth, Y = thickness up) — so the slab does NOT rotate when curved. The bent
// axis' two ends stay FIXED (its chord = its length); the perpendicular axis stays the one-sided
// radial width; the middle bows by the sagitta. radiusMm is the bent (front) edge radius — with the
// chord-invariant convention chord = 2·radius·sin(sweep/2), so the ends land exactly at the chord.
export const buildCurvedSlabGeometry = (
  lengthMm: number,
  depthMm: number,
  thicknessMm: number,
  radiusMm: number,
  sweepDeg: number,
  axis: 'length' | 'depth',
  direction: 1 | -1,
): BufferGeometry => {
  const alongMm = axis === 'length' ? lengthMm : depthMm;
  const acrossMm = axis === 'length' ? depthMm : lengthMm;
  const along = Math.max(0.001, alongMm / 1000);
  const across = Math.max(0.001, acrossMm / 1000);
  const tM = Math.max(0.001, thicknessMm / 1000);
  const half = Math.min(Math.PI - 1e-3, Math.max(1e-3, (Math.abs(sweepDeg) * Math.PI) / 180 / 2));
  const R = Math.max(0.001, radiusMm / 1000);
  // Centre of the bent-edge arc sits below the chord (along the radial axis); the chord runs along
  // the bent axis from 0..along, symmetric about along/2 (apex), so the slab never rotates.
  const czZ = -R * Math.cos(half);
  const cAlong = along / 2;
  const dirSign = direction < 0 ? -1 : 1;
  const cols = Math.max(MIN_COLS, Math.ceil((2 * half) / STEP_RAD));

  // A bent-edge point: along position a (0..along) at radial offset c (0 = front edge, across = back).
  const planePoint = (a: number, c: number): { pAlong: number; pRadial: number } => {
    const alpha = Math.PI / 2 + half - (a / along) * (2 * half);
    const r = R + c;
    return { pAlong: cAlong + r * Math.cos(alpha), pRadial: (czZ + r * Math.sin(alpha)) * dirSign };
  };
  // Map (along, radial, up) → slab-local three.js position [x, y, z] (Y is up). For axis 'length'
  // the bend runs along X (radial = depth = Z); for 'depth' the bend runs along Z (radial = X).
  const place = (pAlong: number, pRadial: number, up: number): [number, number, number] =>
    axis === 'length' ? [pAlong, up, pRadial] : [pRadial, up, pAlong];

  const positions: number[] = [];
  const indices: number[] = [];
  // 4 corners per column: 0 front-bottom, 1 back-bottom, 2 back-top, 3 front-top.
  for (let i = 0; i <= cols; i += 1) {
    const a = (i / cols) * along;
    const fb = planePoint(a, 0);
    const bb = planePoint(a, across);
    positions.push(...place(fb.pAlong, fb.pRadial, 0));
    positions.push(...place(bb.pAlong, bb.pRadial, 0));
    positions.push(...place(bb.pAlong, bb.pRadial, tM));
    positions.push(...place(fb.pAlong, fb.pRadial, tM));
  }
  const quad = (a: number, b: number, c: number, d: number) => indices.push(a, b, c, a, c, d);
  for (let i = 0; i < cols; i += 1) {
    const s = i * 4;
    const n = (i + 1) * 4;
    quad(s + 3, s + 2, n + 2, n + 3); // top (h = tM)
    quad(s + 1, s + 0, n + 0, n + 1); // bottom (h = 0)
    quad(s + 0, s + 3, n + 3, n + 0); // front edge wall (c = 0)
    quad(s + 2, s + 1, n + 1, n + 2); // back edge wall (c = across)
  }
  quad(0, 1, 2, 3); // start end cap
  const e = cols * 4;
  quad(e + 3, e + 2, e + 1, e + 0); // end cap (reversed)

  const geometry = new BufferGeometry();
  geometry.setAttribute('position', new Float32BufferAttribute(positions, 3));
  geometry.setIndex(indices);
  geometry.computeVertexNormals();
  return geometry;
};
