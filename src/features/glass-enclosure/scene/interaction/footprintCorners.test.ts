import { describe, expect, it } from 'vitest';
import { boxCornersMm, resizeBoxFromCorner } from './footprintCorners';

describe('boxCornersMm', () => {
  it('returns the four plan corners of an axis-aligned box', () => {
    const corners = boxCornersMm({
      originX: 0,
      originY: 0,
      lengthMm: 2000,
      crossMm: 200,
      rotationDeg: 0,
    });
    expect(corners).toEqual([
      { x: 0, y: -100 },
      { x: 2000, y: -100 },
      { x: 2000, y: 100 },
      { x: 0, y: 100 },
    ]);
  });

  it('rotates the corners for a 90° box (wall running along +Y)', () => {
    const corners = boxCornersMm({
      originX: 0,
      originY: 0,
      lengthMm: 1000,
      crossMm: 200,
      rotationDeg: 90,
    });
    const round = corners.map((c) => ({ x: Math.round(c.x) + 0, y: Math.round(c.y) + 0 }));
    expect(round).toEqual([
      { x: 100, y: 0 },
      { x: 100, y: 1000 },
      { x: -100, y: 1000 },
      { x: -100, y: 0 },
    ]);
  });
});

describe('resizeBoxFromCorner', () => {
  const box = { originX: 0, originY: 0, lengthMm: 2000, crossMm: 200, rotationDeg: 0 };

  it('keeps the opposite corner fixed and reaches the dragged point', () => {
    const next = resizeBoxFromCorner(box, 2, 3000, 300); // drag end-+ corner
    // opposite corner (index 0 = start-, at (0,-100)) must stay put
    expect(boxCornersMm(next)[0]).toEqual({ x: 0, y: -100 });
    // the dragged corner (index 2) must land on the target
    expect(boxCornersMm(next)[2]).toEqual({ x: 3000, y: 300 });
    expect(next.lengthMm).toBe(3000);
    expect(next.crossMm).toBe(400);
  });

  it('works for a rotated box (opposite corner fixed, dragged corner reached)', () => {
    const rotated = { originX: 0, originY: 0, lengthMm: 1000, crossMm: 200, rotationDeg: 90 };
    const next = resizeBoxFromCorner(rotated, 1, 150, 1500); // drag end-- corner
    const corners = boxCornersMm(next).map((c) => ({
      x: Math.round(c.x) + 0,
      y: Math.round(c.y) + 0,
    }));
    expect(corners[3]).toEqual({ x: -100, y: 0 }); // opposite (index 3) fixed
    expect(corners[1]).toEqual({ x: 150, y: 1500 }); // dragged corner reached
  });

  it('clamps length and cross to the minimum so the box cannot collapse', () => {
    const next = resizeBoxFromCorner(box, 2, 0, -100, 50); // drag onto the opposite corner
    expect(next.lengthMm).toBeGreaterThanOrEqual(50);
    expect(next.crossMm).toBeGreaterThanOrEqual(50);
  });
});
