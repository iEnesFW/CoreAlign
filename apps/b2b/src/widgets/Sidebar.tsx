import {
  BadgePercent,
  FileText,
  LayoutDashboard,
  Package,
  PlusCircle,
  UserCircle,
  Users,
  X,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router-dom';
import { cn } from '@/shared/lib/cn';

interface SidebarProps {
  open: boolean;
  onClose: () => void;
}

export const Sidebar = ({ open, onClose }: SidebarProps) => {
  const { t } = useTranslation();

  const items = [
    { to: '/', label: t('b2b.nav.dashboard'), icon: LayoutDashboard, end: true },
    { to: '/customers', label: t('b2b.nav.customers'), icon: Users },
    { to: '/orders/new', label: t('b2b.nav.newOrder'), icon: PlusCircle },
    { to: '/orders', label: t('b2b.nav.orders'), icon: Package },
    { to: '/invoices', label: t('b2b.nav.invoices'), icon: FileText },
    { to: '/commissions', label: t('b2b.nav.commissions'), icon: BadgePercent },
    { to: '/profile', label: t('b2b.nav.profile'), icon: UserCircle },
  ];

  return (
    <>
      {open ? (
        <div
          className="fixed inset-0 z-30 bg-slate-900/40 backdrop-blur-sm lg:hidden"
          onClick={onClose}
          aria-hidden="true"
        />
      ) : null}

      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-40 flex w-72 flex-col border-r border-slate-100 bg-white transition-transform duration-200 dark:border-slate-800 dark:bg-slate-950',
          'lg:translate-x-0',
          open ? 'translate-x-0' : '-translate-x-full',
        )}
        aria-label={t('b2b.common.primaryNav')}
      >
        <div className="flex h-16 items-center justify-between px-6 lg:hidden">
          <span className="text-base font-semibold text-slate-900 dark:text-slate-100">
            {t('b2b.app.name')}
          </span>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-1.5 text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800"
            aria-label={t('b2b.common.closeMenu')}
          >
            <X size={18} />
          </button>
        </div>

        <nav className="flex-1 overflow-y-auto px-3 py-6">
          <ul className="flex flex-col gap-1">
            {items.map((item) => (
              <li key={item.to}>
                <NavLink
                  to={item.to}
                  end={item.end}
                  onClick={onClose}
                  className={({ isActive }) =>
                    cn(
                      'flex items-center gap-3 rounded-xl px-3.5 py-2.5 text-sm font-medium transition',
                      isActive
                        ? 'bg-amber-50 text-amber-700 dark:bg-amber-900/40 dark:text-amber-200'
                        : 'text-slate-600 hover:bg-slate-50 dark:text-slate-300 dark:hover:bg-slate-900',
                    )
                  }
                >
                  <item.icon size={18} />
                  {item.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>

        <div className="px-6 py-4 text-xs text-slate-400">{t('b2b.app.tagline')}</div>
      </aside>
    </>
  );
};
