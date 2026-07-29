import { describe, expect, it } from 'vitest';
import { arcEndLocal } from '../model/arcGeometry';
import { arcCommitKeepingEnds, curvedPlacementArc } from './arcCommit';
import { chordDirectionDeg, resolveBodyCurvature } from './curvature';
import type { CurvablePose } from './curvature';
import type { ArcCommitInput } from './arcCommit';

const pose = (patch: Partial<CurvablePose> = {}): CurvablePose => ({
  lengthMm: 3000,
  rotationDeg: 0,
  geomArcRadiusMm: null,
  geomArcSweepDeg: null,
  ...patch,
});

const apply = (
  body: CurvablePose,
  input: ArcCommitInput,
  options?: Parameters<typeof arcCommitKeepingEnds>[2],
): CurvablePose => {
  const { patch } = arcCommitKeepingEnds(body, input, options);
  if (!patch) return body;
  return {
    lengthMm: patch.lengthMm ?? body.lengthMm,
    rotationDeg: patch.rotationDeg,
    geomArcRadiusMm: patch.geomArcRadiusMm,
    geomArcSweepDeg: patch.geomArcSweepDeg,
  };
};

/**
 * Both endpoints of a body in its own plan frame, origin at (0,0) — measured exactly the way the
 * renderer and the collision footprint measure them, i.e. through the chord-derived radius. Reading
 * the raw stored radius here would measure a body nobody draws.
 */
const endpoints = (body: CurvablePose) => {
  const rad = (body.rotationDeg * Math.PI) / 180;
  const arc = resolveBodyCurvature(body);
  if (!arc) {
    return {
      start: { x: 0, y: 0 },
      end: { x: body.lengthMm * Math.cos(rad), y: body.lengthMm * Math.sin(rad) },
    };
  }
  const local = arcEndLocal(arc.radiusMm, body.geomArcSweepDeg ?? 0);
  return {
    start: { x: 0, y: 0 },
    end: {
      x: local.xMm * Math.cos(rad) - local.yMm * Math.sin(rad),
      y: local.xMm * Math.sin(rad) + local.yMm * Math.cos(rad),
    },
  };
};

const endDrift = (before: CurvablePose, after: CurvablePose) => {
  const a = endpoints(before).end;
  const b = endpoints(after).end;
  return Math.hypot(a.x - b.x, a.y - b.y);
};

describe('arcCommitKeepingEnds pins both endpoints', () => {
  const cases: { name: string; from: CurvablePose; input: ArcCommitInput }[] = [
    { name: 'straight -> sweep', from: pose(), input: { kind: 'sweep', sweepDeg: 60 } },
    { name: 'straight -> radius', from: pose(), input: { kind: 'radius', radiusMm: 2500 } },
    { name: 'straight -> bow', from: pose(), input: { kind: 'bow', sagittaMm: 400 } },
    {
      name: 'straight -> field survey (same chord)',
      from: pose(),
      input: { kind: 'chordSagitta', chordMm: 3000, sagittaMm: 400 },
    },
    {
      name: 'arc -> deeper sweep',
      from: pose({ geomArcRadiusMm: 3000, geomArcSweepDeg: 60, rotationDeg: -30 }),
      input: { kind: 'sweep', sweepDeg: 120 },
    },
    {
      name: 'arc -> shallower sweep',
      from: pose({ geomArcRadiusMm: 3000, geomArcSweepDeg: 120, rotationDeg: -60 }),
      input: { kind: 'sweep', sweepDeg: 20 },
    },
    {
      name: 'arc -> new radius',
      from: pose({ geomArcRadiusMm: 3000, geomArcSweepDeg: 60, rotationDeg: -30 }),
      input: { kind: 'radius', radiusMm: 1800 },
    },
    {
      name: 'arc -> rebow',
      from: pose({ geomArcRadiusMm: 3000, geomArcSweepDeg: 60, rotationDeg: -30 }),
      input: { kind: 'bow', sagittaMm: 900 },
    },
    {
      name: 'arc -> flip',
      from: pose({ geomArcRadiusMm: 3000, geomArcSweepDeg: 60, rotationDeg: -30 }),
      input: { kind: 'flip' },
    },
    {
      name: 'arc -> straighten',
      from: pose({ geomArcRadiusMm: 3000, geomArcSweepDeg: 60, rotationDeg: -30 }),
      input: { kind: 'straighten' },
    },
    {
      name: 'rotated arc -> sweep',
      from: pose({ rotationDeg: 37 - 30, geomArcRadiusMm: 3000, geomArcSweepDeg: 60 }),
      input: { kind: 'sweep', sweepDeg: 95 },
    },
  ];

  it.each(cases)('$name keeps the far end within 1 mm', ({ from, input }) => {
    const after = apply(from, input);
    expect(endDrift(from, after)).toBeLessThanOrEqual(1);
  });

  it.each(cases)('$name keeps the chord direction', ({ from, input }) => {
    const after = apply(from, input);
    const delta = Math.abs(chordDirectionDeg(after) - chordDirectionDeg(from));
    expect(Math.min(delta, 360 - delta)).toBeLessThanOrEqual(0.02);
  });

  it.each(cases)('$name keeps the chord length', ({ from, input }) => {
    const after = apply(from, input);
    expect(Math.abs(after.lengthMm - from.lengthMm)).toBeLessThanOrEqual(1);
  });
});

