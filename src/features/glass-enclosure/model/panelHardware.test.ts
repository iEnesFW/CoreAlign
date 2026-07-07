import { describe, expect, it } from 'vitest';
import { aggregatePanelHardware } from './panelHardware';
import type { SceneHardwareItem } from './project.types';

const hw = (overrides: Partial<SceneHardwareItem>): SceneHardwareItem => ({
  id: crypto.randomUUID(),
  kind: 'Handle',
  colorHex: '#000000',
  offsetXmm: 0,
  offsetYmm: 0,
  offsetZmm: 0,
  widthMm: 40,
  heightMm: 120,
  depthMm: 20,
  ...overrides,
});

describe('aggregatePanelHardware', () => {
  it('drops render-only items with no catalog link', () => {
    expect(aggregatePanelHardware([hw({ hardwareItemId: null }), hw({})])).toEqual([]);
  });

  it('treats a missing quantity as one', () => {
    expect(aggregatePanelHardware([hw({ hardwareItemId: 'A' })])).toEqual([
      { hardwareItemId: 'A', quantity: 1 },
    ]);
  });

  it('sums quantities of the same catalog item', () => {
    const result = aggregatePanelHardware([
      hw({ hardwareItemId: 'A', quantity: 2 }),
      hw({ hardwareItemId: 'A' }),
      hw({ hardwareItemId: 'B', quantity: 3 }),
    ]);
    expect(result).toEqual([
      { hardwareItemId: 'A', quantity: 3 },
      { hardwareItemId: 'B', quantity: 3 },
    ]);
  });

  it('skips non-positive quantities', () => {
    expect(aggregatePanelHardware([hw({ hardwareItemId: 'A', quantity: 0 })])).toEqual([]);
    expect(aggregatePanelHardware([hw({ hardwareItemId: 'A', quantity: -2 })])).toEqual([]);
  });
});
