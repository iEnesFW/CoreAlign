import type { CornerRadiiMm, WallEdge, WallEdgeNotch } from './project.types';

export interface WallOutlinePoint {
  x: number;
  z: number;
}

const MIN_BITE_MM = 1;

export const hasWallNotch = (notch?: CornerRadiiMm | null): boolean =>
  Boolean(
    notch &&
    ((notch.tl ?? 0) > 0 || (notch.tr ?? 0) > 0 || (notch.bl ?? 0) > 0 || (notch.br ?? 0) > 0),
  );

export const hasEdgeNotch = (edges?: WallEdgeNotch[] | null): boolean =>
  Boolean(edges && edges.some((e) => e.widthMm >= MIN_BITE_MM && e.depthMm >= MIN_BITE_MM));

interface EdgeBite {
  offset: number;
  width: number;
  depth: number;
}

const insertEdgeBites = (
  out: WallOutlinePoint[],
  start: WallOutlinePoint,
  end: WallOutlinePoint,
  bites: EdgeBite[],
  maxDepth: number,
): void => {
  const dx = end.x - start.x;
  const dz = end.z - start.z;
  const len = Math.hypot(dx, dz);
  if (len <= 0 || bites.length === 0) return;
  const ux = dx / len;
  const uz = dz / len;
  // inward = edge direction rotated +90° (the polygon interior for a CCW outline)
  const nx = -uz;
  const nz = ux;
  const sorted = [...bites].sort((a, b) => a.offset - b.offset);
  let lastEnd = 0;
  for (const bite of sorted) {
    const o = Math.max(lastEnd, Math.min(bite.offset, len));
    const w = Math.min(Math.max(0, bite.width), len - o);
    const d = Math.max(0, Math.min(bite.depth, maxDepth));
    if (w < MIN_BITE_MM || d < MIN_BITE_MM) continue;
    out.push({ x: start.x + ux * o, z: start.z + uz * o });
    out.push({ x: start.x + ux * o + nx * d, z: start.z + uz * o + nz * d });
    out.push({ x: start.x + ux * (o + w) + nx * d, z: start.z + uz * (o + w) + nz * d });
    out.push({ x: start.x + ux * (o + w), z: start.z + uz * (o + w) });
    lastEnd = o + w;
  }
};

const EPS = 1e-6;
const orient = (p: WallOutlinePoint, q: WallOutlinePoint, r: WallOutlinePoint): number => {
  const v = (q.z - p.z) * (r.x - q.x) - (q.x - p.x) * (r.z - q.z);
  return Math.abs(v) < EPS ? 0 : v > 0 ? 1 : 2;
};
const onSeg = (p: WallOutlinePoint, q: WallOutlinePoint, r: WallOutlinePoint): boolean =>
  q.x <= Math.max(p.x, r.x) + EPS &&
  q.x >= Math.min(p.x, r.x) - EPS &&
  q.z <= Math.max(p.z, r.z) + EPS &&
  q.z >= Math.min(p.z, r.z) - EPS;
const segmentsTouch = (
  p1: WallOutlinePoint,
  p2: WallOutlinePoint,
  p3: WallOutlinePoint,
  p4: WallOutlinePoint,
): boolean => {
  const o1 = orient(p1, p2, p3);
  const o2 = orient(p1, p2, p4);
  const o3 = orient(p3, p4, p1);
  const o4 = orient(p3, p4, p2);
  if (o1 !== o2 && o3 !== o4) return true;
  if (o1 === 0 && onSeg(p1, p3, p2)) return true;
  if (o2 === 0 && onSeg(p1, p4, p2)) return true;
  if (o3 === 0 && onSeg(p3, p1, p4)) return true;
  if (o4 === 0 && onSeg(p3, p2, p4)) return true;
  return false;
};
// True if two non-adjacent edges of the closed outline meet anywhere (proper crossing OR a
// collinear retrace). Two deep notches on perpendicular edges sharing a corner produce exactly
// this; earcut would then build a garbled (non-simple) triangulation rather than throw.
const outlineSelfIntersects = (pts: WallOutlinePoint[]): boolean => {
  const n = pts.length;
  for (let i = 0; i < n; i += 1) {
    const a = pts[i];
    const b = pts[(i + 1) % n];
    for (let j = i + 2; j < n; j += 1) {
      if (i === 0 && j === n - 1) continue; // adjacent (closing) edge
      if (segmentsTouch(a, b, pts[j], pts[(j + 1) % n])) return true;
    }
  }
  return false;
};

