import { describe, expect, it } from 'vitest';
import type { DataTableColumn } from './DataTable';
import { applyColumnState, normalizeColumnState, parseColumnState } from './columnState';

const cols = (...keys: string[]): DataTableColumn<{ id: string }, string>[] =>
  keys.map((key) => ({ key, label: key.toUpperCase(), cell: () => null }));

describe('normalizeColumnState', () => {
  const all = ['a', 'b', 'c'] as const;

  it('keeps a valid order/hidden and appends nothing when complete', () => {
    const result = normalizeColumnState({ order: ['c', 'a', 'b'], hidden: ['b'] }, all);
    expect(result.order).toEqual(['c', 'a', 'b']);
    expect(result.hidden).toEqual(['b']);
  });

  it('drops stale keys and appends newly-added columns to the end', () => {
    const result = normalizeColumnState({ order: ['zzz', 'b'], hidden: ['gone'] }, all);
    expect(result.order).toEqual(['b', 'a', 'c']);
    expect(result.hidden).toEqual([]);
  });

  it('defaults to full order when state is null', () => {
    const result = normalizeColumnState(null, all);
    expect(result.order).toEqual(['a', 'b', 'c']);
    expect(result.hidden).toEqual([]);
  });
});

describe('applyColumnState', () => {
  it('reorders and hides columns', () => {
    const result = applyColumnState(cols('a', 'b', 'c'), { order: ['c', 'a', 'b'], hidden: ['a'] });
    expect(result.map((c) => c.key)).toEqual(['c', 'b']);
  });

  it('returns the original columns when state is undefined', () => {
    const result = applyColumnState(cols('a', 'b'), undefined);
    expect(result.map((c) => c.key)).toEqual(['a', 'b']);
  });

  it('appends columns missing from order (newly added) at the end', () => {
    const result = applyColumnState(cols('a', 'b', 'c'), { order: ['b'], hidden: [] });
    expect(result.map((c) => c.key)).toEqual(['b', 'a', 'c']);
  });

  it('never renders zero columns even if everything is hidden', () => {
    const result = applyColumnState(cols('a', 'b'), { order: ['a', 'b'], hidden: ['a', 'b'] });
    expect(result.map((c) => c.key)).toEqual(['a', 'b']);
  });
});

describe('parseColumnState', () => {
  it('parses a valid persisted blob', () => {
    expect(parseColumnState({ order: ['a'], hidden: ['b'] })).toEqual({
      order: ['a'],
      hidden: ['b'],
    });
  });

  it('rejects a blob missing arrays', () => {
    expect(parseColumnState({ order: 'a', hidden: [] })).toBeNull();
    expect(parseColumnState(null)).toBeNull();
    expect(parseColumnState(42)).toBeNull();
  });

  it('strips non-string entries defensively', () => {
    expect(parseColumnState({ order: ['a', 3, null], hidden: [true, 'b'] })).toEqual({
      order: ['a'],
      hidden: ['b'],
    });
  });
});
