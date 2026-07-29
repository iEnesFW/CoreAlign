import { describe, expect, it } from 'vitest';
import { settleScene } from './settleScene';
import type { SceneSlabState, SceneState, SceneWallState } from './project.types';

const scene = (over: Partial<SceneState>): SceneState =>
  ({ runs: [], walls: [], slabs: [], surfaces: [], connections: [], ...over }) as SceneState;

const wall = (id: string, geomZ: number, over: Partial<SceneWallState> = {}): SceneWallState =>
  ({
    id,
    originX: 0,
    originY: 0,
    rotationDeg: 0,
    lengthMm: 3000,
    heightMm: 2500,
    thicknessMm: 100,
    geomZ,
    openings: [],
    features: [],
    ...over,
  }) as SceneWallState;

const slab = (
  id: string,
  kind: 'floor' | 'roof',
  elevationMm: number,
  over: Partial<SceneSlabState> = {},
): SceneSlabState =>
  ({
    id,
    kind,
    originX: -500,
    originY: -500,
    rotationDeg: 0,
    lengthMm: 4000,
    depthMm: 2000,
    thicknessMm: 200,
    elevationMm,
    features: [],
    ...over,
  }) as SceneSlabState;

describe('settleScene', () => {
  it('drops an orphaned wall to the ground', () => {
    const before = scene({ walls: [wall('w1', 1500)] });
    const after = settleScene(before);
    expect(after.walls?.[0].geomZ).toBe(0);
  });

  it('rests a wall on the floor slab beneath it', () => {
    const before = scene({
      walls: [wall('w1', 3000)],
      slabs: [slab('floor', 'floor', 0)],
    });
    const after = settleScene(before);
    // Floor top = 0 + 200.
    expect(after.walls?.[0].geomZ).toBe(200);
  });

  it('leaves a ROOF floating — a canopy legitimately spans open air', () => {
    const before = scene({ slabs: [slab('canopy', 'roof', 2600)] });
    expect(settleScene(before)).toBe(before);
  });

  it('a roof still HOLDS UP whatever stands on it', () => {
    const before = scene({
      walls: [wall('parapet', 5000)],
      slabs: [slab('canopy', 'roof', 2600)],
    });
    const after = settleScene(before);
    expect(after.walls?.[0].geomZ).toBe(2800);
  });

  it('collapses a whole stack in one pass', () => {
    // floor at 0 (top 200), a slab floating at 4000 that should land on it, and a wall floating
    // above that which should then land on the settled slab.
    const before = scene({
      walls: [wall('w1', 9000)],
      slabs: [slab('floor', 'floor', 0), slab('deck', 'floor', 4000)],
    });
    const after = settleScene(before);
    const deck = after.slabs?.find((s) => s.id === 'deck');
    expect(deck?.elevationMm).toBe(200);
    // Deck now tops out at 400, so the wall lands there rather than on the floor.
    expect(after.walls?.[0].geomZ).toBe(400);
  });

  it('is idempotent and returns the SAME object when nothing moves', () => {
    const before = scene({
      walls: [wall('w1', 0)],
      slabs: [slab('canopy', 'roof', 2600)],
    });
    const once = settleScene(before);
    expect(once).toBe(before);
  });

  it('does not treat a body as its own support', () => {
    const before = scene({ slabs: [slab('lonely', 'floor', 2000)] });
    expect(settleScene(before).slabs?.[0].elevationMm).toBe(0);
  });

  it('ignores a body that does not overlap in plan', () => {
    const before = scene({
      walls: [wall('w1', 1000, { originX: 50000, originY: 50000 })],
      slabs: [slab('floor', 'floor', 0)],
    });
    expect(settleScene(before).walls?.[0].geomZ).toBe(0);
  });
});
