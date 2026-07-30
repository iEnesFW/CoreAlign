import { describe, expect, it } from 'vitest';
import {
  buildPlanFootprint,
  buildRunFootprint,
  buildWallFootprint,
  clampPlanMove,
  clampPlanMoveNoDeepen,
  clampPlanRotation,
  clampPlanStretch,
  penetratesAny,
  slidePlanMove,
} from './planCollision';
import type { SceneRunState, SceneWallState } from '../../model/project.types';

const wall = (id: string, originX: number, originY: number, lengthMm: number): SceneWallState => ({
  id,
  originX,
  originY,
  lengthMm,
  rotationDeg: 0,
  heightMm: 2600,
  heightEndMm: null,
  thicknessMm: 200,
  colorHex: null,
  openings: [],
  features: [],
});

const run = (
  id: string,
  originX: number,
  originY: number,
  lengthMm: number,
  arcRadiusMm = 0,
): SceneRunState => ({
  id,
  orderIndex: 0,
  label: id,
  lengthMm,
  heightMm: 2400,
  originX,
  originY,
  rotationDeg: 0,
  profileSystemId: 'ps',
  colorId: null,
  hasTopDrip: true,
  hasBottomThreshold: false,
  geomZ: 0,
  geomArcRadiusMm: arcRadiusMm || null,
  geomArcSweepDeg: arcRadiusMm ? 120 : null,
  panels: [],
});

describe('wall <-> run collision (no interpenetration)', () => {
  it('a wall and a glass run now block each other when overlapping', () => {
    const w = buildWallFootprint(wall('w', 0, 0, 2000), 0, 0, 0);
    const r = buildRunFootprint(run('r', 500, 0, 2000), 0, 0, 0);
    expect(penetratesAny(r, [w])).toBe(true);
  });

  it('a run sitting clearly in front of a wall does not collide', () => {
    const w = buildWallFootprint(wall('w', 0, 0, 2000), 0, 0, 0);
    const r = buildRunFootprint(run('r', 0, 400, 2000), 0, 0, 0);
    expect(penetratesAny(r, [w])).toBe(false);
  });

  it('an arc run uses a curved polygon footprint that blocks inside its bow', () => {
    const arc = buildRunFootprint(run('arc', 0, 0, 3000, 1500), 0, 0, 0);
    expect(arc.polygon).toBeDefined();
    expect((arc.polygon ?? []).length).toBeGreaterThan(6);
    const inside = buildWallFootprint(wall('w', 800, 400, 400), 0, 0, 0);
    expect(penetratesAny(inside, [arc])).toBe(true);
  });

  it('an arc wall uses a curved polygon footprint', () => {
    const straight = buildWallFootprint(wall('ws', 0, 0, 3000), 0, 0, 0);
    const curved = buildWallFootprint(
      { ...wall('wc', 0, 0, 3000), geomArcRadiusMm: 1500, geomArcSweepDeg: 90 },
      0,
      0,
      0,
    );
    expect((straight.polygon ?? []).length).toBeLessThanOrEqual(4);
    expect(curved.polygon).toBeDefined();
    expect((curved.polygon ?? []).length).toBeGreaterThan(6);
  });
});

describe('slidePlanMove / clampPlanMoveNoDeepen (objects never interpenetrate further)', () => {
  const wallFp = buildWallFootprint(wall('w', 0, 0, 2000), 0, 0, 0); // X[0,2000] Y[-100,100]

  it('clamps a clear run before it penetrates a wall (no new overlap)', () => {
    const r = run('r', 500, 140, 1000); // Y band [115,165], clear of the wall (top 100)
    const fp = (dx: number, dy: number) => buildRunFootprint(r, dx, dy, 0);
    const slid = slidePlanMove(fp, [wallFp], 0, -100); // drive straight into the wall
    expect(Math.abs(slid.dyMm)).toBeLessThan(100); // stopped at contact, not full travel
    expect(penetratesAny(fp(slid.dxMm, slid.dyMm), [wallFp])).toBe(false);
  });

  it('does not let an already-overlapping run be pushed deeper', () => {
    const r = run('r', 500, 90, 1000); // Y band [65,115], already overlaps the wall
    const fp = (dx: number, dy: number) => buildRunFootprint(r, dx, dy, 0);
    expect(penetratesAny(fp(0, 0), [wallFp])).toBe(true);
    const slid = slidePlanMove(fp, [wallFp], 0, -80); // push deeper into the wall (−Y)
    // The deepening move is essentially fully blocked — at most the no-deepen epsilon of
    // travel toward the wall, never the requested 80 mm. (Retreat is covered by the next test.)
    expect(slid.dyMm).toBeLessThanOrEqual(0);
    expect(slid.dyMm).toBeGreaterThanOrEqual(-5);
  });

  it('lets an already-overlapping run slide free of the overlap', () => {
    const r = run('r', 500, 90, 1000);
    const fp = (dx: number, dy: number) => buildRunFootprint(r, dx, dy, 0);
    const slid = slidePlanMove(fp, [wallFp], 0, 80); // move away from the wall
    expect(slid.dyMm).toBe(80); // full retreat allowed (depth only shrinks)
  });

  it('clampPlanMoveNoDeepen returns the full move when nothing is in the way', () => {
    const r = run('r', 0, 0, 1000);
    const fp = (dx: number, dy: number) => buildRunFootprint(r, dx, dy, 0);
    expect(clampPlanMoveNoDeepen(fp, [], 250, 0)).toEqual({ dxMm: 250, dyMm: 0 });
  });
});

