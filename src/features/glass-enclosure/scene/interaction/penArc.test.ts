import { describe, expect, it } from 'vitest';
import { arcMetricsFromBulge, chordBulgeMm, tessellateArc } from './penArc';

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

describe('arcMetricsFromBulge (live readout radius + sweep)', () => {
  const a = { x: 0, y: 0 };
  const b = { x: 1000, y: 0 };

  it('returns zero for a negligible bulge', () => {
    expect(arcMetricsFromBulge(a, b, 2)).toEqual({ radiusMm: 0, angleDeg: 0 });
  });

  it('reports a minor arc (< 180°) for a shallow bulge', () => {
    const { radiusMm, angleDeg } = arcMetricsFromBulge(a, b, 200);
    expect(radiusMm).toBeGreaterThan(500); // radius exceeds half the chord
    expect(angleDeg).toBeGreaterThan(0);
    expect(angleDeg).toBeLessThan(180);
  });

  it('reports ~180° near a semicircle (bulge ≈ radius ≈ chord/2)', () => {
    const { radiusMm, angleDeg } = arcMetricsFromBulge(a, b, 500);
    expect(radiusMm).toBeCloseTo(500, 0);
    expect(angleDeg).toBeGreaterThan(175);
    expect(angleDeg).toBeLessThan(185);
  });

  it('reports a major arc (> 180°) once the sagitta passes the radius', () => {
    const { angleDeg } = arcMetricsFromBulge(a, b, 800);
    expect(angleDeg).toBeGreaterThan(180);
    expect(angleDeg).toBeLessThanOrEqual(360);
  });

  it('uses the absolute bulge so direction does not change the metrics', () => {
    expect(arcMetricsFromBulge(a, b, -300)).toEqual(arcMetricsFromBulge(a, b, 300));
  });
});
