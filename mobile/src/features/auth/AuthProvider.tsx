import React, { useCallback, useEffect, useMemo } from 'react';
import { authApi, type LoginRequest } from '@/api/endpoints/auth';
import { TokenStorage, registerAuthFailureHandler, registerRefreshHandler } from '@/api/apiClient';
import { useAuthStore } from './authStore';
import { AuthContext, type AuthContextValue } from './authContext';

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user, isAuthenticated, isHydrated, setUser, setHydrated, reset } = useAuthStore();

  useEffect(() => {
    registerRefreshHandler(async (refreshToken) => {
      const session = await authApi.refresh(refreshToken);
      return { accessToken: session.accessToken, refreshToken: session.refreshToken };
    });
    registerAuthFailureHandler(() => {
      reset();
    });
  }, [reset]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const access = await TokenStorage.getAccess();
      if (!access) {
        if (!cancelled) setHydrated(true);
        return;
      }
      try {
        const me = await authApi.me();
        if (!cancelled) setUser(me);
      } catch {
        await TokenStorage.clear();
        if (!cancelled) reset();
      } finally {
        if (!cancelled) setHydrated(true);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [reset, setHydrated, setUser]);

  const login = useCallback(
    async (req: LoginRequest) => {
      const session = await authApi.login(req);
      await TokenStorage.setTokens(session.accessToken, session.refreshToken);
      const me = await authApi.me();
      setUser(me);
    },
    [setUser],
  );

  const logout = useCallback(async () => {
    try {
      await authApi.logout();
    } catch {
      // swallow logout errors; we clear locally regardless
    }
    await TokenStorage.clear();
    reset();
  }, [reset]);

  const refreshUser = useCallback(async () => {
    const me = await authApi.me();
    setUser(me);
  }, [setUser]);

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated, isHydrated, login, logout, refreshUser }),
    [user, isAuthenticated, isHydrated, login, logout, refreshUser],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};