describe('arcCommitKeepingEnds — the S5 regression', () => {
  it('the OLD behaviour (write sweep, leave rotationDeg) swings the far end by metres', () => {
    const before = pose();
    // Reproduce the defect: radius+sweep written, rotationDeg untouched.
    const naive: CurvablePose = { ...before, geomArcRadiusMm: 3000, geomArcSweepDeg: 60 };
    expect(endDrift(before, naive)).toBeGreaterThan(700);

    // The single writer keeps the same curvature but re-rolls the pose.
    const fixed = apply(before, { kind: 'sweep', sweepDeg: 60 });
    expect(fixed.geomArcSweepDeg).toBeCloseTo(60, 1);
    expect(endDrift(before, fixed)).toBeLessThanOrEqual(1);
  });

  it('flipping mirrors the bulge without moving either end', () => {
    const before = pose({ geomArcRadiusMm: 3000, geomArcSweepDeg: 60, rotationDeg: -30 });
    const after = apply(before, { kind: 'flip' });
    expect(after.geomArcSweepDeg).toBe(-60);
    expect(endDrift(before, after)).toBeLessThanOrEqual(1);
  });

  it('repeated no-op sweep commits never drift', () => {
    let body = pose();
    body = apply(body, { kind: 'sweep', sweepDeg: 60 });
    const first = { ...body };
    for (let i = 0; i < 25; i += 1) {
      body = apply(body, { kind: 'sweep', sweepDeg: 60 });
    }
    expect(body.lengthMm).toBe(first.lengthMm);
    expect(body.rotationDeg).toBeCloseTo(first.rotationDeg, 6);
    expect(body.geomArcSweepDeg).toBeCloseTo(first.geomArcSweepDeg ?? 0, 6);
    expect(endDrift(first, body)).toBeLessThanOrEqual(0.01);
  });
});

