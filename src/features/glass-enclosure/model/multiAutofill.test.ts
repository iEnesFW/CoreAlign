import { describe, expect, it } from 'vitest';
import { computeMultiWallGapRuns } from './multiAutofill';
import type { SceneRunState, SceneWallState } from './project.types';

const wall = (
  id: string,
  originX: number,
  originY: number,
  lengthMm: number,
  rotationDeg: number,
  heightMm = 2600,
  thicknessMm = 200,
): SceneWallState => ({
  id,
  originX,
  originY,
  lengthMm,
  rotationDeg,
  heightMm,
  heightEndMm: null,
  thicknessMm,
  colorHex: null,
  openings: [],
  features: [],
});

const run = (
  id: string,
  originX: number,
  originY: number,
  lengthMm: number,
  rotationDeg: number,
  heightMm = 2400,
): SceneRunState => ({
  id,
  orderIndex: 0,
  label: id,
  lengthMm,
  heightMm,
  originX,
  originY,
  rotationDeg,
  profileSystemId: 'ps',
  colorId: null,
  hasTopDrip: true,
  hasBottomThreshold: false,
  geomZ: 0,
  panels: [],
});

const endpointOf = (edge: {
  originX: number;
  originY: number;
  rotationDeg: number;
  lengthMm: number;
}) => {
  const rad = (edge.rotationDeg * Math.PI) / 180;
  return {
    x: edge.originX + edge.lengthMm * Math.cos(rad),
    y: edge.originY + edge.lengthMm * Math.sin(rad),
  };
};

describe('computeMultiWallGapRuns', () => {
  it('fills a collinear 1m gap with a single straight run between the gap-side endpoints', () => {
    const a = wall('a', 0, 0, 2000, 0);
    const b = wall('b', 3000, 0, 2000, 0);
    const edges = computeMultiWallGapRuns([a, b], [a, b], []);
    expect(edges).toHaveLength(1);
    const edge = edges[0];
    expect(edge.rotationDeg % 360).toBe(0);
    expect(edge.originX).toBeGreaterThanOrEqual(2000);
    expect(endpointOf(edge).x).toBeLessThanOrEqual(3000);
    expect(edge.lengthMm).toBeGreaterThanOrEqual(900);
    expect(edge.lengthMm).toBeLessThanOrEqual(1000);
    expect(edge.originY).toBe(0);
  });

  it('produces an L (two connected legs along each wall direction) for a perpendicular corner gap', () => {
    const a = wall('a', 0, 0, 2000, 0);
    const b = wall('b', 2500, 500, 2000, 90);
    const edges = computeMultiWallGapRuns([a, b], [a, b], []);
    expect(edges).toHaveLength(2);
    const group = edges[0].cornerGroup;
    expect(group).toBeDefined();
    expect(edges[1].cornerGroup).toBe(group);
    const rotations = edges.map((e) => Math.round(e.rotationDeg) % 180).sort((x, y) => x - y);
    expect(rotations).toEqual([0, 90]);
  });

  it('never returns an edge that would pass through an unselected wall in between', () => {
    const a = wall('a', 0, 0, 2000, 0);
    const b = wall('b', 6000, 0, 2000, 0);
    const blockerWall = wall('mid', 4000, -1000, 2000, 90);
    const edges = computeMultiWallGapRuns([a, b], [a, b, blockerWall], []);
    for (const edge of edges) {
      const end = endpointOf(edge);
      const crossesBlocker = edge.originX < 4000 && end.x > 4000;
      expect(crossesBlocker).toBe(false);
    }
  });

  it('does not duplicate runs when the gap already contains a glass run', () => {
    const a = wall('a', 0, 0, 2000, 0);
    const b = wall('b', 3000, 0, 2000, 0);
    const existing = run('r1', 2000, 0, 1000, 0);
    const edges = computeMultiWallGapRuns([a, b], [a, b], [existing]);
    expect(edges).toHaveLength(0);
  });

  it('trims a diagonal connector so it does not penetrate the destination wall body', () => {
    const a = wall('a', 0, 0, 2000, 0);
    const b = wall('b', 4000, 800, 2000, 90);
    const edges = computeMultiWallGapRuns([a, b], [a, b], []);
    expect(edges.length).toBeGreaterThanOrEqual(1);
    for (const edge of edges) {
      const end = endpointOf(edge);
      for (const point of [{ x: edge.originX, y: edge.originY }, end]) {
        const insideB = Math.abs(point.x - 4000) < 99 && point.y > 800 + 1 && point.y < 2800 - 1;
        const insideA = point.y > -99 && point.y < 99 && point.x > 1 && point.x < 1999;
        expect(insideB).toBe(false);
        expect(insideA).toBe(false);
      }
    }
  });

  it('uses the shorter of the two adjacent wall heights for the gap run', () => {
    const a = wall('a', 0, 0, 2000, 0, 3000);
    const b = wall('b', 3000, 0, 2000, 0, 2200);
    const edges = computeMultiWallGapRuns([a, b], [a, b], []);
    expect(edges).toHaveLength(1);
    expect(edges[0].heightMm).toBe(2200);
  });

  it('closes both open ends of two parallel offset walls (two runs)', () => {
    const a = wall('a', 0, 0, 2000, 0);
    const b = wall('b', 0, 2000, 2000, 0);
    const edges = computeMultiWallGapRuns([a, b], [a, b], []);
    expect(edges).toHaveLength(2);
    for (const edge of edges) {
      expect(edge.lengthMm).toBeGreaterThanOrEqual(1900);
      expect(edge.lengthMm).toBeLessThanOrEqual(2000);
    }
  });

  it('fills a short collinear gap (400mm) that used to fall in the dead zone', () => {
    const a = wall('a', 0, 0, 2000, 0);
    const b = wall('b', 2400, 0, 2000, 0);
    const edges = computeMultiWallGapRuns([a, b], [a, b], []);
    expect(edges).toHaveLength(1);
    expect(edges[0].lengthMm).toBeGreaterThanOrEqual(380);
    expect(edges[0].lengthMm).toBeLessThanOrEqual(400);
  });

  it('returns nothing for fewer than two walls', () => {
    const a = wall('a', 0, 0, 2000, 0);
    expect(computeMultiWallGapRuns([a], [a], [])).toHaveLength(0);
  });
});
