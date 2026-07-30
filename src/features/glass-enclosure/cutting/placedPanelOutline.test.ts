import { describe, expect, it } from 'vitest';
import {
  panelShapeToken,
  placedPanelPolygonMm,
  placedPanelPolygonPoints,
  type PlacedPanelLike,
} from './placedPanelOutline';
import type { PanelCutShapeDto } from '../model/engineering.types';

const rakedShape: PanelCutShapeDto = {
  topShape: 'raked',
  nominalHeightMm: 2000,
  topRightHeightMm: 2200,
  archRiseMm: null,
  cornerRadiusTlMm: null,
  cornerRadiusTrMm: null,
  cornerRadiusBrMm: null,
  cornerRadiusBlMm: null,
  netAreaMm2: 2_100_000,
};

const archedShape: PanelCutShapeDto = {
  topShape: 'arched',
  nominalHeightMm: 2000,
  topRightHeightMm: null,
  archRiseMm: 120,
  cornerRadiusTlMm: null,
  cornerRadiusTrMm: null,
  cornerRadiusBrMm: null,
  cornerRadiusBlMm: null,
  netAreaMm2: 2_076_394,
};

const roundedShape: PanelCutShapeDto = {
  topShape: 'flat',
  nominalHeightMm: 2000,
  topRightHeightMm: null,
  archRiseMm: null,
  cornerRadiusTlMm: 100,
  cornerRadiusTrMm: 100,
  cornerRadiusBrMm: 100,
  cornerRadiusBlMm: 100,
  netAreaMm2: 1_991_416,
};

const ellipseShape: PanelCutShapeDto = {
  topShape: null,
  nominalHeightMm: 2000,
  topRightHeightMm: null,
  archRiseMm: null,
  cornerRadiusTlMm: null,
  cornerRadiusTrMm: null,
  cornerRadiusBrMm: null,
  cornerRadiusBlMm: null,
  netAreaMm2: 1_570_796,
  shapeKind: 'ellipse',
};

const polygonShape: PanelCutShapeDto = {
  topShape: null,
  nominalHeightMm: 2000,
  topRightHeightMm: null,
  archRiseMm: null,
  cornerRadiusTlMm: null,
  cornerRadiusTrMm: null,
  cornerRadiusBrMm: null,
  cornerRadiusBlMm: null,
  netAreaMm2: 1_000_000,
  shapeKind: 'polygon',
  shapePointsJson: '[{"x":-500,"y":0},{"x":500,"y":0},{"x":0,"y":2000}]',
};

describe('panelShapeToken', () => {
  it('returns null for a plain rectangle', () => {
    expect(panelShapeToken(null)).toBeNull();
    expect(panelShapeToken(undefined)).toBeNull();
    expect(panelShapeToken({ ...rakedShape, topShape: 'flat', topRightHeightMm: null })).toBeNull();
    expect(panelShapeToken({ ...archedShape, archRiseMm: 0 })).toBeNull();
  });

  it('classifies raked, arched, rounded, ellipse and polygon', () => {
    expect(panelShapeToken(rakedShape)).toBe('raked');
    expect(panelShapeToken(archedShape)).toBe('arched');
    expect(panelShapeToken(roundedShape)).toBe('rounded');
    expect(panelShapeToken(ellipseShape)).toBe('ellipse');
    expect(panelShapeToken(polygonShape)).toBe('polygon');
    expect(panelShapeToken({ ...polygonShape, shapePointsJson: 'bad' })).toBeNull();
  });
});