describe('arcCommitKeepingEnds guards', () => {
  it('refuses a radius the server would reject instead of applying it', () => {
    // For a FIXED chord the radius is smallest at a half-circle (chord/2) and grows again past it,
    // so a 150 mm chord only dips under the 100 mm server floor around 180°.
    const result = arcCommitKeepingEnds(pose({ lengthMm: 150 }), { kind: 'sweep', sweepDeg: 180 });
    expect(result.patch).toBeNull();
    expect(result.rejection).toBe('radiusTooSmall');
    expect(result.radiusMm).toBeLessThan(100);
  });

  it('heals a legacy row whose stored radius disagrees with its chord', () => {
    // chord 3000 with R3000/120° is impossible (that triple implies a 5196 mm chord). The commit
    // must re-derive from the authoritative chord rather than preserve the impossible radius.
    const drifted = pose({ lengthMm: 3000, geomArcRadiusMm: 3000, geomArcSweepDeg: 120 });
    const after = apply(drifted, { kind: 'sweep', sweepDeg: 120 });
    const arc = resolveBodyCurvature(after);
    const chord = 2 * (arc?.radiusMm ?? 0) * Math.sin((arc?.sweepRad ?? 0) / 2);
    expect(Math.abs(chord - 3000)).toBeLessThanOrEqual(1);
    expect(after.geomArcRadiusMm).toBeCloseTo(1732, -1);
  });

  it('a negligible bow straightens instead of storing a phantom stub', () => {
    const before = pose({ geomArcRadiusMm: 3000, geomArcSweepDeg: 60, rotationDeg: -30 });
    const after = apply(before, { kind: 'bow', sagittaMm: 0.4 });
    expect(after.geomArcRadiusMm).toBeNull();
    expect(after.geomArcSweepDeg).toBeNull();
    expect(after.rotationDeg).toBeCloseTo(chordDirectionDeg(before), 2);
  });

  it('flip on a straight body is a no-op, not a phantom arc', () => {
    const result = arcCommitKeepingEnds(pose(), { kind: 'flip' });
    expect(result.patch).toBeNull();
    expect(result.rejection).toBe('notCurved');
  });

  it('a bow shallower than the deadzone straightens instead of storing a sliver', () => {
    const before = pose();
    const after = apply(before, { kind: 'bow', sagittaMm: 20 });
    expect(after.geomArcRadiusMm).toBeNull();
    expect(after.geomArcSweepDeg).toBeNull();

    // Past the deadzone the same drag DOES curve — the guard is a deadzone, not a ceiling.
    const curved = apply(before, { kind: 'bow', sagittaMm: 30 });
    expect(curved.geomArcSweepDeg).not.toBeNull();
  });

  it('the resolved radius reproduces the stored chord', () => {
    const body = apply(pose({ lengthMm: 4000 }), { kind: 'sweep', sweepDeg: 75 });
    const arc = resolveBodyCurvature(body);
    expect(arc).not.toBeNull();
    const chord = 2 * (arc?.radiusMm ?? 0) * Math.sin((arc?.sweepRad ?? 0) / 2);
    expect(Math.abs(chord - body.lengthMm)).toBeLessThanOrEqual(1);
  });
});

describe('end-handle resize (chordResize) keeps the curl angle', () => {
  it('scales the chord and re-derives the radius, sweep untouched', () => {
    const before = pose({ lengthMm: 3000, geomArcRadiusMm: 3000, geomArcSweepDeg: 60 });
    const after = apply(before, { kind: 'chordResize', chordMm: 4500 });
    expect(after.lengthMm).toBe(4500);
    expect(after.geomArcSweepDeg).toBeCloseTo(60, 2);
    // radius = chord / (2·sin(sweep/2)) = 4500 / (2·sin30°) = 4500
    expect(after.geomArcRadiusMm).toBeCloseTo(4500, -1);
    expect(chordDirectionDeg(after)).toBeCloseTo(chordDirectionDeg(before), 2);
  });

  it('a straight body just takes the new length', () => {
    const after = apply(pose({ rotationDeg: 20 }), { kind: 'chordResize', chordMm: 5000 });
    expect(after.lengthMm).toBe(5000);
    expect(after.geomArcRadiusMm).toBeNull();
    expect(after.rotationDeg).toBeCloseTo(20, 2);
  });

  it('refuses a shrink that would drive the radius under the server floor', () => {
    const tight = pose({ lengthMm: 3000, geomArcRadiusMm: 1500, geomArcSweepDeg: 180 });
    const result = arcCommitKeepingEnds(tight, { kind: 'chordResize', chordMm: 150 });
    expect(result.patch).toBeNull();
    expect(result.rejection).toBe('radiusTooSmall');
  });
});

