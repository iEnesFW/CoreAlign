import { ExtrudeGeometry, Shape } from 'three';

export interface BentWallParams {
  lengthMm: number;
  bendAtMm: number;
  bendAngleDeg: number;
  thicknessMm: number;
  heightMm: number;
}

interface Vec2 {
  x: number;
  y: number;
}

const MIN_BEND_DEG = 1;
const EPS = 1e-6;

const intersect = (p1: Vec2, d1: Vec2, p2: Vec2, d2: Vec2): Vec2 | null => {
  const denom = d1.x * d2.y - d1.y * d2.x;
  if (Math.abs(denom) < EPS) return null;
  const t = ((p2.x - p1.x) * d2.y - (p2.y - p1.y) * d2.x) / denom;
  return { x: p1.x + t * d1.x, y: p1.y + t * d1.y };
};

// Builds a straight rectangular footprint (the degenerate fallback when there is no usable bend).
const straightOutline = (lengthM: number, halfM: number): Vec2[] => [
  { x: 0, y: halfM },
  { x: lengthM, y: halfM },
  { x: lengthM, y: -halfM },
  { x: 0, y: -halfM },
];

// The plan footprint of an L-wall as a single mitred polygon: segment 1 runs from the origin along
// +x for bendAt, then the wall turns by bendAngle and segment 2 continues for the remaining length.
// Both segments carry the wall thickness (±half about their centreline); the left/right offset lines
// meet at the inner/outer miter vertices, so the join is a clean miter rather than two butted boxes.
export const bentWallFootprintMm = (
  lengthM: number,
  bendAtM: number,
  bendRad: number,
  halfM: number,
): Vec2[] => {
  const b = Math.min(Math.max(bendAtM, halfM), lengthM - halfM);
  const remaining = lengthM - b;
  if (remaining <= EPS) return straightOutline(lengthM, halfM);

  const dir1: Vec2 = { x: 1, y: 0 };
  const n1: Vec2 = { x: 0, y: halfM };
  const dir2: Vec2 = { x: Math.cos(bendRad), y: Math.sin(bendRad) };
  const n2: Vec2 = { x: -Math.sin(bendRad) * halfM, y: Math.cos(bendRad) * halfM };

  const a: Vec2 = { x: 0, y: 0 };
  const c: Vec2 = { x: b + remaining * dir2.x, y: remaining * dir2.y };

  const aL: Vec2 = { x: a.x + n1.x, y: a.y + n1.y };
  const aR: Vec2 = { x: a.x - n1.x, y: a.y - n1.y };
  const cL: Vec2 = { x: c.x + n2.x, y: c.y + n2.y };
  const cR: Vec2 = { x: c.x - n2.x, y: c.y - n2.y };

  const mL = intersect(aL, dir1, cL, dir2);
  const mR = intersect(aR, dir1, cR, dir2);
  if (!mL || !mR) return straightOutline(lengthM, halfM);

  return [aL, mL, cL, cR, mR, aR];
};

// A bent (L-shaped) wall as one continuous solid: the mitred plan footprint extruded up by height.
// Built directly in the wall body frame (x = developed start direction, y = up, z = thickness/plan),
// so it reuses the straight-wall render path (no extra mesh rotation). bendAngleDeg outside a usable
// range, or a bend point at the wall ends, degenerates to a straight wall rather than a void.
export const buildBentWallGeometry = (params: BentWallParams): ExtrudeGeometry => {
  const lengthM = Math.max(0.001, params.lengthMm / 1000);
  const heightM = Math.max(0.001, params.heightMm / 1000);
  const halfM = Math.max(0.0005, params.thicknessMm / 1000 / 2);
  const usableBend = Math.abs(params.bendAngleDeg) >= MIN_BEND_DEG;
  const bendRad = (params.bendAngleDeg * Math.PI) / 180;

  const outline = usableBend
    ? bentWallFootprintMm(lengthM, params.bendAtMm / 1000, bendRad, halfM)
    : straightOutline(lengthM, halfM);

  const shape = new Shape();
  shape.moveTo(outline[0].x, outline[0].y);
  for (let i = 1; i < outline.length; i += 1) shape.lineTo(outline[i].x, outline[i].y);
  shape.closePath();

  const geometry = new ExtrudeGeometry(shape, { depth: heightM, bevelEnabled: false });
  // Footprint built in the x/y plan plane and extruded along z; rotate so plan lies in x/z (ground)
  // and the extrusion becomes world-up y, with the base at y = 0.
  geometry.rotateX(-Math.PI / 2);
  return geometry;
};
