import { describe, expect, it } from 'vitest';
import { mirrorSlabPatch, mirrorSurfacePatch, mirrorWallPatch } from './mirrorBody';
import { bodyDevelopedLengthMm, chordDirectionDeg } from '../geometry/curvature';
import type {
  SceneSlabState,
  SceneSurfaceState,
  SceneWallFeature,
  SceneWallState,
} from './project.types';

const wall = (over: Partial<SceneWallState> = {}): SceneWallState =>
  ({
    id: 'w1',
    originX: 0,
    originY: 0,
    rotationDeg: 0,
    lengthMm: 4000,
    heightMm: 2500,
    thicknessMm: 100,
    openings: [],
    features: [],
    ...over,
  }) as SceneWallState;

const slab = (over: Partial<SceneSlabState> = {}): SceneSlabState =>
  ({
    id: 's1',
    kind: 'roof',
    originX: 0,
    originY: 0,
    rotationDeg: 0,
    lengthMm: 4000,
    depthMm: 2000,
    thicknessMm: 120,
    features: [],
    ...over,
  }) as SceneSlabState;

describe('mirrorWallPatch', () => {
  it('flips the arc instead of leaving it bulging the same way', () => {
    const w = wall({ geomArcRadiusMm: 3000, geomArcSweepDeg: 80, rotationDeg: -40 });

    const patch = mirrorWallPatch(w);

    expect(patch.geomArcSweepDeg).toBe(-80);
    // The old mirror wrote neither of these — that is why the button looked like a no-op.
    expect(patch.rotationDeg).toBeDefined();
    expect(patch.rotationDeg).not.toBe(w.rotationDeg);
  });

  it('keeps the chord direction so the mirrored wall still spans the same line', () => {
    const w = wall({ geomArcRadiusMm: 3000, geomArcSweepDeg: 80, rotationDeg: -40 });

    const after = { ...w, ...mirrorWallPatch(w) };

    expect(chordDirectionDeg(after)).toBeCloseTo(chordDirectionDeg(w), 6);
  });

  it('mirrors openings about the DEVELOPED band, not the chord', () => {
    const w = wall({
      geomArcRadiusMm: 3000,
      geomArcSweepDeg: 80,
      rotationDeg: -40,
      openings: [
        {
          id: 'o1',
          kind: 'window' as const,
          offsetMm: 3000,
          sillMm: 900,
          widthMm: 800,
          heightMm: 1200,
        },
      ],
    } as Partial<SceneWallState>);

    const developed = bodyDevelopedLengthMm(w);
    expect(developed).toBeGreaterThan(w.lengthMm);

    const patch = mirrorWallPatch(w);

    expect(patch.openings?.[0].offsetMm).toBeCloseTo(developed - 3000, 6);
    // Mirroring about the chord would land it here — off the band by the chord/arc difference.
    expect(patch.openings?.[0].offsetMm).not.toBeCloseTo(w.lengthMm - 3000, 3);
  });

  it('mirrors a straight wall about its plain length', () => {
    const w = wall({
      openings: [
        {
          id: 'o1',
          kind: 'window' as const,
          offsetMm: 1000,
          sillMm: 900,
          widthMm: 800,
          heightMm: 1200,
        },
      ],
    } as Partial<SceneWallState>);

    expect(mirrorWallPatch(w).openings?.[0].offsetMm).toBe(3000);
  });

  it('swaps the left/right edge bows and the corner radii together', () => {
    const w = wall({
      geomEdgeArc: { left: 200, right: 0 },
      cornerRadiiMm: { tl: 50, tr: 0, bl: 10, br: 0 },
    });

    const patch = mirrorWallPatch(w);

    expect(patch.geomEdgeArc).toEqual({ front: undefined, back: undefined, left: 0, right: 200 });
    expect(patch.cornerRadiiMm).toEqual({ tl: 0, tr: 50, bl: 0, br: 10 });
  });

  it('swaps a sloped wall end for end', () => {
    const patch = mirrorWallPatch(wall({ heightMm: 2000, heightEndMm: 3000 }));
    expect(patch.heightMm).toBe(3000);
    expect(patch.heightEndMm).toBe(2000);
  });

  it('leaves a straight wall with no asymmetry unchanged', () => {
    const patch = mirrorWallPatch(wall());
    expect(patch.geomArcSweepDeg).toBeUndefined();
    expect(patch.openings).toEqual([]);
  });
});

