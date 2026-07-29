import { radiusFromChordSweep, resolveArc } from './arcGeometry';

export interface BandBody {
  lengthMm: number;
  geomArcRadiusMm?: number | null;
  geomArcSweepDeg?: number | null;
}

export interface BandPoint {
  x: number;
  y: number;
}

const DEG2RAD = Math.PI / 180;

/** One outline vertex per this much sweep — matches the collision band's resolution. */
export const BAND_STEP_RAD = 0.25;

/**
 * The plan outline of a CURVED body: the real annular band, as a closed polygon in world mm.
 *
 * WHY shared: the collision footprint, the DXF/plan export and the 2D plan all need "what shape
 * does this curved body occupy in plan". Each one that re-derived it with
 * `origin + length * dir(rotationDeg)` drew a straight rectangle at a ghost endpoint instead —
 * metres away from the body the user sees. Deriving the radius from the chord (not the stored,
 * integer-rounded one) is what keeps this identical to what the renderer draws.
 */
export const arcBandOutlineMm = (
  body: BandBody,
  originX: number,
  originY: number,
  rotationDeg: number,
  halfWidthMm: number,
): BandPoint[] => {
  const resolved = resolveArc(
    radiusFromChordSweep(body.lengthMm, body.geomArcRadiusMm, body.geomArcSweepDeg),
    body.geomArcSweepDeg ?? 1,
  );
  const { radiusMm: radius, direction, sweepRad: sweep } = resolved;
  const steps = Math.max(6, Math.ceil(sweep / BAND_STEP_RAD));
  const rad = rotationDeg * DEG2RAD;
  const cosR = Math.cos(rad);
  const sinR = Math.sin(rad);
  const toWorld = (lx: number, ly: number): BandPoint => ({
    x: originX + lx * cosR - ly * sinR,
    y: originY + lx * sinR + ly * cosR,
  });

  const outer: BandPoint[] = [];
  const inner: BandPoint[] = [];
  for (let i = 0; i <= steps; i += 1) {
    const phi = (sweep * i) / steps;
    const px = radius * Math.sin(phi);
    const py = direction * radius * (1 - Math.cos(phi));
    const tangent = Math.atan2(direction * Math.sin(phi), Math.cos(phi));
    const nx = -Math.sin(tangent);
    const ny = Math.cos(tangent);
    outer.push(toWorld(px + nx * halfWidthMm, py + ny * halfWidthMm));
    inner.push(toWorld(px - nx * halfWidthMm, py - ny * halfWidthMm));
  }
  return [...outer, ...inner.reverse()];
};
