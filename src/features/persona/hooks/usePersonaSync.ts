import { useEffect } from 'react';
import { usePersonaStore, type UxComplexityMode } from '@/shared/lib/persona';
import { personaApi } from '../api/personaApi';
import { useAuthStore } from '@/shared/lib/store/authStore';
import { safeRequest } from '@/shared/lib/safeRequest';
import { logger } from '@/shared/lib/logger';

const parsePerScreenOverrides = (json: string | null): Record<string, UxComplexityMode> => {
  if (!json) return {};
  const [parsed, error] = ((): [Record<string, UxComplexityMode> | null, Error | null] => {
    try {
      const value = JSON.parse(json) as unknown;
      if (value && typeof value === 'object' && !Array.isArray(value)) {
        const map: Record<string, UxComplexityMode> = {};
        for (const [key, raw] of Object.entries(value as Record<string, unknown>)) {
          if (raw === 'Simple' || raw === 'Pro') map[key] = raw;
        }
        return [map, null];
      }
      return [{}, null];
    } catch (e) {
      return [null, e as Error];
    }
  })();
  if (error) {
    logger.warn('persona-per-screen-parse-failed', { error: error.message });
    return {};
  }
  return parsed ?? {};
};

export const usePersonaSync = (): void => {
  const isAuth = useAuthStore((s) => s.isAuthenticated);
  const userId = useAuthStore((s) => s.user?.id ?? null);
  const setMode = usePersonaStore((s) => s.setMode);
  const setTenantDefault = usePersonaStore((s) => s.setTenantDefault);
  const setPerScreenOverrides = usePersonaStore((s) => s.setPerScreenOverrides);
  const resetForNewUser = usePersonaStore((s) => s.resetForNewUser);

  useEffect(() => {
    const scopedUserId = usePersonaStore.getState().scopedUserId;
    if (scopedUserId !== userId) {
      resetForNewUser(userId);
    }
  }, [userId, resetForNewUser]);

  useEffect(() => {
    if (!isAuth) return;
    let cancelled = false;
    void (async () => {
      const [response, error] = await safeRequest(personaApi.getMine());
      if (cancelled) return;
      if (error) {
        logger.warn('persona-sync-failed', { error: error.message });
        return;
      }
      if (!response?.data) return;
      setTenantDefault(response.data.tenantDefault);
      if (response.data.userOverride) setMode(response.data.userOverride);
      const overrides = parsePerScreenOverrides(response.data.perScreenOverridesJson);
      setPerScreenOverrides(overrides);
    })();
    return () => {
      cancelled = true;
    };
  }, [isAuth, setMode, setTenantDefault, setPerScreenOverrides]);
};

let debounceHandle: ReturnType<typeof setTimeout> | null = null;
const DEBOUNCE_MS = 600;

export const syncPerScreenOverridesDebounced = (
  overrides: Record<string, UxComplexityMode>,
): void => {
  if (debounceHandle) clearTimeout(debounceHandle);
  debounceHandle = setTimeout(() => {
    debounceHandle = null;
    void (async () => {
      const json = Object.keys(overrides).length === 0 ? null : JSON.stringify(overrides);
      const [, error] = await safeRequest(personaApi.update({ perScreenOverridesJson: json }));
      if (error) logger.warn('persona-per-screen-sync-failed', { error: error.message });
    })();
  }, DEBOUNCE_MS);
};
