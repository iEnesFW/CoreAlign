import { describe, expect, it } from 'vitest';
import { bodyChordVectorMm, rotationFromChordAngleDeg } from './curvature';

const wrapDeg = (deg: number) => ((deg + 540) % 360) - 180;

describe('rotationFromChordAngleDeg — plan editors measure the CHORD, bodies store the TANGENT', () => {
  const arc = { lengthMm: 3000, geomArcRadiusMm: 2500, geomArcSweepDeg: 60 };

  it('leaves a straight body untouched — its chord IS its tangent', () => {
    expect(rotationFromChordAngleDeg({ lengthMm: 3000 }, 37)).toBeCloseTo(37, 9);
  });

  it('round-trips: the stored rotation makes the chord point where the handle asked', () => {
    for (const chordDeg of [0, 37, 90, -125, 180]) {
      const rotationDeg = rotationFromChordAngleDeg(arc, chordDeg);
      const chord = bodyChordVectorMm({ ...arc, rotationDeg });
      const actualDeg = (Math.atan2(chord.yMm, chord.xMm) * 180) / Math.PI;
      expect(wrapDeg(actualDeg - chordDeg)).toBeCloseTo(0, 6);
    }
  });

  it('differs from the raw chord angle — writing it verbatim swung the arc', () => {
    // RED before the fix: the 2D endpoint handle wrote the chord angle straight into rotationDeg,
    // so every endpoint drag rotated the curve by half its sweep.
    expect(Math.abs(wrapDeg(rotationFromChordAngleDeg(arc, 0)))).toBeGreaterThan(1);
  });
});
