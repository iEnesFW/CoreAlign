import { describe, expect, it } from 'vitest';
import {
  developedFromTangentMm,
  paneHalfSpanMm,
  paneSurfaceFrame,
  seatOnFaceMm,
  surfaceSagittaMm,
  surfaceSegmentsLocal,
} from './paneSurface';
import type { PaneSurface } from './paneSurface';

const flat = (patch: Partial<PaneSurface> = {}): PaneSurface => ({
  widthMm: 1000,
  heightMm: 2000,
  thicknessMm: 8,
  baseYm: 0.03,
  curve: null,
  ...patch,
});

const curved = (patch: Partial<PaneSurface> = {}): PaneSurface =>
  flat({ curve: { radiusM: 3, direction: 1, phiMid: 0.5 }, ...patch });

/** Distance from the arc centre axis, in the pane's parent frame. */
const radialM = (s: PaneSurface, p: [number, number, number]) => {
  const c = s.curve;
  if (!c) return 0;
  return Math.hypot(p[0], p[2] - c.direction * c.radiusM);
};

describe('a flat pane is the degenerate curved pane', () => {
  it('reproduces the historic flat placement exactly', () => {
    const s = flat();
    // The old code: pane group at [centerX, baseY + heightM/2, 0], hardware at [x, y, z] mm/1000.
    const f = paneSurfaceFrame(s, { uMm: 120, vMm: -300, nMm: 9 });
    expect(f.positionM[0]).toBeCloseTo(0.12, 12);
    expect(f.positionM[1]).toBeCloseTo(0.03 + 1.0 - 0.3, 12);
    expect(f.positionM[2]).toBeCloseTo(0.009, 12);
    expect(f.yawRad).toBe(0);
    expect(f.kappaPerM).toBe(0);
  });

  it('has no curvature, no sagitta and an identity drag conversion', () => {
    const s = flat();
    expect(surfaceSagittaMm(2000, s)).toBe(0);
    expect(developedFromTangentMm(-437.5, s)).toBe(-437.5);
  });
});

