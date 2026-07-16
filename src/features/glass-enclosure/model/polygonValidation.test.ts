import { describe, expect, it } from 'vitest';
import {
  polygonAreaM2,
  polygonAreaMm2,
  polygonSelfIntersects,
  polygonSignedAreaMm2,
  type Point2D,
} from './polygonValidation';

const square: Point2D[] = [
  { x: 0, y: 0 },
  { x: 100, y: 0 },
  { x: 100, y: 100 },
  { x: 0, y: 100 },
];

describe('polygonSelfIntersects', () => {
  it('accepts a simple convex loop', () => {
    expect(polygonSelfIntersects(square)).toBe(false);
  });

  it('accepts a simple concave loop', () => {
    const arrow: Point2D[] = [
      { x: 0, y: 0 },
      { x: 100, y: 0 },
      { x: 50, y: 40 },
      { x: 100, y: 100 },
      { x: 0, y: 100 },
    ];
    expect(polygonSelfIntersects(arrow)).toBe(false);
  });

  it('detects a proper bow-tie crossing', () => {
    const bowtie: Point2D[] = [
      { x: 0, y: 0 },
      { x: 100, y: 100 },
      { x: 100, y: 0 },
      { x: 0, y: 100 },
    ];
    expect(polygonSelfIntersects(bowtie)).toBe(true);
  });

  it('detects a pinch where an edge passes exactly through a vertex (collinear touch)', () => {
    // The bottom edge (0,0)->(100,0) passes exactly through the notch vertex (50,0):
    // strict-crossing determinants are all zero, so only the collinear-touch test catches it.
    const pinched: Point2D[] = [
      { x: 0, y: 0 },
      { x: 100, y: 0 },
      { x: 100, y: 50 },
      { x: 50, y: 0 },
      { x: 0, y: 50 },
    ];
    expect(polygonSelfIntersects(pinched)).toBe(true);
  });

  it('treats fewer than 4 points as non-self-intersecting', () => {
    expect(
      polygonSelfIntersects([
        { x: 0, y: 0 },
        { x: 1, y: 1 },
        { x: 2, y: 0 },
      ]),
    ).toBe(false);
  });
});

describe('polygon area', () => {
  it('computes a 100x100 square area regardless of winding', () => {
    expect(polygonAreaMm2(square)).toBe(10000);
    expect(polygonAreaMm2([...square].reverse())).toBe(10000);
  });

  it('converts mm^2 to m^2', () => {
    const meterSquare: Point2D[] = [
      { x: 0, y: 0 },
      { x: 1000, y: 0 },
      { x: 1000, y: 1000 },
      { x: 0, y: 1000 },
    ];
    expect(polygonAreaM2(meterSquare)).toBeCloseTo(1, 6);
  });

  it('is zero for degenerate (<3 or collinear) inputs', () => {
    expect(
      polygonAreaMm2([
        { x: 0, y: 0 },
        { x: 10, y: 0 },
      ]),
    ).toBe(0);
    expect(
      polygonAreaMm2([
        { x: 0, y: 0 },
        { x: 10, y: 0 },
        { x: 20, y: 0 },
      ]),
    ).toBe(0);
  });

  it('signed area flips sign with winding', () => {
    expect(polygonSignedAreaMm2(square)).toBeGreaterThan(0);
    expect(polygonSignedAreaMm2([...square].reverse())).toBeLessThan(0);
  });
});
