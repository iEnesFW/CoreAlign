import { describe, expect, it } from 'vitest';
import {
  buildRunFootprint,
  buildWallFootprint,
  clampPlanMoveNoDeepen,
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
