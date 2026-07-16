import { describe, expect, it } from 'vitest';
import { enclosedPolygonFromWalls } from './enclosedPolygonFromWalls';
import { arcEndLocal } from './arcGeometry';
import { polygonAreaM2, polygonSelfIntersects } from './polygonValidation';
import type { SceneWallState } from './project.types';

const wall = (over: Partial<SceneWallState> = {}): SceneWallState => ({
  id: 'w',
  originX: 0,
  originY: 0,
  lengthMm: 3000,
  rotationDeg: 0,
  heightMm: 2600,
  thicknessMm: 100,
  ...over,
});

const square = (side: number): SceneWallState[] => [
  wall({ id: 'a', originX: 0, originY: 0, lengthMm: side, rotationDeg: 0 }),
  wall({ id: 'b', originX: side, originY: 0, lengthMm: side, rotationDeg: 90 }),
  wall({ id: 'c', originX: side, originY: side, lengthMm: side, rotationDeg: 180 }),
  wall({ id: 'd', originX: 0, originY: side, lengthMm: side, rotationDeg: 270 }),
];

describe('enclosedPolygonFromWalls', () => {
  it('traces a four-wall square into its four corners with the exact area', () => {
    const poly = enclosedPolygonFromWalls(square(3000));
    expect(poly).not.toBeNull();
    expect(poly).toHaveLength(4);
    expect(polygonAreaM2(poly!)).toBeCloseTo(9, 3);
    expect(polygonSelfIntersects(poly!)).toBe(false);
  });

  it('traces a concave L-shape (six walls) into its six corners with the exact area', () => {
    const l: SceneWallState[] = [
      wall({ id: '0', originX: 0, originY: 0, lengthMm: 4000, rotationDeg: 0 }),
      wall({ id: '1', originX: 4000, originY: 0, lengthMm: 2000, rotationDeg: 90 }),
      wall({ id: '2', originX: 4000, originY: 2000, lengthMm: 2000, rotationDeg: 180 }),
      wall({ id: '3', originX: 2000, originY: 2000, lengthMm: 2000, rotationDeg: 90 }),
      wall({ id: '4', originX: 2000, originY: 4000, lengthMm: 2000, rotationDeg: 180 }),
      wall({ id: '5', originX: 0, originY: 4000, lengthMm: 4000, rotationDeg: 270 }),
    ];
    const poly = enclosedPolygonFromWalls(l);
    expect(poly).not.toBeNull();
    expect(poly).toHaveLength(6);
    expect(polygonAreaM2(poly!)).toBeCloseTo(12, 3);
    expect(polygonSelfIntersects(poly!)).toBe(false);
  });

  it('returns null for fewer than three walls', () => {
    expect(enclosedPolygonFromWalls(square(3000).slice(0, 2))).toBeNull();
  });

  it('returns null for an open chain (a wall removed → degree-1 corners)', () => {
    expect(enclosedPolygonFromWalls(square(3000).slice(0, 3))).toBeNull();
  });

  it('returns null for a T-junction (a stray wall gives a degree-3 corner)', () => {
    const withStray = [
      ...square(3000),
      wall({ id: 'stray', originX: 0, originY: 0, lengthMm: 1500, rotationDeg: 45 }),
    ];
    expect(enclosedPolygonFromWalls(withStray)).toBeNull();
  });

  it('densifies an arc wall inside the loop (more than the corner count of points)', () => {
    const localE = arcEndLocal(2000, 90);
    const chord = Math.hypot(localE.xMm, localE.yMm);
    const chordAngle = (Math.atan2(localE.yMm, localE.xMm) * 180) / Math.PI;
    const s = Math.round(chord);
    const arcLoop: SceneWallState[] = [
      wall({
        id: 'arc',
        originX: 0,
        originY: 0,
        lengthMm: s,
        rotationDeg: -chordAngle,
        geomArcRadiusMm: 2000,
        geomArcSweepDeg: 90,
      }),
      wall({ id: 'b', originX: s, originY: 0, lengthMm: s, rotationDeg: 90 }),
      wall({ id: 'c', originX: s, originY: s, lengthMm: s, rotationDeg: 180 }),
      wall({ id: 'd', originX: 0, originY: s, lengthMm: s, rotationDeg: 270 }),
    ];
    const poly = enclosedPolygonFromWalls(arcLoop);
    expect(poly).not.toBeNull();
    expect(poly!.length).toBeGreaterThan(4);
    expect(polygonSelfIntersects(poly!)).toBe(false);
  });

  it('bows an arc wall to the wall TRUE apex side (outward, not mirrored)', () => {
    const localE = arcEndLocal(2000, 90);
    const chordAngle = (Math.atan2(localE.yMm, localE.xMm) * 180) / Math.PI;
    const s = Math.round(Math.hypot(localE.xMm, localE.yMm));
    const arcLoop: SceneWallState[] = [
      wall({
        id: 'arc',
        originX: 0,
        originY: 0,
        lengthMm: s,
        rotationDeg: -chordAngle,
        geomArcRadiusMm: 2000,
        geomArcSweepDeg: 90,
      }),
      wall({ id: 'b', originX: s, originY: 0, lengthMm: s, rotationDeg: 90 }),
      wall({ id: 'c', originX: s, originY: s, lengthMm: s, rotationDeg: 180 }),
      wall({ id: 'd', originX: 0, originY: s, lengthMm: s, rotationDeg: 270 }),
    ];
    const poly = enclosedPolygonFromWalls(arcLoop);
    expect(poly).not.toBeNull();
    expect(Math.min(...poly!.map((p) => p.y))).toBeLessThan(-400);
  });

  it('rejects a bow-tie of crossed walls (degree-2 but self-intersecting)', () => {
    const s = 100;
    const diag = Math.hypot(s, s);
    const bowtie: SceneWallState[] = [
      wall({ id: '0', originX: 0, originY: 0, lengthMm: diag, rotationDeg: 45 }),
      wall({ id: '1', originX: s, originY: s, lengthMm: s, rotationDeg: 270 }),
      wall({ id: '2', originX: s, originY: 0, lengthMm: diag, rotationDeg: 135 }),
      wall({ id: '3', originX: 0, originY: s, lengthMm: s, rotationDeg: 270 }),
    ];
    expect(enclosedPolygonFromWalls(bowtie)).toBeNull();
  });

  it('traces a square whose walls have mixed stored orientation', () => {
    const side = 3000;
    const mixed: SceneWallState[] = [
      wall({ id: 'a', originX: 0, originY: 0, lengthMm: side, rotationDeg: 0 }),
      wall({ id: 'b', originX: side, originY: 0, lengthMm: side, rotationDeg: 90 }),
      wall({ id: 'c', originX: 0, originY: side, lengthMm: side, rotationDeg: 0 }),
      wall({ id: 'd', originX: 0, originY: side, lengthMm: side, rotationDeg: 270 }),
    ];
    const poly = enclosedPolygonFromWalls(mixed);
    expect(poly).not.toBeNull();
    expect(polygonAreaM2(poly!)).toBeCloseTo(9, 3);
  });

  it('never throws on degenerate input (empty, NaN, duplicates)', () => {
    expect(() => enclosedPolygonFromWalls([])).not.toThrow();
    expect(enclosedPolygonFromWalls([])).toBeNull();
    expect(() =>
      enclosedPolygonFromWalls([wall({ id: 'nan', originX: NaN, originY: 0 }), ...square(3000)]),
    ).not.toThrow();
    const dup = [...square(3000), ...square(3000).map((w) => ({ ...w, id: `${w.id}2` }))];
    expect(() => enclosedPolygonFromWalls(dup)).not.toThrow();
  });
});
