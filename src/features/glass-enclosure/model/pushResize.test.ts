import { describe, expect, it } from 'vitest';
import { computeNeighbourShrink, type StretchBody } from './pushResize';

const body = (over: Partial<StretchBody>): StretchBody => ({
  id: 'b',
  originX: 0,
  originY: 0,
  rotationDeg: 0,
  lengthMm: 1000,
  minLengthMm: 200,
  ...over,
});

const A = body({ id: 'a', originX: 0, originY: 0, lengthMm: 1000 });

describe('computeNeighbourShrink', () => {
  it('shrinks a flush collinear neighbour so A can grow (end face)', () => {
    const b = body({ id: 'b', originX: 1000, originY: 0, lengthMm: 1000 });
    const r = computeNeighbourShrink(A, 'end', b, 500, 0);
    expect(r.selfGrowMm).toBe(500);
    expect(r.neighbour).toEqual({ id: 'b', newLengthMm: 500, newOriginX: 1500, newOriginY: 0 });
  });

  it('consumes the gap first, then shrinks the neighbour', () => {
    const b = body({ id: 'b', originX: 1100, originY: 0, lengthMm: 1000 });
    const r = computeNeighbourShrink(A, 'end', b, 500, 100);
    expect(r.selfGrowMm).toBe(500);
    expect(r.neighbour).toEqual({ id: 'b', newLengthMm: 600, newOriginX: 1500, newOriginY: 0 });
  });

  it('does a partial push when the neighbour hits its min length', () => {
    const b = body({ id: 'b', originX: 1000, originY: 0, lengthMm: 300, minLengthMm: 200 });
    const r = computeNeighbourShrink(A, 'end', b, 500, 0);
    expect(r.selfGrowMm).toBe(100);
    expect(r.neighbour).toEqual({ id: 'b', newLengthMm: 200, newOriginX: 1100, newOriginY: 0 });
  });

  it('handles a neighbour whose axis points back toward A (far edge fixed at origin)', () => {
    const b = body({ id: 'b', originX: 2000, originY: 0, rotationDeg: 180, lengthMm: 1000 });
    const r = computeNeighbourShrink(A, 'end', b, 500, 0);
    expect(r.selfGrowMm).toBe(500);
    expect(r.neighbour).toEqual({ id: 'b', newLengthMm: 500, newOriginX: 2000, newOriginY: 0 });
  });

  it('pushes on the start face too', () => {
    const a = body({ id: 'a', originX: 1000, originY: 0, lengthMm: 1000 });
    const b = body({ id: 'b', originX: 0, originY: 0, lengthMm: 1000 });
    const r = computeNeighbourShrink(a, 'start', b, 300, 0);
    expect(r.selfGrowMm).toBe(300);
    expect(r.neighbour).toEqual({ id: 'b', newLengthMm: 700, newOriginX: 0, newOriginY: 0 });
  });

  it('does not push a non-parallel neighbour', () => {
    const b = body({ id: 'b', originX: 1000, originY: 0, rotationDeg: 90, lengthMm: 1000 });
    const r = computeNeighbourShrink(A, 'end', b, 500, 0);
    expect(r.selfGrowMm).toBe(0);
    expect(r.neighbour).toBeUndefined();
  });

  it('does not push a laterally-offset (non-collinear) neighbour', () => {
    const b = body({ id: 'b', originX: 1000, originY: 200, lengthMm: 1000 });
    const r = computeNeighbourShrink(A, 'end', b, 500, 0);
    expect(r.selfGrowMm).toBe(0);
    expect(r.neighbour).toBeUndefined();
  });

  it('returns the desired grow untouched when collision did not block', () => {
    const b = body({ id: 'b', originX: 1000, originY: 0, lengthMm: 1000 });
    const r = computeNeighbourShrink(A, 'end', b, 500, 500);
    expect(r.selfGrowMm).toBe(500);
    expect(r.neighbour).toBeUndefined();
  });
});
