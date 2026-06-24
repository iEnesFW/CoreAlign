import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { RotateCcw } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Badge, type BadgeVariant } from '@/shared/ui/Badge/Badge';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatDateTime } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useWebhookHistoryQuery, useReplayWebhookMutation } from '../hooks/useProvidersAdmin';
import type { WebhookHistoryFilters, WebhookInboxStatus } from '../api/providersAdminApi';
import type { ProviderInfo } from '../api/providersAdminApi';

interface Props {
  open: boolean;
  provider: ProviderInfo | null;
  onClose: () => void;
}

const STATUS_VARIANT: Record<WebhookInboxStatus, BadgeVariant> = {
  Received: 'default',
  Processed: 'success',
  Failed: 'error',
  Retrying: 'warning',
  Discarded: 'neutral',
  Pending: 'warning',
};

const PAGE_SIZE = 20;

export const WebhookHistoryModal = ({ open, provider, onClose }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();

  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState<WebhookInboxStatus | ''>('');
  const [fromUtc, setFromUtc] = useState('');
  const [toUtc, setToUtc] = useState('');

  const filters = useMemo<WebhookHistoryFilters>(
    () => ({
      providerName: provider?.name,
      category: provider?.category,
      status: statusFilter || undefined,
      fromUtc: fromUtc || undefined,
      toUtc: toUtc || undefined,
      page,
      pageSize: PAGE_SIZE,
    }),
    [provider, statusFilter, fromUtc, toUtc, page],
  );

  const historyQuery = useWebhookHistoryQuery(filters, open && !!provider);
  const replay = useReplayWebhookMutation();

  const items = historyQuery.data?.items ?? [];
  const total = historyQuery.data?.total ?? 0;
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  const handleReplay = async (id: string) => {
    try {
      await replay.mutateAsync(id);
      toast.success(t('Admin.Providers.Toast.WebhookReplayed'));
    } catch (err) {
      toastApiError(err, t('Admin.Providers.Toast.WebhookReplayFailed'));
    }
  };

  if (!provider) return null;

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={t('Admin.Providers.Webhook.Title', { name: provider.displayName })}
      size="2xl"
      footer={
        <Button variant="outline" onClick={onClose} type="button">
          {t('common.close')}
        </Button>
      }
    >
      <div className="space-y-3">
        <div className="grid gap-2 sm:grid-cols-4">
          <div>
            <label className="mb-1 block text-[11px] font-medium text-slate-600 dark:text-slate-400">
              {t('Admin.Providers.Webhook.Status')}
            </label>
            <select
              value={statusFilter}
              onChange={(e) => {
                setStatusFilter(e.target.value as WebhookInboxStatus | '');
                setPage(1);
              }}
              className="w-full rounded border border-slate-200 bg-white px-2 py-1.5 text-xs text-slate-900 focus:border-primary-500 focus:outline-none dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            >
              <option value="">{t('Admin.Providers.Webhook.AllStatuses')}</option>
              {(Object.keys(STATUS_VARIANT) as WebhookInboxStatus[]).map((s) => (
                <option key={s} value={s}>
                  {t(`Admin.Providers.Webhook.StatusValue.${s}`)}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-[11px] font-medium text-slate-600 dark:text-slate-400">
              {t('Admin.Providers.Webhook.From')}
            </label>
            <Input
              type="datetime-local"
              value={fromUtc}
              onChange={(e) => {
                setFromUtc(e.target.value);
                setPage(1);
              }}
            />
          </div>
          <div>
            <label className="mb-1 block text-[11px] font-medium text-slate-600 dark:text-slate-400">
              {t('Admin.Providers.Webhook.To')}
            </label>
            <Input
              type="datetime-local"
              value={toUtc}
              onChange={(e) => {
                setToUtc(e.target.value);
                setPage(1);
              }}
            />
          </div>
          <div className="flex items-end">
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                setStatusFilter('');
                setFromUtc('');
                setToUtc('');
                setPage(1);
              }}
              type="button"
            >
              {t('Admin.Providers.Webhook.Reset')}
            </Button>
          </div>
        </div>

        <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-700">
          <table className="min-w-full text-xs">
            <thead className="bg-slate-50 dark:bg-slate-900/40">
              <tr className="text-left text-slate-500 dark:text-slate-400">
                <th className="px-3 py-2 font-medium">{t('Admin.Providers.Webhook.ReceivedAt')}</th>
                <th className="px-3 py-2 font-medium">{t('Admin.Providers.Webhook.EventType')}</th>
                <th className="px-3 py-2 font-medium">{t('Admin.Providers.Webhook.Status')}</th>
                <th className="px-3 py-2 font-medium">{t('Admin.Providers.Webhook.Retries')}</th>
                <th className="px-3 py-2 font-medium">{t('Admin.Providers.Webhook.Error')}</th>
                <th className="px-3 py-2 font-medium text-right">
                  {t('Admin.Providers.Webhook.Actions')}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {items.length === 0 && (
                <tr>
                  <td
                    colSpan={6}
                    className="px-3 py-8 text-center text-slate-500 dark:text-slate-400"
                  >
                    {historyQuery.isLoading
                      ? t('common.loading')
                      : t('Admin.Providers.Webhook.Empty')}
                  </td>
                </tr>
              )}
              {items.map((item) => (
                <tr key={item.id} className="text-slate-700 dark:text-slate-200">
                  <td className="px-3 py-2 tabular-nums">
                    {formatDateTime(item.receivedAtUtc, locale)}
                  </td>
                  <td className="px-3 py-2">{item.eventType ?? '—'}</td>
                  <td className="px-3 py-2">
                    <Badge variant={STATUS_VARIANT[item.status]} pill>
                      {t(`Admin.Providers.Webhook.StatusValue.${item.status}`)}
                    </Badge>
                  </td>
                  <td className="px-3 py-2 tabular-nums">{item.retryCount}</td>
                  <td className="px-3 py-2 max-w-xs truncate" title={item.processingError ?? ''}>
                    {item.processingError ?? '—'}
                  </td>
                  <td className="px-3 py-2 text-right">
                    <button
                      type="button"
                      onClick={() => handleReplay(item.id)}
                      disabled={replay.isPending}
                      className="inline-flex items-center gap-1 rounded px-2 py-1 text-[11px] font-medium text-primary-600 hover:bg-primary-50 disabled:opacity-50 dark:text-primary-300 dark:hover:bg-primary-500/10"
                    >
                      <RotateCcw size={12} />
                      {t('Admin.Providers.Webhook.Replay')}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {total > 0 && (
          <div className="flex items-center justify-between text-[11px] text-slate-500 dark:text-slate-400">
            <span>{t('Admin.Providers.Webhook.PageInfo', { page, totalPages, total })}</span>
            <div className="flex gap-1">
              <Button
                variant="outline"
                size="sm"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                type="button"
              >
                {t('common.prev')}
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => p + 1)}
                type="button"
              >
                {t('common.next')}
              </Button>
            </div>
          </div>
        )}
      </div>
    </Modal>
  );
};
