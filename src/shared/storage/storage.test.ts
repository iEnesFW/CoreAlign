import { beforeEach, describe, expect, it } from 'vitest';
import { clearAllStorage, createStorageSlot, listStorageKeys } from './storage';

interface Pref {
  theme: 'light' | 'dark';
  size: number;
}

const prefSlot = createStorageSlot<Pref>({
  key: 'pref',
  schema: (raw) => {
    if (!raw || typeof raw !== 'object') return null;
    const r = raw as Record<string, unknown>;
    if (r.theme !== 'light' && r.theme !== 'dark') return null;
    if (typeof r.size !== 'number') return null;
    return { theme: r.theme, size: r.size };
  },
});

beforeEach(() => {
  clearAllStorage();
});

describe('createStorageSlot', () => {
  it('round-trips a typed value', () => {
    prefSlot.set({ theme: 'dark', size: 12 });
    expect(prefSlot.get()).toEqual({ theme: 'dark', size: 12 });
  });

  it('removes the value', () => {
    prefSlot.set({ theme: 'light', size: 10 });
    prefSlot.remove();
    expect(prefSlot.get()).toBeNull();
  });

  it('returns null when the schema rejects a malformed value', () => {
    window.localStorage.setItem('corealign:v1:pref', JSON.stringify({ theme: 'purple', size: 0 }));
    expect(prefSlot.get()).toBeNull();
  });

  it('lists known keys', () => {
    prefSlot.set({ theme: 'dark', size: 1 });
    expect(listStorageKeys()).toContain('pref');
  });
});
