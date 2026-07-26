import { describe, expect, it } from 'vitest';
import { curvedWallPickUv, curvedWallSurfacePoint } from './curvedExtrude';
import { radiusFromChordSweep, resolveArc } from '../../model/arcGeometry';
import { arcCommitKeepingEnds } from '../../geometry/arcCommit';
import type { CurvablePose } from '../../geometry/curvature';

/**
 * S2 — "free-draw on a wall makes a distorted, unrelated hole".
 *
 * A curved wall has ONE surface. The draw pick inverts it, the clamp bounds it, the front/back test
 * splits it and the CSG cutter carves it. If any of them resolves the radius differently, the stroke
 * is sampled in one parameterisation and carved in another, and the hole lands somewhere else —
 * stretched non-linearly along the wall.
 */

// The frame every consumer must use: the radius re-derived from the AUTHORITATIVE chord + sweep.
const surfaceArc = (wall: CurvablePose) =>
  resolveArc(
    radiusFromChordSweep(wall.lengthMm, wall.geomArcRadiusMm, wall.geomArcSweepDeg),
    wall.geomArcSweepDeg ?? 1,
  );

// The frame the pick USED to build: the raw stored radius.
const rawArc = (wall: CurvablePose) =>
  resolveArc(wall.geomArcRadiusMm ?? 0, wall.geomArcSweepDeg ?? 1);

const THICKNESS_M = 0.2;

// Round-trip a point at `frac` along the RENDERED surface through a pick built on `pickArc`.
const pickDriftMm = (
  wall: CurvablePose,
  frac: number,
  pick: ReturnType<typeof resolveArc>,
): number => {
  const render = surfaceArc(wall);
  const uTrue = frac * render.arcLengthMm;
  const p = curvedWallSurfacePoint(
    uTrue,
    1000,
    render.radiusM,
    render.radiusM + THICKNESS_M / 2,
    render.direction,
    render.sweepRad,
    render.arcLengthMm,
  );
  const uv = curvedWallPickUv(
    p[0],
    p[1],
    p[2],
    pick.radiusM,
    pick.direction,
    pick.sweepRad,
    pick.arcLengthMm,
  );
  return Math.abs(uv.u - uTrue);
};

const FRACTIONS = [0, 0.25, 0.5, 0.75, 1];

describe('curved wall surface parameterisation is single-valued', () => {
  const cases: { name: string; wall: CurvablePose }[] = [
    {
      name: 'freshly placed 60° wall',
      wall: { lengthMm: 4000, rotationDeg: 30, geomArcRadiusMm: 4000, geomArcSweepDeg: 60 },
    },
    {
      name: '90° wall',
      wall: { lengthMm: 4000, rotationDeg: 45, geomArcRadiusMm: 2828, geomArcSweepDeg: 90 },
    },
    {
      name: 'chord edited, radius left stale (the S2 row)',
      wall: { lengthMm: 4500, rotationDeg: 30, geomArcRadiusMm: 3000, geomArcSweepDeg: 60 },
    },
    {
      name: 'legacy row whose triple is impossible',
      wall: { lengthMm: 3000, rotationDeg: -60, geomArcRadiusMm: 3000, geomArcSweepDeg: 120 },
    },
    {
      name: 'negative sweep (mirrored bulge)',
      wall: { lengthMm: 4000, rotationDeg: 30, geomArcRadiusMm: 4000, geomArcSweepDeg: -60 },
    },
  ];

  it.each(cases)('$name: the pick lands under the cursor within 1 mm', ({ wall }) => {
    for (const frac of FRACTIONS) {
      expect(pickDriftMm(wall, frac, surfaceArc(wall))).toBeLessThanOrEqual(1);
    }
  });

  it('the OLD raw-radius pick drifts by hundreds of mm on a stale-radius wall', () => {
    const wall: CurvablePose = {
      lengthMm: 4500,
      rotationDeg: 30,
      geomArcRadiusMm: 3000,
      geomArcSweepDeg: 60,
    };
    // Non-linear along the wall — the start is exact, the far end is metres off. That skew is what
    // turned a symmetric free-drawn arch into a lopsided, unrelated hole.
    expect(pickDriftMm(wall, 0, rawArc(wall))).toBeLessThanOrEqual(1);
    expect(pickDriftMm(wall, 1, rawArc(wall))).toBeGreaterThan(500);
    // The shared frame fixes the SAME row without touching the data.
    expect(pickDriftMm(wall, 1, surfaceArc(wall))).toBeLessThanOrEqual(1);
  });

  it('the clamp bound and the carve length are the same developed length', () => {
    for (const { wall } of cases) {
      const render = surfaceArc(wall);
      // Both the draw clamp and applyCurvedWallFeatures take resolved.arcLengthMm; a mismatch here
      // rescales every stored outline into the wrong units.
      expect(render.arcLengthMm).toBeCloseTo(render.radiusMm * render.sweepRad, 6);
    }
  });
});

describe('a wall length edit keeps the row self-consistent', () => {
  it('a keep-sweep chord resize re-derives the radius, so the pick stays exact', () => {
    const wall: CurvablePose = {
      lengthMm: 3000,
      rotationDeg: 30,
      geomArcRadiusMm: 3000,
      geomArcSweepDeg: 60,
    };
    // Red-before: writing lengthMm alone (what the wall inspector and transform toolbar did).
    const naive: CurvablePose = { ...wall, lengthMm: 4500 };
    expect(pickDriftMm(naive, 1, rawArc(naive))).toBeGreaterThan(500);

    // The single writer re-derives the radius for the new chord.
    const { patch } = arcCommitKeepingEnds(wall, { kind: 'chordResize', chordMm: 4500 });
    expect(patch).not.toBeNull();
    const resized: CurvablePose = {
      lengthMm: patch?.lengthMm ?? 0,
      rotationDeg: patch?.rotationDeg ?? 0,
      geomArcRadiusMm: patch?.geomArcRadiusMm ?? null,
      geomArcSweepDeg: patch?.geomArcSweepDeg ?? null,
    };
    expect(resized.geomArcRadiusMm).toBeCloseTo(4500, -1);
    for (const frac of FRACTIONS) {
      expect(pickDriftMm(resized, frac, rawArc(resized))).toBeLessThanOrEqual(1);
    }
  });
});
