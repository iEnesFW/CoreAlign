import type { CornerRadiiMm } from './project.types';

export interface WallOutlinePoint {
  x: number;
  z: number;
}

export const hasWallNotch = (notch?: CornerRadiiMm | null): boolean =>
  Boolean(
    notch &&
    ((notch.tl ?? 0) > 0 || (notch.tr ?? 0) > 0 || (notch.bl ?? 0) > 0 || (notch.br ?? 0) > 0),
  );

// The wall face outline (length × height) with an independent rectangular notch cut
// from each corner — a free per-corner indentation. Returned CCW (bl→br→tr→tl) in mm.
// Each notch is clamped so it can't exceed half the wall, keeping the polygon simple.
export const wallNotchedOutlineMm = (
  lengthMm: number,
  heightStartMm: number,
  heightEndMm: number,
  notch?: CornerRadiiMm | null,
): WallOutlinePoint[] => {
  const L = Math.max(1, lengthMm);
  const hL = Math.max(1, heightStartMm); // left edge height (x=0)
  const hR = Math.max(1, heightEndMm); // right edge height (x=L)
  const cap = (n: number, h: number) => Math.max(0, Math.min(n, L * 0.45, h * 0.45));
  const bl = cap(notch?.bl ?? 0, hL);
  const br = cap(notch?.br ?? 0, hR);
  const tr = cap(notch?.tr ?? 0, hR);
  const tl = cap(notch?.tl ?? 0, hL);
  const pts: WallOutlinePoint[] = [];
  // bottom-left corner (0,0)
  if (bl > 0) pts.push({ x: 0, z: bl }, { x: bl, z: bl }, { x: bl, z: 0 });
  else pts.push({ x: 0, z: 0 });
  // bottom-right corner (L,0)
  if (br > 0) pts.push({ x: L - br, z: 0 }, { x: L - br, z: br }, { x: L, z: br });
  else pts.push({ x: L, z: 0 });
  // top-right corner (L,hR)
  if (tr > 0) pts.push({ x: L, z: hR - tr }, { x: L - tr, z: hR - tr }, { x: L - tr, z: hR });
  else pts.push({ x: L, z: hR });
  // top-left corner (0,hL)
  if (tl > 0) pts.push({ x: tl, z: hL }, { x: tl, z: hL - tl }, { x: 0, z: hL - tl });
  else pts.push({ x: 0, z: hL });
  return pts;
};
