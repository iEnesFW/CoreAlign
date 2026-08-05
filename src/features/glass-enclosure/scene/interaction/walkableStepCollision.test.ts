import { describe, expect, it } from 'vitest';
import { footprintsPenetrate, slidePlanMove } from '@/shared/three-engine';
import { buildSlabFootprint, buildWallFootprint } from './planCollision';
import type { SceneSlabState, SceneWallState } from '../../model/project.types';

/**
 * Stepping onto a low WALKABLE support (a floor plate, a podium) used to switch the drag into the
 * "stacking" arm, which skips `slidePlanMove` entirely — so wall-vs-wall collision went dead for the
 * rest of that drag and a second wall could be pushed straight through the wall already standing on
 * the podium, and committed there (no commit path re-checks). The podium must stop PINNING the body
 * at its edge without also switching collision off, which is what filtering `walkable` supports out
 * of the slide set does.
 */

const podium = (): SceneSlabState =>
  ({
    id: 'podium',
    kind: 'floor',
    originX: 0,
    originY: 0,
    rotationDeg: 0,
    lengthMm: 6000,
    depthMm: 6000,
    thicknessMm: 150,
    elevationMm: 0,
  }) as SceneSlabState;

const wallAt = (id: string, originX: number, geomZ: number): SceneWallState =>
  ({
    id,
    originX,
    originY: 3000,
    rotationDeg: 0,
    lengthMm: 2000,
    heightMm: 2600,
    thicknessMm: 200,
    geomZ,
    openings: [],
    features: [],
  }) as unknown as SceneWallState;

describe('stepping onto a walkable podium keeps solid collision live', () => {
  const podiumFp = buildSlabFootprint(podium(), 0, 0, 0);
  // A wall standing ON the podium (its top is at 150).
  const onPodium = buildWallFootprint(wallAt('standing', 3000, 150), 0, 0, 0);
  const obstacles = [podiumFp, onPodium];

  it('marks the floor plate walkable and the wall not', () => {
    expect(podiumFp.walkable).toBe(true);
    expect(onPodium.walkable).toBeFalsy();
  });

  it('drops only the podium from the slide set', () => {
    const filtered = obstacles.filter((o) => !o.walkable);
    expect(filtered.map((o) => o.ownerId)).toEqual(['standing']);
  });

  it('still blocks a ground-level wall from entering the wall on the podium', () => {
    const dragged = wallAt('dragged', -3000, 0);
    const filtered = obstacles.filter((o) => !o.walkable);
    // Ask for a 6 m slide that would land squarely inside the standing wall.
    const slid = slidePlanMove(
      (dx, dy) => buildWallFootprint(dragged, dx, dy, dragged.rotationDeg),
      filtered,
      6000,
      0,
    );
    expect(slid.dxMm).toBeLessThan(6000);
    // And what it did travel must not have entered the standing wall.
    const landed = buildWallFootprint(dragged, slid.dxMm, slid.dyMm, dragged.rotationDeg);
    expect(footprintsPenetrate(landed, onPodium)).toBe(false);
  });

  it('does NOT let the podium itself pin the body at its edge', () => {
    // Same drag with the podium still in the set: the plate spans the whole area, so an unfiltered
    // slide has nowhere to go — that is the pin the walkable step-up exists to avoid.
    const dragged = wallAt('dragged', -3000, 0);
    const pinned = slidePlanMove(
      (dx, dy) => buildWallFootprint(dragged, dx, dy, dragged.rotationDeg),
      obstacles,
      6000,
      0,
    );
    const free = slidePlanMove(
      (dx, dy) => buildWallFootprint(dragged, dx, dy, dragged.rotationDeg),
      obstacles.filter((o) => !o.walkable),
      6000,
      0,
    );
    expect(free.dxMm).toBeGreaterThan(pinned.dxMm);
  });
});
