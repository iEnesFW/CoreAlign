import { beforeEach, describe, expect, it, vi } from 'vitest';

let currentUser: { id: string } | null = { id: 'user-a' };

vi.mock('@/shared/lib/store/authStore', () => ({
  useAuthStore: { getState: () => ({ user: currentUser }) },
}));

import { createUserScopedSlot } from './userScopedSlot';

describe('createUserScopedSlot', () => {
  beforeEach(() => {
    window.localStorage.clear();
    currentUser = { id: 'user-a' };
  });

  it('scopes stored values per user id (no cross-user leak)', () => {
    const slot = createUserScopedSlot<{ n: number }>({ feature: 'columns', pageKey: 'products' });
    slot.set({ n: 1 });
    expect(slot.get()).toEqual({ n: 1 });

    currentUser = { id: 'user-b' };
    expect(slot.get()).toBeNull();

    slot.set({ n: 2 });
    expect(slot.get()).toEqual({ n: 2 });

    currentUser = { id: 'user-a' };
    expect(slot.get()).toEqual({ n: 1 });
  });

  it('falls back to anon and does not leak anon data to a logged-in user', () => {
    currentUser = null;
    const slot = createUserScopedSlot<{ n: number }>({ feature: 'columns', pageKey: 'products' });
    slot.set({ n: 9 });
    expect(slot.get()).toEqual({ n: 9 });

    currentUser = { id: 'user-a' };
    expect(slot.get()).toBeNull();
  });

  it('removes only the current user scope', () => {
    const slot = createUserScopedSlot<{ n: number }>({ feature: 'columns', pageKey: 'products' });
    slot.set({ n: 1 });

    currentUser = { id: 'user-b' };
    slot.set({ n: 2 });
    slot.remove();
    expect(slot.get()).toBeNull();

    currentUser = { id: 'user-a' };
    expect(slot.get()).toEqual({ n: 1 });
  });
});
