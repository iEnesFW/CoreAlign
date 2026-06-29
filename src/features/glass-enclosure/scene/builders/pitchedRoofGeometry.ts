import { ExtrudeGeometry, Shape } from 'three';

export type PitchType = 'symmetric' | 'monopitch';

// A pitched-roof sheet for a rectangular slab. The pitch cross-section runs across the LENGTH
// (x), height up (y), and is extruded along the DEPTH (z) — the same slab-local frame as the
// flat/barrel slab body (X = length, Y = up, Z = depth), so no extra rotation is applied.
//   symmetric → a gable: eaves at both length ends (y=0), ridge at the middle (y=rise). Extruding
//               the triangular cross-section gives the triangular gable ends.
//   monopitch → a single slope: low eave at x=0, high eave at x=length (aim it via the slab's
//               rotationDeg). The sheet has the slab thickness measured vertically above the pitch.
export const buildPitchedRoofGeometry = (
  lengthMm: number,
  depthMm: number,
  riseMm: number,
  pitchType: PitchType,
  thicknessMm: number,
): ExtrudeGeometry => {
  const lengthM = Math.max(0.001, lengthMm / 1000);
  const depthM = Math.max(0.001, depthMm / 1000);
  const tM = Math.max(0.001, thicknessMm / 1000);
  const riseM = Math.max(0, riseMm) / 1000;
  // Pitch line (bottom of the sheet) across the length.
  const profile =
    pitchType === 'monopitch'
      ? [
          { x: 0, y: 0 },
          { x: lengthM, y: riseM },
        ]
      : [
          { x: 0, y: 0 },
          { x: lengthM / 2, y: riseM },
          { x: lengthM, y: 0 },
        ];
  const shape = new Shape();
  // Top edge (pitch + thickness), left→right …
  shape.moveTo(profile[0].x, profile[0].y + tM);
  for (let i = 1; i < profile.length; i += 1) shape.lineTo(profile[i].x, profile[i].y + tM);
  // … then the pitch line (bottom) right→left, closing the thin sheet.
  for (let i = profile.length - 1; i >= 0; i -= 1) shape.lineTo(profile[i].x, profile[i].y);
  shape.closePath();
  return new ExtrudeGeometry(shape, { depth: depthM, bevelEnabled: false });
};
