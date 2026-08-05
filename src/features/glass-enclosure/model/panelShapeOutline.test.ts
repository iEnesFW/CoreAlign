import { describe, expect, it } from 'vitest';
import { normalizePanelOutline, normalizePanelOutlineJson } from './panelShapeOutline';
import {
  panelPolygonAreaMm2,
  presetPolygonPoints,
  serializePanelPolygonPoints,
} from './panelPolygon';
import { polygonSignedAreaMm2 } from './polygonValidation';

/**
 * The shaped-pane path had NO validation, while the wall/slab free-draw already refused a crossing
 * stroke. That asymmetry is not cosmetic: a bowtie makes earcut produce non-manifold glass AND its
 * shoelace lobes cancel, so the area the BOM prices, the summary weighs and the cut list orders is
 * simply wrong. These lock the gate every producer now passes through.
 */

const W = 1000;
const H = 2000;

describe('normalizePanelOutline', () => {
  it('accepts a plain rectangle and returns it counter-clockwise', () => {
    const cw = [
      { x: -400, y: 100 },
      { x: -400, y: 1900 },
      { x: 400, y: 1900 },
      { x: 400, y: 100 },
    ];
    const result = normalizePanelOutline(cw, W, H);
    expect(result.rejection).toBeNull();
    expect(result.points).toHaveLength(4);
    // One agreed winding, so earcut / DXF / nesting cannot each infer a different one.
    expect(polygonSignedAreaMm2(result.points!)).toBeGreaterThan(0);
  });

  it('leaves an already counter-clockwise contour in its original order', () => {
    const ccw = [
      { x: -400, y: 100 },
      { x: 400, y: 100 },
      { x: 400, y: 1900 },
      { x: -400, y: 1900 },
    ];
    expect(normalizePanelOutline(ccw, W, H).points).toEqual(ccw);
  });

  it('REFUSES a bowtie — the shape that silently under-reports its own area', () => {
    const bowtie = [
      { x: -400, y: 100 },
      { x: 400, y: 1900 },
      { x: 400, y: 100 },
      { x: -400, y: 1900 },
    ];
    // Proof the defect is a money defect and not a rendering one: the lobes cancel.
    expect(panelPolygonAreaMm2(bowtie)).toBeLessThan(
      panelPolygonAreaMm2([
        { x: -400, y: 100 },
        { x: 400, y: 100 },
        { x: 400, y: 1900 },
        { x: -400, y: 1900 },
      ]),
    );
    expect(normalizePanelOutline(bowtie, W, H).rejection).toBe('selfIntersecting');
  });

  it('REFUSES a contour that passes through one of its own vertices', () => {
    const pinched = [
      { x: -400, y: 0 },
      { x: 400, y: 0 },
      { x: 0, y: 1000 },
      { x: 400, y: 2000 },
      { x: -400, y: 2000 },
      { x: 0, y: 1000 },
    ];
    expect(normalizePanelOutline(pinched, W, H).rejection).toBe('selfIntersecting');
  });

  it('clamps into the pane box instead of letting glass hang outside it', () => {
    const overflowing = [
      { x: -9000, y: -500 },
      { x: 9000, y: -500 },
      { x: 9000, y: 9000 },
      { x: -9000, y: 9000 },
    ];
    const result = normalizePanelOutline(overflowing, W, H);
    expect(result.rejection).toBeNull();
    for (const p of result.points!) {
      expect(Math.abs(p.x)).toBeLessThanOrEqual(W / 2);
      expect(p.y).toBeGreaterThanOrEqual(0);
      expect(p.y).toBeLessThanOrEqual(H);
    }
  });

  it('drops duplicate clicks and the repeated closing vertex', () => {
    const sloppy = [
      { x: -400, y: 100 },
      { x: -400, y: 100.4 },
      { x: 400, y: 100 },
      { x: 400, y: 1900 },
      { x: -400, y: 100 },
    ];
    const result = normalizePanelOutline(sloppy, W, H);
    expect(result.rejection).toBeNull();
    expect(result.points).toHaveLength(3);
  });

  it('refuses a shape that collapses to fewer than three corners', () => {
    expect(normalizePanelOutline([{ x: 0, y: 0 }], W, H).rejection).toBe('tooFewPoints');
    expect(
      normalizePanelOutline(
        [
          { x: 0, y: 0 },
          { x: 0, y: 0.2 },
          { x: 0, y: 0.4 },
        ],
        W,
        H,
      ).rejection,
    ).toBe('tooFewPoints');
  });

  it('refuses a sliver the cutter cannot make', () => {
    const sliver = [
      { x: -400, y: 100 },
      { x: 400, y: 100 },
      { x: 400, y: 101 },
    ];
    expect(normalizePanelOutline(sliver, W, H).rejection).toBe('degenerate');
  });

  it('drops non-finite coordinates rather than trusting them', () => {
    const poisoned = [
      { x: -400, y: 100 },
      { x: Number.NaN, y: 500 },
      { x: 400, y: 100 },
      { x: 400, y: 1900 },
    ];
    const result = normalizePanelOutline(poisoned, W, H);
    expect(result.rejection).toBeNull();
    expect(result.points).toHaveLength(3);
  });

  it('accepts every built-in preset unchanged in kind', () => {
    for (const sides of [3, 5, 6]) {
      const result = normalizePanelOutline(presetPolygonPoints(sides, W, H), W, H);
      expect(result.rejection).toBeNull();
      expect(result.points).toHaveLength(sides);
    }
  });

  it('is idempotent — re-normalising an approved outline changes nothing', () => {
    const once = normalizePanelOutline(presetPolygonPoints(6, W, H), W, H).points!;
    expect(normalizePanelOutline(once, W, H).points).toEqual(once);
  });
});

describe('normalizePanelOutlineJson', () => {
  it('round-trips an approved outline', () => {
    const json = serializePanelPolygonPoints(presetPolygonPoints(5, W, H));
    const result = normalizePanelOutlineJson(json, W, H);
    expect(result.rejection).toBeNull();
    expect(JSON.parse(result.json!)).toHaveLength(5);
  });

  it('reports unreadable payloads instead of throwing', () => {
    expect(normalizePanelOutlineJson('not json', W, H).rejection).toBe('unparsable');
    expect(normalizePanelOutlineJson(null, W, H).rejection).toBe('unparsable');
    expect(normalizePanelOutlineJson('{}', W, H).rejection).toBe('unparsable');
  });

  it('returns no json for a refused shape so the caller keeps the old one', () => {
    const bowtie = serializePanelPolygonPoints([
      { x: -400, y: 100 },
      { x: 400, y: 1900 },
      { x: 400, y: 100 },
      { x: -400, y: 1900 },
    ]);
    const result = normalizePanelOutlineJson(bowtie, W, H);
    expect(result.json).toBeNull();
    expect(result.rejection).toBe('selfIntersecting');
  });
});
