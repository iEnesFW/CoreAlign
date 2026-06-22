import { describe, expect, it } from 'vitest';
import { computeOpeningEdges, panelCountForWidth, suggestedPanelCount } from './wallAutofill';
import type { SceneWallState } from './project.types';

const wallWithOpening = (geomZ: number, sillMm: number): SceneWallState => ({
  id: 'w',
  originX: 0,
  originY: 0,
  lengthMm: 3000,
  rotationDeg: 0,
  heightMm: 2600,
  heightEndMm: null,
  thicknessMm: 200,
  colorHex: null,
  geomZ,
  openings: [{ id: 'o', kind: 'window', offsetMm: 1500, widthMm: 1000, sillMm, heightMm: 1500 }],
  features: [],
});

describe('panelCountForWidth (catalog-aware)', () => {
  it('uses the fewest panels that stay within the max panel width', () => {
    expect(panelCountForWidth(3600, 1200)).toBe(3);
    expect(panelCountForWidth(5000, 1000)).toBe(5);
    expect(panelCountForWidth(1250, 1200)).toBe(2);
  });

  it('keeps a single panel when the gap is within the max', () => {
    expect(panelCountForWidth(800, 1200)).toBe(1);
    expect(panelCountForWidth(100, 1200)).toBe(1);
  });

  it('falls back to the ~600mm target when the system has no max', () => {
    expect(panelCountForWidth(1800, undefined)).toBe(suggestedPanelCount(1800));
    expect(panelCountForWidth(1800, 0)).toBe(3);
  });

  it('honours the catalog max up to the 50-panel server ceiling', () => {
    expect(panelCountForWidth(50000, 1000)).toBe(50);
    expect(panelCountForWidth(80000, 1000)).toBe(50);
  });

  it('clamps the no-max fallback to 20 panels', () => {
    expect(panelCountForWidth(50000)).toBe(20);
  });
});

describe('computeOpeningEdges (raised-wall aware)', () => {
  it('adds the wall base elevation to the opening sill for the fill panel', () => {
    const edges = computeOpeningEdges([wallWithOpening(1000, 800)]);
    expect(edges).toHaveLength(1);
    expect(edges[0].geomZ).toBe(1800);
    expect(edges[0].heightMm).toBe(1500);
  });

  it('keeps the sill as-is when the wall sits on the ground', () => {
    const edges = computeOpeningEdges([wallWithOpening(0, 800)]);
    expect(edges).toHaveLength(1);
    expect(edges[0].geomZ).toBe(800);
  });
});
