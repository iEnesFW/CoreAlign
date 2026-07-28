import { describe, expect, it } from 'vitest';
import {
  FREE_STANDING_DEPTH_MM,
  SHADOW_GAP_MM,
  mountedSection,
  resolveMountDepth,
} from './mountDepth';

/**
 * S1 — "the hole and the glass measure the same, but the pane does not seat; a small gap remains".
 *
 * The measurable quantity is the OPEN REVEAL PER FACE: how much of the carved opening's depth is
 * still empty after the assembly is placed. A 200 mm wall carved through and filled with a 50 mm
 * section leaves 75 mm open on each side — that is the gap the user sees.
 */
const revealPerFaceMm = (wallThicknessMm: number, depthMm: number) =>
  (wallThicknessMm - depthMm) / 2;

describe('mount depth fills the carved opening', () => {
  it('the OLD fixed 50 mm section leaves a 75 mm reveal on each face of a 200 mm wall', () => {
    expect(revealPerFaceMm(200, FREE_STANDING_DEPTH_MM)).toBe(75);
    // Only a quarter of the opening depth was filled.
    expect(FREE_STANDING_DEPTH_MM / 200).toBe(0.25);
  });

  it('a hosted run fills the wall minus one shadow gap per face', () => {
    const mount = resolveMountDepth(200);
    expect(mount.hosted).toBe(true);
    expect(mount.depthMm).toBe(180);
    expect(mount.offsetMm).toBe(0);
    expect(mount.frontGapMm).toBe(SHADOW_GAP_MM);
    expect(mount.backGapMm).toBe(SHADOW_GAP_MM);
    expect(revealPerFaceMm(200, mount.depthMm)).toBe(SHADOW_GAP_MM);
    expect(mount.depthMm / 200).toBe(0.9);
  });

  it('scales with the wall: a thin wall and a thick wall both keep the same shadow line', () => {
    for (const thickness of [80, 120, 200, 350, 500]) {
      const mount = resolveMountDepth(thickness);
      expect(mount.frontGapMm).toBeCloseTo(SHADOW_GAP_MM, 6);
      expect(mount.backGapMm).toBeCloseTo(SHADOW_GAP_MM, 6);
      expect(mount.depthMm).toBe(thickness - 2 * SHADOW_GAP_MM);
    }
  });

  it('REGRESSION GATE: a free-standing run keeps exactly the old section', () => {
    for (const host of [null, undefined, 0]) {
      const mount = resolveMountDepth(host);
      expect(mount.hosted).toBe(false);
      expect(mount.depthMm).toBe(FREE_STANDING_DEPTH_MM);
      expect(mount.offsetMm).toBe(0);
      // No shadow gap is invented for a run that has no reveal to sit in.
      expect(mount.frontGapMm).toBe(0);
      expect(mount.backGapMm).toBe(0);
    }
  });

  it('never returns a negative or zero depth on a wall thinner than the gaps', () => {
    const mount = resolveMountDepth(15);
    expect(mount.depthMm).toBeGreaterThan(0);
    expect(mount.depthMm).toBeLessThanOrEqual(15);
  });
});

describe('mount depth overrides', () => {
  it('an explicit depth wins over the derived one', () => {
    const mount = resolveMountDepth(200, { mountDepthMm: 120 });
    expect(mount.depthMm).toBe(120);
    expect(mount.frontGapMm).toBe(40);
    expect(mount.backGapMm).toBe(40);
  });

  it('an explicit shadow gap re-derives the depth', () => {
    const mount = resolveMountDepth(200, { mountShadowGapMm: 0 });
    expect(mount.depthMm).toBe(200);
    expect(mount.frontGapMm).toBe(0);
    expect(mount.backGapMm).toBe(0);
  });

  it('a face-aligned offset shifts the assembly without leaving the wall', () => {
    const mount = resolveMountDepth(200, { mountDepthMm: 100, mountOffsetMm: 50 });
    expect(mount.offsetMm).toBe(50);
    expect(mount.frontGapMm).toBe(0);
    expect(mount.backGapMm).toBe(100);
  });

  it('clamps an offset that would push the assembly out through a face', () => {
    const mount = resolveMountDepth(200, { mountDepthMm: 100, mountOffsetMm: 900 });
    // Slack is (200-100)/2 = 50; the request is honoured up to the surface, not beyond it.
    expect(mount.offsetMm).toBe(50);
    expect(mount.frontGapMm).toBe(0);
    expect(mount.backGapMm).toBe(100);
  });

  it('an override never makes the assembly deeper than a face-to-face fill', () => {
    const mount = resolveMountDepth(200, { mountDepthMm: 400 });
    // Deeper than the wall is allowed (a run may deliberately protrude) but the offset clamp
    // collapses to zero so it stays symmetric rather than lurching to one side.
    expect(mount.offsetMm).toBe(0);
  });
});

describe('mountedSection maps depth onto the Bar cross-section', () => {
  it('puts the resolved depth on the Z axis and the catalogue face on Y', () => {
    // Bar renders boxGeometry [lengthM, height/1000, width/1000] — `width` IS the wall-normal axis.
    const section = mountedSection(60, resolveMountDepth(200));
    expect(section).toEqual({ width: 180, height: 60 });
  });

  it('a free-standing run still gets the historic 50 x face section', () => {
    expect(mountedSection(60, resolveMountDepth(null))).toEqual({ width: 50, height: 60 });
    expect(mountedSection(40, resolveMountDepth(null))).toEqual({ width: 50, height: 40 });
  });
});
