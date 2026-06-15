import { describe, it, expect, beforeEach } from 'vitest';
import { useAuthStore } from '@/features/auth/authStore';

const fakeUser = {
  id: 'u-1',
  tenantId: 't-1',
  tenantName: 'Tenant 1',
  tenantSlug: 'tenant-1',
  username: 'user',
  email: 'user@example.com',
  firstName: 'A',
  lastName: 'B',
  avatarUrl: null,
  roles: ['User'],
  persona: 'customer' as const,
};

describe('useAuthStore', () => {
  beforeEach(() => {
    useAuthStore.getState().clearAuth();
  });

  it('starts unauthenticated', () => {
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
    expect(useAuthStore.getState().user).toBeNull();
  });

  it('setAuth marks the user authenticated and persists to localStorage', () => {
    const expires = new Date(Date.now() + 3_600_000).toISOString();
    useAuthStore.getState().setAuth('token-abc', expires, fakeUser);
    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(true);
    expect(state.user?.email).toBe('user@example.com');
    expect(window.localStorage.getItem('corealign.portal.token')).toBe('token-abc');
  });

  it('clearAuth wipes state and storage', () => {
    const expires = new Date(Date.now() + 3_600_000).toISOString();
    useAuthStore.getState().setAuth('token-abc', expires, fakeUser);
    useAuthStore.getState().clearAuth();
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
    expect(window.localStorage.getItem('corealign.portal.token')).toBeNull();
  });

  it('restore does not rehydrate when token is expired', () => {
    const expired = new Date(Date.now() - 60_000).toISOString();
    window.localStorage.setItem('corealign.portal.token', 'token-old');
    window.localStorage.setItem('corealign.portal.tokenExpires', expired);
    window.localStorage.setItem('corealign.portal.user', JSON.stringify(fakeUser));
    useAuthStore.getState().restore();
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
  });
});
