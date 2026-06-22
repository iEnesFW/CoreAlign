import { describe, expect, it } from 'vitest';
import { distributePanelWidths } from './designerStore';
import { MIN_PANEL_MM } from './panelResize';
import type { ScenePanelState } from './project.types';

const panels = (...widths: number[]): ScenePanelState[] =>
  widths.map(
    (widthMm, i) =>
      ({
        id: `p${i}`,
        runId: 'r',
        panelIndex: i,
        widthMm,
        openingType: 'Fixed',
        glassTypeId: 'g',
        hasHandle: false,
        hasLock: false,
        hasBrushSeal: false,
        notes: null,
      }) as unknown as ScenePanelState,
  );

const widthsOf = (ps: ScenePanelState[]) => ps.map((p) => p.widthMm);
const sum = (ps: ScenePanelState[]) => ps.reduce((s, p) => s + p.widthMm, 0);

describe('distributePanelWidths (MIN floor + sum invariant)', () => {
  it('distributes proportionally and preserves the run length', () => {
    const out = distributePanelWidths(panels(600, 600, 600), 1800);
    expect(sum(out)).toBe(1800);
    expect(widthsOf(out).every((w) => w >= MIN_PANEL_MM)).toBe(true);
  });

  it('never drops a panel below MIN even when the run is too short', () => {
    const out = distributePanelWidths(panels(600, 600, 600, 600, 600), 100);
    expect(widthsOf(out).every((w) => w >= MIN_PANEL_MM)).toBe(true);
    expect(widthsOf(out)).toEqual([100, 100, 100, 100, 100]);
  });

  it('never produces a negative width (count > length corner)', () => {
    const out = distributePanelWidths(panels(100, 100, 100, 100, 100), 5);
    expect(widthsOf(out).every((w) => w >= MIN_PANEL_MM)).toBe(true);
  });

  it('steals from the widest panel so the last panel keeps the minimum', () => {
    const out = distributePanelWidths(panels(1000, 100, 100), 400);
    expect(sum(out)).toBe(400);
    expect(widthsOf(out).every((w) => w >= MIN_PANEL_MM)).toBe(true);
  });

  it('handles a tight length just above count*MIN', () => {
    const out = distributePanelWidths(panels(600, 600, 600), 350);
    expect(sum(out)).toBe(350);
    expect(widthsOf(out).every((w) => w >= MIN_PANEL_MM)).toBe(true);
  });
});
