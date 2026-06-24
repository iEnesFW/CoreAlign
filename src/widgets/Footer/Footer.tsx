import React from 'react';
import { useTranslation } from 'react-i18next';

export const Footer: React.FC = () => {
  const { t } = useTranslation();
  const year = new Date().getFullYear();

  return (
    <footer className="z-10 mt-auto shrink-0 border-t border-slate-200/60 bg-white px-6 py-3 dark:border-slate-800/60 dark:bg-shell">
      <div className="flex flex-col items-center justify-between gap-1 text-xs text-slate-500 dark:text-slate-400 sm:flex-row">
        <p>
          &copy; {year} CoreAlign &mdash;{' '}
          {t('Footer.rights', { defaultValue: 'All rights reserved.' })}
        </p>
        <p className="font-medium text-slate-400 dark:text-slate-500">
          {t('Footer.tagline', { defaultValue: 'Multi-tenant ERP platform' })}
        </p>
      </div>
    </footer>
  );
};
