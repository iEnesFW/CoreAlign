import { BufferGeometry, Float32BufferAttribute } from 'three';

const STEP_RAD = 0.06;
const MIN_COLS = 6;

// The slab plan-arc uses the SAME sign contract as runs/walls (arcFromBow/bowFromArc): a POSITIVE
// sagitta — toward the chord's CCW perpendicular ("+across", what the green handle reports) — is
// stored as a NEGATIVE sweep. This maps the stored sweep sign to the mesh's radial mirror per axis:
// for 'length' the radial is local +Z (= +across), for 'depth' it is local +X (= −across), so the
// two axes need OPPOSITE mirrors for the bulge to land on the dragged side.
export const slabArcDirSign = (axis: 'length' | 'depth', sweepDeg: number): 1 | -1 => {
  const sweepNegative = sweepDeg < 0;
  if (axis === 'length') return sweepNegative ? 1 : -1;
  return sweepNegative ? -1 : 1;
};

// The bow side a fresh inspector-entered curve should default to: the slab's own body side (+Z for
// 'length', +X for 'depth'), expressed as the stored sweep sign under the contract above.
export const slabArcDefaultSweepSign = (axis: 'length' | 'depth'): 1 | -1 =>
  axis === 'length' ? -1 : 1;

interface SlabPlanPointMm {
  x: number;
  z: number;
}

// The DEVELOPED (s, c) frame of a plan-curved slab: s = arc-length along the bend measured at the
// MID-BAND reference radius (features keep their physical size in the middle of the band; the
// inner edge is slightly shorter, the outer slightly longer — unavoidable on a fan), c = radial
// offset from the front edge ∈ [0, across]. The pen/draw tools, feature outlines and fit checks
// all work in (s, c); the forward/inverse maps below convert to/from the slab's LOCAL plan frame.
export interface CurvedSlabFrame {
  axis: 'length' | 'depth';
  alongMm: number;
  acrossMm: number;
  halfRad: number;
  radiusMm: number;
  centerRadialMm: number;
  centerAlongMm: number;
  dirSign: 1 | -1;
  developedMm: number;
}

export const curvedSlabFrame = (
  lengthMm: number,
  depthMm: number,
  radiusMm: number,
  sweepDeg: number,
  axis: 'length' | 'depth',
): CurvedSlabFrame => {
  const alongMm = Math.max(1, axis === 'length' ? lengthMm : depthMm);
  const acrossMm = Math.max(1, axis === 'length' ? depthMm : lengthMm);
  const halfRad = Math.min(
    Math.PI - 1e-3,
    Math.max(1e-3, (Math.abs(sweepDeg) * Math.PI) / 180 / 2),
  );
  const R = Math.max(1, radiusMm);
  return {
    axis,
    alongMm,
    acrossMm,
    halfRad,
    radiusMm: R,
    centerRadialMm: -R * Math.cos(halfRad),
    centerAlongMm: alongMm / 2,
    dirSign: slabArcDirSign(axis, sweepDeg),
    developedMm: (R + acrossMm / 2) * 2 * halfRad,
  };
};

// FORWARD: developed (s, c) → LOCAL plan (x, z). Same circle math as curvedSlabPlanColumnsMm.
export const curvedSlabPointAt = (
  frame: CurvedSlabFrame,
  sMm: number,
  cMm: number,
): SlabPlanPointMm => {
  const theta = Math.min(1, Math.max(0, sMm / frame.developedMm)) * 2 * frame.halfRad;
  const alpha = Math.PI / 2 + frame.halfRad - theta;
  const r = frame.radiusMm + cMm;
  const pAlong = frame.centerAlongMm + r * Math.cos(alpha);
  const pRadial = (frame.centerRadialMm + r * Math.sin(alpha)) * frame.dirSign;
  return frame.axis === 'length' ? { x: pAlong, z: pRadial } : { x: pRadial, z: pAlong };
};

// INVERSE: LOCAL plan (x, z) → developed (s, c). Exact inverse of curvedSlabPointAt.
export const curvedSlabPickSc = (
  frame: CurvedSlabFrame,
  xMm: number,
  zMm: number,
): { s: number; c: number } => {
  const pAlong = frame.axis === 'length' ? xMm : zMm;
  const pRadial = (frame.axis === 'length' ? zMm : xMm) * frame.dirSign;
  const dx = pAlong - frame.centerAlongMm;
  const dy = pRadial - frame.centerRadialMm;
  const r = Math.hypot(dx, dy);
  const alpha = Math.atan2(dy, dx);
  const theta = Math.PI / 2 + frame.halfRad - alpha;
  const s = (theta / (2 * frame.halfRad)) * frame.developedMm;
  return { s, c: r - frame.radiusMm };
};

