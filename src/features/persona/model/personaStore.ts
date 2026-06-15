import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';

export type UxComplexityMode = 'Simple' | 'Pro';

interface PersonaState {
  mode: UxComplexityMode;
  userOverride: UxComplexityMode | null;
  tenantDefault: UxComplexityMode;
  perScreenOverrides: Record<string, UxComplexityMode>;
  scopedUserId: string | null;
  setMode: (mode: UxComplexityMode | null) => void;
  setTenantDefault: (mode: UxComplexityMode) => void;
  setScreenOverride: (screenKey: string, mode: UxComplexityMode | null) => void;
  setPerScreenOverrides: (overrides: Record<string, UxComplexityMode>) => void;
  resetForNewUser: (userId: string | null) => void;
}

const BASE_STORAGE_KEY = 'corealign.persona';

const resolveStorageKey = (): string => {
  if (typeof window === 'undefined') return BASE_STORAGE_KEY;
  try {
    const raw = window.localStorage.getItem('user');
    if (!raw) return `${BASE_STORAGE_KEY}.anon`;
    const parsed = JSON.parse(raw) as { id?: string } | null;
    return parsed?.id ? `${BASE_STORAGE_KEY}.${parsed.id}` : `${BASE_STORAGE_KEY}.anon`;
  } catch {
    return `${BASE_STORAGE_KEY}.anon`;
  }
};

const initialPersonaState = {
  mode: 'Pro' as UxComplexityMode,
  userOverride: null as UxComplexityMode | null,
  tenantDefault: 'Pro' as UxComplexityMode,
  perScreenOverrides: {} as Record<string, UxComplexityMode>,
  scopedUserId: null as string | null,
};

export const usePersonaStore = create<PersonaState>()(
  persist(
    (set) => ({
      ...initialPersonaState,
      setMode: (mode) => {
        set((s) => ({
          userOverride: mode,
          mode: mode ?? s.tenantDefault,
        }));
      },
      setTenantDefault: (mode) => {
        set((s) => ({
          tenantDefault: mode,
          mode: s.userOverride ?? mode,
        }));
      },
      setScreenOverride: (screenKey, mode) => {
        set((s) => {
          const next = { ...s.perScreenOverrides };
          if (mode === null) {
            delete next[screenKey];
          } else {
            next[screenKey] = mode;
          }
          return { perScreenOverrides: next };
        });
      },
      setPerScreenOverrides: (overrides) => {
        set({ perScreenOverrides: overrides });
      },
      resetForNewUser: (userId) => {
        set({ ...initialPersonaState, scopedUserId: userId });
      },
    }),
    {
      name: resolveStorageKey(),
      storage: createJSONStorage(() => localStorage),
      partialize: (s) => ({
        userOverride: s.userOverride,
        mode: s.mode,
        perScreenOverrides: s.perScreenOverrides,
        scopedUserId: s.scopedUserId,
      }),
    },
  ),
);
