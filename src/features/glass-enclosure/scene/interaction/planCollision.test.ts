import { describe, expect, it } from 'vitest';
import { buildRunFootprint, buildWallFootprint, penetratesAny } from './planCollision';
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
  geomArcSweepDeg: arcRadiusMm ? 30 : null,
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
