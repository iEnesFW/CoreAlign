import { Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Sun, Moon } from 'lucide-react';
import { LanguageSwitcher } from '@/widgets/LanguageSwitcher';
import { Logo } from '@/shared/ui/Logo/Logo';

interface LandingNavProps {
  theme: 'light' | 'dark';
  toggleTheme: () => void;
}

export const LandingNav = ({ theme, toggleTheme }: LandingNavProps) => {
  const { t } = useTranslation();
  const location = useLocation();
  const isDark = theme === 'dark';

  const navItems = [
    { path: '/', label: t('LandingPage.nav.home') },
    { path: '/about', label: t('LandingPage.nav.about') },
    { path: '/solutions', label: t('LandingPage.nav.solutions') },
    { path: '/articles', label: t('LandingPage.nav.articles') },
    { path: '/contact', label: t('LandingPage.nav.contact') },
  ];

  return (
    <nav className="sticky top-3 z-50 mx-auto flex h-14 w-[94%] max-w-4xl items-center justify-between rounded-full border border-slate-200/40 bg-white/60 px-4 shadow-md backdrop-blur-xl dark:border-slate-800/40 dark:bg-slate-900/60 dark:shadow-none md:px-6">
      <div className="flex items-center gap-2">
        <Logo size={18} showText={false} />
        <span className="hidden sm:inline text-xs font-extrabold tracking-wider text-slate-900 dark:text-white">
          COREALIGN
        </span>
      </div>

      <div className="flex gap-0.5 md:gap-1.5">
        {navItems.map((item) => {
          const isActive = location.pathname === item.path;
          return (
            <Link
              key={item.path}
              to={item.path}
              className={`rounded-full px-2.5 py-1 text-[11px] font-semibold transition-all duration-300 md:px-3.5 md:py-1.5 md:text-xs ${
                isActive
                  ? 'bg-indigo-500/10 text-indigo-650 dark:bg-indigo-500/20 dark:text-indigo-400'
                  : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900 dark:text-slate-400 dark:hover:bg-slate-800/50 dark:hover:text-slate-200'
              }`}
            >
              {item.label}
            </Link>
          );
        })}
      </div>

      <div className="flex items-center gap-2 md:gap-3">
        <LanguageSwitcher variant="menu" className="scale-75 md:scale-90" />
        <button
          onClick={toggleTheme}
          className="rounded-full p-1.5 text-slate-500 transition hover:bg-slate-200/50 dark:text-slate-400 dark:hover:bg-slate-800/50 md:p-2"
          aria-label="Toggle theme"
        >
          {isDark ? <Sun size={14} /> : <Moon size={14} />}
        </button>
        <Link
          to="/login"
          className="rounded-full bg-indigo-600 px-3 py-1.5 text-[10px] font-bold text-white shadow-sm transition hover:bg-indigo-700 lg:hidden"
        >
          {t('LandingPage.nav.contact') === 'İletişim' ||
          t('LandingPage.nav.contact') === 'Bizimle İletişime Geçin'
            ? 'Giriş'
            : 'Login'}
        </Link>
      </div>
    </nav>
  );
};
