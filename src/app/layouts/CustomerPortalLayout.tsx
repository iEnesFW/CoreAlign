import React, { Suspense, useCallback, useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Bell,
  CreditCard,
  FileText,
  Home,
  LogOut,
  Menu,
  Receipt,
  Settings,
  ShieldCheck,
  Wrench,
  X,
  Layers,
} from 'lucide-react';
import { useAuthStore } from '@/shared/lib/store/authStore';
import { RouteFallback } from '@/shared/ui/RouteFallback/RouteFallback';

interface NavItem {
  to: string;
  labelKey: string;
  icon: React.ComponentType<{ size?: number; className?: string }>;
}

const navItems: NavItem[] = [
  { to: '/customer-portal', labelKey: 'CustomerPortal.Nav.Dashboard', icon: Home },
  { to: '/customer-portal/projects', labelKey: 'CustomerPortal.Nav.Projects', icon: Layers },
  { to: '/customer-portal/warranties', labelKey: 'CustomerPortal.Nav.Warranty', icon: ShieldCheck },
  {
    to: '/customer-portal/service-tickets',
    labelKey: 'CustomerPortal.Nav.ServiceTickets',
    icon: Wrench,
  },
  { to: '/customer-portal/invoices', labelKey: 'CustomerPortal.Nav.Invoices', icon: FileText },
  { to: '/customer-portal/payments', labelKey: 'CustomerPortal.Nav.Payments', icon: CreditCard },
  { to: '/customer-portal/profile', labelKey: 'CustomerPortal.Nav.Profile', icon: Settings },
];

const mobileBottomItems: NavItem[] = [
  { to: '/customer-portal', labelKey: 'CustomerPortal.Nav.Dashboard', icon: Home },
  { to: '/customer-portal/projects', labelKey: 'CustomerPortal.Nav.Projects', icon: Layers },
  { to: '/customer-portal/invoices', labelKey: 'CustomerPortal.Nav.Invoices', icon: Receipt },
  { to: '/customer-portal/payments', labelKey: 'CustomerPortal.Nav.Payments', icon: CreditCard },
];

export const CustomerPortalLayout: React.FC = () => {
  const { t } = useTranslation();
  const user = useAuthStore((s) => s.user);
  const clearAuth = useAuthStore((s) => s.clearAuth);
  const navigate = useNavigate();
  const [isSideOpen, setIsSideOpen] = useState(false);

  const handleLogout = useCallback(() => {
    clearAuth();
    navigate('/login', { replace: true });
  }, [clearAuth, navigate]);

  const closeSide = useCallback(() => setIsSideOpen(false), []);

  const displayName = [user?.firstName, user?.lastName].filter(Boolean).join(' ') || user?.email;

  return (
    <div className="flex flex-col min-h-screen bg-slate-50 dark:bg-slate-950 text-slate-900 dark:text-slate-100">
      <header className="sticky top-0 z-30 flex items-center justify-between gap-3 px-3 sm:px-6 h-14 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 shadow-sm">
        <div className="flex items-center gap-2">
          <button
            type="button"
            className="lg:hidden p-2 -ml-2 rounded-md hover:bg-slate-100 dark:hover:bg-slate-800"
            onClick={() => setIsSideOpen(true)}
            aria-label={t('CustomerPortal.Common.OpenMenu')}
          >
            <Menu size={20} />
          </button>
          <div className="font-semibold tracking-tight">
            {t('CustomerPortal.Common.PortalTitle')}
          </div>
        </div>
        <div className="flex items-center gap-2 sm:gap-3">
          <button
            type="button"
            className="relative p-2 rounded-md hover:bg-slate-100 dark:hover:bg-slate-800"
            aria-label={t('CustomerPortal.Common.Notifications')}
          >
            <Bell size={18} />
          </button>
          <div className="hidden sm:flex items-center gap-2 px-2 py-1 rounded-md bg-slate-100 dark:bg-slate-800 text-sm">
            <span className="font-medium truncate max-w-[14rem]" title={displayName ?? ''}>
              {displayName}
            </span>
          </div>
          <button
            type="button"
            onClick={handleLogout}
            className="flex items-center gap-1.5 px-2.5 py-1.5 text-sm rounded-md text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800"
            aria-label={t('CustomerPortal.Common.Logout')}
          >
            <LogOut size={16} />
            <span className="hidden sm:inline">{t('CustomerPortal.Common.Logout')}</span>
          </button>
        </div>
      </header>

      <div className="flex flex-1 min-h-0">
        <aside className="hidden lg:flex flex-col w-60 shrink-0 border-r border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900">
          <nav className="flex-1 p-3 space-y-1 overflow-y-auto">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === '/customer-portal'}
                className={({ isActive }) =>
                  `flex items-center gap-2.5 px-3 py-2 rounded-md text-sm transition-colors ${
                    isActive
                      ? 'bg-primary-600 text-white'
                      : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'
                  }`
                }
              >
                <item.icon size={18} />
                <span className="truncate">{t(item.labelKey)}</span>
              </NavLink>
            ))}
          </nav>
        </aside>

        {isSideOpen ? (
          <div className="lg:hidden fixed inset-0 z-40">
            <div
              className="absolute inset-0 bg-black/40"
              onClick={closeSide}
              role="presentation"
              aria-hidden
            />
            <aside className="absolute inset-y-0 left-0 w-64 bg-white dark:bg-slate-900 shadow-xl flex flex-col">
              <div className="flex items-center justify-between h-14 px-4 border-b border-slate-200 dark:border-slate-800">
                <div className="font-semibold">{t('CustomerPortal.Common.PortalTitle')}</div>
                <button
                  type="button"
                  onClick={closeSide}
                  className="p-2 rounded-md hover:bg-slate-100 dark:hover:bg-slate-800"
                  aria-label={t('CustomerPortal.Common.CloseMenu')}
                >
                  <X size={18} />
                </button>
              </div>
              <nav className="flex-1 overflow-y-auto p-3 space-y-1">
                {navItems.map((item) => (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    end={item.to === '/customer-portal'}
                    onClick={closeSide}
                    className={({ isActive }) =>
                      `flex items-center gap-2.5 px-3 py-2 rounded-md text-sm ${
                        isActive
                          ? 'bg-primary-600 text-white'
                          : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'
                      }`
                    }
                  >
                    <item.icon size={18} />
                    <span className="truncate">{t(item.labelKey)}</span>
                  </NavLink>
                ))}
              </nav>
            </aside>
          </div>
        ) : null}

        <main className="flex-1 min-w-0 overflow-y-auto pb-20 lg:pb-6">
          <div className="mx-auto w-full max-w-6xl px-3 sm:px-4 lg:px-6 py-4 lg:py-6">
            <Suspense fallback={<RouteFallback />}>
              <Outlet />
            </Suspense>
          </div>
        </main>
      </div>

      <nav className="lg:hidden fixed bottom-0 inset-x-0 z-30 bg-white dark:bg-slate-900 border-t border-slate-200 dark:border-slate-800 grid grid-cols-4">
        {mobileBottomItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.to === '/customer-portal'}
            className={({ isActive }) =>
              `flex flex-col items-center justify-center gap-0.5 py-2 text-[11px] ${
                isActive ? 'text-primary-600' : 'text-slate-600 dark:text-slate-400'
              }`
            }
          >
            <item.icon size={20} />
            <span className="truncate max-w-[5rem]">{t(item.labelKey)}</span>
          </NavLink>
        ))}
      </nav>
    </div>
  );
};

export default CustomerPortalLayout;
