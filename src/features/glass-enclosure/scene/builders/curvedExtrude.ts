import { BufferGeometry, ExtrudeGeometry, Float32BufferAttribute, Shape } from 'three';

const CURVE_STEP_RAD = 0.08;

// WHY: spine passes through the local origin at phi=0; callers lay it flat with
// rotation [-π/2,0,0] so extrude depth becomes world-up height (shared glass + wall body).
export const buildCurvedBandGeometry = (
  radiusM: number,
  direction: 1 | -1,
  phiStart: number,
  phiEnd: number,
  thicknessM: number,
  depthM: number,
): ExtrudeGeometry => {
  const radius = Math.max(0.001, Number.isFinite(radiusM) ? radiusM : 0.001);
  const span = Math.max(1e-4, phiEnd - phiStart);
  const endPhi = phiStart + span;
  const centerY = -direction * radius;
  const outer = radius + thicknessM / 2;
  const inner = Math.max(0.001, radius - thicknessM / 2);
  const toAngle = (phi: number) => (direction === 1 ? Math.PI / 2 - phi : phi - Math.PI / 2);
  const outerClockwise = direction === 1;
  const shape = new Shape();
  shape.absarc(0, centerY, outer, toAngle(phiStart), toAngle(endPhi), outerClockwise);
  shape.absarc(0, centerY, inner, toAngle(endPhi), toAngle(phiStart), !outerClockwise);
  shape.closePath();
  const curveSegments = Math.max(8, Math.ceil(span / CURVE_STEP_RAD));
  return new ExtrudeGeometry(shape, { depth: depthM, bevelEnabled: false, curveSegments });
};

export interface OutlinePointMm {
  x: number;
  y: number;
}

const SHAPED_COL_STEP_RAD = 0.05;

// Vertical [low, high] extent of a (simple) silhouette polygon at a given x, from the y-values
// where its edges cross the vertical line. Convex panel shapes cross exactly twice; null if the
// line misses the polygon. Width-spanning shapes (the panel fills its cell) cross across x.
const spanAtX = (outline: OutlinePointMm[], x: number): [number, number] | null => {
  const ys: number[] = [];
  for (let i = 0; i < outline.length; i += 1) {
    const p1 = outline[i];
    const p2 = outline[(i + 1) % outline.length];
    const d1 = p1.x - x;
    const d2 = p2.x - x;
    if ((d1 <= 0 && d2 > 0) || (d2 <= 0 && d1 > 0)) {
      const t = (x - p1.x) / (p2.x - p1.x);
      ys.push(p1.y + t * (p2.y - p1.y));
    }
  }
  if (ys.length < 2) return null;
  return [Math.min(...ys), Math.max(...ys)];
};

