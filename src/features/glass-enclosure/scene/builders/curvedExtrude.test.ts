import { describe, expect, it } from 'vitest';
import { BufferAttribute, BufferGeometry } from 'three';
import { bendGeometryToArc } from './curvedExtrude';

const makeGeom = (verts: number[]) => {
  const g = new BufferGeometry();
  g.setAttribute('position', new BufferAttribute(new Float32Array(verts), 3));
  return g;
};

describe('bendGeometryToArc', () => {
  it('lands every vertex on the cylinder shell (radius ± thickness) and keeps the height axis', () => {
    const R = 2;
    const t = 0.01;
    const w = 1;
    const g = makeGeom([-w / 2, 0, -t / 2, w / 2, 0, t / 2, 0, 1.5, 0]);
    const bent = bendGeometryToArc(g, R, 1, 0, Math.PI / 4, w);
    const p = bent.attributes.position;
    const centerY = -R;
    for (let i = 0; i < p.count; i += 1) {
      const radial = Math.hypot(p.getX(i), p.getY(i) - centerY);
      expect(radial).toBeGreaterThanOrEqual(R - t / 2 - 1e-5);
      expect(radial).toBeLessThanOrEqual(R + t / 2 + 1e-5);
    }
    // y (flat height) is carried to the Z axis the caller's mesh rotation lifts to world-up
    expect(p.getZ(0)).toBeCloseTo(0, 5);
    expect(p.getZ(2)).toBeCloseTo(1.5, 5);
  });

  it('maps x=-w/2 to phiStart (spine at origin) and x=+w/2 to phiEnd', () => {
    const R = 3;
    const w = 2;
    const g = makeGeom([-w / 2, 0, 0, w / 2, 0, 0]);
    const bent = bendGeometryToArc(g, R, 1, 0, Math.PI / 2, w);
    const p = bent.attributes.position;
    const centerY = -R;
    // phiStart=0 → angle π/2 → spine passes through the local origin
    expect(p.getX(0)).toBeCloseTo(0, 4);
    expect(p.getY(0)).toBeCloseTo(0, 4);
    // phiEnd=π/2 → angle 0 → (R, centerY)
    expect(p.getX(1)).toBeCloseTo(R, 4);
    expect(p.getY(1)).toBeCloseTo(centerY, 4);
  });

  it('bends the opposite way for direction -1 (mirror across the spine)', () => {
    const R = 3;
    const w = 2;
    const g = makeGeom([w / 2, 0, 0]);
    const right = bendGeometryToArc(makeGeom([w / 2, 0, 0]), R, 1, 0, Math.PI / 2, w);
    const left = bendGeometryToArc(g, R, -1, 0, Math.PI / 2, w);
    // same |X|, opposite-sign center offset → the two directions curve to opposite sides
    expect(left.attributes.position.getX(0)).toBeCloseTo(right.attributes.position.getX(0), 4);
    expect(left.attributes.position.getY(0)).toBeCloseTo(-right.attributes.position.getY(0), 4);
  });
});
