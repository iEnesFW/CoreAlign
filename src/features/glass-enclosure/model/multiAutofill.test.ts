import { describe, expect, it } from 'vitest';
import { computeMultiWallGapRuns } from './multiAutofill';
import { arcEndLocal } from './arcGeometry';
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

  it('fills a perpendicular corner of THICK (cube) walls whose legs reach toward each other', () => {
    // 1m-thick perpendicular walls forming a near corner: the L legs run right up to the shared
    // corner, so each leg's trimming MUST exclude the partner wall — otherwise the partner's
    // footprint trims the legs to nothing and L mode silently produces no fill (the bug).
    const a = wall('a', 0, 0, 2000, 0, 2600, 1000);
    const b = wall('b', 2500, 500, 2000, 90, 2600, 1000);
    const edges = computeMultiWallGapRuns([a, b], [a, b], [], 'L');
    expect(edges.length).toBeGreaterThanOrEqual(1);
    const rotations = edges.map((e) => Math.round(e.rotationDeg) % 180).sort((x, y) => x - y);
    expect(rotations[0]).toBe(0); // at least one leg runs along the X wall axis
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

  it('closes both open ends of two parallel offset walls between their near faces (two runs)', () => {
    const a = wall('a', 0, 0, 2000, 0);
    const b = wall('b', 0, 2000, 2000, 0);
    const edges = computeMultiWallGapRuns([a, b], [a, b], []);
    expect(edges).toHaveLength(2);
    for (const edge of edges) {
      // Centrelines are 2000 apart; each 200mm-thick wall's near face is 100mm in, so the
      // infill bridges the 1800mm gap between the faces (nearest corners), not the centres.
      expect(edge.lengthMm).toBeGreaterThanOrEqual(1700);
      expect(edge.lengthMm).toBeLessThanOrEqual(1900);
    }
  });

  it('bridges the NEAREST corners of two thick (cube) walls, not their centrelines', () => {
    const a = wall('a', 0, 0, 2000, 0, 2600, 1000);
    const b = wall('b', 0, 3000, 2000, 0, 2600, 1000);
    const edges = computeMultiWallGapRuns([a, b], [a, b], []);
    expect(edges.length).toBeGreaterThanOrEqual(1);
    for (const edge of edges) {
      // Wall faces: a tops out at y=500, b starts at y=2500 → 2000mm face-to-face gap.
      // A centreline bridge would be 3000mm and would bury the glass 500mm into each wall.
      expect(edge.lengthMm).toBeLessThanOrEqual(2100);
      expect(Math.abs(edge.originY)).toBeGreaterThan(300);
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

  it("'straight' mode bridges a perpendicular corner with one direct run, not L legs", () => {
    const a = wall('a', 0, 0, 2000, 0);
    const b = wall('b', 2500, 500, 2000, 90);
    const edges = computeMultiWallGapRuns([a, b], [a, b], [], 'straight');
    expect(edges).toHaveLength(1);
    expect(edges[0].cornerGroup).toBeUndefined();
    expect(edges[0].geomArcRadiusMm ?? null).toBeNull();
  });

  it("'L' mode falls back to a straight connector when no corner is possible (collinear gap)", () => {
    // L now fills permissively: a real corner becomes L legs, a collinear gap becomes a single
    // straight connector, so the user never just gets the "no fillable gap" warning.
    const a = wall('a', 0, 0, 2000, 0);
    const b = wall('b', 3000, 0, 2000, 0);
    const edges = computeMultiWallGapRuns([a, b], [a, b], [], 'L');
    expect(edges).toHaveLength(1);
    expect(edges[0].geomArcRadiusMm ?? 0).toBe(0);
  });

  it("'arc' mode emits one bent run whose far end lands on the second gap endpoint", () => {
    const a = wall('a', 0, 0, 2000, 0);
    const b = wall('b', 2500, 500, 2000, 90);
    const edges = computeMultiWallGapRuns([a, b], [a, b], [], 'arc');
    expect(edges).toHaveLength(1);
    const edge = edges[0];
    expect(edge.geomArcRadiusMm ?? 0).toBeGreaterThan(0);
    expect(Math.abs(edge.geomArcSweepDeg ?? 0)).toBeGreaterThan(0);
    expect(edge.arcGlassBent).toBe(true);
    const local = arcEndLocal(edge.lengthMm, edge.geomArcRadiusMm ?? 0, edge.geomArcSweepDeg ?? 0);
    const rad = (edge.rotationDeg * Math.PI) / 180;
    const endX = edge.originX + local.xMm * Math.cos(rad) - local.yMm * Math.sin(rad);
    const endY = edge.originY + local.xMm * Math.sin(rad) + local.yMm * Math.cos(rad);
    // Endpoints are refined to the walls' near corners (slid along each end face toward the
    // other wall by thickness/2 = 100mm), so the arc spans those, not the centreline ends.
    const gapEnds = [
      { x: 2000, y: 100 },
      { x: 2400, y: 500 },
    ];
    const near = (px: number, py: number) =>
      gapEnds.some((g) => Math.hypot(g.x - px, g.y - py) < 20);
    expect(near(edge.originX, edge.originY)).toBe(true);
    expect(near(endX, endY)).toBe(true);
    expect(Math.hypot(endX - edge.originX, endY - edge.originY)).toBeGreaterThan(100);
  });

  it("'arc' mode bulges toward the outside corner, not into the room", () => {
    const a = wall('a', 0, 0, 2000, 0);
    const b = wall('b', 2500, 500, 2000, 90);
    const edges = computeMultiWallGapRuns([a, b], [a, b], [], 'arc');
    expect(edges).toHaveLength(1);
    const edge = edges[0];
    // Outside corner = intersection of the two walls' outward rays (a-end +X, b-start -Y).
    const cornerC = { x: 2500, y: 0 };
    const apexLocal = arcEndLocal(
      edge.lengthMm / 2,
      edge.geomArcRadiusMm ?? 0,
      edge.geomArcSweepDeg ?? 0,
    );
    const rad = (edge.rotationDeg * Math.PI) / 180;
    const apexX = edge.originX + apexLocal.xMm * Math.cos(rad) - apexLocal.yMm * Math.sin(rad);
    const apexY = edge.originY + apexLocal.xMm * Math.sin(rad) + apexLocal.yMm * Math.cos(rad);
    const chordMid = { x: 2250, y: 250 };
    const apexToCorner = Math.hypot(apexX - cornerC.x, apexY - cornerC.y);
    const midToCorner = Math.hypot(chordMid.x - cornerC.x, chordMid.y - cornerC.y);
    expect(apexToCorner).toBeLessThan(midToCorner);
  });

  it('lifts the gap run to the shared base elevation when both walls are raised', () => {
    const a = { ...wall('a', 0, 0, 2000, 0), geomZ: 900 };
    const b = { ...wall('b', 3000, 0, 2000, 0), geomZ: 900 };
    const edges = computeMultiWallGapRuns([a, b], [a, b], []);
    expect(edges).toHaveLength(1);
    expect(edges[0].geomZ).toBe(900);
  });

  it('bridges walls at different elevations at the lower base so the run reaches both', () => {
    const a = { ...wall('a', 0, 0, 2000, 0), geomZ: 900 };
    const b = { ...wall('b', 3000, 0, 2000, 0), geomZ: 0 };
    const edges = computeMultiWallGapRuns([a, b], [a, b], []);
    expect(edges).toHaveLength(1);
    expect(edges[0].geomZ).toBe(0);
  });

  it('keeps each corner leg at its own wall height for a mixed-height corner', () => {
    const a = wall('a', 0, 0, 2000, 0, 2400);
    const b = wall('b', 2500, 500, 2000, 90, 2000);
    const edges = computeMultiWallGapRuns([a, b], [a, b], []);
    expect(edges).toHaveLength(2);
    const legA = edges.find((e) => Math.round(e.rotationDeg) % 180 === 0);
    const legB = edges.find((e) => Math.round(e.rotationDeg) % 180 === 90);
    expect(legA?.heightMm).toBe(2400);
    expect(legB?.heightMm).toBe(2000);
  });
});
