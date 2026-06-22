import { describe, expect, it } from 'vitest';
import {
  panelPolygonAreaMm2,
  parsePanelPolygonPoints,
  presetPolygonPoints,
  serializePanelPolygonPoints,
} from './panelPolygon';

describe('parsePanelPolygonPoints', () => {
  it('parses a valid point array', () => {
    const pts = parsePanelPolygonPoints('[{"x":-500,"y":0},{"x":500,"y":0},{"x":0,"y":1000}]');
    expect(pts).toEqual([
      { x: -500, y: 0 },
      { x: 500, y: 0 },
      { x: 0, y: 1000 },
    ]);
  });

  it('returns null for invalid / degenerate input', () => {
    expect(parsePanelPolygonPoints(null)).toBeNull();
    expect(parsePanelPolygonPoints('')).toBeNull();
    expect(parsePanelPolygonPoints('not json')).toBeNull();
    expect(parsePanelPolygonPoints('{"x":1}')).toBeNull();
    expect(parsePanelPolygonPoints('[{"x":0,"y":0},{"x":1,"y":1}]')).toBeNull();
    expect(parsePanelPolygonPoints('[{"x":0,"y":"a"},{"x":1,"y":1},{"x":2,"y":2}]')).toBeNull();
  });
});

describe('panelPolygonAreaMm2', () => {
  it('computes the Shoelace area of a square', () => {
    const square = [
      { x: -500, y: 0 },
      { x: 500, y: 0 },
      { x: 500, y: 1000 },
      { x: -500, y: 1000 },
    ];
    expect(panelPolygonAreaMm2(square)).toBe(1_000_000);
  });

  it('is winding-independent (absolute)', () => {
    const tri = [
      { x: 0, y: 0 },
      { x: 1000, y: 0 },
      { x: 0, y: 1000 },
    ];
    const reversed = [...tri].reverse();
    expect(panelPolygonAreaMm2(tri)).toBe(500_000);
    expect(panelPolygonAreaMm2(reversed)).toBe(500_000);
  });
});

describe('presetPolygonPoints', () => {
  it('builds an N-gon inscribed in the width x height box', () => {
    const tri = presetPolygonPoints(3, 1000, 1000);
    expect(tri).toHaveLength(3);
    expect(tri[0]).toEqual({ x: 0, y: 1000 });
    for (const p of tri) {
      expect(p.x).toBeGreaterThanOrEqual(-500);
      expect(p.x).toBeLessThanOrEqual(500);
      expect(p.y).toBeGreaterThanOrEqual(0);
      expect(p.y).toBeLessThanOrEqual(1000);
    }
    expect(presetPolygonPoints(6, 800, 1200)).toHaveLength(6);
  });

  it('round-trips through serialize/parse', () => {
    const pts = presetPolygonPoints(5, 1000, 2000);
    expect(parsePanelPolygonPoints(serializePanelPolygonPoints(pts))).toEqual(pts);
  });
});
