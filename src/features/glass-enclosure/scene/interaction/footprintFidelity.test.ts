import { describe, expect, it } from 'vitest';
import { footprintsPenetrate } from '@/shared/three-engine';
import { buildSlabFootprint, buildSurfaceFootprint, slabRiseMm } from './planCollision';
import type { SceneSlabState, SceneSurfaceState } from '../../model/project.types';

/**
 * The footprint has to describe the body that is actually drawn. Two places where it did not:
 *  - a barrel/pitched roof extrudes up to `rise + thickness`, but the box claimed only `thickness`,
 *    so anything Alt-stacked onto it sank into the vault and no collision was reported;
 *  - a bowed edge (slab `geomEdgeArc`, surface `edgeArcs`) is drawn — and exported to DXF — as a
 *    curve, but the footprint was the raw rectangle/vertex list, so the bulge was transparent one
 *    way and phantom the other.
 */

const slab = (over: Partial<SceneSlabState> = {}): SceneSlabState =>
  ({
    id: 's',
    kind: 'roof',
    originX: 0,
    originY: 0,
    rotationDeg: 0,
    lengthMm: 4000,
    depthMm: 3000,
    thicknessMm: 200,
    elevationMm: 2600,
    ...over,
  }) as SceneSlabState;

const surface = (over: Partial<SceneSurfaceState> = {}): SceneSurfaceState =>
  ({
    id: 'sf',
    kind: 'floor',
    points: [
      { x: 0, y: 0 },
      { x: 4000, y: 0 },
      { x: 4000, y: 3000 },
      { x: 0, y: 3000 },
    ],
    elevationMm: 0,
    thicknessMm: 120,
    ...over,
  }) as SceneSurfaceState;

describe('a ridge is part of the body', () => {
  it('reports the rise of a barrel and a pitched roof', () => {
    expect(slabRiseMm(slab())).toBe(0);
    expect(slabRiseMm(slab({ arcRiseMm: 800 }))).toBe(800);
    expect(slabRiseMm(slab({ pitchRiseMm: 500 }))).toBe(500);
    // Negative/garbage never shrinks the box.
    expect(slabRiseMm(slab({ arcRiseMm: -400 }))).toBe(0);
  });

  it('extends the footprint top by the ridge', () => {
    const flat = buildSlabFootprint(slab(), 0, 0, 0);
    const vaulted = buildSlabFootprint(slab({ arcRiseMm: 800 }), 0, 0, 0);
    expect(flat.zMaxMm).toBe(2800);
    expect(vaulted.zMaxMm).toBe(3600);
    // The base is untouched — a ridge grows upward, it does not thicken the deck downward.
    expect(vaulted.zMinMm).toBe(flat.zMinMm);
  });

  it('a body parked inside the vault now collides', () => {
    // A slab sitting at 3000 clears a flat 2600+200 roof but cuts straight through an 800 ridge.
    const intruder = buildSlabFootprint(
      slab({ id: 'intruder', elevationMm: 3000, thicknessMm: 200 }),
      0,
      0,
      0,
    );
    expect(footprintsPenetrate(intruder, buildSlabFootprint(slab(), 0, 0, 0))).toBe(false);
    expect(
      footprintsPenetrate(intruder, buildSlabFootprint(slab({ arcRiseMm: 800 }), 0, 0, 0)),
    ).toBe(true);
  });
});

describe('a bowed edge is part of the plan silhouette', () => {
  it('a slab bowed outward claims ground the rectangle did not', () => {
    const straight = buildSlabFootprint(slab({ kind: 'floor' }), 0, 0, 0);
    const bowed = buildSlabFootprint(slab({ kind: 'floor', geomEdgeArc: { front: 600 } }), 0, 0, 0);
    expect(straight.polygon).toBeUndefined();
    expect(bowed.polygon?.length ?? 0).toBeGreaterThan(4);
  });

  it('keeps the plain rectangle when there is no meaningful bow', () => {
    expect(buildSlabFootprint(slab({ geomEdgeArc: {} }), 0, 0, 0).polygon).toBeUndefined();
  });

  it('a surface footprint follows its bowed edges', () => {
    const straight = buildSurfaceFootprint(surface());
    const bowed = buildSurfaceFootprint(surface({ edgeArcs: [600, null, null, null] }));
    expect(bowed.polygon?.length ?? 0).toBeGreaterThan(straight.polygon?.length ?? 0);
  });

  it('a surface with no arcs is byte-for-byte the old vertex footprint', () => {
    expect(buildSurfaceFootprint(surface({ edgeArcs: null })).polygon).toEqual(
      buildSurfaceFootprint(surface()).polygon,
    );
  });

  it('the drag offset still applies to the bowed outline', () => {
    const at0 = buildSurfaceFootprint(surface({ edgeArcs: [600, null, null, null] }));
    const at500 = buildSurfaceFootprint(surface({ edgeArcs: [600, null, null, null] }), 500, 0);
    expect(at500.polygon?.[0].x).toBe((at0.polygon?.[0].x ?? 0) + 500);
  });
});