describe('mirrorSlabPatch', () => {
  it('negates the sweep WITHOUT rolling rotationDeg (symmetric pose)', () => {
    const s = slab({ geomArcRadiusMm: 3000, geomArcSweepDeg: -60, rotationDeg: 25 });

    const patch = mirrorSlabPatch(s);

    expect(patch.geomArcSweepDeg).toBe(60);
    // Rolling a slab would swing the whole deck — the mesh builder owns the mirror.
    expect(patch.rotationDeg).toBe(25);
  });

  it('mirrors slab features about the developed length', () => {
    const s = slab({
      geomArcRadiusMm: 3000,
      geomArcSweepDeg: -60,
      features: [{ id: 'f1', offsetMm: 1000 }],
    } as Partial<SceneSlabState>);

    const developed = bodyDevelopedLengthMm(s);
    expect(mirrorSlabPatch(s).features?.[0].offsetMm).toBeCloseTo(developed - 1000, 6);
  });
});

describe('mirrorSurfacePatch', () => {
  const surface = (over: Partial<SceneSurfaceState> = {}): SceneSurfaceState =>
    ({
      id: 'sf1',
      kind: 'roof',
      points: [
        { x: 0, y: 0 },
        { x: 4000, y: 0 },
        { x: 4000, y: 3000 },
        { x: 0, y: 3000 },
      ],
      thicknessMm: 100,
      elevationMm: 0,
      ...over,
    }) as SceneSurfaceState;

  it('mirrors the points about their centroid', () => {
    const patch = mirrorSurfacePatch(surface());
    expect(patch.points?.map((p) => p.x)).toEqual([4000, 0, 0, 4000]);
    expect(patch.points?.map((p) => p.y)).toEqual([0, 0, 3000, 3000]);
  });

  it('negates every edge bow so an outward bulge does not become an inward dent', () => {
    const patch = mirrorSurfacePatch(surface({ edgeArcs: [500, null, -250, null] }));
    expect(patch.edgeArcs).toEqual([-500, null, 250, null]);
  });

  it('mirroring twice returns the original shape', () => {
    const s = surface({ edgeArcs: [500, null, -250, null] });
    const once = { ...s, ...mirrorSurfacePatch(s) };
    const twice = { ...once, ...mirrorSurfacePatch(once) };
    expect(twice.points).toEqual(s.points);
    expect(twice.edgeArcs).toEqual(s.edgeArcs);
  });

  it('is a no-op on a surface with no points', () => {
    expect(mirrorSurfacePatch(surface({ points: [] }))).toEqual({});
  });
});

describe('mirroring an END-FACE feature', () => {
  const sideFeature = (over: Partial<SceneWallFeature> = {}): SceneWallFeature => ({
    id: 'f',
    shape: 'rect',
    mode: 'hole',
    side: 'right',
    offsetMm: 1000,
    centerZMm: 100,
    widthMm: 300,
    heightMm: 300,
    depthMm: 200,
    ...over,
  });

  const wall4x26 = (features: SceneWallFeature[]): SceneWallState =>
    ({
      id: 'w',
      originX: 0,
      originY: 0,
      rotationDeg: 0,
      lengthMm: 4000,
      heightMm: 2600,
      thicknessMm: 200,
      openings: [],
      features,
    }) as unknown as SceneWallState;

  it('swaps the face and leaves the coordinates alone', () => {
    // offsetMm on left/right is a HEIGHT, not a length — 4000-1000 would have thrown it to 3000,
    // outside the face's 2600 range, and the CSG cutter would have missed the body entirely.
    const patch = mirrorWallPatch(wall4x26([sideFeature()]));
    const mirrored = patch.features?.[0];
    expect(mirrored?.side).toBe('left');
    expect(mirrored?.offsetMm).toBe(1000);
    expect(mirrored?.centerZMm).toBe(100);
  });

  it('does not flip an end-face outline on the wrong axis', () => {
    const patch = mirrorWallPatch(
      wall4x26([
        sideFeature({
          shape: 'free',
          points: [
            { x: 0, z: 0 },
            { x: 120, z: 0 },
            { x: 0, z: 90 },
          ],
        }),
      ]),
    );
    expect(patch.features?.[0].points).toEqual([
      { x: 0, z: 0 },
      { x: 120, z: 0 },
      { x: 0, z: 90 },
    ]);
  });

  it('still mirrors a FRONT-face feature the old way', () => {
    const patch = mirrorWallPatch(wall4x26([sideFeature({ side: 1, offsetMm: 1200 })]));
    expect(patch.features?.[0].side).toBe(1);
    expect(patch.features?.[0].offsetMm).toBe(2800);
  });

  it('mirroring twice returns an end-face feature to itself', () => {
    const once = mirrorWallPatch(wall4x26([sideFeature()]));
    const twice = mirrorWallPatch(wall4x26(once.features ?? []));
    expect(twice.features?.[0].side).toBe('right');
    expect(twice.features?.[0].offsetMm).toBe(1000);
  });
});
