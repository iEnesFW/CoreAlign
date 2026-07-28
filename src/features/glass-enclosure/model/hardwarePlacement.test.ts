import { describe, expect, it } from 'vitest';
import { clampHardwareOffsets, glassClampWidthMm } from './hardwarePlacement';
import { radiusFromChordSweep } from './arcGeometry';

describe('clampHardwareOffsets', () => {
  it('keeps an in-bounds offset untouched', () => {
    const r = clampHardwareOffsets(1000, 2400, {
      offsetXmm: 100,
      offsetYmm: -200,
      widthMm: 60,
      heightMm: 300,
    });
    expect(r).toEqual({ offsetXmm: 100, offsetYmm: -200 });
  });

  it('clamps an offset that would push the item past the panel face', () => {
    // edgeX = 1000/2 - 60/2 = 470; edgeY = 2400/2 - 300/2 = 1050
    const r = clampHardwareOffsets(1000, 2400, {
      offsetXmm: 900,
      offsetYmm: -5000,
      widthMm: 60,
      heightMm: 300,
    });
    expect(r).toEqual({ offsetXmm: 470, offsetYmm: -1050 });
  });

  it('pins the item to centre when it is wider/taller than the panel (edge collapses to 0)', () => {
    const r = clampHardwareOffsets(500, 800, {
      offsetXmm: 300,
      offsetYmm: 300,
      widthMm: 900,
      heightMm: 1200,
    });
    expect(r).toEqual({ offsetXmm: 0, offsetYmm: 0 });
  });
});

describe('glassClampWidthMm reads the pane the renderer actually draws', () => {
  const straight = { lengthMm: 3000 };

  it('deducts the cell joint on a straight run', () => {
    // RED-before: the clamp used the raw 1000 and parked a piece 6 mm off the glass on each edge.
    expect(glassClampWidthMm(1000, straight)).toBe(988);
  });

  it('a BENT arc pane is the full developed band — no joint deduction', () => {
    const bent = {
      lengthMm: 3000,
      geomArcRadiusMm: 3000,
      geomArcSweepDeg: 60,
      arcGlassBent: true,
    };
    expect(glassClampWidthMm(1000, bent)).toBe(1000);
  });

  it('a FACETED arc pane is a flat CHORD across its share of the sweep', () => {
    // panel.widthMm is DEVELOPED on an arc run; the flat pane is drawn at the chord, which is
    // shorter — so the clamp must shrink twice (chord, then joint).
    const faceted = {
      lengthMm: 3000,
      geomArcRadiusMm: 2000,
      geomArcSweepDeg: 90,
      arcGlassBent: false,
    };
    const drawn = glassClampWidthMm(1000, faceted);
    const radiusMm = radiusFromChordSweep(3000, 2000, 90);
    const chord = 2 * radiusMm * Math.sin(1000 / (2 * radiusMm));
    expect(drawn).toBeCloseTo(chord - 12, 6);
    expect(drawn).toBeLessThan(988);
  });

  it('never returns a non-positive width', () => {
    expect(glassClampWidthMm(4, straight)).toBeGreaterThan(0);
  });
});
