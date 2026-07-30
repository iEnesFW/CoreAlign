import { describe, expect, it } from 'vitest';
import {
  restElevationMm,
  restsOnSupportAtMm,
  supportTopBelowMm,
  WALKABLE_STEP_UP_MM,
} from '@/shared/three-engine';
import { buildSlabFootprint, buildWallFootprint } from './planCollision';
import type { PlanFootprint } from './planCollision';
import type { SceneSlabState, SceneWallState } from '../../model/project.types';

const slab = (
  id: string,
  originX: number,
  originY: number,
  lengthMm: number,
  depthMm: number,
  elevationMm: number,
): SceneSlabState => ({
  id,
  kind: 'floor',
  originX,
  originY,
  rotationDeg: 0,
  lengthMm,
  depthMm,
  thicknessMm: 100,
  elevationMm,
});

describe('supportTopBelowMm — the one resolver gravity uses', () => {
  const support = slab('support', 0, 0, 1000, 1000, 0);
  const supportFp = buildSlabFootprint(support, 0, 0, support.rotationDeg);
  const supportTopMm = support.elevationMm + support.thicknessMm;

  it('rests a body squarely on the support', () => {
    const resting = slab('b', 0, 0, 800, 800, supportTopMm);
    const fp = buildSlabFootprint(resting, 0, 0, resting.rotationDeg);
    expect(supportTopBelowMm(fp, [supportFp], supportTopMm)).toBe(supportTopMm);
  });

  it('still rests an OVERHANGING body — overlap, not a centre point', () => {
    // Its centre is past the pedestal edge; a centre probe read ground and the body could never
    // descend again. Overlap sees the pedestal.
    const overhanging = slab('b', 900, 0, 2000, 800, supportTopMm);
    const fp = buildSlabFootprint(overhanging, 0, 0, overhanging.rotationDeg);
    expect(supportTopBelowMm(fp, [supportFp], supportTopMm)).toBe(supportTopMm);
  });

  it('IGNORES a support whose top is above the body — this is the wall-flies-up bug', () => {
    // A roof slab overhead, or a wall standing on the floor you are dragging: overlapping in plan
    // but ABOVE. Taking its top teleported the dragged body up to it.
    const overhead = slab('roof', 0, 0, 1000, 1000, 2600);
    const overheadFp = buildSlabFootprint(overhead, 0, 0, overhead.rotationDeg);
    const onGround = slab('b', 0, 0, 800, 800, 0);
    const fp = buildSlabFootprint(onGround, 0, 0, onGround.rotationDeg);
    expect(supportTopBelowMm(fp, [overheadFp], 0)).toBe(0);
  });

  it('never treats a body as its own support', () => {
    const self = slab('b', 0, 0, 800, 800, 500);
    const fp = buildSlabFootprint(self, 0, 0, self.rotationDeg);
    expect(supportTopBelowMm(fp, [fp], 500)).toBe(0);
  });

  it('falls to the ground when nothing is underneath', () => {
    const away = slab('b', 9000, 9000, 800, 800, 1500);
    const fp = buildSlabFootprint(away, 0, 0, away.rotationDeg);
    expect(supportTopBelowMm(fp, [supportFp], 1500)).toBe(0);
  });

  it('picks the HIGHEST support that is still below', () => {
    const low = slab('low', 0, 0, 1000, 1000, 0);
    const mid = slab('mid', 0, 0, 1000, 1000, 400);
    const lowFp = buildSlabFootprint(low, 0, 0, low.rotationDeg);
    const midFp = buildSlabFootprint(mid, 0, 0, mid.rotationDeg);
    const body = slab('b', 0, 0, 800, 800, 900);
    const fp = buildSlabFootprint(body, 0, 0, body.rotationDeg);
    expect(supportTopBelowMm(fp, [lowFp, midFp], 900)).toBe(500);
  });

  it('explicit Alt-stack still climbs onto something taller (restElevationMm is unguarded)', () => {
    const tall = slab('tall', 0, 0, 1000, 1000, 2000);
    const tallFp = buildSlabFootprint(tall, 0, 0, tall.rotationDeg);
    const body = slab('b', 0, 0, 800, 800, 0);
    const fp = buildSlabFootprint(body, 0, 0, body.rotationDeg);
    expect(restElevationMm(fp, [tallFp], 0)).toBe(2100);
  });
});

