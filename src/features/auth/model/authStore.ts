import { create } from 'zustand';
import { clearHttpCache, setCacheNamespace } from '@/shared/http/httpCache';
import type { UserProfile } from './auth.types';

const namespaceFromUser = (user: UserProfile | null): string =>
  user ? `${user.tenantId}:${user.id}` : 'anon';

const initialUser: UserProfile | null = (() => {
  try {
    return JSON.parse(localStorage.getItem('user') || 'null') as UserProfile | null;
  } catch {
    return null;
  }
})();
setCacheNamespace(namespaceFromUser(initialUser));

interface AuthState {
  accessToken: string | null;
  user: UserProfile | null;
  isAuthenticated: boolean;
  setAuth: (accessToken: string, user: UserProfile) => void;
  clearAuth: () => void;
  updateUser: (user: Partial<UserProfile>) => void;
  setAccessToken: (token: string) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  user: initialUser,
  isAuthenticated: !!initialUser,

  setAuth: (accessToken, user) => {
    localStorage.setItem('user', JSON.stringify(user));
    setCacheNamespace(namespaceFromUser(user));
    set({ accessToken, user, isAuthenticated: true });
  },

  clearAuth: () => {
    localStorage.removeItem('user');
    clearHttpCache();
    setCacheNamespace('anon');
    set({ accessToken: null, user: null, isAuthenticated: false });
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
}));
