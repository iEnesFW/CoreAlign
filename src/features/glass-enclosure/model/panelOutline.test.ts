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
