import { describe, expect, it } from 'vitest';
import { splitPanelsAtLength } from './panelSplit';
import type { ScenePanelState, SceneHardwareItem } from './project.types';

const panel = (
  id: string,
  index: number,
  widthMm: number,
  hardware: SceneHardwareItem[] = [],
): ScenePanelState => ({
  id,
  panelIndex: index,
  widthMm,
  openingType: 'Fixed',
  glassTypeId: 'g',
  hasHandle: false,
  hasLock: false,
  hasBrushSeal: false,
  hardware,
});

const ids = () => {
  let n = 0;
  return () => `new-${(n += 1)}`;
};

describe('splitPanelsAtLength', () => {
  it('splits the single panel spanning the cut, reindexing', () => {
    const result = splitPanelsAtLength([panel('a', 0, 1000)], 400, ids());
    expect(result).not.toBeNull();
    expect(result!.map((p) => p.widthMm)).toEqual([400, 600]);
    expect(result!.map((p) => p.panelIndex)).toEqual([0, 1]);
    expect(result![0].id).toBe('a');
    expect(result![1].id).toBe('new-1');
  });

  it('splits the correct panel in a multi-panel run', () => {
    const result = splitPanelsAtLength([panel('a', 0, 500), panel('b', 1, 500)], 700, ids());
    expect(result!.map((p) => p.widthMm)).toEqual([500, 200, 300]);
    expect(result!.map((p) => p.id)).toEqual(['a', 'b', 'new-1']);
    expect(result!.map((p) => p.panelIndex)).toEqual([0, 1, 2]);
  });

  it('returns null when the cut leaves less than MIN_PANEL_MM on a side', () => {
    expect(splitPanelsAtLength([panel('a', 0, 1000)], 50, ids())).toBeNull();
    expect(splitPanelsAtLength([panel('a', 0, 1000)], 970, ids())).toBeNull();
  });

  it('returns null when the cut is outside all panels', () => {
    expect(splitPanelsAtLength([panel('a', 0, 500)], 900, ids())).toBeNull();
  });

  it('partitions hardware to the correct half and rescales its centre offset', () => {
    const hardwareAt = (id: string, offsetXmm: number): SceneHardwareItem => ({
      id,
      kind: 'Handle',
      colorHex: '#000',
      offsetXmm,
      offsetYmm: 0,
      offsetZmm: 0,
      widthMm: 40,
      heightMm: 120,
      depthMm: 20,
    });
    // 1000mm panel, centre at 0: left fitting 300mm left of centre, right fitting 300mm right.
    const hw = [hardwareAt('left', -300), hardwareAt('right', 300)];
    const result = splitPanelsAtLength([panel('a', 0, 1000, hw)], 500, ids());

    expect(result![0].id).toBe('a');
    expect(result![1].id).toBe('new-1');
    // Left half (0..500, new centre at 250): the left fitting stays, re-homed to -50.
    expect(result![0].hardware.map((h) => h.id)).toEqual(['left']);
    expect(result![0].hardware[0].offsetXmm).toBe(-50);
    // Right half (500..1000, new centre at 750): the right fitting follows it, re-homed to +50.
    expect(result![1].hardware.map((h) => h.id)).toEqual(['right']);
    expect(result![1].hardware[0].offsetXmm).toBe(50);
  });
});