describe('clamped moves never land INSIDE an obstacle (rounding must not overshoot)', () => {
  // POS-F4 claimed Math.round(dxMm * lo) can push the clamped delta past the contact boundary.
  // Randomised sweep over body sizes, angles and approach vectors; any penetrating result fails.
  const box = (
    id: string,
    originX: number,
    originY: number,
    lengthMm: number,
    crossMm: number,
    rotationDeg: number,
    zMin = 0,
    zMax = 2400,
  ) => buildPlanFootprint(id, originX, originY, lengthMm, rotationDeg, crossMm / 2, zMin, zMax);

  it('holds over 4000 randomised approaches', () => {
    let rng = 20260730;
    const next = () => {
      rng = (rng * 1103515245 + 12345) % 2147483648;
      return rng / 2147483648;
    };
    let overshoots = 0;
    for (let i = 0; i < 4000; i += 1) {
      const obsRot = Math.round(next() * 360);
      const obstacle = box(
        'obs',
        Math.round(next() * 2000 - 1000),
        Math.round(next() * 2000 - 1000),
        200 + Math.round(next() * 4000),
        50 + Math.round(next() * 800),
        obsRot,
      );
      const movedRot = Math.round(next() * 360);
      const startX = Math.round(next() * 8000 - 4000);
      const startY = Math.round(next() * 8000 - 4000);
      const lengthMm = 200 + Math.round(next() * 3000);
      const crossMm = 50 + Math.round(next() * 400);
      const dxMm = Math.round(next() * 12000 - 6000);
      const dyMm = Math.round(next() * 12000 - 6000);

      const footprintAt = (dx: number, dy: number) =>
        box('moved', startX + dx, startY + dy, lengthMm, crossMm, movedRot);

      if (penetratesAny(footprintAt(0, 0), [obstacle])) continue;
      const clamped = clampPlanMove(footprintAt, [obstacle], dxMm, dyMm);
      if (penetratesAny(footprintAt(clamped.dxMm, clamped.dyMm), [obstacle])) overshoots += 1;
    }
    expect(overshoots).toBe(0);
  });
});

describe('stretch and rotation clamps also respect the contact boundary', () => {
  const box = (id: string, x: number, y: number, l: number, c: number, rot: number) =>
    buildPlanFootprint(id, x, y, l, rot, c / 2, 0, 2400);

  it('a clamped stretch never ends inside the neighbour', () => {
    let rng = 8675309;
    const next = () => {
      rng = (rng * 1103515245 + 12345) % 2147483648;
      return rng / 2147483648;
    };
    let bad = 0;
    for (let i = 0; i < 2000; i += 1) {
      const obstacle = box(
        'obs',
        Math.round(next() * 3000 - 1500),
        Math.round(next() * 3000 - 1500),
        300 + Math.round(next() * 3000),
        60 + Math.round(next() * 500),
        Math.round(next() * 360),
      );
      const startX = Math.round(next() * 4000 - 2000);
      const startY = Math.round(next() * 4000 - 2000);
      const rot = Math.round(next() * 360);
      const baseLen = 300 + Math.round(next() * 2000);
      const cross = 60 + Math.round(next() * 300);
      const deltaMm = Math.round(next() * 6000 - 3000);
      const at = (d: number) => box('moved', startX, startY, Math.max(50, baseLen + d), cross, rot);
      if (penetratesAny(at(0), [obstacle])) continue;
      const clamped = clampPlanStretch(at, [obstacle], deltaMm);
      if (penetratesAny(at(clamped), [obstacle])) bad += 1;
    }
    expect(bad).toBe(0);
  });

  it('a clamped rotation never ends inside the neighbour', () => {
    let rng = 424242;
    const next = () => {
      rng = (rng * 1103515245 + 12345) % 2147483648;
      return rng / 2147483648;
    };
    let bad = 0;
    for (let i = 0; i < 2000; i += 1) {
      const obstacle = box(
        'obs',
        Math.round(next() * 2000 - 1000),
        Math.round(next() * 2000 - 1000),
        300 + Math.round(next() * 3000),
        60 + Math.round(next() * 500),
        Math.round(next() * 360),
      );
      const startX = Math.round(next() * 3000 - 1500);
      const startY = Math.round(next() * 3000 - 1500);
      const len = 300 + Math.round(next() * 2500);
      const cross = 60 + Math.round(next() * 300);
      const fromDeg = Math.round(next() * 360);
      const toDeg = fromDeg + Math.round(next() * 300 - 150);
      const at = (deg: number) => box('moved', startX, startY, len, cross, deg);
      if (penetratesAny(at(fromDeg), [obstacle])) continue;
      const clamped = clampPlanRotation(at, [obstacle], fromDeg, toDeg);
      if (penetratesAny(at(clamped), [obstacle])) bad += 1;
    }
    expect(bad).toBe(0);
  });
});
