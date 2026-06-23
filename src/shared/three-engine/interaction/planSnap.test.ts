import { describe, expect, it } from 'vitest';
import { EMPTY_SNAP_TARGETS, applyPlanMoveSnap, lineProbePoints } from './planSnap';
import type { PlanSnapTargets } from './planSnap';

const z = (n: number) => (n === 0 ? 0 : n);
const round = (p: { x: number; y: number }) => ({ x: z(Math.round(p.x)), y: z(Math.round(p.y)) });

describe('lineProbePoints', () => {
  it('returns the two centerline ends, four face corners and two side-edge midpoints', () => {
    const pts = lineProbePoints(0, 0, 1000, 0, 100);
    expect(pts).toHaveLength(8);
    expect(pts).toContainEqual({ x: 0, y: 0 });
    expect(pts).toContainEqual({ x: 1000, y: 0 });
    expect(pts).toContainEqual({ x: 0, y: 100 });
    expect(pts).toContainEqual({ x: 0, y: -100 });
    expect(pts).toContainEqual({ x: 1000, y: 100 });
    expect(pts).toContainEqual({ x: 1000, y: -100 });
    // side-edge midpoints (the "middle" tick)
    expect(pts).toContainEqual({ x: 500, y: 100 });
    expect(pts).toContainEqual({ x: 500, y: -100 });
  });

  it('orients the face offset perpendicular to a rotated body', () => {
    const pts = lineProbePoints(0, 0, 1000, 90, 100).map(round);
    expect(pts).toHaveLength(8);
    expect(pts).toContainEqual({ x: 0, y: 0 });
    expect(pts).toContainEqual({ x: 0, y: 1000 });
    expect(pts).toContainEqual({ x: -100, y: 0 });
    expect(pts).toContainEqual({ x: 100, y: 0 });
    expect(pts).toContainEqual({ x: -100, y: 1000 });
    expect(pts).toContainEqual({ x: 100, y: 1000 });
    expect(pts).toContainEqual({ x: -100, y: 500 });
    expect(pts).toContainEqual({ x: 100, y: 500 });
  });
});

describe('applyPlanMoveSnap', () => {
  it('snaps the raw delta to the 5mm plan grid when there are no targets', () => {
    const res = applyPlanMoveSnap([{ x: 0, y: 0 }], 12, 8, EMPTY_SNAP_TARGETS);
    expect(res.dxMm).toBe(10);
    expect(res.dyMm).toBe(10);
    expect(res.guides).toEqual([]);
  });

  it('snaps a probe exactly onto a nearby corner target', () => {
    const targets: PlanSnapTargets = { points: [{ x: 1000, y: 0 }], segments: [] };
    const res = applyPlanMoveSnap([{ x: 940, y: 8 }], 0, 0, targets);
    expect(res.dxMm).toBe(60);
    expect(res.dyMm).toBe(-8);
    expect(res.guides[0]?.kind).toBe('corner');
  });

  it('does not corner-snap beyond the corner tolerance', () => {
    const targets: PlanSnapTargets = { points: [{ x: 1000, y: 700 }], segments: [] };
    const res = applyPlanMoveSnap([{ x: 600, y: 0 }], 0, 0, targets);
    expect(res.dxMm).toBe(0);
    expect(res.dyMm).toBe(0);
    expect(res.guides).toEqual([]);
  });

  it('corner-snaps just within ~100mm but not just beyond it', () => {
    const within: PlanSnapTargets = { points: [{ x: 90, y: 0 }], segments: [] };
    expect(applyPlanMoveSnap([{ x: 0, y: 0 }], 0, 0, within).dxMm).toBe(90);
    const beyond: PlanSnapTargets = { points: [{ x: 130, y: 0 }], segments: [] };
    expect(applyPlanMoveSnap([{ x: 0, y: 0 }], 0, 0, beyond).dxMm).toBe(0);
  });

  it('lets a side-edge midpoint probe stick to a neighbour edge midpoint (middle tick)', () => {
    // a horizontal body; its top-edge midpoint probe is at (500, 100)
    const probes = lineProbePoints(0, 0, 1000, 0, 100);
    const targets: PlanSnapTargets = { points: [{ x: 520, y: 100 }], segments: [] };
    const res = applyPlanMoveSnap(probes, 0, 0, targets);
    expect(res.dxMm).toBe(20);
    expect(res.dyMm).toBe(0);
  });

  it('pulls a probe flush onto a face segment by perpendicular projection', () => {
    const targets: PlanSnapTargets = {
      points: [],
      segments: [{ x1: 1000, y1: -500, x2: 1000, y2: 500 }],
    };
    const res = applyPlanMoveSnap([{ x: 930, y: 100 }], 0, 0, targets);
    expect(res.dxMm).toBe(70);
    expect(res.dyMm).toBe(0);
    expect(res.guides.some((g) => g.kind === 'edge')).toBe(true);
  });

  it('aligns a probe to a target axis within tolerance and emits an axis guide', () => {
    const targets: PlanSnapTargets = { points: [{ x: 1000, y: 40 }], segments: [] };
    const res = applyPlanMoveSnap([{ x: 200, y: 0 }], 0, 0, targets);
    expect(res.dxMm).toBe(0);
    expect(res.dyMm).toBe(40);
    expect(res.guides.some((g) => g.kind === 'axis')).toBe(true);
  });

  it('butts a rectangular body flush against a neighbour face via its face-corner probe (not the centerline)', () => {
    // Neighbour wall A: right face is the vertical segment x=0, corner at (0,0).
    const targets: PlanSnapTargets = {
      points: [{ x: 0, y: 0 }],
      segments: [{ x1: 0, y1: -1000, x2: 0, y2: 1000 }],
    };
    // Wall B (thickness 200 -> halfWidth 100) centred at x=120, vertical, length 2000.
    // Its near face corner sits at x=20; the centerline sits at x=120.
    const probes = lineProbePoints(120, 0, 2000, 90, 100);
    const res = applyPlanMoveSnap(probes, 0, 0, targets);
    // Face corner (20,0) is closer than the centerline (120,0), so it snaps flush
    // (dx=-20) instead of overlapping the neighbour (the old centerline-only dx=-120).
    expect(res.dxMm).toBe(-20);
    expect(res.dyMm).toBeCloseTo(0);
  });
});