describe('restsOnSupportAtMm', () => {
  const support = slab('support', 0, 0, 1000, 1000, 0);
  const supportFp = buildSlabFootprint(support, 0, 0, support.rotationDeg);
  const supportTopMm = support.elevationMm + support.thicknessMm;

  it('reports an OVERHANGING stack as resting (the centre probe does not)', () => {
    const overhanging = slab('b', 900, 0, 2000, 800, supportTopMm);
    const fp = buildSlabFootprint(overhanging, 0, 0, overhanging.rotationDeg);

    expect(restsOnSupportAtMm(fp, [supportFp], overhanging.elevationMm, 5)).toBe(true);
  });

  it('reports a squarely stacked object as resting', () => {
    const squarely = slab('b', 0, 0, 800, 800, supportTopMm);
    const fp = buildSlabFootprint(squarely, 0, 0, squarely.rotationDeg);

    expect(restsOnSupportAtMm(fp, [supportFp], squarely.elevationMm, 5)).toBe(true);
  });

  it('reports a genuinely floating object as NOT resting', () => {
    const floating = slab('b', 0, 0, 800, 800, 1500);
    const fp = buildSlabFootprint(floating, 0, 0, floating.rotationDeg);

    expect(restsOnSupportAtMm(fp, [supportFp], floating.elevationMm, 5)).toBe(false);
  });

  it('a ground-level slab overlapped by a TALL wall still reads as resting', () => {
    // A wall standing on the floor overlaps it in plan; its top must not be mistaken for the
    // floor's support (that would report the floor as floating and break gravity for it).
    const wall: SceneWallState = {
      id: 'w',
      originX: 0,
      originY: 0,
      lengthMm: 1000,
      rotationDeg: 0,
      heightMm: 2600,
      heightEndMm: null,
      thicknessMm: 200,
      colorHex: null,
      openings: [],
      features: [],
    };
    const wallFp = buildWallFootprint(wall, 0, 0, wall.rotationDeg);
    const floor = slab('floor', 0, 0, 4000, 4000, 0);
    const floorFp = buildSlabFootprint(floor, 0, 0, floor.rotationDeg);

    expect(restsOnSupportAtMm(floorFp, [wallFp], floor.elevationMm, 5)).toBe(true);
  });

  it('ignores a support whose top is above the object base', () => {
    const tall = slab('tall', 0, 0, 1000, 1000, 3000);
    const tallFp = buildSlabFootprint(tall, 0, 0, tall.rotationDeg);
    const lowFloating = slab('b', 0, 0, 800, 800, 1500);
    const fp = buildSlabFootprint(lowFloating, 0, 0, lowFloating.rotationDeg);

    expect(restsOnSupportAtMm(fp, [tallFp], lowFloating.elevationMm, 5)).toBe(false);
  });
});

