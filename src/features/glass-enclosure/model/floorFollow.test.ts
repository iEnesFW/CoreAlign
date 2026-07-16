import { describe, expect, it } from 'vitest';
import { computeFloorFollow } from './floorFollow';
import type { SceneRunState, SceneSlabState, SceneState, SceneWallState } from './project.types';

const wall = (over: Partial<SceneWallState> = {}): SceneWallState => ({
  id: 'w',
  originX: 0,
  originY: 0,
  lengthMm: 4000,
  rotationDeg: 0,
  heightMm: 2600,
  heightEndMm: null,
  thicknessMm: 200,
  colorHex: null,
  geomZ: 0,
  openings: [],
  features: [],
  ...over,
});

const run = (over: Partial<SceneRunState> = {}): SceneRunState => ({
  id: 'r',
  orderIndex: 0,
  label: 'r',
  lengthMm: 1000,
  heightMm: 2000,
  originX: 0,
  originY: 0,
  rotationDeg: 0,
  profileSystemId: 'ps',
  colorId: null,
  hasTopDrip: true,
  hasBottomThreshold: false,
  geomZ: 0,
  panels: [],
  ...over,
});

const slab = (over: Partial<SceneSlabState> & { kind: 'floor' | 'roof' }): SceneSlabState => ({
  id: 's',
  originX: 0,
  originY: 0,
  rotationDeg: 0,
  lengthMm: 4000,
  depthMm: 4000,
  thicknessMm: 150,
  elevationMm: 0,
  colorHex: null,
  features: [],
  ...over,
});

const scene = (parts: Partial<SceneState>): SceneState => ({
  runs: [],
  connections: [],
  walls: [],
  slabs: [],
  surfaces: [],
  camera: null,
  metadata: { schemaVersion: 1, savedAt: '' },
  ...parts,
});

// A 4x4m floor whose TOP sits at Z=0 (elevation -150 + thickness 150).
const floor = () => slab({ id: 'floor', kind: 'floor', elevationMm: -150, thicknessMm: 150 });

describe('computeFloorFollow', () => {
  it('carries the wall on the floor, its bonded glass, a free run on the floor, and the roof — by ΔZ', () => {
    const s = scene({
      walls: [
        wall({ id: 'W', geomZ: 0 }), // perimeter wall resting on the floor top (Z=0)
        wall({ id: 'W2', geomZ: 3000, originY: 20000 }), // elevated wall, NOT on the floor
      ],
      runs: [
        run({ id: 'Gb', hostWallId: 'W', geomZ: 1000, originX: 500 }), // glass bonded to W, high up
        run({ id: 'Gf', geomZ: 0, originX: 1000, originY: 2000 }), // free glass resting on the floor
        run({ id: 'Ge', geomZ: 500, originX: 20000, originY: 20000 }), // elsewhere, off the floor
        run({ id: 'Gb2', hostWallId: 'W2', geomZ: 3000, originY: 20000 }), // bonded to the non-moving W2
      ],
      slabs: [
        floor(),
        slab({ id: 'roof', kind: 'roof', elevationMm: 2600 }), // roof on W's top (0 + 2600)
      ],
    });

    // Raise the floor top from 0 to 500 (elevation -150 → 350).
    const follow = computeFloorFollow(s, 'floor', 350, 150);
    expect(follow).not.toBeNull();
    expect(follow!.deltaZMm).toBe(500);
    expect(follow!.wallIds).toEqual(['W']);
    expect([...follow!.runIds].sort()).toEqual(['Gb', 'Gf']);
    expect(follow!.roofSlabIds).toEqual(['roof']);
  });

  it('does NOT move a run bonded to a wall that itself is not on the floor (no drift of linked objects)', () => {
    const s = scene({
      walls: [wall({ id: 'W2', geomZ: 3000, originY: 20000 })],
      runs: [run({ id: 'Gb2', hostWallId: 'W2', geomZ: 3000, originY: 20000 })],
      slabs: [floor()],
    });
    const follow = computeFloorFollow(s, 'floor', 350, 150);
    expect(follow!.wallIds).toEqual([]);
    expect(follow!.runIds).toEqual([]);
  });

  it('moves a run that is both bonded to a moving wall AND on the floor exactly once', () => {
    const s = scene({
      walls: [wall({ id: 'W', geomZ: 0 })],
      runs: [run({ id: 'Gboth', hostWallId: 'W', geomZ: 0, originX: 2000, originY: 60 })],
      slabs: [floor()],
    });
    const follow = computeFloorFollow(s, 'floor', 350, 150);
    expect(follow!.runIds.filter((id) => id === 'Gboth')).toHaveLength(1);
  });

  it('returns null when the top does not move or the slab is not a floor', () => {
    const s = scene({ slabs: [floor(), slab({ id: 'roof', kind: 'roof', elevationMm: 2600 })] });
    expect(computeFloorFollow(s, 'floor', -150, 150)).toBeNull(); // top stays at 0
    expect(computeFloorFollow(s, 'roof', 3000, 150)).toBeNull(); // not a floor
  });
});
