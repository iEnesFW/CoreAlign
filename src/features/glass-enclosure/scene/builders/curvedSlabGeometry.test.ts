import { describe, expect, it } from 'vitest';
import { buildCurvedSlabGeometry } from './curvedSlabGeometry';

const corners = (geo: ReturnType<typeof buildCurvedSlabGeometry>) => {
  const pos = geo.getAttribute('position').array as ArrayLike<number>;
  const vCount = pos.length / 3;
  const cols = vCount / 4 - 1;
  const read = (v: number): [number, number, number] => [
    pos[v * 3],
    pos[v * 3 + 1],
    pos[v * 3 + 2],
  ];
  let minX = Infinity;
  let maxX = -Infinity;
  let minZ = Infinity;
  let maxZ = -Infinity;
  for (let v = 0; v < vCount; v += 1) {
    const x = pos[v * 3];
    const z = pos[v * 3 + 2];
    if (x < minX) minX = x;
    if (x > maxX) maxX = x;
    if (z < minZ) minZ = z;
    if (z > maxZ) maxZ = z;
  }
  return { read, cols, minX, maxX, minZ, maxZ };
};

describe('buildCurvedSlabGeometry', () => {
  it('axis=length: front-edge ends stay fixed at (0,0)/(length,0), bows +Z, one-sided depth, no rotation', () => {
    const lengthMm = 3000;
    const depthMm = 1500;
    const sweepDeg = 120;
    // chord-invariant front-edge radius: chord(=length) = 2R·sin(sweep/2).
    const R = lengthMm / (2 * Math.sin((sweepDeg * Math.PI) / 180 / 2));
    const geo = buildCurvedSlabGeometry(lengthMm, depthMm, 150, R, sweepDeg, 'length', 1);
    const { read, cols, minX, maxX, minZ } = corners(geo);

    // Front-bottom (vertex index 0) of the first + last column = the two fixed chord ends.
    const start = read(0);
    const end = read(cols * 4);
    expect(start[0]).toBeCloseTo(0, 2); // X = 0
    expect(start[2]).toBeCloseTo(0, 2); // Z = 0
    expect(end[0]).toBeCloseTo(3, 2); // X = length (m)
    expect(end[2]).toBeCloseTo(0, 2); // Z = 0

    // The middle column's front edge bows out in +Z (the sagitta), centred at X ≈ length/2.
    const mid = read(Math.floor(cols / 2) * 4);
    expect(mid[2]).toBeGreaterThan(0.1);
    expect(mid[0]).toBeCloseTo(1.5, 0); // a near-apex column sits ≈ length/2 along X

    // One-sided depth (all on +Z). The outer (back) edge legitimately fans wider than the front
    // chord — that's correct for a concentric curved slab, not a rotation — so X may overhang; what
    // matters is the symmetric front edge above, not an X bound.
    expect(minZ).toBeGreaterThan(-0.01);
    void minX;
    void maxX;
  });

  it('axis=length: a negative sweep bows the other way (−Z)', () => {
    const R = 3000 / (2 * Math.sin((120 * Math.PI) / 180 / 2));
    const geo = buildCurvedSlabGeometry(3000, 1500, 150, R, -120, 'length', -1);
    const { maxZ } = corners(geo);
    expect(maxZ).toBeLessThan(0.01); // all on the −Z side
  });

  it('axis=depth: bends along Z, length is the one-sided radial width', () => {
    const depthMm = 2000;
    const sweepDeg = 90;
    const R = depthMm / (2 * Math.sin((sweepDeg * Math.PI) / 180 / 2));
    const geo = buildCurvedSlabGeometry(3000, depthMm, 150, R, sweepDeg, 'depth', 1);
    const { read, cols, minX } = corners(geo);
    // Front-bottom ends now run along Z (the bent axis): Z = 0 and Z = depth.
    const start = read(0);
    const end = read(cols * 4);
    expect(start[2]).toBeCloseTo(0, 2);
    expect(end[2]).toBeCloseTo(2, 2); // depth (m)
    // Length is the radial (one-sided +X), so X never goes negative.
    expect(minX).toBeGreaterThan(-0.01);
  });
});
