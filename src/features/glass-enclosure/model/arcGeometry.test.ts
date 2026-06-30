import { describe, expect, it } from 'vitest';
import {
  arcEndLocal,
  arcFromBow,
  arcFromCornerResize,
  bowArcPlanPoints,
  bowFromArc,
  chordFromRadiusSweep,
  deriveArcFromChordSagitta,
  deriveArcFromRadius,
  deriveArcFromSweep,
  isRealArc,
  minArcRadiusMm,
  resolveArc,
} from './arcGeometry';

describe('isRealArc', () => {
  it('requires BOTH a radius and a non-negligible sweep', () => {
    expect(isRealArc(1500, 180)).toBe(true);
    // Half-arc states must NOT count as an arc (else they render as a degenerate flat band).
    expect(isRealArc(1500, null)).toBe(false);
    expect(isRealArc(1500, 0)).toBe(false);
    expect(isRealArc(null, 180)).toBe(false);
    expect(isRealArc(0, 180)).toBe(false);
    // Straight (both absent).
    expect(isRealArc(null, null)).toBe(false);
  });
});

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
  it('resolveArc renders straight from the stored (radius, sweep); the developed length is derived', () => {
    for (const [radius, sweepDeg] of [
      [955, 180],
      [1910, 90],
      [600, 300],
    ]) {
      const r = resolveArc(radius, sweepDeg);
      expect(r.radiusMm).toBe(radius);
      expect((r.sweepRad * 180) / Math.PI).toBeCloseTo(sweepDeg, 4);
      // Developed glass length is derived (radius·sweep), never an input.
      expect(r.arcLengthMm).toBeCloseTo((radius * sweepDeg * Math.PI) / 180, 3);
    }
  });

  it('resolveArc reads the bulge direction from the sweep sign', () => {
    expect(resolveArc(1000, 90).direction).toBe(1);
    expect(resolveArc(1000, -90).direction).toBe(-1);
  });

  it('minArcRadiusMm is the half-circle floor (chord/2)', () => {
    expect(minArcRadiusMm(3000)).toBe(1500);
  });

  it('deriveArcFromRadius keeps the chord and derives the minor sweep (clamped at the half-circle)', () => {
    const chord = 3000;
    // A half-circle of a 3000mm chord has radius 1500 and sweeps 180°.
    const half = deriveArcFromRadius(chord, 1500);
    expect(half.chordMm).toBe(chord);
    expect(half.sweepDeg).toBeCloseTo(180, 0);
    // A larger radius → shallower (smaller) sweep, chord unchanged.
    const shallow = deriveArcFromRadius(chord, 5000);
    expect(shallow.sweepDeg).toBeLessThan(40);
    expect(shallow.chordMm).toBe(chord);
    // A radius below the floor clamps up to chord/2.
    expect(deriveArcFromRadius(chord, 100).radiusMm).toBe(minArcRadiusMm(chord));
  });

  it('deriveArcFromSweep keeps the chord; radius = chord/(2·sin(sweep/2)) for 1–359° (minor+major)', () => {
    const chord = 3000;
    for (const sweepDeg of [19, 90, 180, 270, 359]) {
      const a = deriveArcFromSweep(chord, sweepDeg);
      expect(a.chordMm).toBe(chord);
      const sweepRad = (sweepDeg * Math.PI) / 180;
      expect(a.radiusMm).toBeCloseTo(chord / (2 * Math.sin(sweepRad / 2)), -1);
    }
    // A major (>180°) arc has a LARGER radius than the half-circle minimum, ends still fixed.
    expect(deriveArcFromSweep(chord, 270).radiusMm).toBeGreaterThan(minArcRadiusMm(chord));
    // Sign (bulge direction) is preserved.
    expect(deriveArcFromSweep(chord, -90).sweepDeg).toBeLessThan(0);
  });

  it('arcFromCornerResize keeps the sweep angle and re-derives the radius for the new chord', () => {
    const a = arcFromCornerResize(2000, 90);
    const b = arcFromCornerResize(4000, 90);
    expect(a.lengthMm).toBe(2000); // lengthMm IS the chord
    expect(b.lengthMm).toBe(4000);
    // Same sweep, double the chord → double the radius.
    expect(b.geomArcRadiusMm).toBeCloseTo(a.geomArcRadiusMm * 2, -1);
  });

  it('chordFromRadiusSweep recovers the chord (2·radius·sin(sweep/2)) for migration', () => {
    // Half-circle: chord = 2·1500·sin(90°) = 3000.
    expect(chordFromRadiusSweep(0, 1500, 180)).toBeCloseTo(3000, -1);
    // Passthrough when there is no arc.
    expect(chordFromRadiusSweep(2580, null, null)).toBe(2580);
  });

  it('arcFromBow keeps the chord (lengthMm) FIXED and only bows; straightens below the threshold', () => {
    const chord = 3000;
    const bow = arcFromBow(chord, 0, 900);
    expect(bow.lengthMm).toBe(chord); // chord never changes when bowing
    expect(bow.geomArcRadiusMm).not.toBeNull();
    const straight = arcFromBow(chord, 0, 5);
    expect(straight.geomArcRadiusMm).toBeNull();
    expect(straight.lengthMm).toBe(chord);
  });

  it('the committed arc renders the SAME ends + apex the bow preview draws (preview == result)', () => {
    // The handle previews via bowArcPlanPoints(sagitta) and commits via arcFromBow; the renderer
    // rebuilds the end from (radius, sweep) rotated by rotationDeg. All must agree or the curve
    // would jump on release and the fixed ends would drift.
    const chord = 3000;
    const startX = 1000;
    const startY = 500;
    const chordDeg = 30;
    const sagitta = 800;
    const cd = (chordDeg * Math.PI) / 180;
    const endX = startX + chord * Math.cos(cd);
    const endY = startY + chord * Math.sin(cd);
    const bow = arcFromBow(chord, chordDeg, sagitta);
    const radius = bow.geomArcRadiusMm as number;
    const sweep = bow.geomArcSweepDeg as number;
    const rot = (bow.rotationDeg * Math.PI) / 180;
    const place = (x: number, y: number) => ({
      x: startX + x * Math.cos(rot) - y * Math.sin(rot),
      y: startY + x * Math.sin(rot) + y * Math.cos(rot),
    });
    // The rendered end lands on the FIXED chord endpoint, within the ~1mm radius/angle rounding
    // (NOT drifting by the sagitta, which is what a sign/side error would do).
    const e = arcEndLocal(radius, sweep);
    const renderedEnd = place(e.xMm, e.yMm);
    expect(renderedEnd.x).toBeCloseTo(endX, -1);
    expect(renderedEnd.y).toBeCloseTo(endY, -1);
    // The rendered apex (at sweep/2) matches the preview apex (same side, same depth).
    const eApex = arcEndLocal(radius, sweep / 2);
    const renderedApex = place(eApex.xMm, eApex.yMm);
    const preview = bowArcPlanPoints(startX, startY, endX, endY, sagitta);
    const apexPreview = preview[Math.floor(preview.length / 2)];
    expect(renderedApex.x).toBeCloseTo(apexPreview.x, -1);
    expect(renderedApex.y).toBeCloseTo(apexPreview.y, -1);
  });
});
