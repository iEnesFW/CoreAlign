import { describe, expect, it } from 'vitest';
import {
  buildCurvedShapedGeometry,
  buildCurvedWallFeatureSolid,
  curvedWallPickUv,
} from './curvedExtrude';

describe('buildCurvedShapedGeometry', () => {
  const R = 2;
  const t = 0.02;
  const w = 1000;
  const h = 2000;
  const rect = [
    { x: -w / 2, y: 0 },
    { x: w / 2, y: 0 },
    { x: w / 2, y: h },
    { x: -w / 2, y: h },
  ];

  it('lands every vertex on the cylinder shell (radius ± half thickness)', () => {
    const g = buildCurvedShapedGeometry(rect, w, R, 1, 0, Math.PI / 4, t);
    const p = g.attributes.position;
    const centerY = -R;
    expect(p.count).toBeGreaterThan(0);
    for (let i = 0; i < p.count; i += 1) {
      const radial = Math.hypot(p.getX(i), p.getY(i) - centerY);
      expect(radial).toBeGreaterThanOrEqual(R - t / 2 - 1e-4);
      expect(radial).toBeLessThanOrEqual(R + t / 2 + 1e-4);
    }
  });

  it('keeps the height on the Z axis within [0, h] (lifted to world-up by the mesh rotation)', () => {
    const g = buildCurvedShapedGeometry(rect, w, R, 1, 0, Math.PI / 4, t);
    const p = g.attributes.position;
    let minZ = Infinity;
    let maxZ = -Infinity;
    for (let i = 0; i < p.count; i += 1) {
      minZ = Math.min(minZ, p.getZ(i));
      maxZ = Math.max(maxZ, p.getZ(i));
    }
    expect(minZ).toBeGreaterThanOrEqual(-1e-4);
    expect(maxZ).toBeLessThanOrEqual(h / 1000 + 1e-4);
    expect(maxZ - minZ).toBeGreaterThan(1.5); // spans most of the 2m height
  });

  it('is densely tessellated along the arc so the curve is smooth, not faceted', () => {
    const g = buildCurvedShapedGeometry(rect, w, R, 1, 0, Math.PI / 4, t);
    // many columns × four surfaces → hundreds of vertices (a flat ExtrudeGeometry cap would
    // have only a handful and would facet when bent).
    expect(g.attributes.position.count).toBeGreaterThan(200);
  });

  it('follows a triangle silhouette (apex column is a near-zero-height sliver, base is full)', () => {
    const tri = [
      { x: -w / 2, y: 0 },
      { x: w / 2, y: 0 },
      { x: 0, y: h },
    ];
    const g = buildCurvedShapedGeometry(tri, w, R, 1, 0, Math.PI / 4, t);
    const p = g.attributes.position;
    let maxZ = -Infinity;
    for (let i = 0; i < p.count; i += 1) maxZ = Math.max(maxZ, p.getZ(i));
    expect(maxZ).toBeGreaterThan(1.5); // the apex reaches near the top
    expect(p.count).toBeGreaterThan(100);
  });
});

