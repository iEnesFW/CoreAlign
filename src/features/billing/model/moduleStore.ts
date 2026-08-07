import type { ModuleDto, ModulePricePlanDto, TenantModuleDto } from './billing.types';

export type BillingCycle = 'monthly' | 'yearly';

export interface CartEntry {
  moduleId: string;
  planId: string;
}

export interface StoreLine {
  moduleId: string;
  planId: string;
  moduleName: string;
  iconKey: string | null;
  planLabel: string;
  durationDays: number;
  unitPrice: number;
  currency: string;
  isRenewal: boolean;
  currentEndUtc: string | null;
}

export interface StoreModule {
  module: ModuleDto;
  plan: ModulePricePlanDto | null;
  owned: TenantModuleDto | null;
  selected: boolean;
}

export interface StoreGroup {
  category: string;
  modules: StoreModule[];
}

const YEARLY_MIN_DAYS = 300;

/**
 * One global billing-cycle switch instead of a plan dropdown on every card — with twenty modules a
 * per-card selector is the difference between a page you can scan and one you cannot.
 */
export const pickPlan = (module: ModuleDto, cycle: BillingCycle): ModulePricePlanDto | null => {
  const active = (module.plans ?? []).filter((p) => p.isActive);
  if (active.length === 0) return null;

  const sorted = [...active].sort((a, b) => a.durationDays - b.durationDays);
  if (cycle === 'yearly') {
    return sorted.find((p) => p.durationDays >= YEARLY_MIN_DAYS) ?? sorted[sorted.length - 1];
  }
  return sorted.find((p) => p.durationDays < YEARLY_MIN_DAYS) ?? sorted[0];
};

export const buildGroups = (
  modules: ModuleDto[],
  activeModules: TenantModuleDto[],
  cycle: BillingCycle,
  cart: CartEntry[],
): StoreGroup[] => {
  const ownedByModuleId = new Map(activeModules.map((a) => [a.moduleId, a]));
  const selectedIds = new Set(cart.map((c) => c.moduleId));
  const byCategory = new Map<string, StoreModule[]>();

  for (const module of modules) {
    if (!module.isActive) continue;
    const category = module.category?.trim() || '';
    const entry: StoreModule = {
      module,
      plan: module.isCore ? null : pickPlan(module, cycle),
      owned: ownedByModuleId.get(module.id) ?? null,
      selected: selectedIds.has(module.id),
    };
    const bucket = byCategory.get(category);
    if (bucket) bucket.push(entry);
    else byCategory.set(category, [entry]);
  }

  return [...byCategory.entries()]
    .map(([category, entries]) => ({
      category,
      modules: entries.sort((a, b) => a.module.sortOrder - b.module.sortOrder),
    }))
    .sort((a, b) => a.modules[0].module.sortOrder - b.modules[0].module.sortOrder);
};

export const buildLines = (
  modules: ModuleDto[],
  activeModules: TenantModuleDto[],
  cart: CartEntry[],
): StoreLine[] => {
  const byModuleId = new Map(modules.map((m) => [m.id, m]));
  const ownedByModuleId = new Map(activeModules.map((a) => [a.moduleId, a]));
  const lines: StoreLine[] = [];

  for (const entry of cart) {
    const module = byModuleId.get(entry.moduleId);
    if (!module) continue;
    const plan = (module.plans ?? []).find((p) => p.id === entry.planId);
    // A cart is persisted per user, so a plan that was retired or repriced since must never be
    // rendered from the stored copy — the line is dropped and the price comes from the live plan.
    if (!plan || !plan.isActive) continue;
    const owned = ownedByModuleId.get(module.id) ?? null;
    lines.push({
      moduleId: module.id,
      planId: plan.id,
      moduleName: module.name,
      iconKey: module.iconKey ?? null,
      planLabel: plan.displayLabel,
      durationDays: plan.durationDays,
      unitPrice: plan.price,
      currency: plan.currency,
      isRenewal: Boolean(owned),
      currentEndUtc: owned?.endUtc ?? null,
    });
  }
  return lines;
};

export const cartTotal = (lines: StoreLine[]): number =>
  Math.round(lines.reduce((sum, l) => sum + l.unitPrice, 0) * 100) / 100;

export const cartCurrency = (lines: StoreLine[], fallback = 'TRY'): string =>
  lines[0]?.currency ?? fallback;

/** The order endpoint refuses a mixed-currency basket, so the UI must not let one be assembled. */
export const hasMixedCurrency = (lines: StoreLine[]): boolean =>
  new Set(lines.map((l) => l.currency.toUpperCase())).size > 1;

export const projectedEndUtc = (line: StoreLine, now: Date): string => {
  const basis =
    line.currentEndUtc && new Date(line.currentEndUtc) > now ? new Date(line.currentEndUtc) : now;
  const next = new Date(basis);
  next.setDate(next.getDate() + line.durationDays);
  return next.toISOString();
};
