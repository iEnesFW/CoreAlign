import { describe, expect, it } from 'vitest';
import {
  AUTO_STACK_MIN_PUSH_MM,
  autoStackEngaged,
  buildPlanFootprint,
  buildPolygonFootprint,
  isFloating,
  penetratesAny,
  restElevationMm,
  slidePlanMove,
} from './planCollision';
import type { PlanFootprint } from './planCollision';

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
    const b = wall('b', 1500, 2000);
    expect(penetratesAny(a, [b])).toBe(true);
  });

  it('allows a perpendicular corner butt-joint (overlap within one half width)', () => {
    const a = wall('a', 0, 2000, 0);
    const b = buildPlanFootprint('b', 2000, 0, 2000, 90, 100, 0, 2600);
    expect(penetratesAny(a, [b])).toBe(false);
  });

  it('blocks a perpendicular wall that slices deep along another wall end face', () => {
    const a = wall('a', 0, 2000, 0);
    const b = buildPlanFootprint('b', 2000, 100, 2000, 270, 100, 0, 2600);
    expect(penetratesAny(a, [b])).toBe(true);
  });

  it('blocks two parallel walls overlapping side by side along their length', () => {
    const a = wall('a', 0, 2000, 0);
    const b = buildPlanFootprint('b', 0, 150, 2000, 0, 100, 0, 2600);
    expect(penetratesAny(a, [b])).toBe(true);
  });
});

const moverAt = (dxMm: number, dyMm: number): PlanFootprint =>
  buildPlanFootprint('mover', -500 + dxMm, -60 + dyMm, 1000, 0, 25, 0, 2400);

describe('slidePlanMove swept collision', () => {
  it('stops a long drag flush at the near face of a thin obstacle (no tunneling)', () => {
    const obstacle = buildPlanFootprint('run', -500, 0, 1000, 0, 25, 0, 2400);
    const result = slidePlanMove(moverAt, [obstacle], 0, 3000);
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

  it('escapes an existing overlap but still cannot tunnel through a different object (B1)', () => {
    const overlapping = buildPlanFootprint('a', -500, -60, 1000, 0, 25, 0, 2400); // overlaps the mover at start
    const ahead = buildPlanFootprint('b', -500, 1000, 1000, 0, 25, 0, 2400); // a fresh object in the path
    const result = slidePlanMove(moverAt, [overlapping, ahead], 0, 3000);
    expect(result.dyMm).toBeGreaterThan(500); // moved free of the overlap
    expect(result.dyMm).toBeLessThan(1100); // but stopped at B, did not pass through to 3000
  });

  it('moves freely out of an existing overlap when nothing else is in the path (B1)', () => {
    const overlapping = buildPlanFootprint('a', -500, -60, 1000, 0, 25, 0, 2400);
    const result = slidePlanMove(moverAt, [overlapping], 0, 3000);
    expect(result.dyMm).toBe(3000);
  });
});

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
    const r = buildPlanFootprint('r', 2000, 200, 600, 0, 200, 0, 2600);
    expect(penetratesAny(r, [lShape])).toBe(true);
  });

  it('does not flag a rectangle sitting in the polygon notch (the missing corner)', () => {
    const r = buildPlanFootprint('r', 1500, 1500, 1000, 0, 200, 0, 2600);
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

describe('isFloating', () => {
  const wallTop = buildPlanFootprint('wall', 0, 0, 2000, 0, 100, 0, 2600);

  it('flags a roof hanging far above the wall it should rest on', () => {
    const roof = buildPlanFootprint('roof', 0, -200, 2000, 0, 400, 3000, 3150);
    expect(isFloating(roof, [wallTop], 50)).toBe(true);
  });

  it('does not flag a roof resting on the wall top', () => {
    const roof = buildPlanFootprint('roof', 0, -200, 2000, 0, 400, 2600, 2750);
    expect(isFloating(roof, [wallTop], 50)).toBe(false);
  });

  it('does not flag an object at/near the ground', () => {
    const floor = buildPlanFootprint('floor', 0, 0, 2000, 0, 400, 0, 150);
    expect(isFloating(floor, [wallTop], 50)).toBe(false);
  });

  it('does not flag a roof embedded in a wall taller than its base', () => {
    const roof = buildPlanFootprint('roof', 0, -200, 2000, 0, 400, 2000, 2150);
    expect(isFloating(roof, [wallTop], 50)).toBe(false);
  });
});

describe('autoStackEngaged (plain-drag climb-on-top trigger)', () => {
  const push = AUTO_STACK_MIN_PUSH_MM;
  it('engages when a firm push is blocked AND lands on a higher surface', () => {
    expect(autoStackEngaged(push + 1, 2.6, 0)).toBe(true);
  });

  it('does not engage for a gentle butt-flush (small block, no deep push)', () => {
    expect(autoStackEngaged(push - 1, 2.6, 0)).toBe(false);
  });

  it('does not engage when there is no surface to rest on (rest == base)', () => {
    expect(autoStackEngaged(push + 200, 0, 0)).toBe(false);
  });

  it('does not engage when the rest surface is below the current base (a drop, not a climb)', () => {
    expect(autoStackEngaged(push + 200, 0.5, 1)).toBe(false);
  });

  it('engages when climbing from an already-elevated base onto something higher', () => {
    expect(autoStackEngaged(push + 50, 2.6, 1)).toBe(true);
  });
});
