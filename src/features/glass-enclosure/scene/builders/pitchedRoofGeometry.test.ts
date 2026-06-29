import { describe, expect, it } from 'vitest';
import type { BufferGeometry } from 'three';
import { buildPitchedRoofGeometry } from './pitchedRoofGeometry';

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

describe('buildPitchedRoofGeometry', () => {
  const lengthMm = 4000;
  const depthMm = 3000;
  const riseMm = 1200;
  const tMm = 150;

  it('symmetric gable: spans the slab footprint, ridge reaches the rise + thickness', () => {
    const g = buildPitchedRoofGeometry(lengthMm, depthMm, riseMm, 'symmetric', tMm);
    const b = bounds(g);
    expect(b.minX).toBeCloseTo(0, 3); // eave at length 0
    expect(b.maxX).toBeCloseTo(4, 3); // eave at length end (m)
    expect(b.minZ).toBeCloseTo(0, 3); // gable end
    expect(b.maxZ).toBeCloseTo(3, 3); // other gable end (depth m)
    expect(b.minY).toBeCloseTo(0, 3); // eaves at base
    expect(b.maxY).toBeCloseTo(1.2 + 0.15, 3); // ridge = rise + thickness
  });

  it('symmetric gable: the peak sits at the length midline (a real ridge, not a flat top)', () => {
    const g = buildPitchedRoofGeometry(lengthMm, depthMm, riseMm, 'symmetric', tMm);
    const p = g.attributes.position;
    let peakX = NaN;
    let peakY = -Infinity;
    for (let i = 0; i < p.count; i += 1) {
      if (p.getY(i) > peakY) {
        peakY = p.getY(i);
        peakX = p.getX(i);
      }
    }
    expect(peakX).toBeCloseTo(2, 2); // ridge at length/2 = 2m
  });

  it('monopitch: low at x=0, high at x=length (single slope)', () => {
    const g = buildPitchedRoofGeometry(lengthMm, depthMm, riseMm, 'monopitch', tMm);
    const p = g.attributes.position;
    let yAtStart = -Infinity;
    let yAtEnd = -Infinity;
    for (let i = 0; i < p.count; i += 1) {
      if (p.getX(i) < 0.01) yAtStart = Math.max(yAtStart, p.getY(i));
      if (p.getX(i) > 4 - 0.01) yAtEnd = Math.max(yAtEnd, p.getY(i));
    }
    expect(yAtStart).toBeCloseTo(0.15, 3); // low eave top = thickness
    expect(yAtEnd).toBeCloseTo(1.2 + 0.15, 3); // high eave top = rise + thickness
  });

  it('degenerates safely with zero rise (a flat sheet, no negative geometry)', () => {
    const g = buildPitchedRoofGeometry(lengthMm, depthMm, 0, 'symmetric', tMm);
    const b = bounds(g);
    expect(b.minY).toBeGreaterThanOrEqual(-1e-6);
    expect(b.maxY).toBeCloseTo(0.15, 3);
  });
});
