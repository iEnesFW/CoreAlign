import {
  BufferGeometry,
  ExtrudeGeometry,
  Float32BufferAttribute,
  Shape,
  ShapeUtils,
  Vector2,
} from 'three';

// Shared angular facet step for the band body AND the feature cutter: two different pitches made
// three-csg-ts leave jagged beat-frequency edges where the grids intersected.
const CURVE_STEP_RAD = 0.03;

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

// Build a silhouette-following FRAME ring for a shaped pane on an arc — the curved analogue of
// buildPanelFrameGeometry. The outline (panel-local mm, x∈[-w/2,w/2], y∈[0,h]) and an inset copy
// (scaled toward the centroid by frameWidthMm, exactly as the flat frame) are BOTH mapped through
// the same cyl() as the glass, so the border hugs the glass edge on the curve. Emitted as a closed
// ribbon (front/back radial faces + outer/inner silhouette walls), densified so a straight outline
// edge follows the arc instead of chording across it.
export const buildCurvedShapedFrameGeometry = (
  outlineMm: OutlinePointMm[],
  widthMm: number,
  radiusM: number,
  direction: 1 | -1,
  phiStart: number,
  phiEnd: number,
  frameDepthM: number,
  frameWidthMm: number,
): BufferGeometry => {
  const geometry = new BufferGeometry();
  if (outlineMm.length < 3) return geometry;
  let minX = Infinity;
  let maxX = -Infinity;
  let minY = Infinity;
  let maxY = -Infinity;
  for (const p of outlineMm) {
    if (p.x < minX) minX = p.x;
    if (p.x > maxX) maxX = p.x;
    if (p.y < minY) minY = p.y;
    if (p.y > maxY) maxY = p.y;
  }
  const bw = maxX - minX;
  const bh = maxY - minY;
  if (bw <= 0 || bh <= 0) return geometry;
  const fw = Math.min(frameWidthMm, Math.min(bw, bh) / 3);
  const sx = (bw - 2 * fw) / bw;
  const sy = (bh - 2 * fw) / bh;
  if (sx <= 0 || sy <= 0) return geometry;
  const cx = (minX + maxX) / 2;
  const cy = (minY + maxY) / 2;
  const inner = outlineMm.map((p) => ({ x: cx + (p.x - cx) * sx, y: cy + (p.y - cy) * sy }));

  const radius = Math.max(0.001, Number.isFinite(radiusM) ? radiusM : 0.001);
  const span = Math.max(1e-4, phiEnd - phiStart);
  const centerY = -direction * radius;
  const w = Math.max(1, widthMm);
  const halfDepth = Math.max(0.001, frameDepthM) / 2;
  const outerR = radius + halfDepth;
  const innerR = Math.max(0.001, radius - halfDepth);
  const toAngle = (phi: number) => (direction === 1 ? Math.PI / 2 - phi : phi - Math.PI / 2);
  const cyl = (xMm: number, yMm: number, r: number): number[] => {
    const phi = phiStart + ((xMm + w / 2) / w) * span;
    const a = toAngle(phi);
    return [Math.cos(a) * r, centerY + Math.sin(a) * r, yMm / 1000];
  };
  const maxSegMm = Math.max(5, (CURVE_STEP_RAD / span) * w);

  const pos: number[] = [];
  // Same reflection handling as buildCurvedWallFeatureSolid: direction === +1 flips cyl()'s winding.
  const flip = direction === 1;
  const tri = (a: number[], b: number[], c: number[]) =>
    flip ? pos.push(...a, ...c, ...b) : pos.push(...a, ...b, ...c);
  const quad = (a: number[], b: number[], c: number[], d: number[]) => {
    tri(a, b, c);
    tri(a, c, d);
  };

  const n = outlineMm.length;
  for (let i = 0; i < n; i += 1) {
    const Oa = outlineMm[i];
    const Ob = outlineMm[(i + 1) % n];
    const Ia = inner[i];
    const Ib = inner[(i + 1) % n];
    const segs = Math.max(1, Math.ceil(Math.abs(Ob.x - Oa.x) / maxSegMm));
    for (let k = 0; k < segs; k += 1) {
      const t0 = k / segs;
      const t1 = (k + 1) / segs;
      const oax = Oa.x + (Ob.x - Oa.x) * t0;
      const oay = Oa.y + (Ob.y - Oa.y) * t0;
      const obx = Oa.x + (Ob.x - Oa.x) * t1;
      const oby = Oa.y + (Ob.y - Oa.y) * t1;
      const iax = Ia.x + (Ib.x - Ia.x) * t0;
      const iay = Ia.y + (Ib.y - Ia.y) * t0;
      const ibx = Ia.x + (Ib.x - Ia.x) * t1;
      const iby = Ia.y + (Ib.y - Ia.y) * t1;
      quad(
        cyl(oax, oay, outerR),
        cyl(obx, oby, outerR),
        cyl(ibx, iby, outerR),
        cyl(iax, iay, outerR),
      );
      quad(
        cyl(iax, iay, innerR),
        cyl(ibx, iby, innerR),
        cyl(obx, oby, innerR),
        cyl(oax, oay, innerR),
      );
      quad(
        cyl(oax, oay, innerR),
        cyl(obx, oby, innerR),
        cyl(obx, oby, outerR),
        cyl(oax, oay, outerR),
      );
      quad(
        cyl(iax, iay, outerR),
        cyl(ibx, iby, outerR),
        cyl(ibx, iby, innerR),
        cyl(iax, iay, innerR),
      );
    }
  }
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

// Densifying polyline mapper for the LIVE previews (pen elastic line, draft outlines) on a curved
// wall: mapping only the vertices and connecting them with straight 3D segments buries any
// horizontal span ≳20cm inside the band (the chord sags R·(1−cos(Δφ/2)) below the surface, far
// beyond the 4mm face lift), which is why the blue preview vanished on horizontal movement.
// Subdivides every edge with the same CURVE_STEP_RAD rule the committed CSG cutter uses, then maps
// each dense point through curvedWallSurfacePoint. The wall-equivalent of curvedSlabMapOutlineMm.
export const curvedWallSurfacePolyline = (
  ptsMm: CurvedWallFeaturePoint[],
  bandRadiusM: number,
  surfaceRM: number,
  direction: 1 | -1,
  sweep: number,
  lengthMm: number,
  close: boolean,
): [number, number, number][] => {
  const span = Math.max(1e-4, sweep);
  const len = Math.max(1, lengthMm);
  const maxSegMm = Math.max(5, (CURVE_STEP_RAD / span) * len);
  const out: [number, number, number][] = [];
  const mapPoint = (p: CurvedWallFeaturePoint) =>
    curvedWallSurfacePoint(p.x, p.z, bandRadiusM, surfaceRM, direction, sweep, lengthMm);
  const edgeCount = close ? ptsMm.length : ptsMm.length - 1;
  for (let i = 0; i < edgeCount; i += 1) {
    const p = ptsMm[i];
    const q = ptsMm[(i + 1) % ptsMm.length];
    out.push(mapPoint(p));
    const segments = Math.ceil(Math.abs(q.x - p.x) / maxSegMm);
    for (let k = 1; k < segments; k += 1) {
      const t = k / segments;
      out.push(mapPoint({ x: p.x + (q.x - p.x) * t, z: p.z + (q.z - p.z) * t }));
    }
  }
  if (ptsMm.length > 0) out.push(mapPoint(ptsMm[close ? 0 : ptsMm.length - 1]));
  return out;
};

// A closed solid that follows the feature's outline along a curved wall, occupying the radial
// band [rNearM, rFarM]. Built in the SAME pre-rotation frame as buildCurvedBandGeometry (arc in
// XY, height along Z) so it can be CSG-subtracted from / unioned with the curved wall body, or
// rendered as a thin on-surface proxy for selection. The outline x maps along the arc (0 → sweep);
// z is the height. Used for holes (through), recesses (partial radial), protrusions (outward) and
// the selectable surface decal on a curved wall.
//
// EXACT OUTLINE SWEEP: every drawn vertex maps 1:1 through cyl() — the previous version resampled
// the outline into as few as 8 single-span vertical columns, which chamfered corners (up to half a
// column pitch of material left standing) and FILLED any vertical concavity (spanAtX kept only the
// min/max crossings). Caps are earcut-triangulated (concave-safe); long edges are subdivided so
// straight outline edges still follow the curve; side walls are radial quads per outline edge with
// the same winding relationship the band's own rim quads used.
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

  const deduped: CurvedWallFeaturePoint[] = [];
  for (const p of outlineMm) {
    const prev = deduped[deduped.length - 1];
    if (!prev || Math.hypot(p.x - prev.x, p.z - prev.z) > 0.5) deduped.push(p);
  }
  if (deduped.length >= 2) {
    const first = deduped[0];
    const last = deduped[deduped.length - 1];
    if (Math.hypot(first.x - last.x, first.z - last.z) <= 0.5) deduped.pop();
  }
  if (deduped.length < 3) return new BufferGeometry();

  let area2 = 0;
  for (let i = 0; i < deduped.length; i += 1) {
    const p = deduped[i];
    const q = deduped[(i + 1) % deduped.length];
    area2 += p.x * q.z - q.x * p.z;
  }
  const loop = area2 >= 0 ? deduped : [...deduped].reverse();

  // Subdivide edges so no sub-segment spans more than CURVE_STEP_RAD of arc — a long straight
  // outline edge otherwise cuts as a chord through the curved band.
  const maxSegMm = Math.max(5, (CURVE_STEP_RAD / span) * len);
  const dense: CurvedWallFeaturePoint[] = [];
  for (let i = 0; i < loop.length; i += 1) {
    const p = loop[i];
    const q = loop[(i + 1) % loop.length];
    dense.push(p);
    const segments = Math.ceil(Math.abs(q.x - p.x) / maxSegMm);
    for (let k = 1; k < segments; k += 1) {
      const t = k / segments;
      dense.push({ x: p.x + (q.x - p.x) * t, z: p.z + (q.z - p.z) * t });
    }
  }
  if (dense.length < 3) return new BufferGeometry();

  const pos: number[] = [];
  // WHY: for direction === +1 the (u,z)→3D map cyl() is a REFLECTION (toAngle decreases as u
  // grows), so CCW-in-(u,z) winding comes out inside-out in 3D — the winding-based CSG then
  // computed band∩cutter instead of band−cutter, keeping only the plug (empirically verified).
  const flip = direction === 1;
  const tri = (a: number[], b: number[], c: number[]) =>
    flip ? pos.push(...a, ...c, ...b) : pos.push(...a, ...b, ...c);
  const quad = (a: number[], b: number[], c: number[], d: number[]) => {
    tri(a, b, c);
    tri(a, c, d);
  };

  // WHY: cap triangles are FLAT while their vertices sit on the cylinder — a triangle spanning
  // more arc than CURVE_STEP_RAD sags r·(1−cos(Δφ/2)) below the band skin and leaves an uncut
  // shell inside wide holes. Subdivide until each cap triangle stays within the same step the
  // outline edges already use, then map every vertex through cyl().
  const emitCap = (
    a: CurvedWallFeaturePoint,
    b: CurvedWallFeaturePoint,
    c: CurvedWallFeaturePoint,
    r: number,
    reversed: boolean,
  ) => {
    const dab = Math.abs(a.x - b.x);
    const dbc = Math.abs(b.x - c.x);
    const dca = Math.abs(c.x - a.x);
    if (Math.max(dab, dbc, dca) > maxSegMm) {
      if (dab >= dbc && dab >= dca) {
        const m = { x: (a.x + b.x) / 2, z: (a.z + b.z) / 2 };
        emitCap(a, m, c, r, reversed);
        emitCap(m, b, c, r, reversed);
      } else if (dbc >= dca) {
        const m = { x: (b.x + c.x) / 2, z: (b.z + c.z) / 2 };
        emitCap(a, b, m, r, reversed);
        emitCap(a, m, c, r, reversed);
      } else {
        const m = { x: (c.x + a.x) / 2, z: (c.z + a.z) / 2 };
        emitCap(a, b, m, r, reversed);
        emitCap(m, b, c, r, reversed);
      }
      return;
    }
    const pa = cyl(a.x, a.z, r);
    const pb = cyl(b.x, b.z, r);
    const pc = cyl(c.x, c.z, r);
    if (reversed) tri(pa, pc, pb);
    else tri(pa, pb, pc);
  };

  const contour = dense.map((p) => new Vector2(p.x, p.z));
  const faces = ShapeUtils.triangulateShape(contour, []);
  for (const face of faces) {
    const i0 = face[0];
    let i1 = face[1];
    let i2 = face[2];
    const a = contour[i0];
    const b = contour[i1];
    const c = contour[i2];
    // Normalize each cap triangle to CCW in (u,z) so the solid's orientation is deterministic
    // (earcut's output winding follows its input; the outline arrives in either direction).
    if ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) < 0) {
      const swap = i1;
      i1 = i2;
      i2 = swap;
    }
    emitCap(dense[i0], dense[i1], dense[i2], rOut, false);
    emitCap(dense[i0], dense[i1], dense[i2], rIn, true);
  }

  for (let i = 0; i < dense.length; i += 1) {
    const a = dense[i];
    const b = dense[(i + 1) % dense.length];
    quad(cyl(a.x, a.z, rIn), cyl(b.x, b.z, rIn), cyl(b.x, b.z, rOut), cyl(a.x, a.z, rOut));
  }

  const geometry = new BufferGeometry();
  geometry.setAttribute('position', new Float32BufferAttribute(pos, 3));
  geometry.computeVertexNormals();
  return geometry;
};
