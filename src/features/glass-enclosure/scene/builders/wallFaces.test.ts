import { describe, expect, it } from 'vitest';
import { Box3, BoxGeometry, ExtrudeGeometry, Shape } from 'three';
import {
  applyWallFaceFeatures,
  buildFaceFeatureGeometry,
  normalizeWallSide,
  wallFaceFrame,
  type WallBoxDims,
} from './wallFaces';

const dims: WallBoxDims = { lengthM: 3, heightM: 2.6, thicknessM: 0.2 };

describe('normalizeWallSide (backward-compat with the old 1 / -1)', () => {
  it('maps the legacy numeric sides', () => {
    expect(normalizeWallSide(1)).toBe('front');
    expect(normalizeWallSide(-1)).toBe('back');
  });
  it('keeps the six named faces and defaults unknown to front', () => {
    expect(normalizeWallSide('top')).toBe('top');
    expect(normalizeWallSide('left')).toBe('left');
    expect(normalizeWallSide(undefined)).toBe('front');
    expect(normalizeWallSide(null)).toBe('front');
  });
});

describe('wallFaceFrame', () => {
  it('places the front face at +z with the full thickness behind it', () => {
    const f = wallFaceFrame('front', dims);
    expect([f.origin.x, f.origin.y, f.origin.z]).toEqual([0, 0, 0.1]);
    expect([f.normal.x, f.normal.y, f.normal.z]).toEqual([0, 0, 1]);
    expect(f.uMaxM).toBe(3);
    expect(f.vMaxM).toBe(2.6);
    expect(f.depthM).toBe(0.2);
  });
  it('places the top face at +y, width along length, depth = height', () => {
    const f = wallFaceFrame('top', dims);
    expect(f.origin.y).toBe(2.6);
    expect([f.normal.x, f.normal.y, f.normal.z]).toEqual([0, 1, 0]);
    expect(f.uMaxM).toBe(3); // along length
    expect(f.vMaxM).toBe(0.2); // along thickness
    expect(f.depthM).toBe(2.6); // a through-cut goes down the height
  });
  it('places the right (end) face at +x, width along height', () => {
    const f = wallFaceFrame('right', dims);
    expect(f.origin.x).toBe(3);
    expect([f.normal.x, f.normal.y, f.normal.z]).toEqual([1, 0, 0]);
    expect(f.uMaxM).toBe(2.6);
    expect(f.depthM).toBe(3);
  });
});

describe('buildFaceFeatureGeometry', () => {
  it('orients a top-face recess so it sits at the top and cuts downward', () => {
    // On a side face v runs along the thickness, 0..thicknessMm (here 0..200), measured from
    // the back edge — not centred like a front-face feature.
    const outline = [
      { x: 1000, z: 50 },
      { x: 2000, z: 50 },
      { x: 2000, z: 150 },
      { x: 1000, z: 150 },
    ];
    const geo = buildFaceFeatureGeometry(outline, wallFaceFrame('top', dims), 0.1, false);
    expect(geo).not.toBeNull();
    const box = new Box3().setFromBufferAttribute(geo!.getAttribute('position') as never);
    // u (length) ∈ [1,2]; v (thickness 50..150mm → z -0.05..0.05); cut from the top (2.6) down 0.1
    expect(box.min.x).toBeCloseTo(1, 3);
    expect(box.max.x).toBeCloseTo(2, 3);
    expect(box.max.y).toBeCloseTo(2.6, 3);
    expect(box.min.y).toBeCloseTo(2.5, 3);
    expect(box.min.z).toBeCloseTo(-0.05, 3);
    expect(box.max.z).toBeCloseTo(0.05, 3);
  });
});

describe('applyWallFaceFeatures (CSG)', () => {
  const body = () => new BoxGeometry(3, 2.6, 0.2).translate(1.5, 1.3, 0);

  it('returns the body unchanged when there are no features', () => {
    const b = body();
    expect(applyWallFaceFeatures(b, [], dims)).toBe(b);
  });

  it('subtracts a top-face hole from a real ExtrudeGeometry wall body (the production path)', () => {
    // Mirror buildWallGeometries: a rectangle profile extruded along thickness, then centred.
    const shape = new Shape();
    shape.moveTo(0, 0);
    shape.lineTo(3, 0);
    shape.lineTo(3, 2.6);
    shape.lineTo(0, 2.6);
    shape.closePath();
    const extrudeBody = new ExtrudeGeometry(shape, { depth: 0.2, bevelEnabled: false });
    extrudeBody.translate(0, 0, -0.1);
    const before = extrudeBody.getAttribute('position').count;
    const result = applyWallFaceFeatures(
      extrudeBody,
      [
        {
          outlineMm: [
            { x: 1300, z: 50 },
            { x: 1700, z: 50 },
            { x: 1700, z: 150 },
            { x: 1300, z: 150 },
          ],
          side: 'top',
          mode: 'hole',
          depthMm: 0,
        },
      ],
      dims,
    );
    expect(result.getAttribute('position').count).toBeGreaterThan(0);
    // the cut must actually change the mesh (more triangles than the plain box body)
    expect(result.getAttribute('position').count).not.toBe(before);
  });

  it('subtracts a top-face hole and still yields a solid geometry', () => {
    const result = applyWallFaceFeatures(
      body(),
      [
        {
          outlineMm: [
            { x: 1300, z: -60 },
            { x: 1700, z: -60 },
            { x: 1700, z: 60 },
            { x: 1300, z: 60 },
          ],
          side: 'top',
          mode: 'hole',
          depthMm: 0,
        },
      ],
      dims,
    );
    expect(result.getAttribute('position').count).toBeGreaterThan(0);
  });
});
