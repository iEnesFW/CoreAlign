import { describe, expect, it } from 'vitest';
import { runPanelTargetMm, withClampedRunLength } from './runPanelSpan';
import type { ScenePanelState, SceneRunState } from './project.types';

/**
 * Σ panel widths is the DEVELOPED glass length (radius·sweep on an arc), never the chord. This
 * used to have two writers — the store split over the developed length while the multi-align
 * toolbar split over the chord and then persisted its result, so equalising the length of an arc
 * run shipped panels several percent short to the cutting list.
 */

const panel = (id: string, widthMm: number): ScenePanelState =>
  ({ id, widthMm, panelIndex: 0 }) as ScenePanelState;

const run = (over: Partial<SceneRunState> = {}): SceneRunState =>
  ({
    id: 'r1',
    label: 'R',
    originX: 0,
    originY: 0,
    rotationDeg: 0,
    lengthMm: 3000,
    heightMm: 2400,
    orderIndex: 0,
    panels: [panel('p1', 1000), panel('p2', 1000), panel('p3', 1000)],
    ...over,
  }) as SceneRunState;

const sum = (r: SceneRunState) => r.panels.reduce((acc, p) => acc + p.widthMm, 0);

describe('withClampedRunLength — panels follow the developed length', () => {
  it('a straight run splits the panels over the length itself', () => {
    const next = withClampedRunLength(run(), 2400);
    expect(next.lengthMm).toBe(2400);
    expect(sum(next)).toBe(2400);
  });

  it('an ARC run splits the panels over radius·sweep, not the chord', () => {
    // R2000, 90° → developed = 2000 * π/2 ≈ 3142, chord = 2·2000·sin(45°) ≈ 2828.
    const arc = run({ geomArcRadiusMm: 2000, geomArcSweepDeg: 90 });
    const next = withClampedRunLength(arc, 2828);

    expect(next.lengthMm).toBe(2828);
    const developed = Math.round((2000 * Math.PI) / 2);
    expect(sum(next)).toBeCloseTo(developed, -1);
    // The chord split is what the second writer used to produce — it must NOT come out here.
    expect(sum(next)).not.toBe(2828);
  });

  it('the panel target never drops below the per-panel floor', () => {
    const tiny = withClampedRunLength(run(), 1);
    expect(sum(tiny)).toBe(runPanelTargetMm(tiny));
    expect(Math.min(...tiny.panels.map((p) => p.widthMm))).toBeGreaterThan(0);
  });

  it('an arc chord may sit below count × MIN — only the developed span bounds the panels', () => {
    // A tight arc legitimately carries more glass than its chord suggests; clamping the CHORD by
    // the panel count would corrupt chord = 2r·sin(sweep/2) on every commit.
    const arc = run({ geomArcRadiusMm: 500, geomArcSweepDeg: 300 });
    const next = withClampedRunLength(arc, 250);
    expect(next.lengthMm).toBe(250);
    expect(sum(next)).toBeGreaterThan(250);
  });
});
