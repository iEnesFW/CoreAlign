import { useTranslation } from 'react-i18next';
import { useMyNotificationsQuery, useMarkNotificationRead } from '../hooks/useMyNotifications';
import type { NotificationMessageView } from '../model/notifications.types';

export const NotificationDropdown = () => {
  const { t } = useTranslation();
  const listQuery = useMyNotificationsQuery({ pageSize: 10 });
  const markRead = useMarkNotificationRead();
  const items: NotificationMessageView[] = listQuery.data ?? [];

  return (
    <div className="w-96 rounded-lg bg-white shadow-lg ring-1 ring-slate-200 dark:bg-slate-800 dark:ring-slate-700">
      <header className="flex items-center justify-between border-b border-slate-100 px-3 py-2 dark:border-slate-700/60">
        <span className="text-xs font-semibold text-slate-900 dark:text-slate-100">
          {t('Notifications.Title')}
        </span>
        <a
          href="/notifications"
          className="text-[10px] font-medium text-primary-600 hover:underline dark:text-primary-300"
        >
          {t('Notifications.SeeAll')}
        </a>
      </header>
      <ul className="divide-y divide-slate-100 dark:divide-slate-700/60">
        {items.map((n) => (
          <li key={n.id} className="px-3 py-2">
            <button
              type="button"
              onClick={() => n.status !== 'Read' && markRead.mutate(n.id)}
              className="flex w-full flex-col items-start gap-0.5 text-left"
            >
              <span className="text-[11px] font-semibold text-slate-800 dark:text-slate-100">
                {n.subject ?? n.templateKey}
              </span>
              <p className="line-clamp-2 text-[11px] text-slate-600 dark:text-slate-300">
                {n.bodyMarkdown}
              </p>
              <span className="text-[10px] text-slate-400">
                {t(`Notifications.Status.${n.status}`)}
              </span>
            </button>
          </li>
        ))}
        {items.length === 0 && (
          <li className="px-3 py-6 text-center text-xs text-slate-500">
            {t('Notifications.Bell.Empty')}
          </li>
        )}
      </ul>
    </div>
  );
};
