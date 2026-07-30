import { describe, expect, it } from 'vitest';
import { bowFromArc, deriveArcFromRadius, radiusFromChordSweep, resolveArc } from './arcGeometry';

/**
 * A stored sweep past 180° is a MAJOR arc. `deriveArcFromRadius` inverts 2·asin(chord/2r), which
 * can only ever return the MINOR twin — so the inspector read a 270° run as 90° and reported a
 * third of the glass it actually needs. The read-out must come from the stored sweep.
 */

const CHORD_MM = 2121;
const SWEEP_DEG = 270;

const displayArc = (chordMm: number, radiusMm: number, sweepDeg: number) => {
  const radius = radiusFromChordSweep(chordMm, radiusMm, sweepDeg);
  return {
    radiusMm: Math.round(radius),
    sweepDeg: Math.abs(sweepDeg),
    arcLengthMm: Math.round(resolveArc(radius, sweepDeg).arcLengthMm),
    sagittaMm: Math.round(Math.abs(bowFromArc(chordMm, radius, sweepDeg))),
  };
};

describe('arc read-out on a MAJOR arc', () => {
  it('reports the stored sweep, not its minor twin', () => {
    const shown = displayArc(CHORD_MM, 1500, SWEEP_DEG);
    expect(shown.sweepDeg).toBe(270);

    // What the old radius-only derivation produced — the bug, kept here so it cannot come back.
    const minorTwin = deriveArcFromRadius(CHORD_MM, 1500);
    expect(Math.round(minorTwin.sweepDeg)).toBe(90);
  });

  it('reports the DEVELOPED length of the real curve (~7069 mm, not ~2356 mm)', () => {
    const shown = displayArc(CHORD_MM, 1500, SWEEP_DEG);
    // r·θ with r ≈ 1500 and θ = 270° = 4.712 rad.
    expect(shown.arcLengthMm).toBeGreaterThan(7000);
    expect(shown.arcLengthMm).toBeLessThan(7150);

    const minorTwin = deriveArcFromRadius(CHORD_MM, 1500);
    expect(minorTwin.arcLengthMm).toBeLessThan(2500);
    // Ordering glass off the old readout would have been roughly a third of what is needed.
    expect(shown.arcLengthMm / minorTwin.arcLengthMm).toBeGreaterThan(2.5);
  });

  it('the bow reaches the FAR apex on a major arc', () => {
    const shown = displayArc(CHORD_MM, 1500, SWEEP_DEG);
    // Past a half circle the apex is 2r − minorSagitta, so it clears the chord by more than r.
    expect(shown.sagittaMm).toBeGreaterThan(1500);
  });

  it('a MINOR arc is unaffected — the two paths agree', () => {
    const chord = 2828;
    const shown = displayArc(chord, 2000, 90);
    const legacy = deriveArcFromRadius(chord, 2000);

    expect(shown.sweepDeg).toBeCloseTo(legacy.sweepDeg, 0);
    expect(shown.arcLengthMm).toBeCloseTo(legacy.arcLengthMm, -1);
    expect(shown.sagittaMm).toBeCloseTo(legacy.sagittaMm, -1);
  });
});
