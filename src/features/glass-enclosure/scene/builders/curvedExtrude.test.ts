import { describe, expect, it } from 'vitest';
import {
  buildCurvedShapedFrameGeometry,
  buildCurvedShapedGeometry,
  buildCurvedWallFeatureSolid,
  curvedWallPickUv,
} from './curvedExtrude';
import { panelOutlinePointsMm } from '../../model/panelOutline';
import { outlineSelfIntersects, sanitizeFreeOutline } from '../../model/wallFeatureGeometry';

const signedVolume = (positions: ArrayLike<number>) => {
  let volume = 0;
  for (let i = 0; i + 8 < positions.length; i += 9) {
    const ax = positions[i];
    const ay = positions[i + 1];
    const az = positions[i + 2];
    const bx = positions[i + 3];
    const by = positions[i + 4];
    const bz = positions[i + 5];
    const cx = positions[i + 6];
    const cy = positions[i + 7];
    const cz = positions[i + 8];
    volume += (ax * (by * cz - bz * cy) - ay * (bx * cz - bz * cx) + az * (bx * cy - by * cx)) / 6;
  }
  return volume;
};

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

  it('maps a polygon IDENTICALLY for chord vs developed widthMm (the bbox-rescale ↔ cyl de-normalize cancel)', () => {
    // The autofill polygon is bbox-rescaled to widthMm by panelOutlinePointsMm, then cyl()
    // de-normalizes by the SAME widthMm — so a triangle maps onto [phiStart,phiEnd] regardless of
    // whether widthMm is the chord or the developed span (guards the deferred widthMm semantic change).
    const points = [
      { x: -400, y: 0 },
      { x: 400, y: 0 },
      { x: 0, y: 1800 },
    ];
    const spec = { widthMm: 0, heightMm: 1800, shapeKind: 'polygon' as const, points };
    const chord = 900;
    const developed = 1400;
    const gc = buildCurvedShapedGeometry(
      panelOutlinePointsMm({ ...spec, widthMm: chord }),
      chord,
      R,
      1,
      0,
      Math.PI / 3,
      t,
    );
    const gd = buildCurvedShapedGeometry(
      panelOutlinePointsMm({ ...spec, widthMm: developed }),
      developed,
      R,
      1,
      0,
      Math.PI / 3,
      t,
    );
    const pc = gc.attributes.position;
    const pd = gd.attributes.position;
    // Earcut triangulation is aspect-dependent, so the triangle count can differ between chord and
    // developed — but the mapped arc SURFACE must be identical: cyl() de-normalizes by the same
    // widthMm, so both occupy the same 3D bounding region. Assert bbox equality (the real invariant).
    const bounds = (p: typeof pc) => {
      const lo = [Infinity, Infinity, Infinity];
      const hi = [-Infinity, -Infinity, -Infinity];
      for (let i = 0; i < p.count; i += 1) {
        for (let a = 0; a < 3; a += 1) {
          const v = p.array[i * 3 + a];
          lo[a] = Math.min(lo[a], v);
          hi[a] = Math.max(hi[a], v);
        }
      }
      return { lo, hi };
    };
    const bc = bounds(pc);
    const bd = bounds(pd);
    for (let a = 0; a < 3; a += 1) {
      expect(bc.lo[a]).toBeCloseTo(bd.lo[a], 5);
      expect(bc.hi[a]).toBeCloseTo(bd.hi[a], 5);
    }
  });

  it('follows a CONCAVE silhouette (an inward notch is NOT filled in — earcut, not convex hull)', () => {
    // An L / notched pane: the old spanAtX column scan filled the notch (convex hull). The earcut
    // fill must leave the notch empty — assert no front-face vertex lands inside the removed corner.
    const notch = [
      { x: -500, y: 0 },
      { x: 500, y: 0 },
      { x: 500, y: 2000 },
      { x: 0, y: 2000 },
      { x: 0, y: 1000 },
      { x: -500, y: 1000 },
    ];
    const g = buildCurvedShapedGeometry(notch, 1000, 2, 1, 0, Math.PI / 4, 0.02);
    const p = g.attributes.position;
    expect(p.count).toBeGreaterThan(0);
    // The polygon fills the whole lower half plus the RIGHT column above y=1000; the removed corner
    // (empty notch) is the top-LEFT region x∈(-500,0), y∈(1000,2000), centre (-250,1500). Map it
    // through the same cyl() the builder uses and confirm no emitted vertex sits at that surface point.
    const span = Math.PI / 4;
    const w = 1000;
    const centerY = -2;
    const cyl = (xMm: number, yMm: number, r: number) => {
      const phi = ((xMm + w / 2) / w) * span;
      const a = Math.PI / 2 - phi;
      return [Math.cos(a) * r, centerY + Math.sin(a) * r, yMm / 1000];
    };
    const [hx, hy, hz] = cyl(-250, 1500, 2 + 0.01);
    let minDist = Infinity;
    for (let i = 0; i < p.count; i += 1) {
      minDist = Math.min(minDist, Math.hypot(p.getX(i) - hx, p.getY(i) - hy, p.getZ(i) - hz));
    }
    // No vertex near the notch centre — the surface does not cover the removed corner.
    expect(minDist).toBeGreaterThan(0.1);
  });
});

