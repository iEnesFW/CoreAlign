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

describe('redistribution re-fits a shaped pane into its new box', () => {
  const fullBox = (halfW: number, h: number) =>
    JSON.stringify([
      { x: -halfW, y: 0 },
      { x: halfW, y: 0 },
      { x: halfW, y: h },
      { x: -halfW, y: h },
    ]);

  it('shrinking a single shaped pane clamps its outline — the persist stays box-valid', () => {
    // A shaped pane only exists on a single-panel run (multi-panel strips shapes), and its
    // outline spans the old box; without the refit, the new width persists against the old
    // silhouette and the server-side box validator refuses the whole panel update.
    const shaped = {
      ...panel('p1', 3000),
      shapeKind: 'polygon',
      shapePointsJson: fullBox(1500, 2400),
    } as ScenePanelState;
    const next = withClampedRunLength(run({ panels: [shaped] }), 2000);
    const points = JSON.parse(next.panels[0].shapePointsJson ?? '[]') as {
      x: number;
      y: number;
    }[];
    expect(next.panels[0].widthMm).toBe(2000);
    expect(points.length).toBeGreaterThanOrEqual(3);
    for (const p of points) expect(Math.abs(p.x)).toBeLessThanOrEqual(1000);
  });

  it('a growing box leaves the stored outline untouched (no churn)', () => {
    const shaped = {
      ...panel('p1', 3000),
      shapeKind: 'polygon',
      shapePointsJson: fullBox(1500, 2400),
    } as ScenePanelState;
    const next = withClampedRunLength(run({ panels: [shaped] }), 4000);
    expect(next.panels[0].shapePointsJson).toBe(shaped.shapePointsJson);
  });
});
