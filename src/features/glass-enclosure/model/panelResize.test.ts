import { describe, expect, it } from 'vitest';
import { MIN_PANEL_MM, cascadePanelWidths } from './panelResize';

const sum = (a: number[]) => a.reduce((s, n) => s + n, 0);

describe('cascadePanelWidths', () => {
  it('grows panel and shrinks the immediate neighbour (simple pair)', () => {
    const out = cascadePanelWidths([600, 600, 600], 0, 1, 100);
    expect(out).toEqual([700, 500, 600]);
    expect(sum(out)).toBe(1800);
  });

  it('cascades the shrink into further panels once the neighbour hits min', () => {
    const out = cascadePanelWidths([600, 600, 600], 0, 1, 600);
    expect(out).toEqual([1200, 100, 500]);
    expect(sum(out)).toBe(1800);
  });

  it('dead-stops when the whole shrink chain is already at min (still zero-sum)', () => {
    const out = cascadePanelWidths([400, 100, 100], 0, 1, 300);
    expect(out).toEqual([400, 100, 100]);
  });

  it('grows partially up to the chain capacity', () => {
    const out = cascadePanelWidths([600, 300, 300], 0, 1, 1000);
    expect(out).toEqual([1000, 100, 100]);
    expect(sum(out)).toBe(1200);
  });

  it('cascades leftward when the neighbour is before the panel', () => {
    const out = cascadePanelWidths([600, 600, 600], 2, 1, 100);
    expect(out).toEqual([600, 500, 700]);
  });

  it('shrinking a panel hands the freed width to its neighbour', () => {
    const out = cascadePanelWidths([600, 600, 600], 0, 1, -100);
    expect(out).toEqual([500, 700, 600]);
  });

  it('shrink is clamped at the panel min', () => {
    const out = cascadePanelWidths([150, 600, 600], 0, 1, -200);
    expect(out).toEqual([MIN_PANEL_MM, 650, 600]);
  });

  it('is a no-op for zero delta or invalid indices', () => {
    expect(cascadePanelWidths([600, 600], 0, 1, 0)).toEqual([600, 600]);
    expect(cascadePanelWidths([600, 600], 0, 5, 100)).toEqual([600, 600]);
    expect(cascadePanelWidths([600, 600], 1, 1, 100)).toEqual([600, 600]);
  });
});
