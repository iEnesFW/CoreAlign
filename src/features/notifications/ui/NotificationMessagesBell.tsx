import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Bell, BellOff } from 'lucide-react';
import {
  useMarkNotificationRead,
  useMyNotificationsQuery,
  useUnreadNotificationCountQuery,
} from '../hooks/useMyNotifications';
import type { NotificationMessageView } from '../model/notifications.types';

const MAX_BADGE = 99;

export const NotificationMessagesBell = () => {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const unreadQuery = useUnreadNotificationCountQuery();
  const listQuery = useMyNotificationsQuery({ pageSize: 10 });
  const markRead = useMarkNotificationRead();

  useEffect(() => {
    const onClickOutside = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', onClickOutside);
    return () => document.removeEventListener('mousedown', onClickOutside);
  }, []);

  const unread = unreadQuery.data?.unread ?? 0;
  const items: NotificationMessageView[] = listQuery.data ?? [];
  const badgeText = unread > MAX_BADGE ? `${MAX_BADGE}+` : String(unread);

  const handleSelect = (n: NotificationMessageView) => {
    if (n.status !== 'Read') markRead.mutate(n.id);
  };

  return (
    <div className="relative" ref={ref}>
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-label={t('Notifications.Bell.Unread')}
        className="relative p-1.5 text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200 rounded-[5px] hover:bg-slate-100 dark:hover:bg-slate-800 transition-all focus:outline-none focus:ring-1 focus:ring-primary-500"
      >
        <Bell size={16} />
        {unread > 0 && (
          <span className="absolute -right-0.5 -top-0.5 inline-flex min-w-[14px] items-center justify-center rounded-full bg-danger-500 px-[3px] text-[9px] font-bold leading-[14px] text-white shadow-sm ring-1 ring-white dark:ring-shell">
            {badgeText}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 mt-2 w-80 overflow-hidden rounded-lg bg-white shadow-lg ring-1 ring-slate-200 dark:bg-slate-800 dark:ring-slate-700">
          <div className="flex items-center justify-between border-b border-slate-100 px-3 py-2 dark:border-slate-700/60">
            <span className="text-xs font-semibold text-slate-900 dark:text-slate-100">
              {t('Notifications.Title')}
            </span>
          </div>
          <div className="max-h-80 overflow-y-auto">
            {items.length === 0 ? (
              <div className="flex flex-col items-center gap-1 px-3 py-8 text-center">
                <BellOff size={20} className="text-slate-300 dark:text-slate-600" />
                <p className="text-xs text-slate-500 dark:text-slate-400">
                  {t('Notifications.Bell.Empty')}
                </p>
              </div>
            ) : (
              <ul className="divide-y divide-slate-100 dark:divide-slate-700/60">
                {items.map((n) => (
                  <li key={n.id}>
                    <button
                      type="button"
                      onClick={() => handleSelect(n)}
                      className={`flex w-full flex-col items-start gap-0.5 px-3 py-2 text-left transition hover:bg-slate-50 dark:hover:bg-slate-700/40 ${n.status === 'Read' ? 'opacity-70' : ''}`}
                    >
                      <span className="truncate text-[11px] font-semibold text-slate-800 dark:text-slate-100">
                        {n.subject ?? n.templateKey}
                      </span>
                      <p className="line-clamp-2 text-[11px] text-slate-600 dark:text-slate-300">
                        {n.bodyMarkdown}
                      </p>
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
