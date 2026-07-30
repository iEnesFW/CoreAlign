import { describe, expect, it } from 'vitest';
import {
  BODY_FLOOR_MM,
  blockedByLock,
  clampPanelPatch,
  clampRunPatch,
  clampSlabPatch,
  clampSurfacePatch,
  clampWallPatch,
} from './sceneGuards';
import type { SceneRunState, SceneWallState } from './project.types';

/**
 * The guard layer is the ONE writer every editor funnels through, so these tests pin the invariant
 * that used to be re-implemented (or forgotten) per panel: a body cannot be committed below its
 * physical floor, a locked body cannot be edited except to unlock it, and an opening cannot be left
 * larger than the wall that now carries it.
 */

const wall = (over: Partial<SceneWallState> = {}) =>
  ({
    id: 'w1',
    label: 'W',
    originX: 0,
    originY: 0,
    rotationDeg: 0,
    lengthMm: 4000,
    heightMm: 2600,
    thicknessMm: 200,
    openings: [],
    features: [],
    ...over,
  }) as unknown as SceneWallState;

const run = (over: Partial<SceneRunState> = {}) =>
  ({
    id: 'r1',
    label: 'R',
    lengthMm: 3000,
    heightMm: 2200,
    originX: 0,
    originY: 0,
    rotationDeg: 0,
    panels: [],
    ...over,
  }) as unknown as SceneRunState;

describe('dimension floors', () => {
  it('refuses a wall thinner, shorter or lower than its floor', () => {
    const patch = clampWallPatch(wall(), { lengthMm: -500, heightMm: 0, thicknessMm: -30 });
    expect(patch.lengthMm).toBe(BODY_FLOOR_MM.wallLength);
    expect(patch.heightMm).toBe(BODY_FLOOR_MM.wallHeight);
    expect(patch.thicknessMm).toBe(BODY_FLOOR_MM.wallThickness);
  });

  it('leaves a legitimate wall value alone (only rounding it)', () => {
    const patch = clampWallPatch(wall(), { lengthMm: 3200.4, thicknessMm: 120 });
    expect(patch.lengthMm).toBe(3200);
    expect(patch.thicknessMm).toBe(120);
  });

  it('refuses a degenerate slab', () => {
    const patch = clampSlabPatch({ lengthMm: -500, depthMm: 0, thicknessMm: -30 });
    expect(patch.lengthMm).toBe(BODY_FLOOR_MM.slabPlan);
    expect(patch.depthMm).toBe(BODY_FLOOR_MM.slabPlan);
    expect(patch.thicknessMm).toBe(BODY_FLOOR_MM.slabThickness);
  });

  it('refuses an inside-out surface', () => {
    expect(clampSurfacePatch({ thicknessMm: -12 }).thicknessMm).toBe(
      BODY_FLOOR_MM.surfaceThickness,
    );
  });

  it('refuses a zero-height run', () => {
    const patch = clampRunPatch({ heightMm: 0, lengthMm: 10 });
    expect(patch.heightMm).toBe(BODY_FLOOR_MM.runHeight);
    expect(patch.lengthMm).toBe(BODY_FLOOR_MM.runLength);
  });

  it('caps a panel height override at the run height instead of stretching the glass', () => {
    expect(clampPanelPatch(run({ heightMm: 2200 }), { heightMm: 99999 }).heightMm).toBe(2200);
    expect(clampPanelPatch(run(), { heightMm: -50 }).heightMm).toBe(BODY_FLOOR_MM.panelHeight);
    expect(clampPanelPatch(run(), { heightMm: 1800 }).heightMm).toBe(1800);
  });

  it('does not invent values for properties the patch does not carry', () => {
    expect(clampWallPatch(wall(), { colorHex: '#123456' })).toEqual({ colorHex: '#123456' });
    expect(clampPanelPatch(run(), { hasHandle: true })).toEqual({ hasHandle: true });
  });
});

describe('lock', () => {
  it('refuses every edit to a locked body', () => {
    expect(blockedByLock({ locked: true }, { lengthMm: 1000 })).toBe(true);
    expect(blockedByLock({ locked: true }, { colorHex: '#fff' })).toBe(true);
  });

  it('still allows the body to be unlocked', () => {
    expect(blockedByLock({ locked: true }, { locked: false })).toBe(false);
  });

  it('leaves unlocked bodies alone', () => {
    expect(blockedByLock({ locked: false }, { lengthMm: 1000 })).toBe(false);
    expect(blockedByLock(undefined, { lengthMm: 1000 })).toBe(false);
  });

  it('refuses a mixed patch that would sneak geometry in with the unlock', () => {
    expect(blockedByLock({ locked: true }, { locked: false, lengthMm: 9999 })).toBe(true);
  });
});

describe('openings follow the wall they live in', () => {
  const withWindow = wall({
    openings: [
      { id: 'o1', kind: 'window', offsetMm: 2000, widthMm: 2000, heightMm: 1400, sillMm: 900 },
    ],
  } as unknown as Partial<SceneWallState>);

  it('re-fits an opening when the wall is shortened under it', () => {
    const patch = clampWallPatch(withWindow, { lengthMm: 600 });
    const opening = patch.openings?.[0];
    expect(opening).toBeDefined();
    // 600 mm wall minus the 20 mm edge margin on each side.
    expect(opening!.widthMm).toBeLessThanOrEqual(560);
    expect(opening!.offsetMm - opening!.widthMm / 2).toBeGreaterThanOrEqual(0);
    expect(opening!.offsetMm + opening!.widthMm / 2).toBeLessThanOrEqual(600);
  });

  it('re-fits an opening when the wall is lowered under it', () => {
    const patch = clampWallPatch(withWindow, { heightMm: 1200 });
    const opening = patch.openings![0];
    expect(opening.heightMm + opening.sillMm).toBeLessThanOrEqual(1200);
  });

  it('does not touch the openings when only appearance changes', () => {
    expect(clampWallPatch(withWindow, { colorHex: '#abcdef' }).openings).toBeUndefined();
  });

  it('leaves an opening that still fits exactly as authored', () => {
    const patch = clampWallPatch(withWindow, { lengthMm: 4000 });
    expect(patch.openings![0]).toMatchObject({ widthMm: 2000, offsetMm: 2000, heightMm: 1400 });
  });
});