// The wall face outline (length × height) with independent rectangular indentations cut from each
// corner AND along each edge (top / bottom / left / right). Returned CCW (bl→br→tr→tl) in mm; a real
// boundary cut (visible from the front/back face and the edge face), not a hole. Edge-notch offset is
// measured along the edge from its CCW start. Corner notches and fillet radii don't combine.
export const wallProfileOutlineMm = (
  lengthMm: number,
  heightStartMm: number,
  heightEndMm: number,
  cornerNotch?: CornerRadiiMm | null,
  edgeNotches?: WallEdgeNotch[] | null,
): WallOutlinePoint[] => {
  const L = Math.max(1, lengthMm);
  const hL = Math.max(1, heightStartMm);
  const hR = Math.max(1, heightEndMm);
  const cap = (n: number, h: number) => Math.max(0, Math.min(n, L * 0.45, h * 0.45));
  const bl = cap(cornerNotch?.bl ?? 0, hL);
  const br = cap(cornerNotch?.br ?? 0, hR);
  const tr = cap(cornerNotch?.tr ?? 0, hR);
  const tl = cap(cornerNotch?.tl ?? 0, hL);
  const maxV = Math.min(hL, hR) * 0.9;
  const maxH = L * 0.9;

  const build = (edges?: WallEdgeNotch[] | null): WallOutlinePoint[] => {
    const bitesOn = (edge: WallEdge): EdgeBite[] =>
      (edges ?? [])
        .filter((e) => e.edge === edge && e.widthMm >= MIN_BITE_MM && e.depthMm >= MIN_BITE_MM)
        .map((e) => ({ offset: e.offsetMm, width: e.widthMm, depth: e.depthMm }));
    const out: WallOutlinePoint[] = [];
    if (bl > 0) out.push({ x: 0, z: bl }, { x: bl, z: bl }, { x: bl, z: 0 });
    else out.push({ x: 0, z: 0 });
    insertEdgeBites(out, { x: bl, z: 0 }, { x: L - br, z: 0 }, bitesOn('bottom'), maxV);
    if (br > 0) out.push({ x: L - br, z: 0 }, { x: L - br, z: br }, { x: L, z: br });
    else out.push({ x: L, z: 0 });
    insertEdgeBites(out, { x: L, z: br }, { x: L, z: hR - tr }, bitesOn('right'), maxH);
    if (tr > 0) out.push({ x: L, z: hR - tr }, { x: L - tr, z: hR - tr }, { x: L - tr, z: hR });
    else out.push({ x: L, z: hR });
    insertEdgeBites(out, { x: L - tr, z: hR }, { x: tl, z: hL }, bitesOn('top'), maxV);
    if (tl > 0) out.push({ x: tl, z: hL }, { x: tl, z: hL - tl }, { x: 0, z: hL - tl });
    else out.push({ x: 0, z: hL });
    insertEdgeBites(out, { x: 0, z: hL - tl }, { x: 0, z: bl }, bitesOn('left'), maxH);
    return out;
  };

  const full = build(edgeNotches);
  // WHY: deep notches on two perpendicular edges sharing a corner overlap into a non-simple
  // polygon (garbled mesh); drop the edge notches for this render rather than ship corruption.
  if (hasEdgeNotch(edgeNotches) && outlineSelfIntersects(full)) return build(null);
  return full;
};

// Corner-only outline (back-compat for callers/tests that predate edge notches).
export const wallNotchedOutlineMm = (
  lengthMm: number,
  heightStartMm: number,
  heightEndMm: number,
  notch?: CornerRadiiMm | null,
): WallOutlinePoint[] => wallProfileOutlineMm(lengthMm, heightStartMm, heightEndMm, notch, null);
