import { useMutation } from '@tanstack/react-query';
import { apiClient } from '@/shared/api/apiClient';
import { useAuthStore } from '@/shared/lib/store/authStore';
import { resolveLocale } from '@/app/i18n/supportedLocales';

interface SetLocaleResponse {
  preferredLocale: string;
}

const STORAGE_KEY = 'corealign.lang';

export const useUpdateLocale = () => {
  const user = useAuthStore((s) => s.user);
  return useMutation({
    mutationKey: ['profile', 'locale'],
    mutationFn: async (locale: string) => {
      const normalized = resolveLocale(locale);
      const previous =
        typeof window !== 'undefined' ? window.localStorage.getItem(STORAGE_KEY) : null;
      if (typeof window !== 'undefined') {
        window.localStorage.setItem(STORAGE_KEY, normalized);
      }
      if (!user) {
        return { preferredLocale: normalized } as SetLocaleResponse;
      }
      try {
        const response = await apiClient.patch<{ data: SetLocaleResponse }>('/profile/locale', {
          locale: normalized,
        });
        return response.data?.data ?? { preferredLocale: normalized };
      } catch (err) {
        if (typeof window !== 'undefined') {
          if (previous) {
            window.localStorage.setItem(STORAGE_KEY, previous);
          } else {
            window.localStorage.removeItem(STORAGE_KEY);
          }
        }
        throw err;
      }
    },
  });
};
