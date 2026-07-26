import { describe, expect, it } from 'vitest';
import { restElevationAtPointMm, restElevationMm, restsOnSupportAtMm } from '@/shared/three-engine';
import { buildSlabFootprint, buildWallFootprint } from './planCollision';
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

const centreOf = (s: SceneSlabState) => ({
  x: s.originX + s.lengthMm / 2,
  y: s.originY,
});

describe('resting detection: overlap probe vs centre probe', () => {
  // The support: a small pedestal at the origin, top surface at 100 mm.
  const support = slab('support', 0, 0, 1000, 1000, 0);
  const supportFp = buildSlabFootprint(support, 0, 0, support.rotationDeg);
  const supportTopMm = support.elevationMm + support.thicknessMm;

  it('a slab resting squarely on the support agrees on both probes', () => {
    const resting = slab('b', 0, 0, 800, 800, supportTopMm);
    const fp = buildSlabFootprint(resting, 0, 0, resting.rotationDeg);
    const c = centreOf(resting);

    expect(restElevationMm(fp, [supportFp], 0)).toBe(supportTopMm);
    expect(restElevationAtPointMm(c.x, c.y, [supportFp], 0)).toBe(supportTopMm);
  });

  it('a slab that overhangs the support is committed by overlap but reads as NOT resting by centre', () => {
    // Sits ON the pedestal (its base == the pedestal top) but hangs far enough that its CENTRE
    // is past the pedestal edge — exactly what an off-centre stack drop produces.
    const overhanging = slab('b', 900, 0, 2000, 800, supportTopMm);
    const fp = buildSlabFootprint(overhanging, 0, 0, overhanging.rotationDeg);
    const c = centreOf(overhanging);

    // The commit path (explicit stack / free-move slab) resolves the elevation by OVERLAP:
    expect(restElevationMm(fp, [supportFp], 0)).toBe(supportTopMm);

    // The resting test reads the CENTRE instead — and the centre is off the support:
    expect(restElevationAtPointMm(c.x, c.y, [supportFp], 0)).toBe(0);

    // So the two disagree: the object was legitimately placed on the support, yet the centre
    // probe reports ground. restingAtStart (|centreProbe - base| < 5) is therefore false, and
    // useObjectGestures falls into `Math.max(baseYM, centerRest)` — the object can never descend.
    const centreProbe = restElevationAtPointMm(c.x, c.y, [supportFp], 0);
    const restingAtStart = Math.abs(centreProbe - overhanging.elevationMm) < 5;
    expect(restingAtStart).toBe(false);
  });

  it('a slab standing on the ground reads as resting (ground fallback agrees)', () => {
    const onGround = slab('b', 9000, 9000, 800, 800, 0);
    const c = centreOf(onGround);

    const centreProbe = restElevationAtPointMm(c.x, c.y, [supportFp], 0);
    expect(Math.abs(centreProbe - onGround.elevationMm) < 5).toBe(true);
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