// Map a feature outline from (s, c) into the LOCAL plan frame, densifying long edges so straight
// outline edges follow the bend instead of cutting chords through it.
export const curvedSlabMapOutlineMm = (
  frame: CurvedSlabFrame,
  outline: { x: number; z: number }[],
): SlabPlanPointMm[] => {
  const maxSegMm = Math.max(5, (STEP_RAD / (2 * frame.halfRad)) * frame.developedMm);
  const mapped: SlabPlanPointMm[] = [];
  for (let i = 0; i < outline.length; i += 1) {
    const p = outline[i];
    const q = outline[(i + 1) % outline.length];
    mapped.push(curvedSlabPointAt(frame, p.x, p.z));
    const segments = Math.ceil(Math.abs(q.x - p.x) / maxSegMm);
    for (let k = 1; k < segments; k += 1) {
      const t = k / segments;
      mapped.push(curvedSlabPointAt(frame, p.x + (q.x - p.x) * t, p.z + (q.z - p.z) * t));
    }
  }
  return mapped;
};

// Shared LOCAL-frame plan sampling (mm) of the bent band: per-column front (c=0) and back
// (c=across) edge points along the sweep. The mesh, the collision footprint and the snap targets
// all derive from THIS so they can never disagree about where the curved body is. The bent axis'
// chord runs 0..along symmetric about along/2; the centre sits below the chord.
export const curvedSlabPlanColumnsMm = (
  lengthMm: number,
  depthMm: number,
  radiusMm: number,
  sweepDeg: number,
  axis: 'length' | 'depth',
): { front: SlabPlanPointMm; back: SlabPlanPointMm }[] => {
  const along = Math.max(1, axis === 'length' ? lengthMm : depthMm);
  const across = Math.max(1, axis === 'length' ? depthMm : lengthMm);
  const half = Math.min(Math.PI - 1e-3, Math.max(1e-3, (Math.abs(sweepDeg) * Math.PI) / 180 / 2));
  const R = Math.max(1, radiusMm);
  const czZ = -R * Math.cos(half);
  const cAlong = along / 2;
  const dirSign = slabArcDirSign(axis, sweepDeg);
  const cols = Math.max(MIN_COLS, Math.ceil((2 * half) / STEP_RAD));
  const point = (a: number, c: number): SlabPlanPointMm => {
    const alpha = Math.PI / 2 + half - (a / along) * (2 * half);
    const r = R + c;
    const pAlong = cAlong + r * Math.cos(alpha);
    const pRadial = (czZ + r * Math.sin(alpha)) * dirSign;
    return axis === 'length' ? { x: pAlong, z: pRadial } : { x: pRadial, z: pAlong };
  };
  const columns: { front: SlabPlanPointMm; back: SlabPlanPointMm }[] = [];
  for (let i = 0; i <= cols; i += 1) {
    const a = (i / cols) * along;
    columns.push({ front: point(a, 0), back: point(a, across) });
  }
  return columns;
};

// Closed LOCAL-frame plan outline (mm) of the bent band: front edge forward + back edge reversed.
export const curvedSlabPlanOutlineMm = (
  lengthMm: number,
  depthMm: number,
  radiusMm: number,
  sweepDeg: number,
  axis: 'length' | 'depth',
): SlabPlanPointMm[] => {
  const columns = curvedSlabPlanColumnsMm(lengthMm, depthMm, radiusMm, sweepDeg, axis);
  return [...columns.map((c) => c.front), ...columns.map((c) => c.back).reverse()];
};

// Bend a flat slab (length × depth × thickness, one-sided from its origin corner) SYMMETRICALLY into
// a single-curvature arc along the chosen PLAN axis, built directly in the slab's LOCAL frame
// (X = length, Z = depth, Y = thickness up) — so the slab does NOT rotate when curved. The bent
// axis' two ends stay FIXED (its chord = its length); the perpendicular axis stays the one-sided
// radial width; the middle bows by the sagitta. radiusMm is the bent (front) edge radius — with the
// chord-invariant convention chord = 2·radius·sin(sweep/2), so the ends land exactly at the chord.
// The bulge side comes from the sweep SIGN via slabArcDirSign (canonical +sagitta → negative sweep).
export const buildCurvedSlabGeometry = (
  lengthMm: number,
  depthMm: number,
  thicknessMm: number,
  radiusMm: number,
  sweepDeg: number,
  axis: 'length' | 'depth',
): BufferGeometry => {
  const tM = Math.max(0.001, thicknessMm / 1000);
  const columns = curvedSlabPlanColumnsMm(lengthMm, depthMm, radiusMm, sweepDeg, axis);

  const positions: number[] = [];
  const indices: number[] = [];
  // 4 corners per column: 0 front-bottom, 1 back-bottom, 2 back-top, 3 front-top.
  for (const col of columns) {
    positions.push(col.front.x / 1000, 0, col.front.z / 1000);
    positions.push(col.back.x / 1000, 0, col.back.z / 1000);
    positions.push(col.back.x / 1000, tM, col.back.z / 1000);
    positions.push(col.front.x / 1000, tM, col.front.z / 1000);
  }
  const cols = columns.length - 1;
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
