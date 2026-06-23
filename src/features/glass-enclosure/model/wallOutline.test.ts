import { describe, expect, it } from 'vitest';
import { hasWallNotch, wallNotchedOutlineMm } from './wallOutline';

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
