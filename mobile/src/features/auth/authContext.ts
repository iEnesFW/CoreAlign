import { createContext, useContext } from 'react';
import type { CurrentUser, LoginRequest } from '@/api/endpoints/auth';

export interface AuthContextValue {
  user: CurrentUser | null;
  isAuthenticated: boolean;
  isHydrated: boolean;
  login: (req: LoginRequest) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

export const useAuth = (): AuthContextValue => {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth must be used inside <AuthProvider>');
  }
  return ctx;
};