// Build a SHAPED glass pane that follows an arc, sampled directly into cylindrical coordinates
// so the curve is smooth (no faceting). Same local frame as buildCurvedBandGeometry: x→angle
// across [phiStart,phiEnd], height→the extrude/Z axis the caller's [-π/2,0,0] mesh lifts up,
// thickness→radial. The silhouette (panel-local mm, x∈[-w/2,w/2], y∈[0,h]) is tessellated along
// the arc into front/back faces + a rim, so a triangle/oval/polygon keeps its outline on a curve.
export const buildCurvedShapedGeometry = (
  outlineMm: OutlinePointMm[],
  widthMm: number,
  radiusM: number,
  direction: 1 | -1,
  phiStart: number,
  phiEnd: number,
  thicknessM: number,
): BufferGeometry => {
  const radius = Math.max(0.001, Number.isFinite(radiusM) ? radiusM : 0.001);
  const span = Math.max(1e-4, phiEnd - phiStart);
  const centerY = -direction * radius;
  const w = Math.max(1, widthMm);
  const outerR = radius + thicknessM / 2;
  const innerR = Math.max(0.001, radius - thicknessM / 2);
  const toAngle = (phi: number) => (direction === 1 ? Math.PI / 2 - phi : phi - Math.PI / 2);
  const cyl = (xMm: number, yMm: number, r: number): [number, number, number] => {
    const phi = phiStart + ((xMm + w / 2) / w) * span;
    const a = toAngle(phi);
    return [Math.cos(a) * r, centerY + Math.sin(a) * r, yMm / 1000];
  };

  let minX = Infinity;
  let maxX = -Infinity;
  for (const p of outlineMm) {
    if (p.x < minX) minX = p.x;
    if (p.x > maxX) maxX = p.x;
  }
  const cols = Math.max(24, Math.ceil(span / SHAPED_COL_STEP_RAD));
  const columns: { x: number; lo: number; hi: number }[] = [];
  // WHY: spanAtX is half-open (needs an endpoint strictly > x), so sampling exactly at minX/maxX
  // misses the edge crossing and drops that column — truncating the pane's leading/trailing edge.
  // Nudge the extreme samples just inside the silhouette so both edge columns register.
  const edgeEps = (maxX - minX) * 1e-4 + 1e-3;
  for (let i = 0; i <= cols; i += 1) {
    let x = minX + ((maxX - minX) * i) / cols;
    if (i === 0) x = minX + edgeEps;
    else if (i === cols) x = maxX - edgeEps;
    const s = spanAtX(outlineMm, x);
    if (s && s[1] - s[0] > 0.5) columns.push({ x, lo: s[0], hi: s[1] });
  }

  const pos: number[] = [];
  const tri = (a: number[], b: number[], c: number[]) => pos.push(...a, ...b, ...c);
  const quad = (a: number[], b: number[], c: number[], d: number[]) => {
    tri(a, b, c);
    tri(a, c, d);
  };
  for (let i = 0; i < columns.length - 1; i += 1) {
    const c0 = columns[i];
    const c1 = columns[i + 1];
    const fb0 = cyl(c0.x, c0.lo, outerR);
    const ft0 = cyl(c0.x, c0.hi, outerR);
    const fb1 = cyl(c1.x, c1.lo, outerR);
    const ft1 = cyl(c1.x, c1.hi, outerR);
    const bb0 = cyl(c0.x, c0.lo, innerR);
    const bt0 = cyl(c0.x, c0.hi, innerR);
    const bb1 = cyl(c1.x, c1.lo, innerR);
    const bt1 = cyl(c1.x, c1.hi, innerR);
    quad(fb0, fb1, ft1, ft0); // front face
    quad(bb1, bb0, bt0, bt1); // back face
    quad(ft0, ft1, bt1, bt0); // top rim (follows the silhouette top)
    quad(bb0, bb1, fb1, fb0); // bottom rim
  }
  if (columns.length > 0) {
    const first = columns[0];
    quad(
      cyl(first.x, first.lo, outerR),
      cyl(first.x, first.hi, outerR),
      cyl(first.x, first.hi, innerR),
      cyl(first.x, first.lo, innerR),
    );
    const last = columns[columns.length - 1];
    quad(
      cyl(last.x, last.lo, innerR),
      cyl(last.x, last.hi, innerR),
      cyl(last.x, last.hi, outerR),
      cyl(last.x, last.lo, outerR),
    );
  }

  const geometry = new BufferGeometry();
  geometry.setAttribute('position', new Float32BufferAttribute(pos, 3));
  geometry.computeVertexNormals();
  return geometry;
};

// Invert the curved-wall surface: a pick point in the wall GROUP's local frame (after worldToLocal,
// i.e. the band mesh's parent frame — the band is the pre-rotation cyl() solid rotated [-π/2,0,0],
// which maps band (x,y,z) → group-local (x, z, -y)) back to (offset along the developed wall,
// height). Lets the draw/pen tools place a feature where the cursor hits a curved wall, instead of
// the flat-box projection (which lands it at the wrong offset). Mirror of buildCurvedBandGeometry.
export const curvedWallPickUv = (
  localX: number,
  localY: number,
  localZ: number,
  radiusM: number,
  direction: 1 | -1,
  sweep: number,
  lengthMm: number,
): { u: number; v: number } => {
  const centerY = -direction * radiusM;
  const a = Math.atan2(-(localZ + centerY), localX);
  const phi = direction === 1 ? Math.PI / 2 - a : a + Math.PI / 2;
  const span = Math.max(1e-4, sweep);
  return { u: (phi / span) * lengthMm, v: localY * 1000 };
};

// FORWARD map (inverse of curvedWallPickUv): a face point (u = offset along the developed arc, v =
// height, both mm) → the GROUP-LOCAL 3D point on the curved band at point-radius `surfaceRM`. Mirrors
// buildCurvedWallFeatureSolid's cyl() (geometry frame: arc in XY centred at (0,centerY), height Z)
// then the band mesh's [-π/2,0,0] pitch, which maps geometry (x,y,z) → group-local (x, z, -y). Used
// to draw the pen preview ON the curved surface instead of the flat chord plane.
export const curvedWallSurfacePoint = (
  uMm: number,
  vMm: number,
  bandRadiusM: number,
  surfaceRM: number,
  direction: 1 | -1,
  sweep: number,
  lengthMm: number,
): [number, number, number] => {
  const span = Math.max(1e-4, sweep);
  const len = Math.max(1, lengthMm);
  const centerY = -direction * Math.max(0.001, bandRadiusM);
  const phi = Math.max(0, Math.min(span, (uMm / len) * span));
  const a = direction === 1 ? Math.PI / 2 - phi : phi - Math.PI / 2;
  const gx = Math.cos(a) * surfaceRM;
  const gy = centerY + Math.sin(a) * surfaceRM;
  return [gx, vMm / 1000, -gy];
};