describe('buildCurvedWallFeatureSolid', () => {
  const R = 2; // wall radius (m)
  const lengthMm = 4000; // developed wall length
  const sweep = Math.PI / 2; // 90° arc
  // A 600mm-wide × 800mm-tall window centred at offset 1000mm, sill 900mm.
  const rect = [
    { x: 700, z: 900 },
    { x: 1300, z: 900 },
    { x: 1300, z: 1700 },
    { x: 700, z: 1700 },
  ];

  it('builds a closed radial solid spanning [rNear, rFar] across the feature arc', () => {
    const rNear = R - 0.15;
    const rFar = R + 0.15;
    const g = buildCurvedWallFeatureSolid(rect, lengthMm, R, 1, sweep, rNear, rFar);
    const p = g.attributes.position;
    const centerY = -R;
    expect(p.count).toBeGreaterThan(0);
    for (let i = 0; i < p.count; i += 1) {
      const radial = Math.hypot(p.getX(i), p.getY(i) - centerY);
      expect(radial).toBeGreaterThanOrEqual(rNear - 1e-4);
      expect(radial).toBeLessThanOrEqual(rFar + 1e-4);
    }
  });

  it('keeps the cut within the feature height band on Z', () => {
    const g = buildCurvedWallFeatureSolid(rect, lengthMm, R, 1, sweep, R - 0.15, R + 0.15);
    const p = g.attributes.position;
    let minZ = Infinity;
    let maxZ = -Infinity;
    for (let i = 0; i < p.count; i += 1) {
      minZ = Math.min(minZ, p.getZ(i));
      maxZ = Math.max(maxZ, p.getZ(i));
    }
    expect(minZ).toBeGreaterThanOrEqual(0.9 - 1e-3);
    expect(maxZ).toBeLessThanOrEqual(1.7 + 1e-3);
  });

  it('places the feature at its arc offset, not at the wall start', () => {
    const g = buildCurvedWallFeatureSolid(rect, lengthMm, R, 1, sweep, R - 0.15, R + 0.15);
    const p = g.attributes.position;
    // offset 700–1300mm of 4000mm over a 90° arc → phi ≈ 0.275–0.51 rad, well off phi=0.
    let minPhi = Infinity;
    for (let i = 0; i < p.count; i += 1) {
      const a = Math.atan2(p.getY(i) - -R, p.getX(i)); // toAngle for direction=1: π/2 − phi
      const phi = Math.PI / 2 - a;
      minPhi = Math.min(minPhi, phi);
    }
    expect(minPhi).toBeGreaterThan(0.2);
  });

  it('round-trips: forward map (offset,height) → group-local → curvedWallPickUv recovers it', () => {
    const radiusM = R;
    const direction: 1 | -1 = 1;
    const sweep = Math.PI / 2;
    const centerY = -direction * radiusM;
    const toAngle = (phi: number) => (direction === 1 ? Math.PI / 2 - phi : phi - Math.PI / 2);
    for (const offsetMm of [200, 1000, 2500, 3800]) {
      for (const heightMm of [100, 1500, 2600]) {
        const phi = (offsetMm / lengthMm) * sweep;
        const a = toAngle(phi);
        // band point (pre-rotation) then the mesh's [-π/2,0,0] mapping (x,y,z) → (x, z, -y).
        const bx = Math.cos(a) * radiusM;
        const by = centerY + Math.sin(a) * radiusM;
        const bz = heightMm / 1000;
        const localX = bx;
        const localY = bz;
        const localZ = -by;
        const uv = curvedWallPickUv(localX, localY, localZ, radiusM, direction, sweep, lengthMm);
        expect(uv.u).toBeCloseTo(offsetMm, 3);
        expect(uv.v).toBeCloseTo(heightMm, 3);
      }
    }
  });

  it('round-trips for the opposite sweep direction too', () => {
    const sweep = Math.PI / 3;
    const centerY = R; // -direction * R with direction = -1
    const offsetMm = 1500;
    const heightMm = 2000;
    const phi = (offsetMm / lengthMm) * sweep;
    const a = phi - Math.PI / 2; // toAngle for direction = -1
    const localX = Math.cos(a) * R;
    const localY = heightMm / 1000;
    const localZ = -(centerY + Math.sin(a) * R);
    const uv = curvedWallPickUv(localX, localY, localZ, R, -1, sweep, lengthMm);
    expect(uv.u).toBeCloseTo(offsetMm, 3);
    expect(uv.v).toBeCloseTo(heightMm, 3);
  });

  it('degenerates safely (empty geometry) for a too-small outline', () => {
    const g = buildCurvedWallFeatureSolid(
      [
        { x: 1000, z: 1000 },
        { x: 1000.1, z: 1000 },
        { x: 1000.1, z: 1000.1 },
      ],
      lengthMm,
      R,
      1,
      sweep,
      R - 0.1,
      R + 0.1,
    );
    expect(g.attributes.position?.count ?? 0).toBe(0);
  });
});
