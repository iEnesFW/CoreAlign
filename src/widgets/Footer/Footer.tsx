import React from 'react';
import { MessageCircle, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';
import { useAiHelperStore } from '@/shared/lib/store/aiHelperStore';

export const Footer: React.FC = () => {
  const { t } = useTranslation();
  const year = new Date().getFullYear();
  const isOpen = useAiHelperStore((state) => state.isOpen);
  const isAvailable = useAiHelperStore((state) => state.isAvailable);
  const toggle = useAiHelperStore((state) => state.toggle);

  return (
    <footer className="z-10 mt-auto shrink-0 border-t border-slate-200/60 bg-white px-6 py-3 dark:border-slate-800/60 dark:bg-shell">
      <div className="flex flex-col items-center justify-between gap-1 text-xs text-slate-500 dark:text-slate-400 sm:flex-row">
        <p>
          &copy; {year} CoreAlign &mdash; {t('Footer.rights')}
        </p>
        <div className="flex items-center gap-3">
          <p className="font-medium text-slate-400 dark:text-slate-500">{t('Footer.tagline')}</p>
          {isAvailable && (
            <button
              type="button"
              onClick={toggle}
              aria-label={t('AiHelper.Launcher')}
              aria-expanded={isOpen}
              className={cn(
                'inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-medium transition-colors',
                'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-1',
                'dark:focus-visible:ring-offset-slate-900',
                isOpen
                  ? 'border-primary-600 bg-primary-600 text-white hover:bg-primary-700'
                  : 'border-slate-200 text-slate-600 hover:border-primary-300 hover:text-primary-700 dark:border-slate-700 dark:text-slate-300 dark:hover:border-primary-500 dark:hover:text-primary-300',
              )}
            >
              {isOpen ? <X className="h-3.5 w-3.5" /> : <MessageCircle className="h-3.5 w-3.5" />}
              <span>{t('AiHelper.Launcher')}</span>
            </button>
          )}
        </div>
      </div>
    </footer>
  );
};