describe('a curved pane puts everything ON the cylinder', () => {
  it('every mounting point sits at the glass radius, whatever the offset', () => {
    const s = curved();
    const shell = s.curve!.radiusM;
    for (const uMm of [-500, -250, 0, 250, 500]) {
      for (const vMm of [-900, 0, 900]) {
        const f = paneSurfaceFrame(s, { uMm, vMm, nMm: 0 });
        expect(radialM(s, f.positionM)).toBeCloseTo(shell, 9);
      }
    }
  });

  it('stepping along the normal leaves the surface perpendicular, by exactly that much', () => {
    for (const direction of [1, -1] as const) {
      const s = curved({ curve: { radiusM: 3, direction, phiMid: 0.4 } });
      const on = paneSurfaceFrame(s, { uMm: 200, vMm: 0, nMm: 0 });
      const off = paneSurfaceFrame(s, { uMm: 200, vMm: 0, nMm: 24 });
      const stepM = Math.hypot(
        off.positionM[0] - on.positionM[0],
        off.positionM[2] - on.positionM[2],
      );
      expect(stepM).toBeCloseTo(0.024, 9);
      // Perpendicular to the glass: the step changes the radius by its full length.
      expect(Math.abs(radialM(s, off.positionM) - radialM(s, on.positionM))).toBeCloseTo(0.024, 9);
    }
  });

  it('a positive normal keeps the SAME world side as the sweep goes to zero', () => {
    // WHY this matters: anchoring the normal to the arc's outward direction instead would flip
    // every mounted piece to the other glass face the instant a straight run picked up a 1° bow.
    for (const direction of [1, -1] as const) {
      const nearlyStraight = curved({ curve: { radiusM: 5000, direction, phiMid: 0 } });
      const f = paneSurfaceFrame(nearlyStraight, { uMm: 0, vMm: 0, nMm: 24 });
      const flatFrame = paneSurfaceFrame(flat(), { uMm: 0, vMm: 0, nMm: 24 });
      expect(f.positionM[2] - flatFrame.positionM[2]).toBeCloseTo(0, 6);
    }
  });

  it('THE OLD BEHAVIOUR: a flat chord step off the pane centre leaves the glass by metres', () => {
    // What PanelFittings did: anchor once at the pane mid, then step ±(width/2 − 50 mm) in the
    // FLAT chord frame. Reproduce it and measure how far off the cylinder it lands.
    const s = curved({ widthMm: 3000, curve: { radiusM: 3, direction: 1, phiMid: 0.5 } });
    const mid = paneSurfaceFrame(s, { uMm: 0, vMm: 0, nMm: 0 });
    const stepM = (s.widthMm / 2 - 50) / 1000;
    const yaw = -mid.yawRad;
    const flatStep: [number, number, number] = [
      mid.positionM[0] + stepM * Math.cos(yaw),
      mid.positionM[1],
      mid.positionM[2] + stepM * Math.sin(yaw),
    ];
    const offSurfaceMm = Math.abs(radialM(s, flatStep) - s.curve!.radiusM) * 1000;
    expect(offSurfaceMm).toBeGreaterThan(100);

    // The shared frame puts the same fitting on the glass.
    const fixed = paneSurfaceFrame(s, { uMm: s.widthMm / 2 - 50, vMm: 0, nMm: 0 });
    expect(Math.abs(radialM(s, fixed.positionM) - s.curve!.radiusM) * 1000).toBeLessThanOrEqual(
      0.001,
    );
  });

  it('the yaw follows the tangent, so a piece never reads as twisted off the surface', () => {
    const s = curved();
    const a = paneSurfaceFrame(s, { uMm: -400, vMm: 0, nMm: 0 });
    const b = paneSurfaceFrame(s, { uMm: 400, vMm: 0, nMm: 0 });
    // 800 mm of developed travel on a 3 m radius turns the tangent by 800/3000 rad.
    expect(Math.abs(a.yawRad - b.yawRad)).toBeCloseTo(0.8 / 3, 9);
  });

  it('reports the signed curvature so a consumer can decide to bend', () => {
    expect(paneSurfaceFrame(curved(), { uMm: 0, vMm: 0, nMm: 0 }).kappaPerM).toBeCloseTo(1 / 3, 12);
    expect(
      paneSurfaceFrame(curved({ curve: { radiusM: 3, direction: -1, phiMid: 0 } }), {
        uMm: 0,
        vMm: 0,
        nMm: 0,
      }).kappaPerM,
    ).toBeCloseTo(-1 / 3, 12);
  });
});

describe('a tangent drag converts to the developed coordinate the surface stores', () => {
  it('a straight drag over-shoots the arc unless converted', () => {
    const s = curved({ curve: { radiusM: 1, direction: 1, phiMid: 0 } });
    // RED-before: the raw tangent length was committed as arc length.
    expect(500 - developedFromTangentMm(500, s)).toBeGreaterThan(30);
    expect(developedFromTangentMm(500, s)).toBeCloseTo(1000 * Math.atan(0.5), 6);
  });

  it('is exact for a flat pane and near-identity for a shallow curve', () => {
    expect(developedFromTangentMm(500, flat())).toBe(500);
    const shallow = curved({ curve: { radiusM: 20, direction: 1, phiMid: 0 } });
    expect(Math.abs(500 - developedFromTangentMm(500, shallow))).toBeLessThan(1);
  });
});

describe('the clamp reads the surface that is actually drawn', () => {
  it('keeps a piece entirely on the glass', () => {
    const s = flat({ widthMm: 988, heightMm: 2340 });
    const span = paneHalfSpanMm(s, 120, 300);
    expect(span.uMm).toBe(988 / 2 - 60);
    expect(span.vMm).toBe(2340 / 2 - 150);
    // Snapped to the edge, the item's far corner lands exactly on the glass edge.
    expect(span.uMm + 60).toBe(988 / 2);
  });

  it('collapses to zero rather than negative for an item wider than the pane', () => {
    const span = paneHalfSpanMm(flat({ widthMm: 100 }), 400, 4000);
    expect(span.uMm).toBe(0);
    expect(span.vMm).toBe(0);
  });
});

