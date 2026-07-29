import { describe, expect, it } from 'vitest';
import { penetratesAny, supportTopBelowMm } from '@/shared/three-engine';
import { buildSlabFootprint, buildSurfaceFootprint, buildWallFootprint } from './planCollision';
import type { SceneSlabState, SceneSurfaceState, SceneWallState } from '../../model/project.types';

/**
 * "I still cannot move an object onto the floor."
 *
 * A floor is BOTH a thing you stand on and a body in the plan. If it lands in the obstacle set with
 * a z-range that overlaps the wall's, the collision solver refuses the move and the wall can never
 * be dragged over it — which is what the report describes. These pin the two floor flavours.
 */

const wall = (over: Partial<SceneWallState> = {}) =>
  ({
    id: 'w',
    label: 'W',
    originX: 0,
    originY: 0,
    rotationDeg: 0,
    lengthMm: 4000,
    heightMm: 2600,
    thicknessMm: 200,
    geomZ: 0,
    openings: [],
    features: [],
    ...over,
  }) as unknown as SceneWallState;

const floorSlab = (over: Partial<SceneSlabState> = {}) =>
  ({
    id: 'floor',
    kind: 'floor',
    originX: -4000,
    originY: -4000,
    rotationDeg: 0,
    lengthMm: 8000,
    depthMm: 8000,
    thicknessMm: 150,
    elevationMm: -150,
    features: [],
    ...over,
  }) as unknown as SceneSlabState;

const drawnFloor = (over: Partial<SceneSurfaceState> = {}) =>
  ({
    id: 'drawn',
    kind: 'floor',
    points: [
      { x: -4000, y: -4000 },
      { x: 4000, y: -4000 },
      { x: 4000, y: 4000 },
      { x: -4000, y: 4000 },
    ],
    elevationMm: 0,
    thicknessMm: 120,
    ...over,
  }) as unknown as SceneSurfaceState;

describe('dragging a body onto a floor', () => {
  it('a PLACED floor slab does not block a wall standing on it', () => {
    const floor = buildSlabFootprint(floorSlab(), 0, 0, 0);
    const moved = buildWallFootprint(wall({ geomZ: 0 }), 0, 0, 0);
    expect(penetratesAny(moved, [floor])).toBe(false);
  });

  it('a PLACED floor slab lifts a wall to its top', () => {
    const floor = buildSlabFootprint(floorSlab(), 0, 0, 0);
    const moved = buildWallFootprint(wall({ geomZ: 0 }), 0, 0, 0);
    expect(supportTopBelowMm(moved, [floor], 0)).toBe(0);
  });

  it('a FREE-DRAWN floor lifts a wall to its top rather than blocking it', () => {
    // A drawn floor sits at elevation 0 with 120 mm of thickness, so its z-range OVERLAPS a wall
    // standing at 0. If it were ever added to the obstacle set the move would be refused outright;
    // it must act as SUPPORT (lift to 120) instead.
    const drawn = buildSurfaceFootprint(drawnFloor());
    const moved = buildWallFootprint(wall({ geomZ: 0 }), 0, 0, 0);
    expect(supportTopBelowMm(moved, [drawn], 120)).toBe(120);
  });

  it('REGRESSION GATE: a wall whose base is already on the drawn floor is not blocked by it', () => {
    const drawn = buildSurfaceFootprint(drawnFloor());
    const moved = buildWallFootprint(wall({ geomZ: 120 }), 0, 0, 0);
    expect(penetratesAny(moved, [drawn])).toBe(false);
  });

  it('a floor at elevation 0 WOULD block a wall sunk into it — the z-overlap case', () => {
    // Documents the actual failure mode: overlap the z-ranges and the solver refuses the move.
    // This is why a floor must be a support, never an obstacle, for a body resting on it.
    const drawn = buildSurfaceFootprint(drawnFloor());
    const sunk = buildWallFootprint(wall({ geomZ: 0 }), 0, 0, 0);
    expect(penetratesAny(sunk, [drawn])).toBe(true);
  });
});

describe('settling must never RAISE a body', () => {
  it('a floor placed at -150 stays at -150 (raising it would turn it into an obstacle)', async () => {
    const { settleScene } = await import('../../model/settleScene');
    const scene = {
      metadata: { schemaVersion: 1 },
      runs: [],
      walls: [],
      slabs: [floorSlab({ elevationMm: -150 })],
      surfaces: [],
      connections: [],
    } as never;
    const settled = settleScene(scene) as unknown as { slabs: SceneSlabState[] };
    // Raised to 0 its z-range becomes [0,150], which OVERLAPS every wall standing at 0 — the
    // collision solver would then refuse to drag anything onto the floor.
    expect(settled.slabs[0].elevationMm).toBe(-150);
  });
});
