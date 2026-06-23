import type { CornerRadiiMm, WallEdge, WallEdgeNotch } from './project.types';

export interface WallOutlinePoint {
  x: number;
  z: number;
}

export const hasWallNotch = (notch?: CornerRadiiMm | null): boolean =>
  Boolean(
    notch &&
    ((notch.tl ?? 0) > 0 || (notch.tr ?? 0) > 0 || (notch.bl ?? 0) > 0 || (notch.br ?? 0) > 0),
  );

export const hasEdgeNotch = (edges?: WallEdgeNotch[] | null): boolean =>
  Boolean(edges && edges.some((e) => e.widthMm > 0 && e.depthMm > 0));

const MIN_BITE_MM = 1;

interface EdgeBite {
  offset: number;
  width: number;
  depth: number;
}

// Insert inward rectangular bites along a straight edge from `start` to `end` (CCW). Each bite
// is positioned by `offset` (distance from `start` along the edge); inward = the edge direction
// rotated +90° (the polygon interior). Bites are clamped to the edge span and `maxDepth`, sorted,
// and skipped when they would overlap — so the result is always a simple, non-self-intersecting
// polygon (a self-intersecting outline throws in three's ExtrudeGeometry).
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

// The wall face outline (length × height) with independent rectangular indentations cut from each
// corner AND along each edge (top / bottom / left / right). Returned CCW (bl→br→tr→tl) in mm; a true
// boundary cut, not a hole — so the indentation is visible from the front/back face and the edge face.
// Edge-notch offset is measured along the edge from its CCW start (bottom & right from the start
// corner, top from the right corner, left from the top corner). Notches are clamped so the polygon
// stays simple. Corner notches and fillet radii don't combine (a notched wall uses sharp corners).
export const wallProfileOutlineMm = (
  lengthMm: number,
  heightStartMm: number,
  heightEndMm: number,
  cornerNotch?: CornerRadiiMm | null,
  edgeNotches?: WallEdgeNotch[] | null,
): WallOutlinePoint[] => {
  const L = Math.max(1, lengthMm);
  const hL = Math.max(1, heightStartMm); // left edge height (x=0)
  const hR = Math.max(1, heightEndMm); // right edge height (x=L)
  const cap = (n: number, h: number) => Math.max(0, Math.min(n, L * 0.45, h * 0.45));
  const bl = cap(cornerNotch?.bl ?? 0, hL);
  const br = cap(cornerNotch?.br ?? 0, hR);
  const tr = cap(cornerNotch?.tr ?? 0, hR);
  const tl = cap(cornerNotch?.tl ?? 0, hL);
  const maxV = Math.min(hL, hR) * 0.9;
  const maxH = L * 0.9;
  const bitesOn = (edge: WallEdge): EdgeBite[] =>
    (edgeNotches ?? [])
      .filter((e) => e.edge === edge && e.widthMm > 0 && e.depthMm > 0)
      .map((e) => ({ offset: e.offsetMm, width: e.widthMm, depth: e.depthMm }));

  const out: WallOutlinePoint[] = [];
  // bottom-left corner
  if (bl > 0) out.push({ x: 0, z: bl }, { x: bl, z: bl }, { x: bl, z: 0 });
  else out.push({ x: 0, z: 0 });
  insertEdgeBites(out, { x: bl, z: 0 }, { x: L - br, z: 0 }, bitesOn('bottom'), maxV);
  // bottom-right corner
  if (br > 0) out.push({ x: L - br, z: 0 }, { x: L - br, z: br }, { x: L, z: br });
  else out.push({ x: L, z: 0 });
  insertEdgeBites(out, { x: L, z: br }, { x: L, z: hR - tr }, bitesOn('right'), maxH);
  // top-right corner
  if (tr > 0) out.push({ x: L, z: hR - tr }, { x: L - tr, z: hR - tr }, { x: L - tr, z: hR });
  else out.push({ x: L, z: hR });
  insertEdgeBites(out, { x: L - tr, z: hR }, { x: tl, z: hL }, bitesOn('top'), maxV);
  // top-left corner
  if (tl > 0) out.push({ x: tl, z: hL }, { x: tl, z: hL - tl }, { x: 0, z: hL - tl });
  else out.push({ x: 0, z: hL });
  insertEdgeBites(out, { x: 0, z: hL - tl }, { x: 0, z: bl }, bitesOn('left'), maxH);
  return out;
};

// Corner-only outline (back-compat for callers/tests that predate edge notches).
export const wallNotchedOutlineMm = (
  lengthMm: number,
  heightStartMm: number,
  heightEndMm: number,
  notch?: CornerRadiiMm | null,
): WallOutlinePoint[] => wallProfileOutlineMm(lengthMm, heightStartMm, heightEndMm, notch, null);
