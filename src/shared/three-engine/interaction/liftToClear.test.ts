import { describe, expect, it } from 'vitest';
import { buildPlanFootprint, liftToClearMm, supportTopBelowMm } from './planCollision';
import type { PlanFootprint } from './planCollision';

/**
 * A floating body (floor plate, roof, podium slab) has NO lateral collision by design, so the
 * vertical axis is the only thing keeping it out of solid matter. Two field reports came from the
 * gravity resolver being used here instead:
 *
 *   "objects I put on the floor ended up INSIDE it after I moved the floor and put it back,
 *    and the floor's thickness changed"   → the plate is authored at −150 so its top is flush with
 *    grade; the ground fallback of 0 ratcheted it up by its own thickness.
 *
 *   "when I move the roof and there is nothing under it, it drops to the ground"
 *    → same fallback, from 2600 straight to 0.
 */

const box = (
  id: string,
  xMm: number,
  yMm: number,
  lengthMm: number,
  halfDepthMm: number,
  zMinMm: number,
  zMaxMm: number,
  walkable = false,
): PlanFootprint => ({
  ...buildPlanFootprint(id, xMm, yMm, lengthMm, 0, halfDepthMm, zMinMm, zMaxMm),
  walkable,
});

// A 4 m × 3 m floor plate whose TOP is flush with grade.
const floorPlate = (baseMm = -150) => box('floor', 0, 1500, 4000, 1500, baseMm, baseMm + 150, true);
const FLOOR_THICKNESS_MM = 150;

// A wall standing on that plate.
const wallOnFloor = box('wall', 0, 100, 4000, 100, 0, 2600);

describe('liftToClearMm — a floating body keeps its height and never enters solid matter', () => {
  it('a floor plate authored below grade STAYS below grade', () => {
    const moved = floorPlate();
    expect(liftToClearMm(moved, [wallOnFloor], -150, FLOOR_THICKNESS_MM)).toBe(-150);
  });

  it('the gravity resolver is what used to raise it — proof the two must not be swapped', () => {
    const moved = floorPlate();
    // supportTopBelowMm falls back to the GROUND (0), i.e. 150 mm above where the plate belongs.
    expect(supportTopBelowMm(moved, [wallOnFloor], -150)).toBe(0);
  });

  it('a roof over open ground keeps its elevation instead of dropping', () => {
    const roof = box('roof', 0, 1500, 4000, 1500, 2600, 2750);
    expect(liftToClearMm(roof, [], 2600, 150)).toBe(2600);
  });

  it('a roof dragged over a TALLER wall rises to sit on it', () => {
    const roof = box('roof', 0, 1500, 4000, 1500, 2600, 2750);
    const tallWall = box('tall', 0, 1500, 4000, 150, 0, 3000);
    expect(liftToClearMm(roof, [tallWall], 2600, 150)).toBe(3000);
  });

  it('a roof passing over a LOWER wall is untouched — it clears it already', () => {
    const roof = box('roof', 0, 1500, 4000, 1500, 2600, 2750);
    const lowWall = box('low', 0, 1500, 4000, 150, 0, 2000);
    expect(liftToClearMm(roof, [lowWall], 2600, 150)).toBe(2600);
  });

  it('touching is not intersecting — a plate whose top meets a wall base stays put', () => {
    const plate = floorPlate();
    // wallOnFloor starts exactly at 0, the plate's top.
    expect(liftToClearMm(plate, [wallOnFloor], -150, FLOOR_THICKNESS_MM)).toBe(-150);
  });

  it('a slab dragged onto a podium steps up onto it', () => {
    const slab = box('slab', 0, 1500, 2000, 1000, 0, 150);
    const podium = box('podium', 0, 1500, 4000, 1500, 0, 600, true);
    expect(liftToClearMm(slab, [podium], 0, 150)).toBe(600);
  });

  it('clearing one obstacle may reveal a second — it resolves in one call', () => {
    const slab = box('slab', 0, 1500, 4000, 1500, 0, 150);
    const low = box('low', 0, 1500, 4000, 1500, 0, 400);
    // Only reachable after the first lift: it starts above the slab and below the lifted position.
    const upper = box('upper', 0, 1500, 4000, 1500, 300, 900);
    expect(liftToClearMm(slab, [low, upper], 0, 150)).toBe(900);
  });

  it('a body never lifts itself', () => {
    const slab = box('slab', 0, 1500, 4000, 1500, 0, 900);
    expect(liftToClearMm(slab, [slab], 0, 900)).toBe(0);
  });

  it('a body clear of everything in PLAN is untouched no matter how tall the neighbour is', () => {
    const slab = box('slab', 0, 1500, 2000, 1000, 0, 150);
    const faraway = box('far', 50_000, 1500, 4000, 1500, 0, 9000);
    expect(liftToClearMm(slab, [faraway], 0, 150)).toBe(0);
  });
});
