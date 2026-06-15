import { beforeEach, describe, expect, it } from 'vitest';
import { useAuthStore } from '@/features/auth/authStore';

const fakeUser = {
  id: 'u-2',
  tenantId: 't-1',
  tenantName: 'Tenant 1',
  tenantSlug: 'tenant-1',
  username: 'jane',
  email: 'jane@example.com',
  firstName: 'Jane',
  lastName: 'Doe',
  avatarUrl: null,
  roles: ['Dealer'],
  persona: 'dealer' as const,
};

describe('useAuthStore extra coverage', () => {
  beforeEach(() => {
    useAuthStore.getState().clearAuth();
  });

  it('restore rehydrates state when token is live', () => {
    const expires = new Date(Date.now() + 600_000).toISOString();
    window.localStorage.setItem('corealign.b2b.token', 'tok');
    window.localStorage.setItem('corealign.b2b.tokenExpires', expires);
    window.localStorage.setItem('corealign.b2b.user', JSON.stringify(fakeUser));
    useAuthStore.getState().restore();
    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(true);
    expect(state.user?.username).toBe('jane');
  });

  it('restore clears auth when token expires within 30 seconds', () => {
    const expires = new Date(Date.now() + 10_000).toISOString();
    window.localStorage.setItem('corealign.b2b.token', 'tok');
    window.localStorage.setItem('corealign.b2b.tokenExpires', expires);
    window.localStorage.setItem('corealign.b2b.user', JSON.stringify(fakeUser));
    useAuthStore.getState().restore();
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
  });

  it('restore returns unauthenticated when user JSON is missing', () => {
    const expires = new Date(Date.now() + 600_000).toISOString();
    window.localStorage.setItem('corealign.b2b.token', 'tok');
    window.localStorage.setItem('corealign.b2b.tokenExpires', expires);
    useAuthStore.getState().restore();
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
  });

  it('restore is resilient to corrupt JSON in user blob', () => {
    const expires = new Date(Date.now() + 600_000).toISOString();
    window.localStorage.setItem('corealign.b2b.token', 'tok');
    window.localStorage.setItem('corealign.b2b.tokenExpires', expires);
    window.localStorage.setItem('corealign.b2b.user', 'not-json');
    useAuthStore.getState().restore();
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
  });

  it('clearAuth flips isAuthenticated even when storage is empty', () => {
    useAuthStore.setState({
      accessToken: 't',
      expiresAtUtc: new Date().toISOString(),
      user: fakeUser,
      isAuthenticated: true,
    });
    useAuthStore.getState().clearAuth();
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
    expect(useAuthStore.getState().user).toBeNull();
  });

  it('setAuth persists JSON-serialized user', () => {
    const expires = new Date(Date.now() + 60_000).toISOString();
    useAuthStore.getState().setAuth('xyz', expires, fakeUser);
    const stored = window.localStorage.getItem('corealign.b2b.user');
    expect(stored).toContain('jane@example.com');
  });
});
