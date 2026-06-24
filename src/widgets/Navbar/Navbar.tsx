import React, { useState, useRef, useEffect, useMemo } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { Menu, Search, Sun, Moon, User, Sliders, LogOut, Command } from 'lucide-react';
import { useTheme } from '@/app/providers/themeContext';
import { useAuthStore } from '@/shared/lib/store/authStore';
import { useLogout } from '@/features/auth/hooks/useAuth';
import { useTranslation } from 'react-i18next';
import { CommandPalette } from '@/shared/ui/CommandPalette/CommandPalette';
import { NotificationBell } from '@/features/collaboration/ui/NotificationBell';
import { useCommandItems } from './commandItems';
import { LanguageSwitcher } from '@/widgets/LanguageSwitcher';
import { FxRateBadge } from '@/features/fx/ui/FxRateBadge';

interface NavbarProps {
  toggleSidebar: () => void;
}

const PAGE_TITLE_KEYS: Record<string, string> = {
  '': 'dashboard',
  dashboard: 'dashboard',
  customers: 'customers',
  orders: 'orders',
  invoices: 'invoices',
  vendors: 'vendors',
  products: 'products',
  inventory: 'inventory',
  mrp: 'mrp',
  reports: 'reports',
  settings: 'settings',
  accounting: 'accounting',
  warranty: 'warranty',
  installation: 'installation',
  purchasing: 'purchasing',
  activity: 'activity',
  profile: 'profile',
  admin: 'admin',
  marketplace: 'marketplace',
  'glass-enclosure': 'glassEnclosure',
};

const iconButton =
  'grid h-8 w-8 place-items-center rounded-lg text-slate-500 transition-colors hover:bg-slate-100 hover:text-slate-800 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500/50 dark:text-slate-400 dark:hover:bg-white/5 dark:hover:text-slate-100';

