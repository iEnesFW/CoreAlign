import { describe, it, expect, beforeEach } from 'vitest';
import { useAuthStore } from '@/features/auth/authStore';

const fakeUser = {
  id: 'u-1',
  tenantId: 't-1',
  tenantName: 'Tenant 1',
  tenantSlug: 'tenant-1',
  username: 'dealer',
  email: 'dealer@example.com',
  firstName: 'A',
  lastName: 'B',
  avatarUrl: null,
  roles: ['User'],
  persona: 'dealer' as const,
};

describe('b2b useAuthStore', () => {
  beforeEach(() => {
    useAuthStore.getState().clearAuth();
  });

  it('starts unauthenticated', () => {
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
  });

  it('setAuth persists into b2b namespace', () => {
    useAuthStore
      .getState()
      .setAuth('b2b-token', new Date(Date.now() + 3_600_000).toISOString(), fakeUser);
    expect(window.localStorage.getItem('corealign.b2b.token')).toBe('b2b-token');
  });

  it('clearAuth wipes b2b namespace', () => {
    useAuthStore
      .getState()
      .setAuth('b2b-token', new Date(Date.now() + 3_600_000).toISOString(), fakeUser);
    useAuthStore.getState().clearAuth();
    expect(window.localStorage.getItem('corealign.b2b.token')).toBeNull();
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
  });

  it('restore skips expired tokens', () => {
    window.localStorage.setItem('corealign.b2b.token', 'old');
    window.localStorage.setItem(
      'corealign.b2b.tokenExpires',
      new Date(Date.now() - 60_000).toISOString(),
    );
    window.localStorage.setItem('corealign.b2b.user', JSON.stringify(fakeUser));
    useAuthStore.getState().restore();
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
  });
});
