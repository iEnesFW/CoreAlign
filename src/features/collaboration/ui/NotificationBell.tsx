import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Bell, BellOff, CheckCheck } from 'lucide-react';
import { useAuthStore } from '@/features/auth/model/authStore';
import {
  useMarkAllNotificationsRead,
  useMarkNotificationRead,
  useNotifications,
  useUnreadCount,
} from '../hooks/useCollab';
import type { CollabEntityType, Notification } from '../model/collab.types';
import { useRelativeTime } from './useRelativeTime';

const POLL_INTERVAL_MS = 30 * 1000;
const MAX_BADGE = 99;

/**
 * Build a deep-link URL for the entity that fired the notification. Order +
 * VendorBill open their respective list pages with the detail panel and the
 * comments tab pre-selected; Shipments deep-link by shipment id (the orders
 * page picks it up).
 */
const buildTarget = (n: Notification): string => {
  switch (n.entityType) {
    case 'Order':
      return `/dashboard/orders?selected=${n.entityId}&tab=comments`;
    case 'VendorBill':
      return `/dashboard/invoices?selected=${n.entityId}&tab=comments`;
    case 'Shipment':
      return `/dashboard/orders?shipment=${n.entityId}&tab=comments`;
    case 'SubscriptionOrder':
      return `/dashboard/billing/orders/${n.entityId}`;
    default:
      return '/dashboard';
  }
};

export const NotificationBell = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const relative = useRelativeTime();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  const unreadQuery = useUnreadCount({ pollMs: POLL_INTERVAL_MS, enabled: isAuthenticated });
  const listQuery = useNotifications({ take: 30, enabled: open && isAuthenticated });
  const markRead = useMarkNotificationRead();
  const markAllRead = useMarkAllNotificationsRead();

  const unreadCount = unreadQuery.data?.data ?? 0;
  const notifications = useMemo(() => listQuery.data?.data ?? [], [listQuery.data]);

  useEffect(() => {
    const onClickOutside = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', onClickOutside);
    return () => document.removeEventListener('mousedown', onClickOutside);
  }, []);

  const handleSelect = (n: Notification) => {
    if (!n.isRead) {
      markRead.mutate(n.id);
    }
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
        aria-label={t('collab.notifications.title')}
        aria-expanded={open}
        className="relative p-1.5 text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200 rounded-[5px] hover:bg-slate-100 dark:hover:bg-slate-800 transition-all focus:outline-none focus:ring-1 focus:ring-indigo-500"
      >
        <Bell size={16} />
        {unreadCount > 0 && (
          <span className="absolute -right-0.5 -top-0.5 inline-flex min-w-[14px] items-center justify-center rounded-full bg-rose-500 px-[3px] text-[9px] font-bold leading-[14px] text-white shadow-sm ring-1 ring-white dark:ring-[#0B0F19]">
            {badgeText}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 mt-2 w-80 overflow-hidden rounded-lg bg-white shadow-lg ring-1 ring-slate-200 dark:bg-slate-800 dark:ring-slate-700">
          <div className="flex items-center justify-between border-b border-slate-100 px-3 py-2 dark:border-slate-700/60">
            <span className="text-xs font-semibold text-slate-900 dark:text-slate-100">
              {t('collab.notifications.title')}
            </span>
            <button
              type="button"
              onClick={handleMarkAll}
              disabled={unreadCount === 0 || markAllRead.isPending}
              className="inline-flex items-center gap-1 rounded px-1.5 py-0.5 text-[10px] font-medium text-indigo-600 hover:bg-indigo-50 disabled:cursor-not-allowed disabled:opacity-40 dark:text-indigo-300 dark:hover:bg-indigo-500/10"
            >
              <CheckCheck size={11} />
              {t('collab.notifications.markAllRead')}
            </button>
          </div>

          <div className="max-h-80 overflow-y-auto">
            {listQuery.isPending ? (
              <div className="px-3 py-6 text-center text-xs text-slate-500">
                {t('common.loading')}
              </div>
            ) : notifications.length === 0 ? (
              <div className="flex flex-col items-center gap-1 px-3 py-8 text-center">
                <BellOff size={20} className="text-slate-300 dark:text-slate-600" />
                <p className="text-xs text-slate-500 dark:text-slate-400">
                  {t('collab.notifications.empty')}
                </p>
              </div>
            ) : (
              <ul className="divide-y divide-slate-100 dark:divide-slate-700/60">
                {notifications.map((n) => (
                  <li key={n.id}>
                    <button
                      type="button"
                      onClick={() => handleSelect(n)}
                      className={`flex w-full flex-col items-start gap-0.5 px-3 py-2 text-left transition hover:bg-slate-50 dark:hover:bg-slate-700/40 ${
                        n.isRead ? 'opacity-70' : ''
                      }`}
                    >
                      <div className="flex w-full items-center gap-1.5">
                        {!n.isRead && (
                          <span className="inline-block h-1.5 w-1.5 shrink-0 rounded-full bg-indigo-500" />
                        )}
                        <span className="truncate text-[11px] font-semibold text-slate-800 dark:text-slate-100">
                          {n.title}
                        </span>
                        <span className="ml-auto shrink-0 text-[10px] text-slate-400">
                          {relative(n.createdAtUtc)}
                        </span>
                      </div>
                      <p className="line-clamp-2 text-[11px] text-slate-600 dark:text-slate-300">
                        {n.body}
                      </p>
                      <span className="text-[10px] text-slate-400 dark:text-slate-500">
                        {t(`collab.entity.${entityKey(n.entityType)}`)}
                      </span>
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

const entityKey = (
  type: CollabEntityType,
): 'order' | 'vendorBill' | 'shipment' | 'subscriptionOrder' => {
  switch (type) {
    case 'VendorBill':
      return 'vendorBill';
    case 'Shipment':
      return 'shipment';
    case 'SubscriptionOrder':
      return 'subscriptionOrder';
    case 'Order':
    default:
      return 'order';
  }
};
