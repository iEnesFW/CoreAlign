import { ExtrudeGeometry, Shape } from 'three';
import { barrelArcProfilePoints } from '../../model/barrelRoof';

const PROFILE_SEGMENTS = 32;

// A barrel (single-curvature) roof sheet for a rectangular slab. The arc cross-section
// runs across the LENGTH (eaves at the length ends, ridge in the middle); the sheet is
// extruded along the DEPTH. Output is already in the slab's local frame — X = length,
// Y = up, Z = depth — matching the oriented flat-slab body, so no rotateX is applied.
export const buildBarrelRoofGeometry = (
  lengthMm: number,
  depthMm: number,
  riseMm: number,
  thicknessMm: number,
): ExtrudeGeometry => {
  const profile = barrelArcProfilePoints(lengthMm, riseMm, PROFILE_SEGMENTS);
  const tM = thicknessMm / 1000;
  const shape = new Shape();
  shape.moveTo(profile[0].x / 1000, profile[0].y / 1000 + tM);
  for (let i = 1; i < profile.length; i += 1) {
    shape.lineTo(profile[i].x / 1000, profile[i].y / 1000 + tM);
  }
  for (let i = profile.length - 1; i >= 0; i -= 1) {
    shape.lineTo(profile[i].x / 1000, profile[i].y / 1000);
  }
  shape.closePath();
  return new ExtrudeGeometry(shape, { depth: depthMm / 1000, bevelEnabled: false });
};
