import { describe, expect, it } from 'vitest';
import { buildCurvedShapedGeometry } from './curvedExtrude';

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
