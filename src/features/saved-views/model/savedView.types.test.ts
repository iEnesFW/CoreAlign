import { describe, expect, it } from 'vitest';
import {
  addView,
  parseActiveViewId,
  parseSavedViews,
  removeViewFrom,
  renameViewIn,
  type SavedView,
} from './savedView.types';

const view = (id: string, name = `view-${id}`): SavedView => ({
  id,
  name,
  filters: { search: 'x' },
  sort: null,
  columnState: null,
});

describe('parseSavedViews', () => {
  it('parses a valid persisted list with sort and columnState', () => {
    const result = parseSavedViews([
      {
        id: 'v1',
        name: 'Aktif ürünler',
        filters: { statusFilter: 'Active' },
        sort: { key: 'price', dir: 'desc' },
        columnState: { order: ['product', 'price'], hidden: ['status'] },
      },
    ]);
    expect(result).toHaveLength(1);
    expect(result![0].sort).toEqual({ key: 'price', dir: 'desc' });
    expect(result![0].columnState).toEqual({ order: ['product', 'price'], hidden: ['status'] });
  });

  it('rejects non-array blobs', () => {
    expect(parseSavedViews({ id: 'v1' })).toBeNull();
    expect(parseSavedViews('nope')).toBeNull();
    expect(parseSavedViews(null)).toBeNull();
  });

  it('drops malformed items and sanitizes bad sort/filters', () => {
    const result = parseSavedViews([
      { id: '', name: 'bad' },
      { id: 'v1', name: '' },
      { id: 'v2', name: 'ok', filters: 'not-an-object', sort: { key: 'x', dir: 'sideways' } },
    ]);
    expect(result).toHaveLength(1);
    expect(result![0]).toEqual({
      id: 'v2',
      name: 'ok',
      filters: {},
      sort: null,
      columnState: null,
    });
  });
});

describe('parseActiveViewId', () => {
  it('accepts non-empty strings and rejects everything else', () => {
    expect(parseActiveViewId('v1')).toBe('v1');
    expect(parseActiveViewId('')).toBeNull();
    expect(parseActiveViewId(42)).toBeNull();
    expect(parseActiveViewId(null)).toBeNull();
  });
});

describe('view list helpers', () => {
  it('adds, renames and removes immutably', () => {
    const base = [view('a'), view('b')];
    const added = addView(base, view('c'));
    expect(added.map((v) => v.id)).toEqual(['a', 'b', 'c']);
    expect(base).toHaveLength(2);

    const renamed = renameViewIn(added, 'b', 'Yeni ad');
    expect(renamed.find((v) => v.id === 'b')!.name).toBe('Yeni ad');
    expect(renamed.find((v) => v.id === 'a')!.name).toBe('view-a');

    const removed = removeViewFrom(renamed, 'a');
    expect(removed.map((v) => v.id)).toEqual(['b', 'c']);
  });
});
