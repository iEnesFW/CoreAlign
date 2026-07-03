import { describe, expect, it } from 'vitest';
import { findAttachedRunIds } from './wallAttachment';
import { arcPointAt, radiusFromChordSweep } from './arcGeometry';
import type { SceneRunState, SceneWallState } from './project.types';

const wall = (overrides: Partial<SceneWallState> = {}): SceneWallState => ({
  id: 'w',
  originX: 0,
  originY: 0,
  lengthMm: 3000,
  rotationDeg: 0,
  heightMm: 2600,
  heightEndMm: null,
  thicknessMm: 200,
  colorHex: null,
  geomZ: 0,
  openings: [],
  features: [],
  ...overrides,
});

const run = (
  originX: number,
  originY: number,
  rotationDeg: number,
  lengthMm: number,
): SceneRunState => ({
  id: 'run-1',
  orderIndex: 0,
  label: 'r',
  lengthMm,
  heightMm: 2000,
  originX,
  originY,
  rotationDeg,
  profileSystemId: 'ps',
  colorId: null,
  hasTopDrip: true,
  hasBottomThreshold: false,
  geomZ: 0,
  panels: [],
});

describe('findAttachedRunIds', () => {
  it('attaches a run filling a straight wall opening', () => {
    const w = wall();
    const fill = run(800, 0, 0, 1000);
    expect(findAttachedRunIds(w, [fill])).toEqual(['run-1']);
  });

  it('attaches a run embedded mid-arc on a curved wall (the moved-wall-left-glass bug)', () => {
    const sweepDeg = 90;
    const w = wall({ lengthMm: 2828, geomArcRadiusMm: 2000, geomArcSweepDeg: sweepDeg });
    const r = radiusFromChordSweep(w.lengthMm, w.geomArcRadiusMm, w.geomArcSweepDeg);
    const p1 = arcPointAt(r, 1, (30 * Math.PI) / 180);
    const p2 = arcPointAt(r, 1, (60 * Math.PI) / 180);
    const chord = Math.hypot(p2.x - p1.x, p2.z - p1.z);
    const deg = (Math.atan2(p2.z - p1.z, p2.x - p1.x) * 180) / Math.PI;
    const fill = run(p1.x, p1.z, deg, Math.round(chord));
    expect(findAttachedRunIds(w, [fill])).toEqual(['run-1']);
  });

  it('does not attach a distant run to an arc wall', () => {
    const w = wall({ lengthMm: 2828, geomArcRadiusMm: 2000, geomArcSweepDeg: 90 });
    const far = run(6000, 6000, 0, 1000);
    expect(findAttachedRunIds(w, [far])).toEqual([]);
  });

  it('does not attach a run on the phantom straight axis of an arc wall', () => {
    const w = wall({ lengthMm: 2828, geomArcRadiusMm: 2000, geomArcSweepDeg: 90 });
    const phantom = run(1200, 0, 0, 600);
    expect(findAttachedRunIds(w, [phantom])).toEqual([]);
  });

  it('treats a half-arc wall (radius without sweep) as straight', () => {
    const w = wall({ geomArcRadiusMm: 2000, geomArcSweepDeg: null });
    const fill = run(800, 0, 0, 1000);
    expect(findAttachedRunIds(w, [fill])).toEqual(['run-1']);
  });
});
