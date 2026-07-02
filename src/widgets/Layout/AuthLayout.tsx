import React from 'react';
import { useTranslation } from 'react-i18next';
import { Sun, Moon } from 'lucide-react';
import { useTheme } from '@/app/providers/themeContext';
import { Logo } from '@/shared/ui/Logo/Logo';
import { AuthShowcase } from './AuthShowcase';
import { AuthBackdrop } from '@/shared/ui/Background/AuthBackdrop';

interface AuthLayoutProps {
  children: React.ReactNode;
}

/**
 * AuthLayout — premium split-screen shell for every auth page (login / register
 * / forgot / reset / verify). Left = brand showcase (AuthShowcase, hidden below
 * `lg`). Right = the form slot (`children`) with language + theme toggles and a
 * live status badge. The `children` API is unchanged, so all existing auth
 * forms drop in as-is.
 *
 * The `.ca-marketing` class on the root enables the Sora display font for
 * headings, consistent with the public surface.
 */
export const AuthLayout: React.FC<AuthLayoutProps> = ({ children }) => {
  const { t, i18n } = useTranslation();
  const { theme, toggleTheme } = useTheme();
  const isEn = (i18n.language || '').toLowerCase().startsWith('en');

  const setLang = (lng: 'tr' | 'en') => {
    if ((lng === 'en') !== isEn) void i18n.changeLanguage(lng);
  };

  return (
    <div className="ca-marketing relative flex min-h-screen w-full bg-white text-slate-900 dark:bg-[#0a0e17] dark:text-slate-100">
      <AuthShowcase theme={theme} />

      {/* right — form panel */}
      <div className="relative flex flex-1 flex-col">
        {/* subtle backdrop for mobile (no showcase below lg) */}
        <div className="pointer-events-none absolute inset-0 overflow-hidden opacity-50 lg:hidden">
          <AuthBackdrop theme={theme} />
        </div>

        {/* top controls */}
        <div className="absolute inset-x-5 top-5 z-20 flex items-center justify-end gap-3">
          <div
            className="flex items-center gap-0.5 rounded-full border p-0.5 dark:border-white/10"
            style={{ borderColor: 'rgba(20,30,60,0.10)' }}
          >
            <button
              type="button"
              onClick={() => setLang('tr')}
              className={`rounded-full px-3 py-1 text-xs font-semibold transition-colors ${
                !isEn
                  ? 'bg-primary-600 text-white'
                  : 'text-slate-500 hover:text-slate-800 dark:text-slate-400 dark:hover:text-slate-100'
              }`}
            >
              TR
            </button>
            <button
              type="button"
              onClick={() => setLang('en')}
              className={`rounded-full px-3 py-1 text-xs font-semibold transition-colors ${
                isEn
                  ? 'bg-primary-600 text-white'
                  : 'text-slate-500 hover:text-slate-800 dark:text-slate-400 dark:hover:text-slate-100'
              }`}
            >
              EN
            </button>
          </div>

          <button
            type="button"
            onClick={toggleTheme}
            aria-label={t('AuthLayout.ToggleTheme', { defaultValue: 'Tema değiştir' })}
            className="grid h-9 w-9 place-items-center rounded-full border text-slate-500 transition-colors hover:bg-slate-100 hover:text-slate-800 dark:border-white/10 dark:text-slate-400 dark:hover:bg-white/5 dark:hover:text-white"
            style={{ borderColor: 'rgba(20,30,60,0.10)' }}
          >
            {theme === 'dark' ? <Sun size={17} /> : <Moon size={16} />}
          </button>

          <div
            className="hidden items-center gap-2 rounded-full border px-3 py-1.5 sm:flex dark:border-white/10"
            style={{ borderColor: 'rgba(20,30,60,0.10)' }}
          >
            <span className="h-[7px] w-[7px] animate-pulse rounded-full bg-success-500" />
            <span className="text-xs font-medium text-slate-500 dark:text-slate-400">
              {t('AuthLayout.AllSystemsOperational', { defaultValue: 'Tüm Sistemler Çalışıyor' })}
            </span>
          </div>
        </div>

        {/* form */}
        <div className="relative z-10 m-auto w-full max-w-[420px] px-6 py-24">
          <div className="mb-9 flex justify-center lg:hidden">
            <Logo size={34} />
          </div>
          {children}
        </div>

        {/* footer */}
        <div className="pointer-events-none absolute inset-x-0 bottom-5 z-10 text-center text-[11.5px] text-slate-400 dark:text-slate-500">
          {t('AuthLayout.Copyright', {
            defaultValue: '© {{year}} CoreAlign Inc. Tüm hakları saklıdır.',
            year: new Date().getFullYear(),
          })}
        </div>
      </div>
    </div>
  );
};
