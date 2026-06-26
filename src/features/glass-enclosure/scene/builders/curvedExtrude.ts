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
  for (let i = 0; i <= cols; i += 1) {
    const x = minX + ((maxX - minX) * i) / cols;
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
