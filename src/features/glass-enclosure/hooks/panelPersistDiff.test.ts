import { describe, expect, it } from 'vitest';
import { panelDimsChanged, snapshotPanelDims } from './useDesignerEntityActions';
import type { ScenePanelState } from '../model/project.types';

const panel = (over: Partial<ScenePanelState> = {}): ScenePanelState => ({
  id: 'p1',
  panelIndex: 0,
  widthMm: 1000,
  openingType: 'Fixed',
  glassTypeId: 'g1',
  hasHandle: false,
  hasLock: false,
  hasBrushSeal: false,
  hardware: [],
  heightMm: null,
  shapePointsJson: null,
  ...over,
});

const snapshotOf = (p: ScenePanelState) => snapshotPanelDims({ panels: [p] }).get(p.id)!;

describe('a run edit persists every panel the store re-fitted', () => {
  it('sees a width redistribution', () => {
    const before = snapshotOf(panel());

    expect(panelDimsChanged(before, panel({ widthMm: 1200 }))).toBe(true);
  });

  // The width-only comparison two call sites used missed this: shortening a run re-fits a taller
  // panel override, and the server kept the old pane for the cut list.
  it('sees a height override re-fit', () => {
    const before = snapshotOf(panel({ heightMm: 2200 }));

    expect(panelDimsChanged(before, panel({ heightMm: 1200 }))).toBe(true);
  });

  // A shaped pane inherits the run height, so a run-height change re-fits the OUTLINE while the
  // panel's own width/height fields never move — a dimension-only diff never notices.
  it('sees a silhouette re-fit that leaves the dimensions untouched', () => {
    const before = snapshotOf(panel({ shapePointsJson: '[[0,0],[1000,0],[1000,2400]]' }));

    expect(
      panelDimsChanged(before, panel({ shapePointsJson: '[[0,0],[1000,0],[1000,1200]]' })),
    ).toBe(true);
  });

  it('sees a silhouette being cleared', () => {
    const before = snapshotOf(panel({ shapePointsJson: '[[0,0],[1000,0],[1000,2400]]' }));

    expect(panelDimsChanged(before, panel({ shapePointsJson: null }))).toBe(true);
  });

  it('stays quiet when nothing dimensional moved', () => {
    const before = snapshotOf(panel({ heightMm: 2200, shapePointsJson: '[[0,0]]' }));

    expect(
      panelDimsChanged(
        before,
        panel({ heightMm: 2200, shapePointsJson: '[[0,0]]', hasHandle: true }),
      ),
    ).toBe(false);
  });

  it('treats an absent height the same as null', () => {
    const before = snapshotOf(panel({ heightMm: null }));
    const { heightMm: _omitted, ...withoutHeight } = panel();

    expect(panelDimsChanged(before, withoutHeight as ScenePanelState)).toBe(false);
  });

  it('snapshots every panel of the run by id', () => {
    const snapshot = snapshotPanelDims({
      panels: [panel(), panel({ id: 'p2', widthMm: 800, heightMm: 1800 })],
    });

    expect([...snapshot.keys()]).toEqual(['p1', 'p2']);
    expect(snapshot.get('p2')).toEqual({ widthMm: 800, heightMm: 1800, shapePointsJson: null });
  });

  it('treats an unknown run as an empty snapshot', () => {
    expect(snapshotPanelDims(undefined).size).toBe(0);
  });
});
