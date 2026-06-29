import { describe, expect, it } from 'vitest';
import {
  arcChordFromRadiusSweep,
  arcFromBow,
  bowArcPlanPoints,
  bowFromArc,
  deriveArcFromChordSagitta,
  deriveArcFromRadius,
  minArcRadiusMm,
  resolveArc,
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

describe('chord-invariant arc model', () => {
  it('arcFromBow keeps lengthMm = the CHORD (never the developed arc length)', () => {
    // The old bug ballooned a 3000mm chord to its ~7268mm developed length.
    const bow = arcFromBow(3000, 0, 600);
    expect(bow.lengthMm).toBe(3000);
    expect(bow.geomArcRadiusMm).toBeGreaterThan(0);
    // The developed length is reported separately and IS larger than the chord for a real bow.
    expect(bow.arcLengthMm).toBeGreaterThan(3000);
  });

  it('arcFromBow returns a straight result (chord preserved) under the straighten threshold', () => {
    const bow = arcFromBow(3000, 45, 5);
    expect(bow.geomArcRadiusMm).toBeNull();
    expect(bow.geomArcSweepDeg).toBeNull();
    expect(bow.lengthMm).toBe(3000);
  });

  it('resolveArc derives a radius+arc-length whose chord equals the input chord', () => {
    const chord = 3000;
    for (const sweepDeg of [30, 90, 180, 300]) {
      const r = resolveArc(chord, sweepDeg);
      const renderedChord = 2 * r.radiusMm * Math.sin(r.sweepRad / 2);
      expect(renderedChord).toBeCloseTo(chord, 3);
      expect(r.arcLengthMm).toBeCloseTo(r.radiusMm * r.sweepRad, 3);
    }
  });

  it('resolveArc reads the bulge direction from the sweep sign', () => {
    expect(resolveArc(3000, 90).direction).toBe(1);
    expect(resolveArc(3000, -90).direction).toBe(-1);
  });

  it('arcChordFromRadiusSweep recovers the chord from radius+sweep (legacy migration)', () => {
    // A legacy arc stored radius 12047 + sweep 345.7° (a near-full circle of a 3000mm chord).
    expect(arcChordFromRadiusSweep(72685, 12047, 345.7)).toBeCloseTo(3000, -1);
    // Idempotent for chord-invariant data: feeding the already-correct chord round-trips.
    const r = resolveArc(3000, 120);
    expect(arcChordFromRadiusSweep(3000, r.radiusMm, 120)).toBe(3000);
    // Passthrough when there is no arc.
    expect(arcChordFromRadiusSweep(3000, null, null)).toBe(3000);
  });

  it('deriveArcFromRadius treats its first arg as the chord and floors radius at chord/2', () => {
    const d = deriveArcFromRadius(3000, 1800);
    expect(d.chordMm).toBe(3000);
    expect(d.radiusMm).toBeGreaterThanOrEqual(1500);
    expect(d.sweepDeg).toBeLessThanOrEqual(180);
    // A radius below the half-circle floor is clamped up to chord/2.
    expect(deriveArcFromRadius(3000, 100).radiusMm).toBe(1500);
  });

  it('minArcRadiusMm is the half-circle radius (chord/2)', () => {
    expect(minArcRadiusMm(3000)).toBe(1500);
  });
});
