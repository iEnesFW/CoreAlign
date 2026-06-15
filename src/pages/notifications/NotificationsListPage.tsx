import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  useMyNotificationsQuery,
  useMarkNotificationRead,
} from '@/features/notifications/hooks/useMyNotifications';
import type {
  NotificationMessageView,
  NotificationStatus,
} from '@/features/notifications/model/notifications.types';

const STATUS_OPTIONS: NotificationStatus[] = [
  'Pending',
  'Sent',
  'Delivered',
  'Failed',
  'Bounced',
  'Read',
];

export const NotificationsListPage = () => {
  const { t } = useTranslation();
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [statusFilter, setStatusFilter] = useState<NotificationStatus | ''>('');
  const listQuery = useMyNotificationsQuery({ unreadOnly, pageSize: 50 });
  const markRead = useMarkNotificationRead();
  const items: NotificationMessageView[] = (listQuery.data ?? []).filter(
    (n) => !statusFilter || n.status === statusFilter,
  );

  return (
    <div className="space-y-4 p-4">
      <header className="flex items-center justify-between">
        <h1 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
          {t('Notifications.Title')}
        </h1>
        <div className="flex items-center gap-2">
          <label className="inline-flex items-center gap-1 text-xs text-slate-600 dark:text-slate-300">
            <input
              type="checkbox"
              checked={unreadOnly}
              onChange={(e) => setUnreadOnly(e.target.checked)}
            />
            {t('Notifications.UnreadOnly')}
          </label>
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as NotificationStatus | '')}
            className="rounded border border-slate-200 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-800"
          >
            <option value="">{t('Notifications.AllStatuses')}</option>
            {STATUS_OPTIONS.map((s) => (
              <option key={s} value={s}>
                {t(`Notifications.Status.${s}`)}
              </option>
            ))}
          </select>
        </div>
      </header>

      <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-800">
        <table className="w-full text-xs">
          <thead className="bg-slate-50 dark:bg-slate-900/40">
            <tr className="text-left text-slate-500">
              <th className="px-3 py-2">{t('Notifications.TableSubject')}</th>
              <th className="px-3 py-2">{t('Notifications.TableCategory')}</th>
              <th className="px-3 py-2">{t('Notifications.TableChannel')}</th>
              <th className="px-3 py-2">{t('Notifications.TableStatus')}</th>
              <th className="px-3 py-2">{t('Notifications.TableCreatedAt')}</th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {items.length === 0 && (
              <tr>
                <td colSpan={6} className="px-3 py-6 text-center text-slate-500">
                  {t('Notifications.Bell.Empty')}
                </td>
              </tr>
            )}
            {items.map((n) => (
              <tr key={n.id} className="border-t border-slate-100 dark:border-slate-700/60">
                <td className="px-3 py-2">{n.subject ?? n.templateKey}</td>
                <td className="px-3 py-2">{t(`Notifications.Category.${n.categoryKey}`)}</td>
                <td className="px-3 py-2">{t(`Notifications.Channel.${n.channel}`)}</td>
                <td className="px-3 py-2">{t(`Notifications.Status.${n.status}`)}</td>
                <td className="px-3 py-2">{new Date(n.createdAtUtc).toLocaleString()}</td>
                <td className="px-3 py-2 text-right">
                  {n.status !== 'Read' && (
                    <button
                      type="button"
                      onClick={() => markRead.mutate(n.id)}
                      className="text-indigo-600 hover:underline"
                    >
                      {t('Notifications.MarkRead')}
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default NotificationsListPage;