describe('bend decision and face seating', () => {
  it('sagitta grows with span and tightens with radius', () => {
    const r2 = curved({ curve: { radiusM: 2, direction: 1, phiMid: 0 } });
    const r1 = curved({ curve: { radiusM: 1, direction: 1, phiMid: 0 } });
    expect(surfaceSagittaMm(600, r2)).toBeGreaterThan(20);
    expect(surfaceSagittaMm(600, r1)).toBeGreaterThan(surfaceSagittaMm(600, r2));
    // A small lock barely misses the surface — it must stay a rigid block.
    expect(surfaceSagittaMm(44, r1)).toBeLessThan(1);
  });

  it('seats a piece on the outer face for the pane thickness it is actually on', () => {
    expect(seatOnFaceMm(8, 20)).toBe(14);
    // RED-before: the seed always assumed 8 mm glass, so 12 mm glass buried the piece by 2 mm.
    expect(seatOnFaceMm(12, 20) - seatOnFaceMm(8, 20)).toBe(2);
  });
});

describe('a long piece BENDS to the surface instead of chording across it', () => {
  it('a flat pane returns exactly one full-span segment (the rigid box, unchanged)', () => {
    const segs = surfaceSegmentsLocal(flat(), { uMm: 0, vMm: 0, nMm: 10 }, 600);
    expect(segs).toEqual([{ xM: 0, zM: 0, yawRad: 0, spanMm: 600 }]);
  });

  it('every segment of a bent piece lands on the cylinder', () => {
    const s = curved({ curve: { radiusM: 2, direction: 1, phiMid: 0.3 } });
    const centre = { uMm: 150, vMm: 0, nMm: 0 };
    const segs = surfaceSegmentsLocal(s, centre, 600);
    expect(segs.length).toBeGreaterThan(1);
    const origin = paneSurfaceFrame(s, centre);
    const cos = Math.cos(-origin.yawRad);
    const sin = Math.sin(-origin.yawRad);
    for (const seg of segs) {
      // Re-apply the origin frame to get back to the parent frame, then measure the radius.
      const px = origin.positionM[0] + (seg.xM * cos - seg.zM * sin);
      const pz = origin.positionM[2] + (seg.xM * sin + seg.zM * cos);
      expect(radialM(s, [px, 0, pz])).toBeCloseTo(s.curve!.radiusM, 9);
    }
  });

  it('RED-before: one rigid box of the same span misses the glass by tens of mm', () => {
    for (const [radiusM, spanMm, minMissMm] of [
      [2, 600, 20],
      [1, 600, 40],
    ] as const) {
      const s = curved({ curve: { radiusM, direction: 1, phiMid: 0 } });
      expect(surfaceSagittaMm(spanMm, s)).toBeGreaterThan(minMissMm);
      // Bent: the worst segment centre is on the surface, so the miss collapses to the segment's
      // own sagitta — two orders of magnitude smaller.
      const segs = surfaceSegmentsLocal(s, { uMm: 0, vMm: 0, nMm: 0 }, spanMm);
      expect(surfaceSagittaMm(segs[0].spanMm, s)).toBeLessThan(1);
    }
  });

  it('segment spans sum back to the piece width', () => {
    const s = curved({ curve: { radiusM: 1.5, direction: -1, phiMid: 0.2 } });
    const segs = surfaceSegmentsLocal(s, { uMm: 0, vMm: 0, nMm: 0 }, 745);
    expect(segs.reduce((a, b) => a + b.spanMm, 0)).toBeCloseTo(745, 9);
  });
});
