import { describe, expect, it } from 'vitest';
import type { BufferGeometry } from 'three';
import { bentWallFootprintMm, buildBentWallGeometry } from './bentWallGeometry';

const bounds = (g: BufferGeometry) => {
  const p = g.getAttribute('position');
  let minX = Infinity;
  let maxX = -Infinity;
  let minY = Infinity;
  let maxY = -Infinity;
  let minZ = Infinity;
  let maxZ = -Infinity;
  for (let i = 0; i < p.count; i += 1) {
    minX = Math.min(minX, p.getX(i));
    maxX = Math.max(maxX, p.getX(i));
    minY = Math.min(minY, p.getY(i));
    maxY = Math.max(maxY, p.getY(i));
    minZ = Math.min(minZ, p.getZ(i));
    maxZ = Math.max(maxZ, p.getZ(i));
  }
  return { minX, maxX, minY, maxY, minZ, maxZ };
};

describe('bentWallFootprintMm', () => {
  it('is a 6-vertex mitred hexagon for a real bend', () => {
    const outline = bentWallFootprintMm(4, 2, Math.PI / 2, 0.05);
    expect(outline).toHaveLength(6);
  });

  it('keeps the start segment thickness centred on the centreline (±half)', () => {
    const half = 0.05;
    const outline = bentWallFootprintMm(4, 2, Math.PI / 2, half);
    // The first and last vertices are the start cap (left/right of the origin).
    expect(outline[0].y).toBeCloseTo(half, 6);
    expect(outline[5].y).toBeCloseTo(-half, 6);
    expect(outline[0].x).toBeCloseTo(0, 6);
    expect(outline[5].x).toBeCloseTo(0, 6);
  });

  it('a 90° bend turns the second segment a full quarter-length in plan', () => {
    // length 4, bend at 2 → segment 2 is 2m long; a +90° turn sends its end to (2, 2).
    const outline = bentWallFootprintMm(4, 2, Math.PI / 2, 0.05);
    const maxY = Math.max(...outline.map((v) => v.y));
    expect(maxY).toBeGreaterThan(1.9);
  });

  it('clamps a past-the-end bend point to stay a valid (near-straight) footprint', () => {
    const outline = bentWallFootprintMm(4, 4, Math.PI / 2, 0.05);
    expect(outline).toHaveLength(6);
  });

  it('degenerates to a 4-vertex rectangle when the offset lines are parallel (180° fold-back)', () => {
    const outline = bentWallFootprintMm(4, 2, Math.PI, 0.05);
    expect(outline).toHaveLength(4);
  });
});

describe('buildBentWallGeometry', () => {
  const base = { lengthMm: 4000, bendAtMm: 2000, thicknessMm: 100, heightMm: 2500 };

  it('extrudes up to the wall height with the base at y = 0', () => {
    const g = buildBentWallGeometry({ ...base, bendAngleDeg: 90 });
    const b = bounds(g);
    expect(b.minY).toBeCloseTo(0, 3);
    expect(b.maxY).toBeCloseTo(2.5, 3);
  });

  it('a 90° bend spreads the footprint across the ground plane (a real L, not a straight box)', () => {
    const g = buildBentWallGeometry({ ...base, bendAngleDeg: 90 });
    const b = bounds(g);
    // Segment 2 (2m) turns into the z axis, so the plan depth is far larger than the thickness.
    expect(b.maxZ - b.minZ).toBeGreaterThan(1.5);
    // Segment 1 still runs ~2m along x before the bend.
    expect(b.maxX).toBeGreaterThan(1.9);
  });

  it('a near-zero bend angle degenerates to a straight wall (thickness-only depth)', () => {
    const g = buildBentWallGeometry({ ...base, bendAngleDeg: 0 });
    const b = bounds(g);
    expect(b.maxZ - b.minZ).toBeCloseTo(0.1, 3); // just the 100mm thickness
    expect(b.maxX).toBeCloseTo(4, 3); // full length along x
  });

  it('keeps the wall thickness centred on z = 0 at the start', () => {
    const g = buildBentWallGeometry({ ...base, bendAngleDeg: 60 });
    const b = bounds(g);
    expect(Math.abs(b.maxZ + b.minZ)).toBeLessThan(b.maxZ - b.minZ); // straddles 0, not one-sided
  });
});
