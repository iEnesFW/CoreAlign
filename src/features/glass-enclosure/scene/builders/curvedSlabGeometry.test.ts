import { describe, expect, it } from 'vitest';
import {
  buildCurvedSlabGeometry,
  curvedSlabFrame,
  curvedSlabPickSc,
  curvedSlabPointAt,
  slabArcDefaultSweepSign,
  slabArcDirSign,
} from './curvedSlabGeometry';

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

// Canonical sign contract (arcFromBow/bowFromArc pair): +sagitta = the chord's CCW "+across"
// direction → stored sweep NEGATIVE. For axis='length' +across = local +Z; for axis='depth'
// +across = local −X — so the same sweep sign mirrors OPPOSITE ways on the two axes.
describe('slabArcDirSign', () => {
  it('maps the canonical sweep sign to the bulge side per axis', () => {
    expect(slabArcDirSign('length', -120)).toBe(1); // +sagitta → bulge local +Z
    expect(slabArcDirSign('length', 120)).toBe(-1); // −sagitta → bulge local −Z
    expect(slabArcDirSign('depth', -90)).toBe(-1); // +sagitta → bulge local −X
    expect(slabArcDirSign('depth', 90)).toBe(1); // −sagitta → bulge local +X
  });

  it('defaults a fresh curve to the slab body side on both axes', () => {
    expect(slabArcDirSign('length', slabArcDefaultSweepSign('length'))).toBe(1); // body +Z
    expect(slabArcDirSign('depth', slabArcDefaultSweepSign('depth'))).toBe(1); // body +X
  });
});

describe('curvedSlabFrame developed (s,c) maps', () => {
  it('pickSc is the exact inverse of pointAt on both axes and both sweep signs', () => {
    for (const axis of ['length', 'depth'] as const) {
      for (const sweep of [-120, 90]) {
        const chord = axis === 'length' ? 3000 : 1200;
        const radius = chord / (2 * Math.sin((Math.abs(sweep) * Math.PI) / 180 / 2));
        const frame = curvedSlabFrame(3000, 1200, radius, sweep, axis);
        const probes: [number, number][] = [
          [120, 40],
          [frame.developedMm / 2, frame.acrossMm / 2],
          [frame.developedMm - 120, frame.acrossMm - 40],
        ];
        for (const [s, c] of probes) {
          const p = curvedSlabPointAt(frame, s, c);
          const sc = curvedSlabPickSc(frame, p.x, p.z);
          expect(sc.s).toBeCloseTo(s, 3);
          expect(sc.c).toBeCloseTo(c, 3);
        }
      }
    }
  });

  it('front-edge s endpoints land on the bent-axis chord corners (chord-invariant)', () => {
    const sweep = -120;
    const radius = 3000 / (2 * Math.sin((120 * Math.PI) / 180 / 2));
    const frame = curvedSlabFrame(3000, 1200, radius, sweep, 'length');
    const start = curvedSlabPointAt(frame, 0, 0);
    const end = curvedSlabPointAt(frame, frame.developedMm, 0);
    expect(start.x).toBeCloseTo(0, 0);
    expect(start.z).toBeCloseTo(0, 0);
    expect(end.x).toBeCloseTo(3000, 0);
    expect(end.z).toBeCloseTo(0, 0);
  });
});

describe('buildCurvedSlabGeometry', () => {
  it('axis=length, +sagitta (negative sweep): ends fixed at (0,0)/(length,0), bows +Z, no rotation', () => {
    const lengthMm = 3000;
    const depthMm = 1500;
    const sweepDeg = -120;
    // chord-invariant front-edge radius: chord(=length) = 2R·sin(sweep/2).
    const R = lengthMm / (2 * Math.sin((Math.abs(sweepDeg) * Math.PI) / 180 / 2));
    const geo = buildCurvedSlabGeometry(lengthMm, depthMm, 150, R, sweepDeg, 'length');
    const { read, cols, minZ } = corners(geo);

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
    // chord — that's correct for a concentric curved slab, not a rotation.
    expect(minZ).toBeGreaterThan(-0.01);
  });

  it('axis=length, −sagitta (positive sweep) bows the other way (−Z)', () => {
    const R = 3000 / (2 * Math.sin((120 * Math.PI) / 180 / 2));
    const geo = buildCurvedSlabGeometry(3000, 1500, 150, R, 120, 'length');
    const { maxZ } = corners(geo);
    expect(maxZ).toBeLessThan(0.01); // all on the −Z side
  });

  it('axis=depth, −sagitta (positive sweep, body side): bends along Z, bows +X', () => {
    const depthMm = 2000;
    const sweepDeg = 90;
    const R = depthMm / (2 * Math.sin((sweepDeg * Math.PI) / 180 / 2));
    const geo = buildCurvedSlabGeometry(3000, depthMm, 150, R, sweepDeg, 'depth');
    const { read, cols, minX } = corners(geo);
    // Front-bottom ends now run along Z (the bent axis): Z = 0 and Z = depth.
    const start = read(0);
    const end = read(cols * 4);
    expect(start[2]).toBeCloseTo(0, 2);
    expect(end[2]).toBeCloseTo(2, 2); // depth (m)
    // Positive sweep keeps the body one-sided on +X (the slab's own side), so X never goes negative.
    expect(minX).toBeGreaterThan(-0.01);
  });

  it('axis=depth, +sagitta (negative sweep) mirrors the bow to −X', () => {
    const depthMm = 2000;
    const R = depthMm / (2 * Math.sin((90 * Math.PI) / 180 / 2));
    const geo = buildCurvedSlabGeometry(3000, depthMm, 150, R, -90, 'depth');
    const { maxX } = corners(geo);
    expect(maxX).toBeLessThan(0.01); // all on the −X side
  });

  it('round-trip: the committed sweep sign puts the mesh apex on the dragged (+across) side for both axes', () => {
    // Simulates the Q-handle commit: drag +sagitta → sweep = −|sweep| → apex must land on +across.
    for (const axis of ['length', 'depth'] as const) {
      const chord = 2400;
      const sweepAbs = 100;
      const R = chord / (2 * Math.sin((sweepAbs * Math.PI) / 180 / 2));
      const committedSweep = -sweepAbs; // +sagitta under the canonical contract
      const geo = buildCurvedSlabGeometry(
        axis === 'length' ? chord : 1200,
        axis === 'length' ? 1200 : chord,
        150,
        R,
        committedSweep,
        axis,
      );
      const { read, cols } = corners(geo);
      const apex = read(Math.floor(cols / 2) * 4);
      // +across in LOCAL coords: axis='length' → +Z; axis='depth' → −X.
      const acrossCoord = axis === 'length' ? apex[2] : -apex[0];
      expect(acrossCoord).toBeGreaterThan(0.05);
    }
  });
});
