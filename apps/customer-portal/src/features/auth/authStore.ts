import { create } from 'zustand';

const STORAGE_KEY_USER = 'corealign.portal.user';
const STORAGE_KEY_TOKEN = 'corealign.portal.token';
const STORAGE_KEY_EXPIRES = 'corealign.portal.tokenExpires';

export interface PortalUser {
  id: string;
  tenantId: string;
  tenantName: string;
  tenantSlug: string;
  username: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  avatarUrl: string | null;
  roles: string[];
  persona: 'tenant' | 'customer' | 'dealer';
}

interface AuthState {
  accessToken: string | null;
  expiresAtUtc: string | null;
  user: PortalUser | null;
  isAuthenticated: boolean;
  setAuth: (accessToken: string, expiresAtUtc: string, user: PortalUser) => void;
  clearAuth: () => void;
  restore: () => void;
}

const safeReadJson = <T>(key: string): T | null => {
  try {
    const raw = typeof window !== 'undefined' ? window.localStorage.getItem(key) : null;
    return raw ? (JSON.parse(raw) as T) : null;
  } catch {
    return null;
  }
};

const isTokenLive = (expiresAtUtc: string | null): boolean => {
  if (!expiresAtUtc) return false;
  const exp = Date.parse(expiresAtUtc);
  if (Number.isNaN(exp)) return false;
  return exp - Date.now() > 30_000;
};

const readInitial = (): {
  accessToken: string | null;
  expiresAtUtc: string | null;
  user: PortalUser | null;
} => {
  if (typeof window === 'undefined') {
    return { accessToken: null, expiresAtUtc: null, user: null };
  }
  const expiresAtUtc = window.localStorage.getItem(STORAGE_KEY_EXPIRES);
  const accessToken = window.localStorage.getItem(STORAGE_KEY_TOKEN);
  const user = safeReadJson<PortalUser>(STORAGE_KEY_USER);
  if (!user || !accessToken || !isTokenLive(expiresAtUtc)) {
    return { accessToken: null, expiresAtUtc: null, user: null };
  }
  return { accessToken, expiresAtUtc, user };
};

const initial = readInitial();

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: initial.accessToken,
  expiresAtUtc: initial.expiresAtUtc,
  user: initial.user,
  isAuthenticated: !!initial.user,

  setAuth: (accessToken, expiresAtUtc, user) => {
    if (typeof window !== 'undefined') {
      window.localStorage.setItem(STORAGE_KEY_USER, JSON.stringify(user));
      window.localStorage.setItem(STORAGE_KEY_TOKEN, accessToken);
      window.localStorage.setItem(STORAGE_KEY_EXPIRES, expiresAtUtc);
    }
    set({ accessToken, expiresAtUtc, user, isAuthenticated: true });
  },

  clearAuth: () => {
    if (typeof window !== 'undefined') {
      window.localStorage.removeItem(STORAGE_KEY_USER);
      window.localStorage.removeItem(STORAGE_KEY_TOKEN);
      window.localStorage.removeItem(STORAGE_KEY_EXPIRES);
    }
    set({ accessToken: null, expiresAtUtc: null, user: null, isAuthenticated: false });
  },

  restore: () => {
    const { accessToken, expiresAtUtc, user } = readInitial();
    set({ accessToken, expiresAtUtc, user, isAuthenticated: !!user });
  },
}));
