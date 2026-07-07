import { describe, expect, it } from 'vitest';
import {
  findAttachedRunIds,
  findAttachedWallIds,
  moveWallWithAttachments,
  resolveAttachedRunIds,
  resolveAttachedWallIds,
} from './wallAttachment';
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

describe('resolveAttachedRunIds (persistent host bond)', () => {
  it('matches geometry when no run carries a host bond', () => {
    const w = wall();
    const fill = run(800, 0, 0, 1000);
    expect(resolveAttachedRunIds(w, [fill])).toEqual(findAttachedRunIds(w, [fill]));
  });

  it('keeps a bonded run that has DRIFTED out of the attach band', () => {
    const w = wall();
    // Far from the wall — geometry alone would drop it (the "once broken, never re-caught" bug).
    const drifted: SceneRunState = { ...run(9000, 9000, 0, 1000), hostWallId: 'w' };
    expect(findAttachedRunIds(w, [drifted])).toEqual([]);
    expect(resolveAttachedRunIds(w, [drifted])).toEqual(['run-1']);
  });

  it('does not claim a drifted run bonded to a different wall', () => {
    const w = wall({ id: 'w' });
    const other: SceneRunState = { ...run(9000, 9000, 0, 1000), hostWallId: 'other-wall' };
    expect(resolveAttachedRunIds(w, [other])).toEqual([]);
  });

  it('does not double-count a run that is both bonded and geometrically attached', () => {
    const w = wall();
    const both: SceneRunState = { ...run(800, 0, 0, 1000), hostWallId: 'w' };
    expect(resolveAttachedRunIds(w, [both])).toEqual(['run-1']);
  });
});

describe('resolveAttachedWallIds (persistent host bond)', () => {
  it('matches geometry when the run carries no host bond', () => {
    const w = wall({ id: 'w' });
    const fill = run(800, 0, 0, 1000);
    expect(resolveAttachedWallIds(fill, [w])).toEqual(findAttachedWallIds(fill, [w]));
  });

  it('returns the explicit host first even when the run has drifted off it', () => {
    const host = wall({ id: 'host' });
    const drifted: SceneRunState = { ...run(9000, 9000, 0, 1000), hostWallId: 'host' };
    expect(findAttachedWallIds(drifted, [host])).toEqual([]);
    expect(resolveAttachedWallIds(drifted, [host])).toEqual(['host']);
  });

  it('falls back to geometry when the host wall no longer exists', () => {
    const w = wall({ id: 'w' });
    const fill: SceneRunState = { ...run(800, 0, 0, 1000), hostWallId: 'deleted-wall' };
    expect(resolveAttachedWallIds(fill, [w])).toEqual(['w']);
  });

  it('does not duplicate the host when geometry already reports it', () => {
    const w = wall({ id: 'w' });
    const fill: SceneRunState = { ...run(800, 0, 0, 1000), hostWallId: 'w' };
    expect(resolveAttachedWallIds(fill, [w])).toEqual(['w']);
  });
});

describe('moveWallWithAttachments (rigid co-move)', () => {
  it('translates attached runs by the origin delta, rotation unchanged', () => {
    const before = { originX: 0, originY: 0, rotationDeg: 0 };
    const after = { originX: 500, originY: -200, rotationDeg: 0 };
    const [moved] = moveWallWithAttachments(before, after, [run(1000, 500, 30, 800)]);
    expect(moved.originX).toBe(1500);
    expect(moved.originY).toBe(300);
    expect(moved.rotationDeg).toBe(30);
  });

  it('rotates attached runs about the wall origin and advances their rotation', () => {
    const before = { originX: 0, originY: 0, rotationDeg: 0 };
    const after = { originX: 0, originY: 0, rotationDeg: 90 };
    const [moved] = moveWallWithAttachments(before, after, [run(2000, 0, 10, 800)]);
    expect(moved.originX).toBe(0);
    expect(moved.originY).toBe(2000);
    expect(moved.rotationDeg).toBe(100);
  });

  it('combines translation and rotation', () => {
    const before = { originX: 0, originY: 0, rotationDeg: 0 };
    const after = { originX: 1000, originY: 0, rotationDeg: 90 };
    const [moved] = moveWallWithAttachments(before, after, [run(2000, 0, 0, 800)]);
    expect(moved.originX).toBe(1000);
    expect(moved.originY).toBe(2000);
    expect(moved.rotationDeg).toBe(90);
  });

  it('is identity when the pose is unchanged', () => {
    const pose = { originX: 300, originY: 400, rotationDeg: 45 };
    const r = run(1000, 500, 20, 800);
    const [moved] = moveWallWithAttachments(pose, pose, [r]);
    expect(moved.originX).toBe(r.originX);
    expect(moved.originY).toBe(r.originY);
    expect(moved.rotationDeg).toBe(r.rotationDeg);
  });
});
