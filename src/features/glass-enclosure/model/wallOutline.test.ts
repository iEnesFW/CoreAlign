import { describe, expect, it } from 'vitest';
import {
  hasEdgeNotch,
  hasWallNotch,
  wallNotchedOutlineMm,
  wallProfileOutlineMm,
} from './wallOutline';
import type { WallEdgeNotch } from './project.types';

const notch = (
  edge: WallEdgeNotch['edge'],
  offsetMm: number,
  widthMm: number,
  depthMm: number,
) => ({
  id: `${edge}-${offsetMm}`,
  edge,
  offsetMm,
  widthMm,
  depthMm,
});

describe('hasWallNotch', () => {
  it('is false for no/zero notch and true when any corner is notched', () => {
    expect(hasWallNotch(null)).toBe(false);
    expect(hasWallNotch({})).toBe(false);
    expect(hasWallNotch({ tl: 0, tr: 0, bl: 0, br: 0 })).toBe(false);
    expect(hasWallNotch({ tr: 100 })).toBe(true);
  });
});

describe('wallNotchedOutlineMm', () => {
  it('is a plain 4-point rectangle when there is no notch', () => {
    const pts = wallNotchedOutlineMm(4000, 2600, 2600, {});
    expect(pts).toEqual([
      { x: 0, z: 0 },
      { x: 4000, z: 0 },
      { x: 4000, z: 2600 },
      { x: 0, z: 2600 },
    ]);
  });

  it('cuts a rectangular bite at the top-left corner only', () => {
    const pts = wallNotchedOutlineMm(4000, 2600, 2600, { tl: 300 });
    // bl, br, tr stay sharp; tl becomes a 3-point inward step removing [0..300]x[2300..2600]
    expect(pts).toEqual([
      { x: 0, z: 0 },
      { x: 4000, z: 0 },
      { x: 4000, z: 2600 },
      { x: 300, z: 2600 },
      { x: 300, z: 2300 },
      { x: 0, z: 2300 },
    ]);
  });

  it('notches all four corners independently', () => {
    const pts = wallNotchedOutlineMm(4000, 2600, 2600, { tl: 100, tr: 200, bl: 50, br: 80 });
    // 3 points per notched corner = 12 points
    expect(pts).toHaveLength(12);
    // bottom-left bite
    expect(pts).toContainEqual({ x: 0, z: 50 });
    expect(pts).toContainEqual({ x: 50, z: 0 });
    // top-right bite
    expect(pts).toContainEqual({ x: 4000, z: 2400 });
    expect(pts).toContainEqual({ x: 3800, z: 2600 });
  });

  it('clamps a notch so it cannot exceed ~half the wall', () => {
    const pts = wallNotchedOutlineMm(1000, 1000, 1000, { tl: 99999 });
    for (const p of pts) {
      expect(p.x).toBeGreaterThanOrEqual(0);
      expect(p.x).toBeLessThanOrEqual(1000);
      expect(p.z).toBeGreaterThanOrEqual(0);
      expect(p.z).toBeLessThanOrEqual(1000);
    }
  });
});

describe('hasEdgeNotch', () => {
  it('is true only when a notch has positive width and depth', () => {
    expect(hasEdgeNotch(null)).toBe(false);
    expect(hasEdgeNotch([])).toBe(false);
    expect(hasEdgeNotch([notch('top', 100, 0, 200)])).toBe(false);
    expect(hasEdgeNotch([notch('top', 100, 200, 0)])).toBe(false);
    expect(hasEdgeNotch([notch('top', 100, 200, 200)])).toBe(true);
  });
});

describe('wallProfileOutlineMm edge notches', () => {
  it('cuts an inward bite from the bottom edge (rises into the wall)', () => {
    const pts = wallProfileOutlineMm(4000, 2600, 2600, null, [notch('bottom', 1000, 500, 300)]);
    expect(pts).toContainEqual({ x: 1000, z: 0 });
    expect(pts).toContainEqual({ x: 1000, z: 300 });
    expect(pts).toContainEqual({ x: 1500, z: 300 });
    expect(pts).toContainEqual({ x: 1500, z: 0 });
  });

  it('bites inward in the correct direction for each edge', () => {
    // right edge bites toward -x (left, into the wall)
    expect(
      wallProfileOutlineMm(4000, 2600, 2600, null, [notch('right', 500, 400, 300)]),
    ).toContainEqual({ x: 3700, z: 500 });
    // top edge bites toward -z (down)
    expect(
      wallProfileOutlineMm(4000, 2600, 2600, null, [notch('top', 1000, 500, 300)]),
    ).toContainEqual({ x: 3000, z: 2300 });
    // left edge bites toward +x (right)
    expect(
      wallProfileOutlineMm(4000, 2600, 2600, null, [notch('left', 500, 400, 300)]),
    ).toContainEqual({ x: 300, z: 2100 });
  });

  it('clamps bite depth so it cannot pierce the opposite edge', () => {
    const pts = wallProfileOutlineMm(4000, 2600, 2600, null, [notch('bottom', 1000, 500, 99999)]);
    for (const p of pts) {
      expect(p.x).toBeGreaterThanOrEqual(0);
      expect(p.x).toBeLessThanOrEqual(4000);
      expect(p.z).toBeGreaterThanOrEqual(0);
      expect(p.z).toBeLessThanOrEqual(2600);
    }
    // depth clamped to 0.9 × height = 2340, so the bite never reaches the 2600 top
    expect(pts).toContainEqual({ x: 1000, z: 2340 });
  });

  it('composes edge notches with corner notches', () => {
    const pts = wallProfileOutlineMm(4000, 2600, 2600, { tl: 300 }, [
      notch('bottom', 1000, 500, 300),
    ]);
    // corner notch still present
    expect(pts).toContainEqual({ x: 300, z: 2600 });
    // bottom bite still present
    expect(pts).toContainEqual({ x: 1000, z: 300 });
  });
});
