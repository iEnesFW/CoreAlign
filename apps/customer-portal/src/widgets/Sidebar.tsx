import {
  CheckSquare,
  FileText,
  LayoutDashboard,
  Package,
  Store,
  UserCircle,
  X,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router-dom';
import { cn } from '@/shared/lib/cn';
import { useApprovalsPendingCount } from '@/features/approvals/hooks';

interface SidebarProps {
  open: boolean;
  onClose: () => void;
}

export const Sidebar = ({ open, onClose }: SidebarProps) => {
  const { t } = useTranslation();
  const pendingCount = useApprovalsPendingCount();

  const items: Array<{
    to: string;
    label: string;
    icon: typeof LayoutDashboard;
    end: boolean;
    badge?: number;
  }> = [
    { to: '/', label: t('nav.dashboard'), icon: LayoutDashboard, end: true },
    { to: '/orders', label: t('nav.orders'), icon: Package, end: false },
    {
      to: '/approvals',
      label: t('nav.approvals'),
      icon: CheckSquare,
      end: false,
      badge: pendingCount.data ?? 0,
    },
    { to: '/invoices', label: t('nav.invoices'), icon: FileText, end: false },
    { to: '/dealers', label: t('nav.dealers'), icon: Store, end: false },
    { to: '/profile', label: t('nav.profile'), icon: UserCircle, end: false },
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
        aria-label={t('common.primaryNav')}
      >
        <div className="flex h-16 items-center justify-between px-6 lg:hidden">
          <span className="text-base font-semibold text-slate-900 dark:text-slate-100">
            {t('app.name')}
          </span>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-1.5 text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800"
            aria-label={t('common.closeMenu')}
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
                        ? 'bg-sky-50 text-sky-700 dark:bg-sky-900/40 dark:text-sky-200'
                        : 'text-slate-600 hover:bg-slate-50 dark:text-slate-300 dark:hover:bg-slate-900',
                    )
                  }
                >
                  <item.icon size={18} />
                  <span className="flex-1">{item.label}</span>
                  {item.badge && item.badge > 0 ? (
                    <span className="inline-flex min-w-[1.25rem] items-center justify-center rounded-full bg-amber-500 px-1.5 text-xs font-semibold text-white">
                      {item.badge > 99 ? '99+' : item.badge}
                    </span>
                  ) : null}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>

        <div className="px-6 py-4 text-xs text-slate-400">{t('app.tagline')}</div>
      </aside>
    </>
  );
};
