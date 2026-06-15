import { describe, expect, it } from 'vitest';
import {
  buildPlanFootprint,
  buildPolygonFootprint,
  penetratesAny,
  restElevationMm,
  slidePlanMove,
} from './planCollision';
import type { PlanFootprint } from './planCollision';

// Wall footprint: 200mm-thick (halfWidth 100) horizontal wall.
const wall = (id: string, startX: number, lengthMm: number, rot = 0): PlanFootprint =>
  buildPlanFootprint(id, startX, 0, lengthMm, rot, 100, 0, 2600);

describe('footprint penetration (oriented-rectangle SAT)', () => {
  it('does not flag two collinear walls separated by a gap', () => {
    const a = wall('a', 0, 2000);
    const b = wall('b', 2200, 2000);
    expect(penetratesAny(a, [b])).toBe(false);
  });

  it('blocks a deep body-into-body overlap of two collinear walls', () => {
    const a = wall('a', 0, 2000);
    const b = wall('b', 1500, 2000); // 500mm overlap
    expect(penetratesAny(a, [b])).toBe(true);
  });

  it('allows a perpendicular corner butt-joint (overlap within one half width)', () => {
    const a = wall('a', 0, 2000, 0); // ends at (2000, 0)
    const b = buildPlanFootprint('b', 2000, 0, 2000, 90, 100, 0, 2600); // starts at corner
    expect(penetratesAny(a, [b])).toBe(false);
  });

  it('blocks a perpendicular wall that slices deep along another wall end face', () => {
    const a = wall('a', 0, 2000, 0); // body x[0,2000], y[-100,100]
    const b = buildPlanFootprint('b', 2000, 100, 2000, 270, 100, 0, 2600); // slices down A's end
    expect(penetratesAny(a, [b])).toBe(true);
  });

  it('blocks two parallel walls overlapping side by side along their length', () => {
    const a = wall('a', 0, 2000, 0); // body y[-100,100]
    const b = buildPlanFootprint('b', 0, 150, 2000, 0, 100, 0, 2600); // body y[50,250]
    expect(penetratesAny(a, [b])).toBe(true);
  });
});

// Mover: a 50mm-thick run centered on a horizontal segment at the origin.
const moverAt = (dxMm: number, dyMm: number): PlanFootprint =>
  buildPlanFootprint('mover', -500 + dxMm, -60 + dyMm, 1000, 0, 25, 0, 2400);

describe('slidePlanMove swept collision', () => {
  it('stops a long drag flush at the near face of a thin obstacle (no tunneling)', () => {
    // A 50mm run lying across the path at y=0.
    const obstacle = buildPlanFootprint('run', -500, 0, 1000, 0, 25, 0, 2400);
    const result = slidePlanMove(moverAt, [obstacle], 0, 3000);
    // Must NOT return the full 3000mm move (which would teleport behind).
    expect(result.dyMm).toBeLessThan(200);
    expect(result.dyMm).toBeGreaterThanOrEqual(0);
  });

  it('allows a clear move when nothing is in the path', () => {
    const obstacle = buildPlanFootprint('run', 5000, 5000, 1000, 0, 25, 0, 2400);
    const result = slidePlanMove(moverAt, [obstacle], 0, 3000);
    expect(result.dyMm).toBe(3000);
  });

  it('stops short for a small drag into the obstacle', () => {
    const obstacle = buildPlanFootprint('run', -500, 0, 1000, 0, 25, 0, 2400);
    const result = slidePlanMove(moverAt, [obstacle], 0, 120);
    expect(result.dyMm).toBeLessThan(60);
  });
});

// An L-shaped (non-convex) plan polygon at z [0, 2600].
const lShape: PlanFootprint = buildPolygonFootprint(
  'poly',
  [
    { x: 0, y: 0 },
    { x: 3000, y: 0 },
    { x: 3000, y: 1000 },
    { x: 1000, y: 1000 },
    { x: 1000, y: 3000 },
    { x: 0, y: 3000 },
  ],
  0,
  2600,
);

describe('polygon footprint penetration', () => {
  it('flags a rectangle that pokes into a non-convex polygon body', () => {
    const r = buildPlanFootprint('r', 2000, 200, 600, 0, 200, 0, 2600); // inside the L base arm
    expect(penetratesAny(r, [lShape])).toBe(true);
  });

  it('does not flag a rectangle sitting in the polygon notch (the missing corner)', () => {
    const r = buildPlanFootprint('r', 1500, 1500, 1000, 0, 200, 0, 2600); // in the cut-out notch
    expect(penetratesAny(r, [lShape])).toBe(false);
  });

  it('does not flag overlap when the z-ranges are disjoint (resting above)', () => {
    const above = buildPlanFootprint('r', 2000, 200, 600, 0, 200, 2600, 2750);
    expect(penetratesAny(above, [lShape])).toBe(false);
  });

  it('flags an overlap even when a probed corner lands exactly on the other edge', () => {
    const square = buildPolygonFootprint(
      'sq',
      [
        { x: 0, y: 0 },
        { x: 2000, y: 0 },
        { x: 2000, y: 2000 },
        { x: 0, y: 2000 },
      ],
      0,
      2600,
    );
    // Inside the square but with one corner exactly on its right edge (x=2000).
    const r = buildPlanFootprint('r', 2000, 1000, 600, 180, 300, 0, 2600);
    expect(penetratesAny(r, [square])).toBe(true);
  });
});

describe('restElevationMm (stacking)', () => {
  const wallTop = buildPlanFootprint('wall', 0, 0, 2000, 0, 100, 0, 2600);
  const lowRun = buildPlanFootprint('run', 0, 0, 2000, 0, 25, 0, 2400);

  it('lifts a mover onto the tallest support it overlaps', () => {
    const mover = buildPlanFootprint('mover', 0, -200, 2000, 0, 400, 2450, 2600);
    expect(restElevationMm(mover, [wallTop, lowRun], 2450)).toBe(2600);
  });

  it('keeps the fallback elevation when nothing is overlapped', () => {
    const mover = buildPlanFootprint('mover', 9000, 9000, 1000, 0, 400, 2450, 2600);
    expect(restElevationMm(mover, [wallTop, lowRun], 2450)).toBe(2450);
  });

  it('ignores supports shorter than the current resting height', () => {
    const mover = buildPlanFootprint('mover', 0, -200, 2000, 0, 400, 2500, 2650);
    expect(restElevationMm(mover, [lowRun], 2500)).toBe(2500);
  });

  it('never rests a footprint on itself', () => {
    expect(restElevationMm(wallTop, [wallTop], 0)).toBe(0);
  });
});
