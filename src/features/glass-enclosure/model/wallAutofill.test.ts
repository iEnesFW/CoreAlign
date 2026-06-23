import { describe, expect, it } from 'vitest';
import { computeOpeningEdges, panelCountForWidth, suggestedPanelCount } from './wallAutofill';
import type { SceneRunState, SceneWallState } from './project.types';

const fillRun = (
  originX: number,
  lengthMm: number,
  geomZ: number,
  heightMm: number,
): SceneRunState => ({
  id: 'r',
  orderIndex: 0,
  label: 'r',
  lengthMm,
  heightMm,
  originX,
  originY: 0,
  rotationDeg: 0,
  profileSystemId: 'ps',
  colorId: null,
  hasTopDrip: true,
  hasBottomThreshold: false,
  geomZ,
  panels: [],
});

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

  it('skips an opening that an existing glass run already covers (idempotent re-fill)', () => {
    const wall = wallWithOpening(0, 800);
    // The opening edge sits at originX≈1000, length 1000, geomZ 800, height 1500.
    const covering = fillRun(1000, 1000, 800, 1500);
    expect(computeOpeningEdges([wall], [covering])).toHaveLength(0);
    expect(computeOpeningEdges([wall], [])).toHaveLength(1);
  });

  it('still fills when an existing run is at a different elevation (not the same opening)', () => {
    const wall = wallWithOpening(0, 800);
    const elsewhere = fillRun(1000, 1000, 5000, 1500);
    expect(computeOpeningEdges([wall], [elsewhere])).toHaveLength(1);
  });

  it('glazes a shaped (ellipse) wall hole with an ellipse panel, not a rectangle', () => {
    const wall: SceneWallState = {
      ...wallWithOpening(0, 800),
      openings: [],
      features: [
        {
          id: 'f',
          shape: 'ellipse',
          mode: 'hole',
          side: 1,
          offsetMm: 1500,
          centerZMm: 1200,
          widthMm: 900,
          heightMm: 1400,
          depthMm: 0,
          points: undefined,
          colorHex: null,
        },
      ],
    };
    const edges = computeOpeningEdges([wall]);
    expect(edges).toHaveLength(1);
    expect(edges[0].shapeKind).toBe('ellipse');
    expect(edges[0].shapePointsJson ?? null).toBeNull();
  });

  it('glazes a free-drawn wall hole with a polygon panel whose points fill the hole bounds', () => {
    const wall: SceneWallState = {
      ...wallWithOpening(0, 800),
      openings: [],
      features: [
        {
          id: 'f',
          shape: 'free',
          mode: 'hole',
          side: 1,
          offsetMm: 1500,
          centerZMm: 1200,
          widthMm: 800,
          heightMm: 1000,
          depthMm: 0,
          // a triangle in feature-local coords (+z up), spanning the bounds
          points: [
            { x: -400, z: -500 },
            { x: 400, z: -500 },
            { x: 0, z: 500 },
          ],
          colorHex: null,
        },
      ],
    };
    const edges = computeOpeningEdges([wall]);
    expect(edges).toHaveLength(1);
    expect(edges[0].shapeKind).toBe('polygon');
    const pts = JSON.parse(edges[0].shapePointsJson ?? '[]') as { x: number; y: number }[];
    // bottom-centred, y-up: apex at top-centre (y≈height), base at y≈0
    expect(pts).toContainEqual({ x: 0, y: 1000 });
    expect(pts.some((p) => p.y === 0)).toBe(true);
  });
});
