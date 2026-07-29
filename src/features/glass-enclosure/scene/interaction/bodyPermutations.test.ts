import { describe, expect, it } from 'vitest';
import { penetratesAny, supportTopBelowMm } from '@/shared/three-engine';
import { buildSlabFootprint, buildWallFootprint } from './planCollision';
import { boxCornersMm, resizeBoxFromCorner } from './footprintCorners';
import type { BoxFootprint } from './footprintCorners';
import type { SceneSlabState, SceneWallState } from '../../model/project.types';

/**
 * The relative-position matrix the designer has to get right: beside, in front, touching, sunk
 * into, and stacked. Every one of these is a drag the user can perform, and each has a different
 * correct answer — a touching neighbour must be allowed, an overlapping one refused, and a stacked
 * one must both be allowed AND report the support it now stands on.
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

const slab = (over: Partial<SceneSlabState> = {}) =>
  ({
    id: 's',
    kind: 'roof',
    originX: 0,
    originY: -1000,
    rotationDeg: 0,
    lengthMm: 3000,
    depthMm: 2000,
    thicknessMm: 150,
    elevationMm: 2600,
    features: [],
    ...over,
  }) as unknown as SceneSlabState;

const fpWall = (o: Partial<SceneWallState>) =>
  buildWallFootprint(wall(o), 0, 0, wall(o).rotationDeg);

describe('body-vs-body permutations', () => {
  const base = fpWall({ id: 'base' });

  it('BESIDE: a wall parallel and clear of another does not collide', () => {
    expect(penetratesAny(fpWall({ id: 'other', originY: 3000 }), [base])).toBe(false);
  });

  it('TOUCHING: walls butted face to face are allowed to stay in contact', () => {
    // Two 200 mm walls with centrelines 200 mm apart are exactly skin to skin.
    expect(penetratesAny(fpWall({ id: 'other', originY: 200 }), [base])).toBe(false);
  });

  it('OVERLAPPING: a wall driven into another is refused', () => {
    expect(penetratesAny(fpWall({ id: 'other', originY: 60 }), [base])).toBe(true);
  });

  it('END TO END: collinear walls meeting at the ends do not collide', () => {
    expect(penetratesAny(fpWall({ id: 'other', originX: 4000 }), [base])).toBe(false);
  });

  it('CROSSING: a perpendicular wall through the middle is refused', () => {
    expect(
      penetratesAny(
        buildWallFootprint(
          wall({ id: 'other', originX: 2000, originY: -2000, rotationDeg: 90 }),
          0,
          0,
          90,
        ),
        [base],
      ),
    ).toBe(true);
  });

  it('STACKED: a body above another does not collide with it and reads its top as support', () => {
    const upper = fpWall({ id: 'upper', geomZ: 2600 });
    expect(penetratesAny(upper, [base])).toBe(false);
    expect(supportTopBelowMm(upper, [base], 2600)).toBe(2600);
  });

  it('A ROOF over a wall clears it and rests on its top', () => {
    const roof = buildSlabFootprint(slab({ elevationMm: 2600 }), 0, 0, 0);
    expect(penetratesAny(roof, [base])).toBe(false);
    expect(supportTopBelowMm(roof, [base], 2600)).toBe(2600);
  });

  it('A ROOF sunk to wall mid-height IS refused (it would pass through the wall)', () => {
    const sunk = buildSlabFootprint(slab({ elevationMm: 1200 }), 0, 0, 0);
    expect(penetratesAny(sunk, [base])).toBe(true);
  });
});

describe('Q corner handle resize', () => {
  const box = (): BoxFootprint => ({
    originX: 0,
    originY: 0,
    lengthMm: 4000,
    crossMm: 2000,
    rotationDeg: 0,
  });

  it('dragging a corner changes the size and keeps the opposite corner pinned', () => {
    const before = boxCornersMm(box());
    const resized = resizeBoxFromCorner(box(), 0, 1000, 500, 50, 0, 0);
    const after = boxCornersMm(resized);
    // Corner 0 moved; the diagonally opposite corner (2) must not have.
    expect(after[2].x).toBeCloseTo(before[2].x, 6);
    expect(after[2].y).toBeCloseTo(before[2].y, 6);
    expect(resized.lengthMm).not.toBe(4000);
  });

  it('never collapses below the minimum size', () => {
    // Drag corner 0 far past the opposite corner.
    const resized = resizeBoxFromCorner(box(), 0, 999999, 999999, 50, 0, 0);
    expect(resized.lengthMm).toBeGreaterThanOrEqual(50);
    expect(resized.crossMm).toBeGreaterThanOrEqual(50);
  });

  it('keeps the rotation of a rotated body', () => {
    const rotated = { ...box(), rotationDeg: 37 };
    const resized = resizeBoxFromCorner(rotated, 1, 1500, 900, 50, 0, 0);
    expect(resized.rotationDeg).toBe(37);
  });

  it('a resize that changes nothing returns the same dimensions', () => {
    const corners = boxCornersMm(box());
    const resized = resizeBoxFromCorner(box(), 0, corners[0].x, corners[0].y, 50, 0, 0);
    expect(resized.lengthMm).toBeCloseTo(4000, 6);
    expect(resized.crossMm).toBeCloseTo(2000, 6);
  });
});