describe('placedPanelPolygonPoints', () => {
  it('returns null for a rectangle so the caller can draw a fast <rect>', () => {
    const rect: PlacedPanelLike = { x: 0, y: 0, widthMm: 1000, heightMm: 2000, rotated: false };
    expect(placedPanelPolygonPoints(rect)).toBeNull();
  });

  it('maps a raked silhouette into sheet coordinates (y-down)', () => {
    const placed: PlacedPanelLike = {
      x: 0,
      y: 0,
      widthMm: 1000,
      heightMm: 2200,
      rotated: false,
      shape: rakedShape,
    };
    // bottom edge at y=2200, tall right corner at y=0, short left corner at y=200
    expect(placedPanelPolygonPoints(placed)).toBe('0,2200 1000,2200 1000,0 0,200');
  });

  it('rotates the silhouette 90° into the placed box', () => {
    const placed: PlacedPanelLike = {
      x: 0,
      y: 0,
      widthMm: 2200,
      heightMm: 1000,
      rotated: true,
      shape: rakedShape,
    };
    expect(placedPanelPolygonPoints(placed)).toBe('0,0 0,1000 2200,1000 2000,0');
  });

  it('samples extra points for an arched crown', () => {
    const placed: PlacedPanelLike = {
      x: 0,
      y: 0,
      widthMm: 1000,
      heightMm: 2120,
      rotated: false,
      shape: archedShape,
    };
    const points = placedPanelPolygonPoints(placed);
    expect(points).not.toBeNull();
    expect(points!.split(' ').length).toBeGreaterThan(4);
  });

  it('draws a rounded-only panel as a plain rect (token still rounded)', () => {
    const placed: PlacedPanelLike = {
      x: 0,
      y: 0,
      widthMm: 1000,
      heightMm: 2000,
      rotated: false,
      shape: roundedShape,
    };
    expect(placedPanelPolygonPoints(placed)).toBeNull();
    expect(panelShapeToken(roundedShape)).toBe('rounded');
  });

  it('draws an ellipse silhouette polygon', () => {
    const placed: PlacedPanelLike = {
      x: 0,
      y: 0,
      widthMm: 1000,
      heightMm: 2000,
      rotated: false,
      shape: ellipseShape,
    };
    const points = placedPanelPolygonPoints(placed);
    expect(points).not.toBeNull();
    expect(points!.split(' ').length).toBeGreaterThan(8);
  });

  it('draws a free polygon silhouette from its points', () => {
    const placed: PlacedPanelLike = {
      x: 0,
      y: 0,
      widthMm: 1000,
      heightMm: 2000,
      rotated: false,
      shape: polygonShape,
    };
    // triangle (-500,0),(500,0),(0,2000) → sheet y-down: (0,2000),(1000,2000),(500,0)
    expect(placedPanelPolygonPoints(placed)).toBe('0,2000 1000,2000 500,0');
  });
});

/**
 * The DXF export was the one consumer that still cut the blank RECTANGLE while the viewer drew the
 * true silhouette from this helper — so a raked/arched/elliptical/polygon panel was mis-cut and its
 * offcut wasted. The export now reads the numeric variant below.
 */
describe('placedPanelPolygonMm — the numeric source the DXF export cuts from', () => {
  it('returns the same points the SVG string carries', () => {
    const placed: PlacedPanelLike = {
      x: 0,
      y: 0,
      widthMm: 1000,
      heightMm: 2200,
      rotated: false,
      shape: rakedShape,
    };
    expect(placedPanelPolygonMm(placed)).toEqual([
      { x: 0, y: 2200 },
      { x: 1000, y: 2200 },
      { x: 1000, y: 0 },
      { x: 0, y: 200 },
    ]);
  });

  it('a shaped panel is NOT its blank rectangle', () => {
    const placed: PlacedPanelLike = {
      x: 0,
      y: 0,
      widthMm: 1000,
      heightMm: 2200,
      rotated: false,
      shape: rakedShape,
    };
    const outline = placedPanelPolygonMm(placed);
    expect(outline).toHaveLength(4);
    // The top edge is raked: its two corners sit at DIFFERENT heights, which a rect can't express.
    expect(outline?.[2].y).not.toBe(outline?.[3].y);
  });

  it('stays null for a plain rectangle so the export keeps its rect fallback', () => {
    const rect: PlacedPanelLike = { x: 0, y: 0, widthMm: 1000, heightMm: 2000, rotated: false };
    expect(placedPanelPolygonMm(rect)).toBeNull();
  });
});