describe('curvedPlacementArc births an arc on the dragged chord', () => {
  it('lands the far end where the straight ghost ended', () => {
    const chordMm = 4000;
    const chordDeg = 25;
    const arc = curvedPlacementArc(chordMm, chordDeg);
    expect(arc).not.toBeNull();
    const born: CurvablePose = {
      lengthMm: arc?.lengthMm ?? chordMm,
      rotationDeg: arc?.rotationDeg ?? chordDeg,
      geomArcRadiusMm: arc?.geomArcRadiusMm ?? null,
      geomArcSweepDeg: arc?.geomArcSweepDeg ?? null,
    };
    const straightGhost = pose({ lengthMm: chordMm, rotationDeg: chordDeg });
    expect(endDrift(straightGhost, born)).toBeLessThanOrEqual(1);
    expect(chordDirectionDeg(born)).toBeCloseTo(chordDeg, 2);
  });

  it('the OLD placement (raw drag angle as rotationDeg) missed the ghost by metres', () => {
    const chordMm = 4000;
    const chordDeg = 25;
    const naive = pose({
      lengthMm: chordMm,
      rotationDeg: chordDeg,
      geomArcRadiusMm: chordMm,
      geomArcSweepDeg: 60,
    });
    expect(endDrift(pose({ lengthMm: chordMm, rotationDeg: chordDeg }), naive)).toBeGreaterThan(
      700,
    );
  });

  it('the stored radius reproduces the dragged chord', () => {
    const arc = curvedPlacementArc(4000, 0);
    const resolved = resolveBodyCurvature({
      lengthMm: arc?.lengthMm ?? 0,
      geomArcRadiusMm: arc?.geomArcRadiusMm ?? null,
      geomArcSweepDeg: arc?.geomArcSweepDeg ?? null,
    });
    const chord = 2 * (resolved?.radiusMm ?? 0) * Math.sin((resolved?.sweepRad ?? 0) / 2);
    expect(Math.abs(chord - 4000)).toBeLessThanOrEqual(1);
  });
});

describe('symmetric pose (slabs) never rolls rotationDeg', () => {
  const opts = { pose: 'symmetric' as const };

  it('bowing a slab leaves the axis direction alone', () => {
    const before = pose({ lengthMm: 4000, rotationDeg: 33 });
    const after = apply(before, { kind: 'bow', sagittaMm: 300 }, opts);
    expect(after.rotationDeg).toBe(33);
    expect(after.geomArcSweepDeg).toBeLessThan(0);
    expect(after.geomArcRadiusMm).not.toBeNull();
  });

  it('re-bowing an already curved slab still leaves the axis alone', () => {
    const before = pose({
      lengthMm: 4000,
      rotationDeg: 33,
      geomArcRadiusMm: 3000,
      geomArcSweepDeg: -80,
    });
    const after = apply(before, { kind: 'bow', sagittaMm: 900 }, opts);
    expect(after.rotationDeg).toBe(33);
  });

  it('straightening a slab leaves the axis alone', () => {
    const before = pose({
      lengthMm: 4000,
      rotationDeg: 33,
      geomArcRadiusMm: 3000,
      geomArcSweepDeg: -80,
    });
    const after = apply(before, { kind: 'straighten' }, opts);
    expect(after.rotationDeg).toBe(33);
    expect(after.geomArcSweepDeg).toBeNull();
  });

  it('the rolled convention WOULD have moved it — proving the option matters', () => {
    const before = pose({ lengthMm: 4000, rotationDeg: 33 });
    const rolled = apply(before, { kind: 'bow', sagittaMm: 300 });
    expect(Math.abs(rolled.rotationDeg - 33)).toBeGreaterThan(1);
  });

  it('the slab sign contract holds: +sagitta -> negative sweep', () => {
    const plus = apply(pose({ lengthMm: 4000 }), { kind: 'bow', sagittaMm: 300 }, opts);
    const minus = apply(pose({ lengthMm: 4000 }), { kind: 'bow', sagittaMm: -300 }, opts);
    expect(plus.geomArcSweepDeg ?? 0).toBeLessThan(0);
    expect(minus.geomArcSweepDeg ?? 0).toBeGreaterThan(0);
    expect(plus.geomArcRadiusMm).toBe(minus.geomArcRadiusMm);
  });
});
