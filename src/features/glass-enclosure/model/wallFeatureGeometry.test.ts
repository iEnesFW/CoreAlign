import { describe, expect, it } from 'vitest';
import {
  formatDraftDimensionMm,
  outlineSelfIntersects,
  sanitizeFreeOutline,
  simplifyFreePoints,
} from './wallFeatureGeometry';

describe('formatDraftDimensionMm (live draw readout)', () => {
  it('shows width × height for a rectangle', () => {
    expect(formatDraftDimensionMm({ shape: 'rect', widthMm: 600, heightMm: 400 })).toBe(
      '600 × 400 mm',
    );
  });

  it('shows a diameter for a circle (square bbox)', () => {
    expect(formatDraftDimensionMm({ shape: 'circle', widthMm: 500, heightMm: 500 })).toBe(
      '⌀ 500 mm',
    );
  });

  it('uses the larger side as the circle diameter', () => {
    expect(formatDraftDimensionMm({ shape: 'circle', widthMm: 480, heightMm: 500 })).toBe(
      '⌀ 500 mm',
    );
  });

  it('rounds fractional millimetres', () => {
    expect(formatDraftDimensionMm({ shape: 'rect', widthMm: 600.7, heightMm: 399.4 })).toBe(
      '601 × 399 mm',
    );
  });

  it('shows width × height for a free / polygon shape (bounding box)', () => {
    expect(formatDraftDimensionMm({ shape: 'free', widthMm: 123, heightMm: 456 })).toBe(
      '123 × 456 mm',
    );
  });
});

describe('simplifyFreePoints (RDP — freehand stroke thinning)', () => {
  it('collapses a near-collinear stroke to its endpoints', () => {
    const stroke = [
      { x: 0, z: 0 },
      { x: 25, z: 1 },
      { x: 50, z: 0 },
      { x: 75, z: 1 },
      { x: 100, z: 0 },
    ];
    expect(simplifyFreePoints(stroke, 5)).toEqual([
      { x: 0, z: 0 },
      { x: 100, z: 0 },
    ]);
  });

  it('keeps a vertex that deviates beyond the tolerance', () => {
    const stroke = [
      { x: 0, z: 0 },
      { x: 50, z: 60 },
      { x: 100, z: 0 },
      { x: 150, z: 0 },
      { x: 200, z: 0 },
    ];
    const out = simplifyFreePoints(stroke, 5);
    expect(out).toContainEqual({ x: 50, z: 60 });
    expect(out.length).toBeLessThan(stroke.length);
  });

  it('returns a short stroke (<= 3 points) unchanged', () => {
    const stroke = [
      { x: 0, z: 0 },
      { x: 10, z: 10 },
      { x: 20, z: 0 },
    ];
    expect(simplifyFreePoints(stroke, 5)).toBe(stroke);
  });
});

describe('outlineSelfIntersects', () => {
  it('is false for a simple square', () => {
    expect(
      outlineSelfIntersects([
        { x: 0, z: 0 },
        { x: 100, z: 0 },
        { x: 100, z: 100 },
        { x: 0, z: 100 },
      ]),
    ).toBe(false);
  });

  it('is true for a bowtie', () => {
    expect(
      outlineSelfIntersects([
        { x: 0, z: 0 },
        { x: 100, z: 100 },
        { x: 100, z: 0 },
        { x: 0, z: 100 },
      ]),
    ).toBe(true);
  });

  it('is false for fewer than four points', () => {
    expect(
      outlineSelfIntersects([
        { x: 0, z: 0 },
        { x: 100, z: 0 },
        { x: 50, z: 100 },
      ]),
    ).toBe(false);
  });
});

describe('sanitizeFreeOutline (freehand close-loop repair)', () => {
  it('returns an already-simple polygon unchanged', () => {
    const poly = [
      { x: 0, z: 0 },
      { x: 100, z: 0 },
      { x: 100, z: 100 },
      { x: 0, z: 100 },
    ];
    expect(sanitizeFreeOutline(poly)).toBe(poly);
  });

  it('trims a self-crossing tail hook back to a simple loop', () => {
    const poly = [
      { x: 0, z: 0 },
      { x: 100, z: 0 },
      { x: 100, z: 100 },
      { x: 0, z: 100 },
      { x: 50, z: -50 },
    ];
    const out = sanitizeFreeOutline(poly);
    expect(out).not.toBeNull();
    expect(outlineSelfIntersects(out!)).toBe(false);
  });
});
