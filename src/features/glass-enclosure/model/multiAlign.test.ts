import { describe, expect, it } from 'vitest';
import { alignTargetCenter, alignTargetEndpoints, type AlignTarget } from './multiAlign';
import type { SceneRunState } from './project.types';

const runTarget = (over: Record<string, unknown>): AlignTarget => ({
  kind: 'run',
  run: {
    id: 'r1',
    originX: 0,
    originY: 0,
    rotationDeg: 0,
    lengthMm: 1414,
    heightMm: 2000,
    geomArcRadiusMm: null,
    geomArcSweepDeg: null,
    panels: [],
    ...over,
  } as unknown as SceneRunState,
});

describe('multiAlign — arc-aware endpoints', () => {
  it('straight run end is origin + length along rotation', () => {
    const ep = alignTargetEndpoints(runTarget({ lengthMm: 2000, rotationDeg: 0 }))!;
    expect(ep.start).toEqual({ x: 0, y: 0 });
    expect(ep.end.x).toBeCloseTo(2000, 3);
    expect(ep.end.y).toBeCloseTo(0, 3);
  });

  it('arc run end bows off the phantom straight end (uses arcEndLocal)', () => {
    const ep = alignTargetEndpoints(
      runTarget({ geomArcRadiusMm: 1000, geomArcSweepDeg: 90, lengthMm: 1414, rotationDeg: 0 }),
    )!;
    // A phantom straight end (origin + length·dir(rot)) would sit at (1414, 0); the real arc end
    // bows well off the chord line.
    expect(Math.abs(ep.end.y)).toBeGreaterThan(100);
    expect(Math.abs(ep.end.x - 1414)).toBeGreaterThan(100);
  });

  it('arc run center is the midpoint of start and the real arc end', () => {
    const target = runTarget({
      geomArcRadiusMm: 1000,
      geomArcSweepDeg: 90,
      lengthMm: 1414,
      rotationDeg: 0,
    });
    const ep = alignTargetEndpoints(target)!;
    const center = alignTargetCenter(target);
    expect(center.x).toBeCloseTo((ep.start.x + ep.end.x) / 2, 3);
    expect(center.y).toBeCloseTo((ep.start.y + ep.end.y) / 2, 3);
  });
});
