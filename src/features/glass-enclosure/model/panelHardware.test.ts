import { describe, expect, it } from 'vitest';
import {
  aggregatePanelHardware,
  combinePanelHardware,
  sceneHardwareKindToCategory,
} from './panelHardware';
import type { HardwareCategoryKind } from './glassEnclosure.types';
import type { SceneHardwareItem } from './project.types';

const CATALOG: { id: string; category: HardwareCategoryKind }[] = [
  { id: 'handle-1', category: 'Handle' },
  { id: 'handle-2', category: 'Handle' },
  { id: 'lock-1', category: 'Lock' },
  { id: 'gasket-1', category: 'Gasket' },
  { id: 'brush-1', category: 'Brush' },
];

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

describe('combinePanelHardware', () => {
  it('folds a true has* bool into a quoted piece via its category catalog item', () => {
    const result = combinePanelHardware({ hardware: [], hasHandle: true }, CATALOG);
    expect(result).toEqual([{ hardwareItemId: 'handle-1', quantity: 1 }]);
  });

  it('maps each bool to its own category (handle/lock/brush)', () => {
    const result = combinePanelHardware(
      { hardware: [], hasHandle: true, hasLock: true, hasBrushSeal: true },
      CATALOG,
    );
    expect(result).toEqual([
      { hardwareItemId: 'handle-1', quantity: 1 },
      { hardwareItemId: 'lock-1', quantity: 1 },
      { hardwareItemId: 'brush-1', quantity: 1 },
    ]);
  });

  it('quotes a brush seal separately from an explicit gasket strip (no silent drop)', () => {
    // A gasket strip (category Gasket) must NOT suppress the brush-seal bool (category Brush).
    const result = combinePanelHardware(
      { hardware: [hw({ hardwareItemId: 'gasket-1' })], hasBrushSeal: true },
      CATALOG,
    );
    expect(result).toEqual([
      { hardwareItemId: 'gasket-1', quantity: 1 },
      { hardwareItemId: 'brush-1', quantity: 1 },
    ]);
  });

  it('suppresses the bool when the exact catalog item is already an explicit object', () => {
    const result = combinePanelHardware(
      { hardware: [hw({ hardwareItemId: 'handle-1' })], hasHandle: true },
      CATALOG,
    );
    expect(result).toEqual([{ hardwareItemId: 'handle-1', quantity: 1 }]);
  });

  it('suppresses the bool when a DIFFERENT catalog item of the same category is quoted (no double)', () => {
    const result = combinePanelHardware(
      { hardware: [hw({ hardwareItemId: 'handle-2' })], hasHandle: true },
      CATALOG,
    );
    expect(result).toEqual([{ hardwareItemId: 'handle-2', quantity: 1 }]);
  });

  it('does not let a render-only (unquoted) object suppress the bool', () => {
    const result = combinePanelHardware(
      { hardware: [hw({ kind: 'Handle', hardwareItemId: null })], hasHandle: true },
      CATALOG,
    );
    expect(result).toEqual([{ hardwareItemId: 'handle-1', quantity: 1 }]);
  });

  it('skips false bools and bools with no catalog match', () => {
    expect(combinePanelHardware({ hardware: [], hasHandle: false }, CATALOG)).toEqual([]);
    expect(combinePanelHardware({ hardware: [], hasLock: true }, [])).toEqual([]);
  });
});

describe('sceneHardwareKindToCategory', () => {
  it('maps render kinds to catalog categories for auto-linking', () => {
    expect(sceneHardwareKindToCategory('Handle')).toBe('Handle');
    expect(sceneHardwareKindToCategory('PullHandle')).toBe('Handle');
    expect(sceneHardwareKindToCategory('GasketStrip')).toBe('Gasket');
    expect(sceneHardwareKindToCategory('CornerJoint')).toBe('CornerPost');
    expect(sceneHardwareKindToCategory('DripProfile')).toBe('DripCap');
    expect(sceneHardwareKindToCategory('Accessory')).toBe('Other');
  });
});
