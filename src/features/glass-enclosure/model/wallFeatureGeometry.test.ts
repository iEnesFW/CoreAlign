import { describe, expect, it } from 'vitest';
import { formatDraftDimensionMm } from './wallFeatureGeometry';

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
