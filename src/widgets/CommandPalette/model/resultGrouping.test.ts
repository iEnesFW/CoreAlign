import { describe, expect, it } from 'vitest';
import { buildGroups, flattenGroups } from './resultGrouping';
import type { PaletteKind, PaletteResult } from './paletteTypes';

const make = (kind: PaletteKind, n: number): PaletteResult[] =>
  Array.from({ length: n }, (_, i) => ({
    id: `${kind}-${i}`,
    kind,
    label: `${kind} ${i}`,
    to: `/x/${i}`,
  }));

describe('buildGroups', () => {
  it('keeps the fixed group order and drops empty groups', () => {
    const groups = buildGroups({ product: make('product', 1), customer: make('customer', 1) });
    expect(groups.map((g) => g.kind)).toEqual(['customer', 'product']);
  });

  it('caps each group at capPerKind', () => {
    const groups = buildGroups({ order: make('order', 9) }, 5);
    expect(groups[0].results).toHaveLength(5);
  });

  it('returns an empty array when nothing matches', () => {
    expect(buildGroups({})).toEqual([]);
  });
});

describe('flattenGroups', () => {
  it('flattens in group order for deterministic keyboard navigation', () => {
    const groups = buildGroups({ customer: make('customer', 2), order: make('order', 2) });
    const flat = flattenGroups(groups);
    expect(flat.map((r) => r.id)).toEqual(['customer-0', 'customer-1', 'order-0', 'order-1']);
  });
});
