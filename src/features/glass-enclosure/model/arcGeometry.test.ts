import { describe, expect, it } from 'vitest';
import {
  arcFromChordKeepingSweep,
  arcFromSweepKeepingLength,
  arcLengthFromRadiusSweep,
  bowArcPlanPoints,
  bowFromArc,
  bowToArcKeepingLength,
  deriveArcFromChordSagitta,
  deriveArcFromRadius,
  minArcRadiusMm,
  resolveArc,
  sampleArcPlan,
} from './arcGeometry';

describe('bowArcPlanPoints', () => {
  const startX = 0;
  const startY = 0;
  const endX = 3000;
  const endY = 0;

  it('returns the straight chord for a negligible bow', () => {
    const pts = bowArcPlanPoints(startX, startY, endX, endY, 0);
    expect(pts).toHaveLength(2);
    expect(pts[0]).toEqual({ x: 0, y: 0 });
    expect(pts[1]).toEqual({ x: 3000, y: 0 });
  });

  it('starts and ends exactly at the chord endpoints', () => {
    const pts = bowArcPlanPoints(startX, startY, endX, endY, 600);
    expect(pts[0].x).toBeCloseTo(0, 3);
    expect(pts[0].y).toBeCloseTo(0, 3);
    expect(pts[pts.length - 1].x).toBeCloseTo(3000, 3);
    expect(pts[pts.length - 1].y).toBeCloseTo(0, 3);
  });

  it('passes through the apex at the chord midpoint (sagitta away from the chord)', () => {
    const sagitta = 600;
    const pts = bowArcPlanPoints(startX, startY, endX, endY, sagitta);
    const mid = pts[Math.floor(pts.length / 2)];
    expect(mid.x).toBeCloseTo(1500, 0); // midline
    expect(mid.y).toBeCloseTo(sagitta, 0); // bows out by the sagitta
  });

  it('every sample lies on the arc circle (constant radius from the centre)', () => {
    const sagitta = 900;
    const pts = bowArcPlanPoints(startX, startY, endX, endY, sagitta);
    const r = sagitta / 2 + (3000 * 3000) / (8 * sagitta);
    const cy = sagitta - r; // centre on the across axis below the apex
    for (const p of pts) {
      expect(Math.hypot(p.x - 1500, p.y - cy)).toBeCloseTo(r, 2);
    }
  });

  it('a major bow (sagitta > chord/2) curls past a half-circle (apex beyond the diameter)', () => {
    const sagitta = 2400; // well past chord/2 = 1500
    const pts = bowArcPlanPoints(startX, startY, endX, endY, sagitta);
    const maxY = Math.max(...pts.map((p) => p.y));
    // The arc wraps past the semicircle, so the deepest point is the apex itself, not the diameter.
    expect(maxY).toBeCloseTo(sagitta, 0);
    // And it reaches behind the chord line (negative y) as it curls back to the endpoints.
    expect(Math.min(...pts.map((p) => p.y))).toBeLessThan(0);
  });
});

describe('bowFromArc', () => {
  it('round-trips a minor arc through deriveArcFromChordSagitta', () => {
    const chord = 3000;
    const sagitta = 500; // minor (< chord/2)
    const d = deriveArcFromChordSagitta(chord, sagitta);
    const back = bowFromArc(chord, d.radiusMm, d.sweepDeg);
    expect(Math.abs(back)).toBeCloseTo(sagitta, 0);
  });

  it('reports the FAR apex for a major arc, not the shallow near point', () => {
    const chord = 3000;
    const sagitta = 2400; // major (> chord/2) → sweep > 180°
    const d = deriveArcFromChordSagitta(chord, sagitta);
    expect(d.sweepDeg).toBeGreaterThan(180);
    const back = bowFromArc(chord, d.radiusMm, d.sweepDeg);
    // Within a couple of mm — deriveArcFromChordSagitta rounds the radius to an integer.
    expect(Math.abs(Math.abs(back) - sagitta)).toBeLessThan(2);
    // The shallow (minor) sagitta would be far smaller — guard against the old understating bug.
    const minorSag = d.radiusMm - Math.sqrt(d.radiusMm * d.radiusMm - (chord / 2) ** 2);
    expect(Math.abs(back)).toBeGreaterThan(minorSag * 2);
  });

  it('carries the sweep sign (bulge direction)', () => {
    expect(bowFromArc(3000, 2000, 90)).toBeLessThan(0);
    expect(bowFromArc(3000, 2000, -90)).toBeGreaterThan(0);
  });
});