export interface CurvedWallFeaturePoint {
  x: number; // offset ALONG the wall, mm (0 → wall.lengthMm)
  z: number; // height, mm
}

// A closed solid that follows the feature's outline along a curved wall, occupying the radial
// band [rNearM, rFarM]. Built in the SAME pre-rotation frame as buildCurvedBandGeometry (arc in
// XY, height along Z) so it can be CSG-subtracted from / unioned with the curved wall body, or
// rendered as a thin on-surface proxy for selection. The outline x maps along the arc (0 → sweep);
// z is the height. Used for holes (through), recesses (partial radial), protrusions (outward) and
// the selectable surface decal on a curved wall.
export const buildCurvedWallFeatureSolid = (
  outlineMm: CurvedWallFeaturePoint[],
  lengthMm: number,
  radiusM: number,
  direction: 1 | -1,
  sweep: number,
  rNearM: number,
  rFarM: number,
): BufferGeometry => {
  const radius = Math.max(0.001, Number.isFinite(radiusM) ? radiusM : 0.001);
  const span = Math.max(1e-4, sweep);
  const centerY = -direction * radius;
  const len = Math.max(1, lengthMm);
  const rIn = Math.max(0.0005, Math.min(rNearM, rFarM));
  const rOut = Math.max(rIn + 1e-4, Math.max(rNearM, rFarM));
  const toAngle = (phi: number) => (direction === 1 ? Math.PI / 2 - phi : phi - Math.PI / 2);
  const cyl = (xMm: number, zMm: number, r: number): [number, number, number] => {
    const phi = Math.max(0, Math.min(span, (xMm / len) * span));
    const a = toAngle(phi);
    return [Math.cos(a) * r, centerY + Math.sin(a) * r, zMm / 1000];
  };

  let minX = Infinity;
  let maxX = -Infinity;
  for (const p of outlineMm) {
    if (p.x < minX) minX = p.x;
    if (p.x > maxX) maxX = p.x;
  }
  if (!Number.isFinite(minX) || maxX - minX < 0.5) return new BufferGeometry();
  // Column density tracks the arc length the feature spans, so a wider feature stays smooth.
  const featureSpanRad = ((maxX - minX) / len) * span;
  const cols = Math.max(8, Math.ceil(featureSpanRad / SHAPED_COL_STEP_RAD));
  const ySpanAt = (x: number) =>
    spanAtX(
      outlineMm.map((p) => ({ x: p.x, y: p.z })),
      x,
    );
  const edgeEps = (maxX - minX) * 1e-4 + 1e-3;
  const columns: { x: number; lo: number; hi: number }[] = [];
  for (let i = 0; i <= cols; i += 1) {
    let x = minX + ((maxX - minX) * i) / cols;
    if (i === 0) x = minX + edgeEps;
    else if (i === cols) x = maxX - edgeEps;
    const s = ySpanAt(x);
    if (s && s[1] - s[0] > 0.5) columns.push({ x, lo: s[0], hi: s[1] });
  }
  if (columns.length < 2) return new BufferGeometry();

  const pos: number[] = [];
  const tri = (a: number[], b: number[], c: number[]) => pos.push(...a, ...b, ...c);
  const quad = (a: number[], b: number[], c: number[], d: number[]) => {
    tri(a, b, c);
    tri(a, c, d);
  };
  for (let i = 0; i < columns.length - 1; i += 1) {
    const c0 = columns[i];
    const c1 = columns[i + 1];
    const fb0 = cyl(c0.x, c0.lo, rOut);
    const ft0 = cyl(c0.x, c0.hi, rOut);
    const fb1 = cyl(c1.x, c1.lo, rOut);
    const ft1 = cyl(c1.x, c1.hi, rOut);
    const bb0 = cyl(c0.x, c0.lo, rIn);
    const bt0 = cyl(c0.x, c0.hi, rIn);
    const bb1 = cyl(c1.x, c1.lo, rIn);
    const bt1 = cyl(c1.x, c1.hi, rIn);
    quad(fb0, fb1, ft1, ft0); // outer face
    quad(bb1, bb0, bt0, bt1); // inner face
    quad(ft0, ft1, bt1, bt0); // top rim
    quad(bb0, bb1, fb1, fb0); // bottom rim
  }
  const first = columns[0];
  quad(
    cyl(first.x, first.lo, rOut),
    cyl(first.x, first.hi, rOut),
    cyl(first.x, first.hi, rIn),
    cyl(first.x, first.lo, rIn),
  );
  const last = columns[columns.length - 1];
  quad(
    cyl(last.x, last.lo, rIn),
    cyl(last.x, last.hi, rIn),
    cyl(last.x, last.hi, rOut),
    cyl(last.x, last.lo, rOut),
  );

  const geometry = new BufferGeometry();
  geometry.setAttribute('position', new Float32BufferAttribute(pos, 3));
  geometry.computeVertexNormals();
  return geometry;
};
