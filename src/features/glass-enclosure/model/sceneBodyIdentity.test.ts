import { describe, expect, it, beforeEach } from 'vitest';
import { useDesignerStore } from './designerStore';
import type { SceneSlabState, SceneWallState } from './project.types';

/**
 * The scene's bodies are keyed by id and EVERY lookup in the designer is `find(x => x.id === id)`.
 * A second body carrying an existing id is therefore a ghost: it renders, but it can never be
 * selected, edited or deleted. Adding an id that already exists must replace it.
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
    openings: [],
    features: [],
    ...over,
  }) as SceneWallState;

const slab = (id: string, over: Partial<SceneSlabState> = {}): SceneSlabState =>
  ({
    id,
    kind: 'floor',
    originX: 0,
    originY: 0,
    rotationDeg: 0,
    lengthMm: 4000,
    depthMm: 3000,
    thicknessMm: 150,
    elevationMm: -150,
    ...over,
  }) as SceneSlabState;

describe('scene body ids stay unique', () => {
  beforeEach(() => {
    useDesignerStore.getState().applyScene({
      runs: [],
      walls: [],
      slabs: [],
      surfaces: [],
      connections: [],
    } as never);
  });

  it('re-adding a wall id replaces it instead of stacking a ghost', () => {
    const store = useDesignerStore.getState();
    store.addWall(wall('w1', { lengthMm: 3000 }));
    store.addWall(wall('w1', { lengthMm: 5000 }));

    const walls = useDesignerStore.getState().scene.walls ?? [];
    expect(walls).toHaveLength(1);
    expect(walls[0].lengthMm).toBe(5000);
  });

  it('distinct wall ids still both land', () => {
    const store = useDesignerStore.getState();
    store.addWall(wall('w1'));
    store.addWall(wall('w2'));
    expect((useDesignerStore.getState().scene.walls ?? []).map((w) => w.id)).toEqual(['w1', 'w2']);
  });

  it('re-adding a slab id replaces it', () => {
    const store = useDesignerStore.getState();
    store.addSlab(slab('s1', { thicknessMm: 150 }));
    store.addSlab(slab('s1', { thicknessMm: 220 }));

    const slabs = useDesignerStore.getState().scene.slabs ?? [];
    expect(slabs).toHaveLength(1);
    expect(slabs[0].thicknessMm).toBe(220);
  });

  it('a replaced run keeps its place in the order', () => {
    const store = useDesignerStore.getState();
    const base = {
      label: 'R',
      originX: 0,
      originY: 0,
      rotationDeg: 0,
      heightMm: 2400,
      profileSystemId: null,
      colorId: null,
      hasTopDrip: false,
      hasBottomThreshold: false,
    };
    store.addRun({ ...base, id: 'r1', lengthMm: 1000 } as never);
    store.addRun({ ...base, id: 'r2', lengthMm: 2000 } as never);
    store.addRun({ ...base, id: 'r1', lengthMm: 3000 } as never);

    const runs = useDesignerStore.getState().scene.runs;
    expect(runs.map((r) => r.id)).toEqual(['r1', 'r2']);
    expect(runs[0].lengthMm).toBe(3000);
    expect(runs[0].orderIndex).toBe(0);
    expect(runs[1].orderIndex).toBe(1);
  });
});
