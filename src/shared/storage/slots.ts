import { createStorageSlot } from './storage';

export interface AuthSlotShape {
  accessToken: string;
  refreshExpiresAt?: number;
}

export const authSlot = createStorageSlot<AuthSlotShape>({
  key: 'auth',
  schema: (raw) => {
    if (!raw || typeof raw !== 'object') return null;
    const r = raw as Record<string, unknown>;
    if (typeof r.accessToken !== 'string') return null;
    return {
      accessToken: r.accessToken,
      refreshExpiresAt: typeof r.refreshExpiresAt === 'number' ? r.refreshExpiresAt : undefined,
    };
  },
});

export const themeSlot = createStorageSlot<'light' | 'dark' | 'system'>({
  key: 'theme',
  schema: (raw) => (raw === 'light' || raw === 'dark' || raw === 'system' ? raw : null),
});

export interface RefreshLockShape {
  until: number;
}

export const refreshLockSlot = createStorageSlot<RefreshLockShape>({
  key: 'auth:refresh-lock',
  schema: (raw) => {
    if (!raw || typeof raw !== 'object') return null;
    const r = raw as Record<string, unknown>;
    if (typeof r.until !== 'number') return null;
    return { until: r.until };
  },
});