describe('placement probe: the top of what is UNDER the ghost', () => {
  // Placement asks the same resolver with baseMm = +Infinity (nothing is "above" a body that has
  // not been placed) and a -Infinity ground sentinel so "no support at all" is distinguishable
  // from "rests on the ground".
  const NO_SUPPORT = Number.NEGATIVE_INFINITY;
  const topUnder = (ghost: PlanFootprint, supports: PlanFootprint[]) =>
    supportTopBelowMm(ghost, supports, Number.POSITIVE_INFINITY, NO_SUPPORT, 5);

  const wallAt = (id: string, x: number, geomZ: number, heightMm = 2600) =>
    buildWallFootprint(
      {
        id,
        originX: x,
        originY: 0,
        rotationDeg: 0,
        lengthMm: 3000,
        heightMm,
        thicknessMm: 200,
        geomZ,
        openings: [],
        features: [],
      } as unknown as SceneWallState,
      0,
      0,
      0,
    );

  it('honours a raised wall geomZ — the old resolver read heightMm alone', () => {
    // A 2600 wall standing on a 400 deck tops out at 3000, not 2600.
    const wall = wallAt('w', 0, 400);
    const ghost = buildSlabFootprint(slab('ghost', 0, 0, 2000, 2000, 0), 0, 0, 0);
    expect(topUnder(ghost, [wall])).toBe(3000);
  });

  it('ignores a wall the ghost is NOT over — the old resolver took the NEAREST centreline', () => {
    const wall = wallAt('w', 0, 0);
    const away = buildSlabFootprint(slab('ghost', 40000, 40000, 2000, 2000, 0), 0, 0, 0);
    expect(topUnder(away, [wall])).toBe(NO_SUPPORT);
  });

  it('can rest a roof on another roof — slabs were not considered at all before', () => {
    const lower = buildSlabFootprint(slab('roof1', 0, 0, 4000, 4000, 2600), 0, 0, 0);
    const ghost = buildSlabFootprint(slab('ghost', 0, 0, 2000, 2000, 0), 0, 0, 0);
    expect(topUnder(ghost, [lower])).toBe(2700);
  });

  it('takes the HIGHEST overlapping support', () => {
    const low = wallAt('low', 0, 0, 2000);
    const high = wallAt('high', 0, 0, 3200);
    const ghost = buildSlabFootprint(slab('ghost', 0, 0, 2000, 2000, 0), 0, 0, 0);
    expect(topUnder(ghost, [low, high])).toBe(3200);
  });
});

describe('step-up onto a WALKABLE floor — the drag climb probe', () => {
  // A 100 mm platform: a floor slab authored at elevation 0 tops out at its thickness.
  const platform = buildSlabFootprint(slab('platform', 0, 0, 4000, 3000, 0), 0, 0, 0);
  const standingWall = (dxMm: number) =>
    buildWallFootprint(
      {
        id: 'w',
        originX: -3500 + dxMm,
        originY: 1500,
        rotationDeg: 0,
        lengthMm: 2000,
        heightMm: 2400,
        thicknessMm: 200,
        geomZ: 0,
        openings: [],
        features: [],
      } as unknown as SceneWallState,
      0,
      0,
      0,
    );

  it('a wall dragged onto the platform climbs onto it', () => {
    // RED before the fix: supportTopBelowMm could never exceed base + tolerance, so the climb
    // branch in useObjectGestures was structurally dead and the wall was pinned at the edge.
    const over = standingWall(3600);
    expect(supportTopBelowMm(over, [platform], 0, 0, 5, WALKABLE_STEP_UP_MM)).toBe(100);
  });

  it('gravity (no step-up allowance) still refuses to see the platform', () => {
    const over = standingWall(3600);
    expect(supportTopBelowMm(over, [platform], 0)).toBe(0);
  });

  it('a wall clear of the platform stays on the ground', () => {
    expect(supportTopBelowMm(standingWall(0), [platform], 0, 0, 5, WALKABLE_STEP_UP_MM)).toBe(0);
  });

  it('does NOT climb a non-walkable body of the same height', () => {
    const curb = buildSlabFootprint(
      { ...slab('curb', 0, 0, 4000, 3000, 0), kind: 'roof' } as SceneSlabState,
      0,
      0,
      0,
    );
    expect(supportTopBelowMm(standingWall(3600), [curb], 0, 0, 5, WALKABLE_STEP_UP_MM)).toBe(0);
  });

  it('does NOT climb a storey-high walkable deck — that still needs Alt', () => {
    const mezzanine = buildSlabFootprint(slab('mezz', 0, 0, 4000, 3000, 3000), 0, 0, 0);
    expect(supportTopBelowMm(standingWall(3600), [mezzanine], 0, 0, 5, WALKABLE_STEP_UP_MM)).toBe(
      0,
    );
  });
});
