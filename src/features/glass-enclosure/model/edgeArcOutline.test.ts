import { describe, expect, it } from 'vitest';
import {
  bowedPolygonOutline,
  edgeArcOutline,
  hasEdgeArc,
  hasPolygonEdgeArc,
} from './edgeArcOutline';
import { polygonAreaMm2, polygonSelfIntersects, type Point2D } from './polygonValidation';

describe('edgeArcOutline', () => {
  it('returns the four rectangle corners when no edge is arced', () => {
    const out = edgeArcOutline(2000, 1000, {});
    expect(out).toEqual([
      { x: 0, y: 0 },
      { x: 2000, y: 0 },
      { x: 2000, y: 1000 },
      { x: 0, y: 1000 },
    ]);
  });

  it('bows the front edge OUTWARD (-y), growing the area and staying simple', () => {
    const plain = polygonAreaMm2(edgeArcOutline(2000, 1000, {}));
    const arced = edgeArcOutline(2000, 1000, { front: 300 }, 32);
    expect(arced.length).toBeGreaterThan(4);
    expect(polygonAreaMm2(arced)).toBeGreaterThan(plain);
    expect(Math.min(...arced.map((p) => p.y))).toBeLessThan(-250);
    expect(polygonSelfIntersects(arced)).toBe(false);
  });

  it('bows each edge on its own outward side without self-intersecting', () => {
    const out = edgeArcOutline(2000, 1000, { front: 200, back: 200, left: 200, right: 200 }, 24);
    expect(Math.min(...out.map((p) => p.y))).toBeLessThan(-150);
    expect(Math.max(...out.map((p) => p.y))).toBeGreaterThan(1150);
    expect(Math.min(...out.map((p) => p.x))).toBeLessThan(-150);
    expect(Math.max(...out.map((p) => p.x))).toBeGreaterThan(2150);
    expect(polygonSelfIntersects(out)).toBe(false);
  });

  it('ignores a negligible sagitta and a degenerate rectangle', () => {
    expect(edgeArcOutline(2000, 1000, { front: 0.5 })).toHaveLength(4);
    expect(edgeArcOutline(0, 1000, { front: 300 })).toHaveLength(0);
    expect(edgeArcOutline(2000, -5, { front: 300 })).toHaveLength(0);
  });

  it('hasEdgeArc detects a real bow', () => {
    expect(hasEdgeArc(undefined)).toBe(false);
    expect(hasEdgeArc({})).toBe(false);
    expect(hasEdgeArc({ front: 0.5 })).toBe(false);
    expect(hasEdgeArc({ front: 200 })).toBe(true);
  });
});

describe('bowedPolygonOutline', () => {
  const square: Point2D[] = [
    { x: 0, y: 0 },
    { x: 1000, y: 0 },
    { x: 1000, y: 1000 },
    { x: 0, y: 1000 },
  ];

  it('returns the base polygon unchanged when no edge is bowed', () => {
    expect(bowedPolygonOutline(square, null)).toEqual(square);
    expect(bowedPolygonOutline(square, [0, 0, 0, 0])).toEqual(square);
    expect(bowedPolygonOutline(square, [0.5, 0, 0, 0])).toEqual(square);
  });

  it('bows a single polygon edge into a densified, still-simple outline', () => {
    const bowed = bowedPolygonOutline(square, [200, null, null, null], 24);
    expect(bowed.length).toBeGreaterThan(4);
    expect(polygonSelfIntersects(bowed)).toBe(false);
    expect(polygonAreaMm2(bowed)).not.toBe(polygonAreaMm2(square));
  });

  it('hasPolygonEdgeArc detects a real bow', () => {
    expect(hasPolygonEdgeArc(null)).toBe(false);
    expect(hasPolygonEdgeArc([0, 0, 0, 0])).toBe(false);
    expect(hasPolygonEdgeArc([0, 200, 0, 0])).toBe(true);
  });
});
