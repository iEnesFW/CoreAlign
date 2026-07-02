import { Fragment, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Bell } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Select } from '@/shared/ui/Select/Select';
import { Checkbox } from '@/shared/ui/Checkbox/Checkbox';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import {
  useMyNotificationsQuery,
  useMarkNotificationRead,
  useAcknowledgeNotification,
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
  const acknowledge = useAcknowledgeNotification();
  const [noteFor, setNoteFor] = useState<string | null>(null);
  const [noteDraft, setNoteDraft] = useState('');
  const items: NotificationMessageView[] = (listQuery.data ?? []).filter(
    (n) => !statusFilter || n.status === statusFilter,
  );

  const openNoteEditor = (id: string, existing: string | null) => {
    setNoteFor(id);
    setNoteDraft(existing ?? '');
  };
  const submitAcknowledge = (id: string) =>
    acknowledge.mutate(
      { id, note: noteDraft.trim() || undefined },
      { onSuccess: () => setNoteFor(null) },
    );

  return (
    <ListPageTemplate
      header={<PageHeader icon={<Bell size={20} />} title={t('Notifications.Title')} />}
      toolbar={
        <div className="flex flex-wrap items-center gap-3">
          <Checkbox
            checked={unreadOnly}
            onChange={(e) => setUnreadOnly(e.target.checked)}
            label={t('Notifications.UnreadOnly')}
          />
          <Select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as NotificationStatus | '')}
            className="w-full sm:w-48"
          >
            <option value="">{t('Notifications.AllStatuses')}</option>
            {STATUS_OPTIONS.map((s) => (
              <option key={s} value={s}>
                {t(`Notifications.Status.${s}`)}
              </option>
            ))}
          </Select>
        </div>
      }
    >
      <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-800">
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
              <Fragment key={n.id}>
                <tr className="border-t border-slate-100 dark:border-slate-700/60">
                  <td className="px-3 py-2">{n.subject ?? n.templateKey}</td>
                  <td className="px-3 py-2">{t(`Notifications.Category.${n.categoryKey}`)}</td>
                  <td className="px-3 py-2">{t(`Notifications.Channel.${n.channel}`)}</td>
                  <td className="px-3 py-2">{t(`Notifications.Status.${n.status}`)}</td>
                  <td className="px-3 py-2">{new Date(n.createdAtUtc).toLocaleString()}</td>
                  <td className="px-3 py-2">
                    <div className="flex items-center justify-end gap-1">
                      {n.status !== 'Read' && (
                        <Button variant="ghost" size="sm" onClick={() => markRead.mutate(n.id)}>
                          {t('Notifications.MarkRead')}
                        </Button>
                      )}
                      {n.isAcknowledged ? (
                        <span className="inline-flex items-center gap-1">
                          <span className="rounded bg-emerald-50 px-2 py-0.5 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300">
                            {t('Notifications.Acknowledged')}
                          </span>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => openNoteEditor(n.id, n.acknowledgmentNote)}
                          >
                            {t('Notifications.EditNote')}
                          </Button>
                        </span>
                      ) : (
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => openNoteEditor(n.id, n.acknowledgmentNote)}
                        >
                          {t('Notifications.Acknowledge')}
                        </Button>
                      )}
                    </div>
                  </td>
                </tr>
                {noteFor === n.id && (
                  <tr className="bg-slate-50 dark:bg-slate-900/40">
                    <td colSpan={6} className="px-3 py-3">
                      <label
                        htmlFor={`ack-note-${n.id}`}
                        className="mb-1 block text-slate-600 dark:text-slate-300"
                      >
                        {t('Notifications.NoteLabel')}
                      </label>
                      <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
                        <Input
                          id={`ack-note-${n.id}`}
                          value={noteDraft}
                          maxLength={2000}
                          onChange={(e) => setNoteDraft(e.target.value)}
                          aria-label={t('Notifications.NoteLabel')}
                          className="flex-1"
                        />
                        <Button
                          size="sm"
                          onClick={() => submitAcknowledge(n.id)}
                          isLoading={acknowledge.isPending}
                        >
                          {t('Common.Save')}
                        </Button>
                        <Button variant="ghost" size="sm" onClick={() => setNoteFor(null)}>
                          {t('Common.Cancel')}
                        </Button>
                      </div>
                    </td>
                  </tr>
                )}
              </Fragment>
            ))}
          </tbody>
        </table>
      </div>
    </ListPageTemplate>
  );
};

export default NotificationsListPage;