export const Navbar: React.FC<NavbarProps> = ({ toggleSidebar }) => {
  const { theme, toggleTheme } = useTheme();
  const location = useLocation();
  const navigate = useNavigate();
  const [isProfileOpen, setIsProfileOpen] = useState(false);
  const [paletteOpen, setPaletteOpen] = useState(false);
  const profileRef = useRef<HTMLDivElement>(null);

  const user = useAuthStore((state) => state.user);
  const logout = useLogout();
  const { t } = useTranslation();
  const commandItems = useCommandItems(navigate);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (profileRef.current && !profileRef.current.contains(event.target as Node)) {
        setIsProfileOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault();
        setPaletteOpen((v) => !v);
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, []);

  const pageTitle = useMemo(() => {
    const segment = location.pathname.replace(/^\/dashboard\/?/, '').split('/')[0] ?? '';
    const key = PAGE_TITLE_KEYS[segment];
    const fallback = segment
      ? segment.charAt(0).toUpperCase() + segment.slice(1).replace(/-/g, ' ')
      : t('Navbar.pageTitles.dashboard', { defaultValue: 'Dashboard' });
    return key ? t(`Navbar.pageTitles.${key}`, { defaultValue: fallback }) : fallback;
  }, [location.pathname, t]);

  const handleLogout = () => {
    logout.mutate();
  };

  const fallbackName = t('common.user', { defaultValue: 'User' });
  const initials =
    user && user.firstName && user.lastName
      ? `${user.firstName[0]}${user.lastName[0]}`.toUpperCase()
      : 'U';
  const name = user ? `${user.firstName || ''} ${user.lastName || ''}`.trim() : fallbackName;
  const subtitle = user?.tenantName ?? user?.roles?.[0] ?? fallbackName;

  return (
    <header className="sticky top-0 z-30 flex h-12 items-center justify-between gap-3 border-b border-slate-200/70 bg-white/70 px-3 backdrop-blur-xl transition-colors duration-200 supports-[backdrop-filter]:bg-white/60 sm:px-4 dark:border-white/5 dark:bg-shell/70 dark:supports-[backdrop-filter]:bg-shell/60">
      <div className="flex min-w-0 items-center gap-2.5">
        <button
          type="button"
          onClick={toggleSidebar}
          aria-label={t('Navbar.openMenu', { defaultValue: 'Open menu' })}
          className={`-ml-1 lg:hidden ${iconButton}`}
        >
          <Menu size={18} />
        </button>
        <div className="hidden min-w-0 items-center gap-2 sm:flex">
          <span
            aria-hidden="true"
            className="h-4 w-1 rounded-full bg-gradient-to-b from-primary-500 to-accent-500"
          />
          <h1 className="truncate text-[13px] font-semibold tracking-tight text-slate-900 dark:text-white">
            {pageTitle}
          </h1>
        </div>
      </div>

      <div className="hidden max-w-md flex-1 px-2 md:block">
        <button
          type="button"
          onClick={() => setPaletteOpen(true)}
          aria-label={t('Navbar.openSearch', { defaultValue: 'Search' })}
          className="group flex w-full items-center gap-2.5 rounded-xl border border-slate-200/80 bg-slate-50/70 py-1.5 pl-3 pr-2 text-left shadow-sm transition-all duration-200 hover:border-slate-300 hover:bg-white hover:shadow focus:outline-none focus-visible:border-primary-400 focus-visible:ring-2 focus-visible:ring-primary-500/25 dark:border-white/5 dark:bg-white/[0.04] dark:hover:border-white/10 dark:hover:bg-white/[0.06]"
        >
          <Search className="h-3.5 w-3.5 shrink-0 text-slate-400 transition-colors group-hover:text-primary-500" />
          <span className="flex-1 truncate text-xs text-slate-400 dark:text-slate-500">
            {t('common.search', { defaultValue: 'Search anything…' })}
          </span>
          <kbd className="hidden items-center gap-0.5 rounded-md border border-slate-200 bg-white px-1.5 py-0.5 text-[10px] font-medium text-slate-400 shadow-sm sm:inline-flex dark:border-white/10 dark:bg-white/5 dark:text-slate-400">
            <Command size={10} />K
          </kbd>
        </button>
      </div>

      <div className="flex items-center gap-1">
        <div className="mr-1 hidden lg:inline-flex">
          <FxRateBadge currencyCode="USD" />
        </div>

        <button
          type="button"
          onClick={() => setPaletteOpen(true)}
          aria-label={t('Navbar.openSearch', { defaultValue: 'Search' })}
          className={`md:hidden ${iconButton}`}
        >
          <Search size={16} />
        </button>

        <button
          type="button"
          onClick={toggleTheme}
          className={iconButton}
          aria-label={t('Navbar.toggleTheme', { defaultValue: 'Toggle theme' })}
        >
          {theme === 'dark' ? <Sun size={16} /> : <Moon size={16} />}
        </button>

        <NotificationBell />

        <div className="relative ml-1" ref={profileRef}>
          <button
            type="button"
            onClick={() => setIsProfileOpen(!isProfileOpen)}
            aria-haspopup="menu"
            aria-expanded={isProfileOpen}
            aria-label={t('Navbar.profileMenu', { defaultValue: 'Profile menu' })}
            className="flex items-center gap-2 rounded-xl border border-transparent p-1 pr-1.5 transition-all hover:border-slate-200/80 hover:bg-slate-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500/50 dark:hover:border-white/10 dark:hover:bg-white/5"
          >
            <span className="grid h-7 w-7 place-items-center rounded-lg bg-gradient-to-br from-primary-500 to-primary-700 text-[11px] font-semibold text-white shadow-sm shadow-primary-500/30 ring-1 ring-white/70 dark:ring-white/10">
              {initials}
            </span>
            <span className="hidden text-left leading-tight sm:block">
              <span className="block max-w-[120px] truncate text-xs font-semibold text-slate-700 dark:text-slate-100">
                {name}
              </span>
              <span className="block max-w-[120px] truncate text-[10px] font-medium text-slate-500 dark:text-slate-400">
                {subtitle}
              </span>
            </span>
          </button>

          {isProfileOpen && (
            <div
              role="menu"
              aria-label={t('Navbar.profileMenu', { defaultValue: 'Profile menu' })}
              className="animate-zoom-in absolute right-0 mt-2 w-56 origin-top-right overflow-hidden rounded-xl border border-slate-200/80 bg-white/95 shadow-xl shadow-slate-900/5 ring-1 ring-black/5 backdrop-blur-xl focus:outline-none dark:border-white/10 dark:bg-slate-900/95 dark:ring-white/5"
            >
              <div className="flex items-center gap-2.5 border-b border-slate-100 px-3 py-2.5 dark:border-white/5">
                <span className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-gradient-to-br from-primary-500 to-primary-700 text-xs font-semibold text-white shadow-sm shadow-primary-500/30">
                  {initials}
                </span>
                <span className="min-w-0">
                  <span className="block truncate text-xs font-semibold text-slate-900 dark:text-white">
                    {name}
                  </span>
                  <span className="block truncate text-[10px] text-slate-500 dark:text-slate-400">
                    {user?.email ?? subtitle}
                  </span>
                </span>
              </div>

              <div className="p-1">
                <Link
                  to="/dashboard/profile"
                  role="menuitem"
                  onClick={() => setIsProfileOpen(false)}
                  className="group flex items-center gap-2.5 rounded-lg px-2.5 py-2 text-[11px] font-medium text-slate-700 transition-colors hover:bg-primary-50 hover:text-primary-700 dark:text-slate-300 dark:hover:bg-primary-500/10 dark:hover:text-primary-300"
                >
                  <User className="h-3.5 w-3.5 text-slate-400 transition-colors group-hover:text-primary-500" />
                  {t('auth.profile')}
                </Link>
                <Link
                  to="/dashboard/activity"
                  role="menuitem"
                  onClick={() => setIsProfileOpen(false)}
                  className="group flex items-center gap-2.5 rounded-lg px-2.5 py-2 text-[11px] font-medium text-slate-700 transition-colors hover:bg-primary-50 hover:text-primary-700 dark:text-slate-300 dark:hover:bg-primary-500/10 dark:hover:text-primary-300"
                >
                  <Sliders className="h-3.5 w-3.5 text-slate-400 transition-colors group-hover:text-primary-500" />
                  {t('activity.title')}
                </Link>
                <div className="px-1.5 py-1">
                  <LanguageSwitcher variant="inline" />
                </div>
              </div>

              <div className="border-t border-slate-100 p-1 dark:border-white/5">
                <button
                  type="button"
                  role="menuitem"
                  onClick={handleLogout}
                  className="group flex w-full items-center gap-2.5 rounded-lg px-2.5 py-2 text-[11px] font-medium text-danger-600 transition-colors hover:bg-danger-50 dark:text-danger-400 dark:hover:bg-danger-500/10"
                >
                  <LogOut className="h-3.5 w-3.5 text-danger-500 transition-colors" />
                  {t('auth.sign_out')}
                </button>
              </div>
            </div>
          )}
        </div>
      </div>

      {paletteOpen && (
        <CommandPalette
          onClose={() => setPaletteOpen(false)}
          items={commandItems}
          placeholder={t('common.search', { defaultValue: 'Search pages and actions…' })}
        />
      )}
    </header>
  );
};
