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

  it('gives the new right half a fresh id and no hardware', () => {
    const hw = [
      {
        id: 'h',
        kind: 'Handle',
        colorHex: '#000',
        offsetXmm: 0,
        offsetYmm: 0,
        offsetZmm: 0,
        widthMm: 40,
        heightMm: 120,
        depthMm: 20,
      } as SceneHardwareItem,
    ];
    const result = splitPanelsAtLength([panel('a', 0, 1000, hw)], 500, ids());
    expect(result![0].hardware).toBe(hw);
    expect(result![1].hardware).toEqual([]);
    expect(result![1].id).toBe('new-1');
  });
});
