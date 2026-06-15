import { create } from 'zustand';
import { clearHttpCache, setCacheNamespace } from '@/shared/http/httpCache';
import { setLoggerContext } from '@/shared/lib/logger';
import { invalidateTenantScopedCaches } from '@/shared/lib/pwaCache';
import type { UserProfile } from './auth.types';

const namespaceFromUser = (user: UserProfile | null): string =>
  user ? `${user.tenantId}:${user.id}` : 'anon';

const applyLoggerContext = (user: UserProfile | null): void => {
  setLoggerContext({
    userId: user?.id ?? null,
    tenantId: user?.tenantId ?? null,
  });
};

const initialUser: UserProfile | null = (() => {
  try {
    return JSON.parse(localStorage.getItem('user') || 'null') as UserProfile | null;
  } catch {
    return null;
  }
})();
setCacheNamespace(namespaceFromUser(initialUser));
applyLoggerContext(initialUser);

interface AuthState {
  accessToken: string | null;
  user: UserProfile | null;
  isAuthenticated: boolean;
  authReady: boolean;
  setAuth: (accessToken: string, user: UserProfile) => void;
  clearAuth: () => void;
  updateUser: (user: Partial<UserProfile>) => void;
  setAccessToken: (token: string) => void;
  setAuthReady: (ready: boolean) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  user: initialUser,
  isAuthenticated: !!initialUser,
  authReady: !initialUser,

  setAuth: (accessToken, user) => {
    const previousUser = JSON.parse(localStorage.getItem('user') || 'null') as UserProfile | null;
    const tenantChanged = previousUser !== null && previousUser.tenantId !== user.tenantId;
    if (tenantChanged) {
      void invalidateTenantScopedCaches();
    }
    localStorage.setItem('user', JSON.stringify(user));
    setCacheNamespace(namespaceFromUser(user));
    applyLoggerContext(user);
    set({ accessToken, user, isAuthenticated: true, authReady: true });
  },

  clearAuth: () => {
    localStorage.removeItem('user');
    clearHttpCache();
    void invalidateTenantScopedCaches();
    setCacheNamespace('anon');
    applyLoggerContext(null);
    set({ accessToken: null, user: null, isAuthenticated: false, authReady: true });
  },

  updateUser: (userData) =>
    set((state) => {
      const updatedUser = state.user ? { ...state.user, ...userData } : null;
      if (updatedUser) {
        localStorage.setItem('user', JSON.stringify(updatedUser));
      }
      return { user: updatedUser };
    }),

  setAccessToken: (token) => set({ accessToken: token }),

  setAuthReady: (ready) => set({ authReady: ready }),
}));
