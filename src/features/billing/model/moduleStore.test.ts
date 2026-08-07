import { describe, expect, it } from 'vitest';

import type { ModuleDto, ModulePricePlanDto, TenantModuleDto } from './billing.types';
import {
  buildGroups,
  buildLines,
  cartTotal,
  hasMixedCurrency,
  pickPlan,
  projectedEndUtc,
} from './moduleStore';

const plan = (over: Partial<ModulePricePlanDto> = {}): ModulePricePlanDto => ({
  id: 'p1',
  moduleId: 'm1',
  code: 'Monthly',
  displayLabel: 'Aylık',
  durationDays: 30,
  price: 99,
  currency: 'TRY',
  isActive: true,
  sortOrder: 0,
  ...over,
});

const mod = (over: Partial<ModuleDto> = {}): ModuleDto => ({
  id: 'm1',
  code: 'Sales',
  name: 'Satış',
  description: null,
  category: 'Satış & CRM',
  iconKey: 'shopping-cart',
  sortOrder: 10,
  isActive: true,
  isCore: false,
  plans: [
    plan(),
    plan({ id: 'p2', code: 'Yearly', displayLabel: 'Yıllık', durationDays: 365, price: 999 }),
  ],
  ...over,
});

const owned = (over: Partial<TenantModuleDto> = {}): TenantModuleDto => ({
  id: 't1',
  moduleId: 'm1',
  code: 'Sales',
  name: 'Satış',
  startUtc: '2026-01-01T00:00:00Z',
  endUtc: '2026-12-31T00:00:00Z',
  isCurrentlyActive: true,
  source: 'Paid',
  notes: null,
  ...over,
});

describe('pickPlan', () => {
  it('picks the short plan for monthly and the long one for yearly', () => {
    expect(pickPlan(mod(), 'monthly')?.durationDays).toBe(30);
    expect(pickPlan(mod(), 'yearly')?.durationDays).toBe(365);
  });

  it('falls back to the longest plan when no yearly-length plan exists', () => {
    const m = mod({ plans: [plan({ durationDays: 30 }), plan({ id: 'p9', durationDays: 90 })] });
    expect(pickPlan(m, 'yearly')?.durationDays).toBe(90);
  });

  it('ignores retired plans and returns null when nothing is sellable', () => {
    expect(pickPlan(mod({ plans: [plan({ isActive: false })] }), 'monthly')).toBeNull();
    expect(pickPlan(mod({ plans: [] }), 'monthly')).toBeNull();
  });
});

describe('buildGroups', () => {
  it('gives core modules no plan so they can never be added to a cart', () => {
    const groups = buildGroups([mod({ isCore: true })], [], 'monthly', []);
    expect(groups[0].modules[0].plan).toBeNull();
  });

  it('marks an owned module so the card can offer renewal instead of purchase', () => {
    const groups = buildGroups([mod()], [owned()], 'monthly', []);
    expect(groups[0].modules[0].owned?.endUtc).toBe('2026-12-31T00:00:00Z');
  });

  it('drops inactive modules and orders categories by their first module', () => {
    const groups = buildGroups(
      [
        mod({ id: 'z', code: 'Z', category: 'Finans', sortOrder: 80 }),
        mod(),
        mod({ id: 'x', code: 'X', isActive: false }),
      ],
      [],
      'monthly',
      [],
    );
    expect(groups.map((g) => g.category)).toEqual(['Satış & CRM', 'Finans']);
    expect(groups.flatMap((g) => g.modules).map((m) => m.module.id)).not.toContain('x');
  });
});

describe('buildLines', () => {
  it('reads the price from the live catalog, never from the persisted cart', () => {
    const lines = buildLines([mod()], [], [{ moduleId: 'm1', planId: 'p2' }]);
    expect(lines).toHaveLength(1);
    expect(lines[0].unitPrice).toBe(999);
    expect(lines[0].planLabel).toBe('Yıllık');
  });

  it('drops a cart entry whose plan was retired since it was saved', () => {
    const m = mod({ plans: [plan({ isActive: false })] });
    expect(buildLines([m], [], [{ moduleId: 'm1', planId: 'p1' }])).toHaveLength(0);
  });

  it('drops a cart entry for a module that no longer exists', () => {
    expect(buildLines([mod()], [], [{ moduleId: 'gone', planId: 'p1' }])).toHaveLength(0);
  });

  it('flags an owned module as a renewal', () => {
    const lines = buildLines([mod()], [owned()], [{ moduleId: 'm1', planId: 'p1' }]);
    expect(lines[0].isRenewal).toBe(true);
    expect(lines[0].currentEndUtc).toBe('2026-12-31T00:00:00Z');
  });
});

describe('totals', () => {
  it('sums to the cent', () => {
    const lines = buildLines(
      [
        mod(),
        mod({ id: 'm2', code: 'X', plans: [plan({ id: 'p3', moduleId: 'm2', price: 0.15 })] }),
      ],
      [],
      [
        { moduleId: 'm1', planId: 'p1' },
        { moduleId: 'm2', planId: 'p3' },
      ],
    );
    expect(cartTotal(lines)).toBe(99.15);
  });

  it('detects a mixed-currency basket, which the order endpoint refuses', () => {
    const lines = buildLines(
      [
        mod(),
        mod({ id: 'm2', code: 'X', plans: [plan({ id: 'p3', moduleId: 'm2', currency: 'EUR' })] }),
      ],
      [],
      [
        { moduleId: 'm1', planId: 'p1' },
        { moduleId: 'm2', planId: 'p3' },
      ],
    );
    expect(hasMixedCurrency(lines)).toBe(true);
  });
});

describe('projectedEndUtc', () => {
  const now = new Date('2026-06-01T00:00:00Z');

  it('extends from the existing end date so an early renewal does not burn time', () => {
    const [line] = buildLines(
      [mod()],
      [owned({ endUtc: '2026-08-01T00:00:00Z' })],
      [{ moduleId: 'm1', planId: 'p1' }],
    );
    expect(projectedEndUtc(line, now).slice(0, 10)).toBe('2026-08-31');
  });

  it('extends from today when the previous grant already lapsed', () => {
    const [line] = buildLines(
      [mod()],
      [owned({ endUtc: '2026-01-01T00:00:00Z' })],
      [{ moduleId: 'm1', planId: 'p1' }],
    );
    expect(projectedEndUtc(line, now).slice(0, 10)).toBe('2026-07-01');
  });
});
