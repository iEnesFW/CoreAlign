import { describe, expect, it } from 'vitest';
import { computeRoofOverWalls, computeRoofSurfaceOverWalls } from './roofFromWalls';
import { polygonAreaM2 } from './polygonValidation';
import type { SceneWallState } from './project.types';

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

describe('computeRoofOverWalls', () => {
  it('covers the bounding box of a four-wall room at the tallest wall top', () => {
    // A 4000 x 3000 room (thickness 200 -> halfWidth 100 spills the bbox 100mm past each edge).
    const roof = computeRoofOverWalls([
      wall({ id: 'bottom', originX: 0, originY: 0, rotationDeg: 0, lengthMm: 4000 }),
      wall({ id: 'left', originX: 0, originY: 0, rotationDeg: 90, lengthMm: 3000 }),
      wall({ id: 'top', originX: 0, originY: 3000, rotationDeg: 0, lengthMm: 4000 }),
      wall({
        id: 'right',
        originX: 4000,
        originY: 0,
        rotationDeg: 90,
        lengthMm: 3000,
        heightMm: 3200,
      }),
    ]);
    expect(roof).not.toBeNull();
    expect(roof!.kind).toBe('roof');
    expect(roof!.originX).toBe(-100);
    expect(roof!.originY).toBe(-100);
    expect(roof!.lengthMm).toBe(4200);
    expect(roof!.depthMm).toBe(3200);
    expect(roof!.elevationMm).toBe(3200); // rests on the tallest wall top
  });

  it('returns null for fewer than three walls', () => {
    expect(computeRoofOverWalls([wall({ id: 'a' }), wall({ id: 'b' })])).toBeNull();
  });
});

describe('computeRoofSurfaceOverWalls', () => {
  it('produces a polygon-exact roof surface hugging the wall centerlines', () => {
    const roof = computeRoofSurfaceOverWalls([
      wall({ id: 'bottom', originX: 0, originY: 0, rotationDeg: 0, lengthMm: 4000 }),
      wall({ id: 'left', originX: 0, originY: 0, rotationDeg: 90, lengthMm: 3000 }),
      wall({ id: 'top', originX: 0, originY: 3000, rotationDeg: 0, lengthMm: 4000 }),
      wall({
        id: 'right',
        originX: 4000,
        originY: 0,
        rotationDeg: 90,
        lengthMm: 3000,
        heightMm: 3200,
      }),
    ]);
    expect(roof).not.toBeNull();
    expect(roof!.kind).toBe('roof');
    expect(roof!.points).toHaveLength(4);
    expect(roof!.elevationMm).toBe(3200);
    expect(polygonAreaM2(roof!.points)).toBeCloseTo(12, 3);
  });

  it('returns null when the walls do not form a clean ring (bbox fallback territory)', () => {
    expect(computeRoofSurfaceOverWalls([wall({ id: 'a' }), wall({ id: 'b' })])).toBeNull();
    expect(
      computeRoofSurfaceOverWalls([
        wall({ id: 'bottom', originX: 0, originY: 0, rotationDeg: 0, lengthMm: 4000 }),
        wall({ id: 'left', originX: 0, originY: 0, rotationDeg: 90, lengthMm: 3000 }),
        wall({ id: 'right', originX: 4000, originY: 0, rotationDeg: 90, lengthMm: 3000 }),
      ]),
    ).toBeNull();
  });
});
