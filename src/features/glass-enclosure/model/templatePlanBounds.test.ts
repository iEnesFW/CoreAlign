import { describe, expect, it } from 'vitest';
import { buildGlassTemplate, templatePlanBounds } from './templates';

/**
 * The placement ghost shows the template's overall plan box and the click centres it. If these
 * bounds under-claim, the ghost's collision check clears a spot the composition does not fit.
 */
describe('templatePlanBounds', () => {
  it('boxes the L-walls composition including wall thickness', () => {
    const t = buildGlassTemplate('l-walls');
    const b = templatePlanBounds(t);
    // Two 200-thick walls: one 3000 along +X (thickness in ±Y), one 2000 along +Y from (3000,0)
    // (thickness in ±X) — so the box carries each wall's half-thickness on ITS normal.
    expect(b.minXMm).toBeLessThanOrEqual(0);
    expect(b.maxXMm).toBeGreaterThanOrEqual(3100);
    expect(b.minYMm).toBeLessThanOrEqual(-100);
    expect(b.maxYMm).toBeGreaterThanOrEqual(2000);
    expect(b.zMaxMm).toBe(2600);
  });

  it('every built-in template produces a real, positive box', () => {
    for (const key of ['l-walls', 'u-walls'] as const) {
      const b = templatePlanBounds(buildGlassTemplate(key));
      expect(b.maxXMm - b.minXMm).toBeGreaterThan(0);
      expect(b.maxYMm - b.minYMm).toBeGreaterThan(0);
      expect(b.zMaxMm).toBeGreaterThan(0);
    }
  });

  it('an empty template collapses to a zero box instead of infinities', () => {
    const b = templatePlanBounds({ walls: [], slabs: [], runs: [] });
    expect(b).toEqual({ minXMm: 0, maxXMm: 0, minYMm: 0, maxYMm: 0, zMaxMm: 0 });
  });

  it('a rotated wall contributes its true corners, not an axis-aligned guess', () => {
    const b = templatePlanBounds({
      walls: [
        {
          originX: 0,
          originY: 0,
          rotationDeg: 90,
          lengthMm: 2000,
          heightMm: 2600,
          heightEndMm: null,
          thicknessMm: 200,
          colorHex: null,
          geomZ: 0,
          openings: [],
          features: [],
        },
      ],
      slabs: [],
      runs: [],
    });
    // Along +Y with 100 half-thickness in X.
    expect(b.maxYMm).toBeCloseTo(2000, 6);
    expect(b.minXMm).toBeCloseTo(-100, 6);
    expect(b.maxXMm).toBeCloseTo(100, 6);
  });
});
