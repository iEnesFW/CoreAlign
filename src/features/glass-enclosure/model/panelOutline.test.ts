import { describe, expect, it } from 'vitest';
import { panelIsShaped, panelOutlinePointsMm } from './panelOutline';

describe('panelOutlinePointsMm', () => {
  it('a flat head is a plain bottom-centred rectangle', () => {
    const pts = panelOutlinePointsMm({ widthMm: 1000, heightMm: 2000 });
    expect(pts).toEqual([
      { x: -500, y: 0 },
      { x: 500, y: 0 },
      { x: 500, y: 2000 },
      { x: -500, y: 2000 },
    ]);
  });

  it('a raked head makes a trapezoid (different left/right top heights)', () => {
    const pts = panelOutlinePointsMm({
      widthMm: 1000,
      heightMm: 2000,
      topShape: 'raked',
      topRightHeightMm: 1500,
    });
    expect(pts).toEqual([
      { x: -500, y: 0 },
      { x: 500, y: 0 },
      { x: 500, y: 1500 },
      { x: -500, y: 2000 },
    ]);
  });

  it('an arched head samples a crown rising above the head line', () => {
    const pts = panelOutlinePointsMm({
      widthMm: 1000,
      heightMm: 2000,
      topShape: 'arched',
      archRiseMm: 300,
    });
    expect(pts.length).toBeGreaterThan(4);
    const maxY = Math.max(...pts.map((p) => p.y));
    expect(maxY).toBeCloseTo(2300, 0);
    expect(pts[0]).toEqual({ x: -500, y: 0 });
    expect(pts[1]).toEqual({ x: 500, y: 0 });
  });

  it('arched with zero rise degenerates to a rectangle', () => {
    const pts = panelOutlinePointsMm({
      widthMm: 800,
      heightMm: 1800,
      topShape: 'arched',
      archRiseMm: 0,
    });
    expect(pts).toEqual([
      { x: -400, y: 0 },
      { x: 400, y: 0 },
      { x: 400, y: 1800 },
      { x: -400, y: 1800 },
    ]);
  });

  it('scales a polygon to fill the panel cell (tracks resize, no stale gap)', () => {
    const pts = panelOutlinePointsMm({
      widthMm: 1000,
      heightMm: 2000,
      shapeKind: 'polygon',
      // a small triangle authored in a 200×200 box
      points: [
        { x: -100, y: 0 },
        { x: 100, y: 0 },
        { x: 0, y: 200 },
      ],
    });
    expect(pts).toEqual([
      { x: -500, y: 0 },
      { x: 500, y: 0 },
      { x: 0, y: 2000 },
    ]);
  });

  it('an ellipse samples a closed oval bounded by width x height', () => {
    const pts = panelOutlinePointsMm({ widthMm: 1000, heightMm: 2000, shapeKind: 'ellipse' });
    expect(pts.length).toBeGreaterThan(8);
    const xs = pts.map((p) => p.x);
    const ys = pts.map((p) => p.y);
    expect(Math.max(...xs)).toBeCloseTo(500, 0);
    expect(Math.min(...xs)).toBeCloseTo(-500, 0);
    expect(Math.max(...ys)).toBeCloseTo(2000, 0);
    expect(Math.min(...ys)).toBeCloseTo(0, 0);
  });

  it('rounds the corners of a plain rect into arcs that stay in the cell', () => {
    const pts = panelOutlinePointsMm({
      widthMm: 1000,
      heightMm: 2000,
      cornerRadiiMm: { tl: 100, tr: 100, br: 100, bl: 100 },
    });
    expect(pts.length).toBeGreaterThan(4);
    expect(Math.max(...pts.map((p) => p.x))).toBeLessThanOrEqual(500.001);
    expect(Math.min(...pts.map((p) => p.x))).toBeGreaterThanOrEqual(-500.001);
    expect(Math.max(...pts.map((p) => p.y))).toBeLessThanOrEqual(2000.001);
    expect(Math.min(...pts.map((p) => p.y))).toBeGreaterThanOrEqual(-0.001);
    // the sharp corner vertices are replaced by fillet arcs
    expect(pts.some((p) => p.x === -500 && p.y === 0)).toBe(false);
    expect(pts.some((p) => p.x === 500 && p.y === 2000)).toBe(false);
  });

  it('a rect with no / zero radii stays a sharp 4-point rectangle', () => {
    expect(panelOutlinePointsMm({ widthMm: 1000, heightMm: 2000, cornerRadiiMm: {} })).toHaveLength(
      4,
    );
    expect(
      panelOutlinePointsMm({
        widthMm: 1000,
        heightMm: 2000,
        cornerRadiiMm: { tl: 0, tr: 0, br: 0, bl: 0 },
      }),
    ).toHaveLength(4);
  });

  it('clamps an oversized corner radius to the half-dimension (no overflow)', () => {
    const pts = panelOutlinePointsMm({
      widthMm: 1000,
      heightMm: 2000,
      cornerRadiiMm: { tl: 5000, tr: 5000, br: 5000, bl: 5000 },
    });
    expect(pts.length).toBeGreaterThan(4);
    expect(Math.max(...pts.map((p) => p.x))).toBeLessThanOrEqual(500.001);
    expect(Math.min(...pts.map((p) => p.x))).toBeGreaterThanOrEqual(-500.001);
  });

  it('an arched head ignores corner radii (the crown owns the top edge)', () => {
    const pts = panelOutlinePointsMm({
      widthMm: 1000,
      heightMm: 2000,
      topShape: 'arched',
      archRiseMm: 300,
      cornerRadiiMm: { tl: 100, tr: 100, br: 100, bl: 100 },
    });
    expect(pts[0]).toEqual({ x: -500, y: 0 });
    expect(pts[1]).toEqual({ x: 500, y: 0 });
  });
});

describe('panelIsShaped', () => {
  it('plain rectangle is not shaped', () => {
    expect(panelIsShaped({})).toBe(false);
    expect(panelIsShaped({ topShape: 'flat' })).toBe(false);
    expect(panelIsShaped({ topShape: 'arched', archRiseMm: 0 })).toBe(false);
  });

  it('raked / arched-with-rise / any fillet are shaped', () => {
    expect(panelIsShaped({ topShape: 'raked' })).toBe(true);
    expect(panelIsShaped({ topShape: 'arched', archRiseMm: 200 })).toBe(true);
    expect(panelIsShaped({ cornerRadiiMm: { tl: 50 } })).toBe(true);
  });
});
