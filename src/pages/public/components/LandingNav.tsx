import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Sun, Moon, Menu, X, ArrowRight } from 'lucide-react';
import { LanguageSwitcher } from '@/widgets/LanguageSwitcher';
import { Logo } from '@/shared/ui/Logo/Logo';

interface LandingNavProps {
  theme: 'light' | 'dark';
  toggleTheme: () => void;
}

export const LandingNav = ({ theme, toggleTheme }: LandingNavProps) => {
  const { t } = useTranslation();
  const location = useLocation();
  const [open, setOpen] = useState(false);
  const isDark = theme === 'dark';

  const isEn = location.pathname === '/en' || location.pathname.startsWith('/en/');
  const prefix = isEn ? '/en' : '';
  const basePath = isEn ? location.pathname.replace(/^\/en/, '') || '/' : location.pathname;

  const navItems = [
    { path: '/', label: t('LandingPage.nav.home') },
    { path: '/solutions', label: t('LandingPage.nav.solutions') },
    { path: '/about', label: t('LandingPage.nav.about') },
    { path: '/articles', label: t('LandingPage.nav.articles') },
    { path: '/contact', label: t('LandingPage.nav.contact') },
  ];

  const linkTo = (p: string) => (p === '/' ? prefix || '/' : `${prefix}${p}`);

  return (
    <div className="sticky top-3 z-50 px-3 sm:top-4 sm:px-5">
      <nav className="mx-auto flex h-14 w-full max-w-7xl items-center justify-between gap-2 rounded-2xl border border-white/60 bg-white/75 px-3 shadow-lg shadow-slate-900/5 ring-1 ring-black/[0.03] backdrop-blur-xl md:px-4 dark:border-white/10 dark:bg-slate-900/70 dark:shadow-black/40 dark:ring-white/5">
        <Link to={linkTo('/')} className="flex shrink-0 items-center" aria-label="CoreAlign">
          <Logo size={28} showText />
        </Link>

        <div className="hidden items-center gap-0.5 lg:flex">
          {navItems.map((item) => {
            const isActive = basePath === item.path;
            return (
              <Link
                key={item.path}
                to={linkTo(item.path)}
                aria-current={isActive ? 'page' : undefined}
                className={`rounded-full px-3.5 py-1.5 text-[13px] font-semibold transition-all duration-200 ${
                  isActive
                    ? 'bg-primary-500/10 text-primary-600 ring-1 ring-inset ring-primary-500/20 dark:bg-primary-500/15 dark:text-primary-300'
                    : 'text-slate-600 hover:bg-slate-900/5 hover:text-slate-900 dark:text-slate-300 dark:hover:bg-white/5 dark:hover:text-white'
                }`}
              >
                {item.label}
              </Link>
            );
          })}
        </div>

        <div className="flex items-center gap-1.5">
          <LanguageSwitcher variant="menu" className="hidden scale-90 sm:block" />
          <button
            type="button"
            onClick={toggleTheme}
            className="grid h-9 w-9 place-items-center rounded-full text-slate-500 transition-colors hover:bg-slate-900/5 hover:text-slate-800 dark:text-slate-400 dark:hover:bg-white/5 dark:hover:text-slate-100"
            aria-label={t('Navbar.toggleTheme', { defaultValue: 'Toggle theme' })}
          >
            {isDark ? <Sun size={16} /> : <Moon size={16} />}
          </button>
          <Link
            to="/login"
            className="hidden rounded-full px-3.5 py-1.5 text-[13px] font-semibold text-slate-700 transition-colors hover:text-primary-600 sm:inline-flex dark:text-slate-200 dark:hover:text-primary-300"
          >
            {t('LandingPage.nav.login')}
          </Link>
          <a
            href={`${prefix || ''}/#demo`}
            className="group hidden items-center gap-1.5 rounded-full bg-gradient-to-r from-primary-600 to-primary-500 px-4 py-1.5 text-[13px] font-bold text-white shadow-sm shadow-primary-500/30 transition-all hover:-translate-y-px hover:shadow-md hover:shadow-primary-500/40 sm:inline-flex"
          >
            {t('LandingPage.nav.demo')}
            <ArrowRight size={14} className="transition-transform group-hover:translate-x-0.5" />
          </a>
          <button
            type="button"
            onClick={() => setOpen((v) => !v)}
            className="grid h-9 w-9 place-items-center rounded-full text-slate-600 transition-colors hover:bg-slate-900/5 lg:hidden dark:text-slate-300 dark:hover:bg-white/5"
            aria-label={open ? t('Navbar.closeMenu') : t('Navbar.openMenu')}
            aria-expanded={open}
          >
            {open ? <X size={18} /> : <Menu size={18} />}
          </button>
        </div>
      </nav>

      {open && (
        <div className="animate-fade-up mx-auto mt-2 w-full max-w-7xl overflow-hidden rounded-2xl border border-white/60 bg-white/90 p-2 shadow-xl backdrop-blur-xl lg:hidden dark:border-white/10 dark:bg-slate-900/90">
          {navItems.map((item) => {
            const isActive = basePath === item.path;
            return (
              <Link
                key={item.path}
                to={linkTo(item.path)}
                onClick={() => setOpen(false)}
                className={`block rounded-xl px-4 py-3 text-sm font-semibold transition-colors ${
                  isActive
                    ? 'bg-primary-500/10 text-primary-600 dark:text-primary-300'
                    : 'text-slate-700 hover:bg-slate-900/5 dark:text-slate-200 dark:hover:bg-white/5'
                }`}
              >
                {item.label}
              </Link>
            );
          })}
          <div className="mt-1 flex items-center gap-2 border-t border-slate-200/70 p-2 dark:border-slate-700/60">
            <Link
              to="/login"
              onClick={() => setOpen(false)}
              className="flex-1 rounded-xl border border-slate-200 px-4 py-2.5 text-center text-sm font-semibold text-slate-700 dark:border-slate-700 dark:text-slate-200"
            >
              {t('LandingPage.nav.login')}
            </Link>
            <a
              href={`${prefix || ''}/#demo`}
              onClick={() => setOpen(false)}
              className="flex-1 rounded-xl bg-primary-600 px-4 py-2.5 text-center text-sm font-bold text-white"
            >
              {t('LandingPage.nav.demo')}
            </a>
          </div>
        </div>
      )}
    </div>
  );
};