describe('arc-length-invariant arc model', () => {
  it('resolveArc keeps the glass length (arc length) and derives sweep = arcLength/radius', () => {
    const arcLength = 3000;
    for (const radiusMm of [477, 955, 1910, 5000]) {
      const r = resolveArc(arcLength, radiusMm, 1);
      // The developed length always equals lengthMm — curving never changes the glass length.
      expect(r.arcLengthMm).toBe(arcLength);
      expect(r.sweepRad).toBeCloseTo(arcLength / r.radiusMm, 6);
    }
  });

  it('resolveArc lets the radius go down to a full circle (arcLength/2π), not just a half-circle', () => {
    // radius 477 ≈ 3000/2π → a 360° wrap of the 3000mm glass. The old chord model floored at 1500.
    const r = resolveArc(3000, 477, 1);
    expect((r.sweepRad * 180) / Math.PI).toBeGreaterThan(355);
    // A radius below the full-circle floor is clamped up to it (no over-wrap past 360°).
    expect(resolveArc(3000, 100, 1).radiusMm).toBe(minArcRadiusMm(3000));
  });

  it('resolveArc reads the bulge direction from the sweep sign', () => {
    expect(resolveArc(3000, 1000, 90).direction).toBe(1);
    expect(resolveArc(3000, 1000, -90).direction).toBe(-1);
  });

  it('minArcRadiusMm is the full-circle radius (arcLength/2π)', () => {
    expect(minArcRadiusMm(3000)).toBe(Math.ceil(3000 / (2 * Math.PI))); // 478
  });

  it('deriveArcFromRadius keeps the glass length and derives sweep (radius → angle, freely)', () => {
    const arcLength = 3000;
    // A semicircle of 3000mm of glass has radius 3000/π ≈ 955 (NOT 1500 — that was the chord model).
    const half = deriveArcFromRadius(arcLength, 955);
    expect(half.arcLengthMm).toBe(arcLength);
    expect(half.sweepDeg).toBeCloseTo(180, 0);
    // Tighter radius → larger angle, all the way to a full circle, freely settable.
    const tight = deriveArcFromRadius(arcLength, 478);
    expect(tight.sweepDeg).toBeGreaterThan(355);
    // A radius below the full-circle floor clamps up.
    expect(deriveArcFromRadius(arcLength, 100).radiusMm).toBe(minArcRadiusMm(arcLength));
  });

  it('arcLengthFromRadiusSweep recovers the glass length from radius+sweep (migration)', () => {
    // radius·sweep = developed length, for any data (idempotent).
    const r = resolveArc(3000, 955, 1);
    expect(arcLengthFromRadiusSweep(3000, r.radiusMm, (r.sweepRad * 180) / Math.PI)).toBeCloseTo(
      3000,
      -1,
    );
    // Passthrough when there is no arc.
    expect(arcLengthFromRadiusSweep(3000, null, null)).toBe(3000);
  });

  it('arcFromChordKeepingSweep scales the glass length with the chord while holding the curl angle', () => {
    const a = arcFromChordKeepingSweep(2000, 90);
    const b = arcFromChordKeepingSweep(4000, 90);
    // Doubling the chord at the same sweep doubles the radius and the glass length.
    expect(b.geomArcRadiusMm).toBeCloseTo(a.geomArcRadiusMm * 2, -1);
    expect(b.lengthMm).toBeCloseTo(a.lengthMm * 2, -1);
  });

  it('bowToArcKeepingLength holds the glass length fixed and derives the radius from the bow sweep', () => {
    const arcLength = 3000;
    const bow = bowToArcKeepingLength(2000, 0, 600, arcLength);
    expect(bow.lengthMm).toBe(arcLength); // glass length never changes when bowing
    expect(bow.geomArcSweepDeg).not.toBeNull();
    const sweepRad = (Math.abs(bow.geomArcSweepDeg as number) * Math.PI) / 180;
    expect(bow.geomArcRadiusMm).toBeCloseTo(arcLength / sweepRad, -1); // radius = arcLength/sweep
  });

  it('arcFromSweepKeepingLength maps a dragged sweep to radius = arcLength/sweep (1–360°)', () => {
    const arcLength = 3000;
    for (const sweepDeg of [19, 90, 180, 270, 360]) {
      const a = arcFromSweepKeepingLength(arcLength, sweepDeg);
      const sweepRad = (sweepDeg * Math.PI) / 180;
      expect(a.geomArcSweepDeg).toBe(sweepDeg);
      expect(a.geomArcRadiusMm).toBeCloseTo(arcLength / sweepRad, -1);
    }
    // A negligible sweep returns to straight.
    expect(arcFromSweepKeepingLength(arcLength, 0.5).geomArcRadiusMm).toBeNull();
    // Sign is preserved (bulge direction).
    expect(arcFromSweepKeepingLength(arcLength, -90).geomArcSweepDeg).toBe(-90);
  });

  it('sampleArcPlan returns the straight run for ~zero sweep and curls for a real sweep', () => {
    const straight = sampleArcPlan(0, 0, 0, 3000, 0);
    expect(straight).toHaveLength(2);
    expect(straight[1].x).toBeCloseTo(3000, 3);
    expect(straight[1].y).toBeCloseTo(0, 3);

    const curved = sampleArcPlan(0, 0, 0, 3000, 180);
    // Starts at the origin and the developed length (sum of segment chords) ≈ the glass length.
    expect(curved[0].x).toBeCloseTo(0, 3);
    expect(curved[0].y).toBeCloseTo(0, 3);
    let dev = 0;
    for (let i = 1; i < curved.length; i += 1) {
      dev += Math.hypot(curved[i].x - curved[i - 1].x, curved[i].y - curved[i - 1].y);
    }
    expect(dev).toBeCloseTo(3000, -2); // polyline approximation of the 3000mm arc
  });
});
