import { useEffect, type ReactNode } from 'react';
import axios from 'axios';
import { useAuthStore } from '@/shared/lib/store/authStore';
import { authApi } from '../api/authApi';
import { isApiError } from '@/shared/api/ApiError';

let bootstrapStarted = false;

const DEFINITIVE_AUTH_STATUSES = new Set([401, 403]);

const statusOf = (error: unknown): number | undefined => {
  if (isApiError(error)) return error.statusCode;
  if (axios.isAxiosError(error)) return error.response?.status;
  return undefined;
};

const restoreSession = async (): Promise<void> => {
  const { setAuth, clearAuth, setAuthReady } = useAuthStore.getState();
  try {
    const response = await authApi.refreshToken();
    if (response.isSuccess && response.data) {
      setAuth(response.data.accessToken, response.data.user);
      return;
    }
    clearAuth();
  } catch (error) {
    const status = statusOf(error);
    if (status !== undefined && DEFINITIVE_AUTH_STATUSES.has(status)) {
      clearAuth();
    }
  } finally {
    setAuthReady(true);
  }
};

export const AuthBootstrap = ({ children }: { children: ReactNode }) => {
  const authReady = useAuthStore((s) => s.authReady);

  useEffect(() => {
    if (authReady || bootstrapStarted) return;
    bootstrapStarted = true;
    void restoreSession();
  }, [authReady]);

  return <>{children}</>;
};