describe('buildCurvedShapedFrameGeometry', () => {
  const R = 2;
  const w = 1000;
  const h = 2000;
  const frameDepth = 0.03;
  const tri = [
    { x: -w / 2, y: 0 },
    { x: w / 2, y: 0 },
    { x: 0, y: h },
  ];

  it('builds a non-empty ring that hugs the cylinder band (radius ± half frame depth)', () => {
    const g = buildCurvedShapedFrameGeometry(tri, w, R, 1, 0, Math.PI / 4, frameDepth, 35);
    const p = g.attributes.position;
    expect(p.count).toBeGreaterThan(0);
    const centerY = -R;
    for (let i = 0; i < p.count; i += 1) {
      const radial = Math.hypot(p.getX(i), p.getY(i) - centerY);
      expect(radial).toBeGreaterThanOrEqual(R - frameDepth / 2 - 1e-4);
      expect(radial).toBeLessThanOrEqual(R + frameDepth / 2 + 1e-4);
    }
  });

  it('keeps the frame within the panel arc span and height (no overflow past the hole)', () => {
    const span = Math.PI / 4;
    const g = buildCurvedShapedFrameGeometry(tri, w, R, 1, 0, span, frameDepth, 35);
    const p = g.attributes.position;
    const centerY = -R;
    let minZ = Infinity;
    let maxZ = -Infinity;
    let minPhi = Infinity;
    let maxPhi = -Infinity;
    for (let i = 0; i < p.count; i += 1) {
      minZ = Math.min(minZ, p.getZ(i));
      maxZ = Math.max(maxZ, p.getZ(i));
      const phi = Math.PI / 2 - Math.atan2(p.getY(i) - centerY, p.getX(i));
      minPhi = Math.min(minPhi, phi);
      maxPhi = Math.max(maxPhi, phi);
    }
    expect(minZ).toBeGreaterThanOrEqual(-1e-4);
    expect(maxZ).toBeLessThanOrEqual(h / 1000 + 1e-4);
    expect(minPhi).toBeGreaterThanOrEqual(-1e-3);
    expect(maxPhi).toBeLessThanOrEqual(span + 1e-3);
  });

  it('leaves a hollow centre (the inset is punched out — a frame, not a filled pane)', () => {
    const g = buildCurvedShapedFrameGeometry(tri, w, R, 1, 0, Math.PI / 4, frameDepth, 35);
    // The centroid of the triangle at mid-radius must NOT be covered: the nearest frame vertex to
    // the pane centre stays a frame-width away (no vertex sits at the deep interior).
    const p = g.attributes.position;
    const centerY = -R;
    const midPhi = Math.PI / 8;
    const a = Math.PI / 2 - midPhi;
    const cxWorld = Math.cos(a) * R;
    const cyWorld = centerY + Math.sin(a) * R;
    const czWorld = h / 3 / 1000; // triangle centroid height
    let nearest = Infinity;
    for (let i = 0; i < p.count; i += 1) {
      nearest = Math.min(
        nearest,
        Math.hypot(p.getX(i) - cxWorld, p.getY(i) - cyWorld, p.getZ(i) - czWorld),
      );
    }
    expect(nearest).toBeGreaterThan(0.05);
  });

  it('degenerates safely to empty geometry for <3 points or a zero-area outline', () => {
    expect(
      buildCurvedShapedFrameGeometry(
        [
          { x: 0, y: 0 },
          { x: 10, y: 0 },
        ],
        w,
        R,
        1,
        0,
        Math.PI / 4,
        frameDepth,
        35,
      ).attributes.position?.count ?? 0,
    ).toBe(0);
    expect(
      buildCurvedShapedFrameGeometry(
        [
          { x: 0, y: 0 },
          { x: 0, y: 100 },
          { x: 0, y: 200 },
        ],
        w,
        R,
        1,
        0,
        Math.PI / 4,
        frameDepth,
        35,
      ).attributes.position?.count ?? 0,
    ).toBe(0);
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

  it.each([1, -1] as const)(
    'emits an OUTWARD-oriented (positive-volume) cutter for direction %d — an inverted cutter makes the CSG keep only the plug',
    (direction) => {
      const g = buildCurvedWallFeatureSolid(
        rect,
        lengthMm,
        R,
        direction,
        sweep,
        R - 0.15,
        R + 0.15,
      );
      expect(signedVolume(g.getAttribute('position').array)).toBeGreaterThan(0);
    },
  );

  it.each([1, -1] as const)(
    'densifies CAP triangles for a WIDE outline so no flat chord sags below the band skin (direction %d)',
    (direction) => {
      const wide = [
        { x: 800, z: 800 },
        { x: 2000, z: 800 },
        { x: 2000, z: 1600 },
        { x: 800, z: 1600 },
      ];
      const g = buildCurvedWallFeatureSolid(
        wide,
        lengthMm,
        R,
        direction,
        sweep,
        R - 0.15,
        R + 0.15,
      );
      const positions = g.getAttribute('position').array;
      expect(signedVolume(positions)).toBeGreaterThan(0);
      const centerY = -direction * R;
      const phiOf = (x: number, y: number) => {
        const a = Math.atan2(y - centerY, x);
        return direction === 1 ? Math.PI / 2 - a : a + Math.PI / 2;
      };
      let maxSpan = 0;
      for (let i = 0; i + 8 < positions.length; i += 9) {
        const phis = [0, 3, 6].map((o) => phiOf(positions[i + o], positions[i + o + 1]));
        maxSpan = Math.max(maxSpan, Math.max(...phis) - Math.min(...phis));
      }
      expect(maxSpan).toBeLessThanOrEqual(0.031);
    },
  );

  it('handles a concave freehand-like outline with a positive volume', () => {
    const concave = [
      { x: 1000, z: 800 },
      { x: 1600, z: 700 },
      { x: 1900, z: 1100 },
      { x: 1600, z: 1000 },
      { x: 1450, z: 1500 },
      { x: 1150, z: 1350 },
    ];
    const g = buildCurvedWallFeatureSolid(concave, lengthMm, R, -1, sweep, R - 0.15, R + 0.15);
    expect(signedVolume(g.getAttribute('position').array)).toBeGreaterThan(0);
  });
});

describe('sanitizeFreeOutline', () => {
  const square = [
    { x: 0, z: 0 },
    { x: 400, z: 0 },
    { x: 400, z: 400 },
    { x: 0, z: 400 },
  ];

  it('passes a simple loop through untouched', () => {
    expect(outlineSelfIntersects(square)).toBe(false);
    expect(sanitizeFreeOutline(square)).toEqual(square);
  });

  it('trims a closing hook that crosses the loop', () => {
    const hooked = [...square, { x: 200, z: -80 }];
    expect(outlineSelfIntersects(hooked)).toBe(true);
    const cleaned = sanitizeFreeOutline(hooked);
    expect(cleaned).not.toBeNull();
    expect(outlineSelfIntersects(cleaned ?? [])).toBe(false);
    expect(cleaned).toHaveLength(4);
  });

  it('never returns a self-crossing loop — repairs to a simple polygon or rejects', () => {
    const bowtie = [
      { x: 0, z: 0 },
      { x: 400, z: 400 },
      { x: 400, z: 0 },
      { x: 0, z: 400 },
    ];
    expect(outlineSelfIntersects(bowtie)).toBe(true);
    const repaired = sanitizeFreeOutline(bowtie);
    expect(repaired === null || !outlineSelfIntersects(repaired)).toBe(true);
    const densePoints: { x: number; z: number }[] = [];
    for (let i = 0; i < bowtie.length; i += 1) {
      const p = bowtie[i];
      const q = bowtie[(i + 1) % bowtie.length];
      for (let k = 0; k < 10; k += 1) {
        densePoints.push({
          x: p.x + ((q.x - p.x) * k) / 10,
          z: p.z + ((q.z - p.z) * k) / 10,
        });
      }
    }
    expect(outlineSelfIntersects(densePoints)).toBe(true);
    const dense = sanitizeFreeOutline(densePoints);
    expect(dense === null || !outlineSelfIntersects(dense)).toBe(true);
  });
});

describe('buildCurvedShapedGeometry winding', () => {
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

  // The (u,z) -> 3D map is ORIENTATION-REVERSING for direction === +1 (toAngle = PI/2 - phi, so the
  // angle DECREASES as x grows). A triangle wound CCW in the flat outline therefore comes out
  // CLOCKWISE on the cylinder, and the shaped pane renders inside-out: you see its back faces and
  // the glass looks hollow from the outside. The cutter (buildCurvedWallFeatureSolid) already
  // mirrors its emit for +1; the shaped PANE did not, which is the "arc wall hole looks wrong on
  // positive sweep" report.
  it.each([1, -1] as const)('is outward-facing for direction %i', (direction) => {
    const g = buildCurvedShapedGeometry(rect, w, R, direction, 0, Math.PI / 4, t);
    expect(signedVolume(g.getAttribute('position').array)).toBeGreaterThan(0);
  });
});
