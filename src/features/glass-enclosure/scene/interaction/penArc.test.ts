import { describe, expect, it } from 'vitest';
import { chordBulgeMm, tessellateArc } from './penArc';

const apexOf = (pts: { x: number; y: number }[]) => pts[Math.floor(pts.length / 2)];

describe('tessellateArc', () => {
  const a = { x: 0, y: 0 };
  const b = { x: 1000, y: 0 };

  it('keeps a minor-arc apex on the bulge side at the sagitta distance', () => {
    const pts = tessellateArc(a, b, 300);
    expect(apexOf(pts).y).toBeGreaterThan(250);
    expect(apexOf(pts).y).toBeLessThan(350);
  });

  it('keeps a semicircle apex on the bulge side (no flip at chord/2)', () => {
    const pts = tessellateArc(a, b, 500);
    expect(apexOf(pts).y).toBeGreaterThan(450);
  });

  it('keeps a major-arc apex on the bulge side (sweeps the long way)', () => {
    const pts = tessellateArc(a, b, 600);
    expect(apexOf(pts).y).toBeGreaterThan(550);
  });

  it('mirrors the apex when the bulge is negative', () => {
    const pts = tessellateArc(a, b, -600);
    expect(apexOf(pts).y).toBeLessThan(-550);
  });

  it('ends exactly at B', () => {
    const pts = tessellateArc(a, b, 600);
    const end = pts[pts.length - 1];
    expect(Math.hypot(end.x - b.x, end.y - b.y)).toBeLessThan(1e-6);
  });

  it('returns just the endpoint for a negligible bulge', () => {
    expect(tessellateArc(a, b, 2)).toEqual([b]);
  });

  it('measures bulge as signed perpendicular distance from the chord', () => {
    expect(chordBulgeMm(a, b, { x: 500, y: 400 })).toBeCloseTo(400, 6);
    expect(chordBulgeMm(a, b, { x: 500, y: -400 })).toBeCloseTo(-400, 6);
  });
});
