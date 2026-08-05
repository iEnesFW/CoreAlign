import { describe, expect, it, beforeEach } from 'vitest';
import { useDesignerStore } from './designerStore';
import type { SceneWallState } from './project.types';

/**
 * `applyScenePatch` used to hand its updater a `structuredClone` of the whole scene, which minted a
 * fresh identity for EVERY nested collection. WallObject's geometry memo keyed on those references,
 * so a plain move re-ran the curved-band CSG for every wall that carried features or openings —
 * hundreds of milliseconds per hole, seconds at a wide sweep, right after the pointer was released.
 *
 * These assertions pin the contract that replaced it: patches are immutable, so untouched bodies
 * (and the untouched collections of a moved body) keep their identity.
 */

const wall = (id: string, over: Partial<SceneWallState> = {}): SceneWallState =>
  ({
    id,
    originX: 0,
    originY: 0,
    rotationDeg: 0,
    lengthMm: 3000,
    heightMm: 2400,
    thicknessMm: 200,
    openings: [
      { id: `${id}-o`, kind: 'window', offsetMm: 1200, sillMm: 900, widthMm: 800, heightMm: 1200 },
    ],
    features: [
      {
        id: `${id}-f`,
        shape: 'rect',
        mode: 'hole',
        side: 1,
        offsetMm: 600,
        centerZMm: 1000,
        widthMm: 400,
        heightMm: 400,
        depthMm: 200,
      },
    ],
    ...over,
  }) as SceneWallState;

describe('applyScenePatch keeps geometry identities stable', () => {
  beforeEach(() => {
    useDesignerStore.getState().applyScene({
      runs: [],
      walls: [wall('w1'), wall('w2')],
      slabs: [],
      surfaces: [],
      connections: [],
    } as never);
  });

  it('an untouched wall comes out of a patch as the SAME object', () => {
    const before = useDesignerStore.getState().scene.walls ?? [];
    const untouched = before.find((w) => w.id === 'w2');

    useDesignerStore.getState().applyScenePatch((s) => ({
      ...s,
      walls: (s.walls ?? []).map((w) => (w.id === 'w1' ? { ...w, originX: 500 } : w)),
    }));

    const after = useDesignerStore.getState().scene.walls ?? [];
    expect(after.find((w) => w.id === 'w2')).toBe(untouched);
  });

  it('a MOVED wall keeps its features and openings by reference', () => {
    const before = (useDesignerStore.getState().scene.walls ?? []).find((w) => w.id === 'w1');

    useDesignerStore.getState().applyScenePatch((s) => ({
      ...s,
      walls: (s.walls ?? []).map((w) => (w.id === 'w1' ? { ...w, originX: 500, originY: 250 } : w)),
    }));

    const after = (useDesignerStore.getState().scene.walls ?? []).find((w) => w.id === 'w1');
    expect(after?.originX).toBe(500);
    expect(after?.features).toBe(before?.features);
    expect(after?.openings).toBe(before?.openings);
  });

  it('still records an undoable history entry that survives later edits', () => {
    useDesignerStore.getState().applyScenePatch((s) => ({
      ...s,
      walls: (s.walls ?? []).map((w) => (w.id === 'w1' ? { ...w, originX: 500 } : w)),
    }));
    useDesignerStore.getState().applyScenePatch((s) => ({
      ...s,
      walls: (s.walls ?? []).map((w) => (w.id === 'w1' ? { ...w, originX: 900 } : w)),
    }));

    useDesignerStore.getState().undo();
    const undone = (useDesignerStore.getState().scene.walls ?? []).find((w) => w.id === 'w1');
    expect(undone?.originX).toBe(500);
  });
});
