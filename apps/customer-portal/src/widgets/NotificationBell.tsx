import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Bell, BellOff, CheckCheck } from 'lucide-react';
import { useAuthStore } from '@/features/auth/authStore';
import {
  useMarkAllPortalNotificationsRead,
  useMarkPortalNotificationRead,
  usePortalNotifications,
  usePortalUnreadCount,
} from '@/features/portal/profileHooks';
import type { PortalNotification } from '@/features/portal/notificationsApi';

const POLL_INTERVAL_MS = 30 * 1000;
const MAX_BADGE = 99;

const buildTarget = (n: PortalNotification): string => {
  switch (n.entityType) {
    case 'Order':
      return `/orders/${n.entityId}`;
    case 'Invoice':
      return `/invoices/${n.entityId}`;
    default:
      return '/notifications';
  }
};

const formatRelative = (iso: string): string => {
  const diffMs = Date.now() - new Date(iso).getTime();
  const min = Math.round(diffMs / 60000);
  if (min < 1) return 'now';
  if (min < 60) return `${min}m`;
  const hr = Math.round(min / 60);
  if (hr < 24) return `${hr}h`;
  const d = Math.round(hr / 24);
  return `${d}d`;
};

export const NotificationBell = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  const unreadQuery = usePortalUnreadCount({ pollMs: POLL_INTERVAL_MS, enabled: isAuthenticated });
  const listQuery = usePortalNotifications({ take: 10, enabled: open && isAuthenticated });
  const markRead = useMarkPortalNotificationRead();
  const markAllRead = useMarkAllPortalNotificationsRead();

  const unreadCount = unreadQuery.data ?? 0;
  const notifications = useMemo(() => listQuery.data ?? [], [listQuery.data]);

  useEffect(() => {
    const onClickOutside = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', onClickOutside);
    return () => document.removeEventListener('mousedown', onClickOutside);
  }, []);

  const handleSelect = (n: PortalNotification) => {
    if (!n.isRead) markRead.mutate(n.id);
    setOpen(false);
    navigate(buildTarget(n));
  };

  const handleMarkAll = () => {
    if (unreadCount === 0) return;
    markAllRead.mutate();
  };

  const badgeText = unreadCount > MAX_BADGE ? `${MAX_BADGE}+` : String(unreadCount);

  return (
    <div className="relative" ref={ref}>
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-label={t('notifications.bell.title')}
        aria-expanded={open}
        className="relative rounded-lg p-2 text-slate-500 transition hover:bg-slate-100 hover:text-slate-700 focus:outline-none focus:ring-2 focus:ring-sky-500 dark:text-slate-400 dark:hover:bg-slate-800 dark:hover:text-slate-200"
      >
        <Bell size={16} />
        {unreadCount > 0 && (
          <span className="absolute -right-0.5 -top-0.5 inline-flex min-w-[16px] items-center justify-center rounded-full bg-rose-500 px-1 text-[10px] font-bold leading-[16px] text-white shadow-sm ring-1 ring-white dark:ring-slate-950">
            {badgeText}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 mt-2 w-80 overflow-hidden rounded-xl border border-slate-100 bg-white shadow-xl dark:border-slate-800 dark:bg-slate-900">
          <div className="flex items-center justify-between border-b border-slate-100 px-3 py-2 dark:border-slate-800">
            <span className="text-xs font-semibold text-slate-900 dark:text-slate-100">
              {t('notifications.bell.title')}
            </span>
            <button
              type="button"
              onClick={handleMarkAll}
              disabled={unreadCount === 0 || markAllRead.isPending}
              className="inline-flex items-center gap-1 rounded px-1.5 py-0.5 text-[11px] font-medium text-sky-600 hover:bg-sky-50 disabled:cursor-not-allowed disabled:opacity-40 dark:text-sky-300 dark:hover:bg-sky-500/10"
            >
              <CheckCheck size={12} />
              {t('notifications.bell.markAllRead')}
            </button>
          </div>

          <div className="max-h-80 overflow-y-auto">
            {listQuery.isPending && open ? (
              <div className="px-3 py-6 text-center text-xs text-slate-500">
                {t('common.loading')}
              </div>
            ) : notifications.length === 0 ? (
              <div className="flex flex-col items-center gap-1 px-3 py-8 text-center">
                <BellOff size={20} className="text-slate-300 dark:text-slate-600" />
                <p className="text-xs text-slate-500 dark:text-slate-400">
                  {t('notifications.bell.empty')}
                </p>
              </div>
            ) : (
              <ul className="divide-y divide-slate-100 dark:divide-slate-800">
                {notifications.map((n) => (
                  <li key={n.id}>
                    <button
                      type="button"
                      onClick={() => handleSelect(n)}
                      className={`flex w-full flex-col items-start gap-0.5 px-3 py-2 text-left transition hover:bg-slate-50 dark:hover:bg-slate-800/40 ${
                        n.isRead ? 'opacity-70' : ''
                      }`}
                    >
                      <div className="flex w-full items-center gap-1.5">
                        {!n.isRead && (
                          <span className="inline-block h-1.5 w-1.5 shrink-0 rounded-full bg-sky-500" />
                        )}
                        <span className="truncate text-[11px] font-semibold text-slate-800 dark:text-slate-100">
                          {n.title}
                        </span>
                        <span className="ml-auto shrink-0 text-[10px] text-slate-400">
                          {formatRelative(n.createdAtUtc)}
                        </span>
                      </div>
                      <p className="line-clamp-2 text-[11px] text-slate-600 dark:text-slate-300">
                        {n.body}
                      </p>
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>

          <div className="border-t border-slate-100 px-3 py-2 text-center dark:border-slate-800">
            <button
              type="button"
              onClick={() => {
                setOpen(false);
                navigate('/profile');
              }}
              className="text-[11px] font-medium text-sky-600 hover:underline dark:text-sky-300"
            >
              {t('notifications.bell.viewAll')}
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
