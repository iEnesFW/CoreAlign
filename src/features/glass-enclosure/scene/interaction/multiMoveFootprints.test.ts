import { describe, expect, it } from 'vitest';
import { captureMultiBodies, EMPTY_MULTI_BODIES, multiBodyFootprints } from './multiMoveFootprints';
import type { MultiSelection } from '../../model/designerStore';
import type {
  SceneRunState,
  SceneSlabState,
  SceneState,
  SceneWallState,
} from '../../model/project.types';

const wall = (id: string, originX = 0): SceneWallState =>
  ({
    id,
    originX,
    originY: 0,
    rotationDeg: 0,
    lengthMm: 2000,
    heightMm: 2600,
    thicknessMm: 200,
    openings: [],
    features: [],
  }) as unknown as SceneWallState;

const run = (id: string, originX = 0): SceneRunState =>
  ({
    id,
    originX,
    originY: 0,
    rotationDeg: 0,
    lengthMm: 1500,
    heightMm: 2400,
    panels: [],
  }) as unknown as SceneRunState;

const slab = (id: string): SceneSlabState =>
  ({
    id,
    kind: 'floor',
    originX: 0,
    originY: 0,
    rotationDeg: 0,
    lengthMm: 3000,
    depthMm: 3000,
    thicknessMm: 150,
    elevationMm: -150,
  }) as SceneSlabState;

const scene = (): SceneState =>
  ({
    walls: [wall('w1'), wall('w2', 4000)],
    runs: [run('r1'), run('r2', 4000)],
    slabs: [slab('s1')],
    surfaces: [],
    connections: [],
  }) as unknown as SceneState;

const multi = (over: Partial<MultiSelection> = {}): MultiSelection =>
  ({ runIds: [], wallIds: [], slabIds: [], ...over }) as MultiSelection;

describe('captureMultiBodies', () => {
  it('returns nothing when the dragged body is not part of the selection', () => {
    const bodies = captureMultiBodies(scene(), multi({ wallIds: ['w2'] }), {
      kind: 'wall',
      id: 'w1',
    });
    expect(bodies).toBe(EMPTY_MULTI_BODIES);
  });

  it('captures the co-movers and excludes the dragged body itself', () => {
    const bodies = captureMultiBodies(
      scene(),
      multi({ runIds: ['r1', 'r2'], wallIds: ['w1'], slabIds: ['s1'] }),
      { kind: 'run', id: 'r1' },
    );
    expect(bodies.runs.map((r) => r.id)).toEqual(['r2']);
    expect(bodies.walls.map((w) => w.id)).toEqual(['w1']);
    expect(bodies.slabs.map((s) => s.id)).toEqual(['s1']);
  });

  it('skips runs already carried by another list — a bonded run must not be counted twice', () => {
    const bodies = captureMultiBodies(
      scene(),
      multi({ runIds: ['r1', 'r2'], wallIds: ['w1'] }),
      { kind: 'wall', id: 'w1' },
      new Set(['r1']),
    );
    expect(bodies.runs.map((r) => r.id)).toEqual(['r2']);
  });
});

describe('multiBodyFootprints', () => {
  it('offsets every co-mover by the drag delta so the clamp can see them', () => {
    const bodies = captureMultiBodies(scene(), multi({ runIds: ['r1'], wallIds: ['w2'] }), {
      kind: 'run',
      id: 'r1',
    });
    const at0 = multiBodyFootprints(bodies, 0, 0);
    const at1000 = multiBodyFootprints(bodies, 1000, 0);

    expect(at0.map((f) => f.ownerId)).toEqual(['w2']);
    expect(at1000).toHaveLength(1);
    // The offset body is genuinely somewhere else — both ends of its centreline shifted.
    expect(at1000[0].x1).toBe(at0[0].x1 + 1000);
    expect(at1000[0].x2).toBe(at0[0].x2 + 1000);
    expect(at1000[0].y1).toBe(at0[0].y1);
  });

  it('is empty for an empty capture — a solo drag pays nothing', () => {
    expect(multiBodyFootprints(EMPTY_MULTI_BODIES, 500, 500)).toEqual([]);
  });
});
