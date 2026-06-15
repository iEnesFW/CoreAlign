import { create } from 'zustand';
import type { CurrentUser } from '@/api/endpoints/auth';

interface AuthState {
  user: CurrentUser | null;
  isHydrated: boolean;
  isAuthenticated: boolean;
  setUser: (user: CurrentUser | null) => void;
  setHydrated: (value: boolean) => void;
  reset: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isHydrated: false,
  isAuthenticated: false,
  setUser: (user) => set({ user, isAuthenticated: user !== null }),
  setHydrated: (value) => set({ isHydrated: value }),
  reset: () => set({ user: null, isAuthenticated: false }),
}));
