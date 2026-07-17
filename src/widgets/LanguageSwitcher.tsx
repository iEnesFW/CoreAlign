import { useState, useRef, useEffect, useCallback } from 'react';
import { Globe, Check } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { SUPPORTED_LOCALES, type LocaleDescriptor } from '@/app/i18n/supportedLocales';
import { changeI18nLanguage } from '@/app/i18n/config';
import { useUpdateLocale } from '@/features/auth/hooks/useUpdateLocale';

interface LanguageSwitcherProps {
  variant?: 'menu' | 'inline';
  className?: string;
}

export const LanguageSwitcher = ({ variant = 'menu', className }: LanguageSwitcherProps) => {
  const { t, i18n } = useTranslation();
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const updateLocale = useUpdateLocale();

  useEffect(() => {
    const onClick = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', onClick);
    return () => document.removeEventListener('mousedown', onClick);
  }, []);

  const handleSelect = useCallback(
    (locale: LocaleDescriptor) => {
      void changeI18nLanguage(locale.code);
      updateLocale.mutate(locale.code);
      setIsOpen(false);
    },
    [updateLocale],
  );

  const currentCode = (i18n.language ?? 'en').slice(0, 2).toLowerCase();
  const current = SUPPORTED_LOCALES.find((l) => l.code === currentCode) ?? SUPPORTED_LOCALES[0];

  if (variant === 'inline') {
    return (
      <div className={className} data-testid="language-switcher-inline">
        <label className="block text-[11px] font-medium text-slate-700 dark:text-slate-300 mb-1">
          {t('Locale.label', { defaultValue: 'Language' })}
        </label>
        <select
          aria-label={t('Locale.label', { defaultValue: 'Language' })}
          value={current.code}
          onChange={(e) => {
            const next = SUPPORTED_LOCALES.find((l) => l.code === e.target.value);
            if (next) handleSelect(next);
          }}
          className="w-full px-2 py-1.5 rounded-[5px] border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 text-xs"
        >
          {SUPPORTED_LOCALES.map((locale) => (
            <option key={locale.code} value={locale.code}>
              {locale.nativeLabel}
            </option>
          ))}
        </select>
      </div>
    );
  }

  return (
    <div
      ref={containerRef}
      className={`relative ${className ?? ''}`}
      data-testid="language-switcher"
    >
      <button
        type="button"
        onClick={() => setIsOpen((v) => !v)}
        aria-haspopup="listbox"
        aria-expanded={isOpen}
        aria-label={t('Locale.label', { defaultValue: 'Language' })}
        className="flex items-center gap-1.5 px-2 py-1.5 rounded-[5px] text-[11px] font-medium text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
      >
        <Globe className="h-3.5 w-3.5 text-slate-500" />
        <span className="uppercase tracking-wide">{current.code}</span>
      </button>
      {isOpen && (
        <ul
          role="listbox"
          className="absolute right-0 mt-2 min-w-[160px] rounded-[5px] shadow-lg bg-white dark:bg-slate-800 ring-1 ring-slate-200 dark:ring-slate-700 py-1 z-40"
        >
          {SUPPORTED_LOCALES.map((locale) => {
            const isActive = locale.code === current.code;
            return (
              <li key={locale.code}>
                <button
                  type="button"
                  role="option"
                  aria-selected={isActive}
                  onClick={() => handleSelect(locale)}
                  className="w-full flex items-center justify-between px-3 py-1.5 text-[11px] font-medium text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-700/50"
                >
                  <span className="flex items-center gap-2">
                    <span className="uppercase text-[9px] font-bold bg-slate-100 dark:bg-slate-700 text-slate-500 dark:text-slate-400 px-1.5 py-0.5 rounded-[3px]">
                      {locale.code}
                    </span>
                    <span>{locale.nativeLabel}</span>
                  </span>
                  {isActive && <Check className="h-3.5 w-3.5 text-primary-500" />}
                </button>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
};
