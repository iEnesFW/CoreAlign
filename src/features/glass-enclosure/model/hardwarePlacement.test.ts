import { describe, expect, it } from 'vitest';
import { clampHardwareOffsets } from './hardwarePlacement';

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
