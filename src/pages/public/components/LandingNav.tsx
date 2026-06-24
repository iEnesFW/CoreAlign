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

  const loginLabel =
    t('LandingPage.nav.contact') === 'İletişim' ||
    t('LandingPage.nav.contact') === 'Bizimle İletişime Geçin'
      ? 'Giriş'
      : 'Login';

  return (
    <nav className="sticky top-3 z-50 mx-auto flex h-14 w-[94%] max-w-4xl items-center justify-between gap-2 rounded-full border border-white/50 bg-white/70 px-3 shadow-lg shadow-slate-900/5 ring-1 ring-black/[0.03] backdrop-blur-xl md:px-5 dark:border-white/10 dark:bg-slate-900/70 dark:shadow-black/30 dark:ring-white/5">
      <Link to="/" className="flex items-center gap-2" aria-label="CoreAlign">
        <Logo size={20} showText={false} />
        <span className="hidden bg-gradient-to-r from-primary-600 via-primary-500 to-accent-500 bg-clip-text text-xs font-extrabold tracking-[0.14em] text-transparent sm:inline">
          COREALIGN
        </span>
      </Link>

      <div className="flex items-center gap-0.5 md:gap-1">
        {navItems.map((item) => {
          const isActive = location.pathname === item.path;
          return (
            <Link
              key={item.path}
              to={item.path}
              aria-current={isActive ? 'page' : undefined}
              className={`rounded-full px-2.5 py-1.5 text-[11px] font-semibold transition-all duration-200 md:px-3.5 md:text-xs ${
                isActive
                  ? 'bg-primary-500/10 text-primary-600 ring-1 ring-inset ring-primary-500/20 dark:bg-primary-500/15 dark:text-primary-300'
                  : 'text-slate-600 hover:bg-slate-900/5 hover:text-slate-900 dark:text-slate-400 dark:hover:bg-white/5 dark:hover:text-white'
              }`}
            >
              {item.label}
            </Link>
          );
        })}
      </div>

      <div className="flex items-center gap-1.5 md:gap-2">
        <LanguageSwitcher variant="menu" className="scale-75 md:scale-90" />
        <button
          type="button"
          onClick={toggleTheme}
          className="grid h-8 w-8 place-items-center rounded-full text-slate-500 transition-colors hover:bg-slate-900/5 hover:text-slate-800 dark:text-slate-400 dark:hover:bg-white/5 dark:hover:text-slate-100"
          aria-label={t('Navbar.toggleTheme', { defaultValue: 'Toggle theme' })}
        >
          {isDark ? <Sun size={15} /> : <Moon size={15} />}
        </button>
        <Link
          to="/login"
          className="rounded-full bg-gradient-to-r from-primary-600 to-primary-500 px-3.5 py-1.5 text-[11px] font-bold text-white shadow-sm shadow-primary-500/30 transition-all hover:-translate-y-px hover:shadow-md hover:shadow-primary-500/40 lg:hidden"
        >
          {loginLabel}
        </Link>
      </div>
    </nav>
  );
};
