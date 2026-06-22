import { describe, expect, it } from 'vitest';
import { barrelArcProfilePoints } from './barrelRoof';

describe('barrelArcProfilePoints', () => {
  it('is flat (two points, y=0) when rise is zero', () => {
    expect(barrelArcProfilePoints(2000, 0)).toEqual([
      { x: 0, y: 0 },
      { x: 2000, y: 0 },
    ]);
  });

  it('peaks at the centre and meets the eaves at zero', () => {
    const pts = barrelArcProfilePoints(2000, 300, 24);
    expect(pts).toHaveLength(25);
    expect(pts[0]).toEqual({ x: 0, y: 0 });
    expect(pts[pts.length - 1].x).toBe(2000);
    expect(pts[pts.length - 1].y).toBeCloseTo(0, 6);
    const mid = pts[12];
    expect(mid.x).toBe(1000);
    expect(mid.y).toBeCloseTo(300, 6);
  });

  it('is symmetric about the centre', () => {
    const pts = barrelArcProfilePoints(2000, 250, 20);
    for (let i = 0; i < pts.length; i += 1) {
      const mirror = pts[pts.length - 1 - i];
      expect(pts[i].y).toBeCloseTo(mirror.y, 6);
    }
  });

  it('stays within the bounding box (0..length, 0..rise)', () => {
    const pts = barrelArcProfilePoints(1500, 400, 32);
    for (const p of pts) {
      expect(p.x).toBeGreaterThanOrEqual(0);
      expect(p.x).toBeLessThanOrEqual(1500);
      expect(p.y).toBeGreaterThanOrEqual(0);
      expect(p.y).toBeLessThanOrEqual(400 + 1e-6);
    }
  });
});
