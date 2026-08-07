import { create } from 'zustand';

import { createUserScopedSlot } from '@/shared/storage/userScopedSlot';

import type { BillingCycle, CartEntry } from './moduleStore';

interface PersistedCart {
  cycle: BillingCycle;
  entries: CartEntry[];
}

/**
 * Only module and plan ids are stored — never a price or a label. A cart can sit in localStorage
 * for weeks, so every displayed figure is re-read from the live catalog; persisting the price
 * would let a stale one reach both the screen and the order.
 */
const parseCart = (raw: unknown): PersistedCart | null => {
  if (typeof raw !== 'object' || raw === null) return null;
  const value = raw as Record<string, unknown>;
  const cycle = value.cycle === 'yearly' ? 'yearly' : 'monthly';
  if (!Array.isArray(value.entries)) return { cycle, entries: [] };
  const entries: CartEntry[] = [];
  for (const item of value.entries) {
    if (typeof item !== 'object' || item === null) continue;
    const row = item as Record<string, unknown>;
    if (typeof row.moduleId !== 'string' || typeof row.planId !== 'string') continue;
    if (entries.some((e) => e.moduleId === row.moduleId)) continue;
    entries.push({ moduleId: row.moduleId, planId: row.planId });
  }
  return { cycle, entries };
};

const slot = createUserScopedSlot<PersistedCart>({
  feature: 'cart',
  pageKey: 'billing',
  schema: parseCart,
});

interface CartState {
  cycle: BillingCycle;
  entries: CartEntry[];
  hydrated: boolean;
  hydrate: () => void;
  setCycle: (cycle: BillingCycle) => void;
  toggle: (moduleId: string, planId: string) => void;
  remove: (moduleId: string) => void;
  clear: () => void;
}

const persist = (cycle: BillingCycle, entries: CartEntry[]) => {
  slot.set({ cycle, entries });
};

export const useCartStore = create<CartState>((set, get) => ({
  cycle: 'monthly',
  entries: [],
  hydrated: false,
  hydrate: () => {
    if (get().hydrated) return;
    const stored = slot.get();
    set({ cycle: stored?.cycle ?? 'monthly', entries: stored?.entries ?? [], hydrated: true });
  },
  setCycle: (cycle) => {
    // Switching the cycle must re-point every line at the new plan, so the entries are dropped
    // rather than left holding plan ids from the other cycle.
    set({ cycle, entries: [] });
    persist(cycle, []);
  },
  toggle: (moduleId, planId) => {
    const { cycle, entries } = get();
    const next = entries.some((e) => e.moduleId === moduleId)
      ? entries.filter((e) => e.moduleId !== moduleId)
      : [...entries, { moduleId, planId }];
    set({ entries: next });
    persist(cycle, next);
  },
  remove: (moduleId) => {
    const { cycle, entries } = get();
    const next = entries.filter((e) => e.moduleId !== moduleId);
    set({ entries: next });
    persist(cycle, next);
  },
  clear: () => {
    const { cycle } = get();
    set({ entries: [] });
    persist(cycle, []);
  },
}));
