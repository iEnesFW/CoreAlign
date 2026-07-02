import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { CircleStop, Pencil, Play, Plus, Repeat, Send, XCircle } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Select } from '@/shared/ui/Select/Select';
import { Badge } from '@/shared/ui/Badge/Badge';
import type { BadgeVariant } from '@/shared/ui/Badge/Badge';
import {
  useCancelRecurringInvoice,
  usePauseRecurringInvoice,
  useRecurringInvoiceQuery,
  useRecurringInvoicesQuery,
  useResumeRecurringInvoice,
  useRunRecurringInvoiceNow,
} from '@/features/invoices/hooks/useRecurringInvoiceQueries';
import type { RecurringInvoiceStatus } from '@/features/invoices/model/recurringInvoice.types';
import { RecurringInvoiceFormModal } from './components/RecurringInvoiceFormModal';

const STATUS_VARIANT: Record<RecurringInvoiceStatus, BadgeVariant> = {
  Active: 'success',
  Paused: 'warning',
  Completed: 'info',
  Cancelled: 'danger',
};

const STATUSES: RecurringInvoiceStatus[] = ['Active', 'Paused', 'Completed', 'Cancelled'];

export const RecurringInvoicesPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const confirm = useConfirm();

  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<RecurringInvoiceStatus | ''>('');
  const [page, setPage] = useState(1);
  const [createOpen, setCreateOpen] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);

  const listQuery = useRecurringInvoicesQuery({
    search: search.trim() || undefined,
    status: status || undefined,
    page,
    pageSize: 25,
  });
  const editQuery = useRecurringInvoiceQuery(editId);
  const pauseMutation = usePauseRecurringInvoice();
  const resumeMutation = useResumeRecurringInvoice();
  const cancelMutation = useCancelRecurringInvoice();
  const runNowMutation = useRunRecurringInvoiceNow();

  const items = listQuery.data?.data?.items ?? [];
  const total = listQuery.data?.data?.total ?? 0;
  const totalPages = listQuery.data?.data?.totalPages ?? 0;

  const statusLabel = (s: RecurringInvoiceStatus) =>
    t(`RecurringInvoices.status.${s}` as const, { defaultValue: s });
  const frequencyLabel = (f: string) =>
    t(`RecurringInvoices.frequency.${f}` as const, { defaultValue: f });

  const runAction = async (
    action: () => Promise<unknown>,
    successKey: string,
    fallback: string,
  ) => {
    try {
      await action();
      toast.success(t(successKey, { defaultValue: fallback }));
    } catch (err) {
      toastApiError(err);
    }
  };

  const onRunNow = (id: string) =>
    runAction(
      () => runNowMutation.mutateAsync(id),
      'RecurringInvoices.toast.generated',
      'Fatura üretildi.',
    );
  const onPause = (id: string) =>
    runAction(
      () => pauseMutation.mutateAsync(id),
      'RecurringInvoices.toast.paused',
      'Duraklatıldı.',
    );
  const onResume = (id: string) =>
    runAction(
      () => resumeMutation.mutateAsync(id),
      'RecurringInvoices.toast.resumed',
      'Yeniden başlatıldı.',
    );

  const onCancel = async (id: string, name: string) => {
    const ok = await confirm({
      title: t('RecurringInvoices.cancel.title', { defaultValue: 'Tekrarlayan Faturayı İptal Et' }),
      message: t('RecurringInvoices.cancel.message', {
        defaultValue: '{{n}} iptal edilsin mi? Bu işlem geri alınamaz.',
        n: name,
      }),
      confirmLabel: t('common.confirm', { defaultValue: 'Onayla' }),
      tone: 'danger',
    });
    if (!ok) return;
    await runAction(
      () => cancelMutation.mutateAsync(id),
      'RecurringInvoices.toast.cancelled',
      'İptal edildi.',
    );
  };

  const busy =
    pauseMutation.isPending ||
    resumeMutation.isPending ||
    cancelMutation.isPending ||
    runNowMutation.isPending;

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<Repeat size={20} />}
          title={t('RecurringInvoices.title', { defaultValue: 'Tekrarlayan Faturalar' })}
          subtitle={t('RecurringInvoices.pageSubtitle', {
            defaultValue: 'Abonelik/retainer faturalarını otomatik üretin.',
          })}
          actions={
            <Button size="sm" onClick={() => setCreateOpen(true)}>
              <Plus size={14} />
              {t('RecurringInvoices.new', { defaultValue: 'Yeni' })}
            </Button>
          }
        />
      }
      toolbar={
        <div className="flex flex-wrap items-center gap-2">
          <input
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            placeholder={t('RecurringInvoices.searchPlaceholder', { defaultValue: 'Şablon ara…' })}
            className="w-full rounded-md border border-slate-300 bg-white px-2.5 py-1.5 text-sm text-slate-800 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 sm:w-56"
          />
          <Select
            value={status}
            onChange={(e) => {
              setStatus(e.target.value as RecurringInvoiceStatus | '');
              setPage(1);
            }}
            className="w-full sm:w-44"
          >
            <option value="">
              {t('RecurringInvoices.allStatuses', { defaultValue: 'Tüm durumlar' })}
            </option>
            {STATUSES.map((s) => (
              <option key={s} value={s}>
                {statusLabel(s)}
              </option>
            ))}
          </Select>
          <span className="ml-auto text-[11px] text-slate-500 dark:text-slate-400">
            {t('RecurringInvoices.count', { defaultValue: '{{count}} şablon', count: total })}
          </span>
        </div>
      }
      pagination={
        totalPages > 1 ? (
          <div className="flex items-center justify-end gap-1 text-xs">
            <button
              type="button"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
              className="rounded border border-slate-200 bg-white px-2 py-1 disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900"
            >
              {t('common.prev', { defaultValue: 'Önceki' })}
            </button>
            <span className="px-2 text-slate-500">
              {page} / {totalPages}
            </span>
            <button
              type="button"
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="rounded border border-slate-200 bg-white px-2 py-1 disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900"
            >
              {t('common.next', { defaultValue: 'Sonraki' })}
            </button>
          </div>
        ) : undefined
      }
    >
      <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
        {listQuery.isPending ? (
          <div className="px-3 py-8 text-center text-sm text-slate-500">
            {t('common.loading', { defaultValue: 'Yükleniyor…' })}
          </div>
        ) : items.length === 0 ? (
          <div className="px-3 py-10 text-center text-sm text-slate-500 dark:text-slate-400">
            {t('RecurringInvoices.empty', {
              defaultValue: 'Tekrarlayan fatura şablonu bulunamadı.',
            })}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2 text-left">
                  {t('RecurringInvoices.cols.name', { defaultValue: 'Şablon' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('RecurringInvoices.cols.customer', { defaultValue: 'Müşteri' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('RecurringInvoices.cols.frequency', { defaultValue: 'Sıklık' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('RecurringInvoices.cols.nextRun', { defaultValue: 'Sonraki' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('RecurringInvoices.cols.generated', { defaultValue: 'Üretilen' })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('RecurringInvoices.cols.status', { defaultValue: 'Durum' })}
                </th>
                <th className="px-3 py-2" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {items.map((r) => {
                const active = r.status === 'Active';
                const paused = r.status === 'Paused';
                const editable = active || paused;
                return (
                  <tr key={r.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
                    <td className="px-3 py-2 font-medium text-slate-800 dark:text-slate-100">
                      {r.name}
                    </td>
                    <td className="px-3 py-2 text-slate-700 dark:text-slate-300">
                      {r.customerName}
                    </td>
                    <td className="px-3 py-2 text-xs text-slate-500 dark:text-slate-400">
                      {r.intervalCount > 1 ? `${r.intervalCount}× ` : ''}
                      {frequencyLabel(r.frequency)}
                    </td>
                    <td className="px-3 py-2 text-xs text-slate-500 dark:text-slate-400">
                      {formatDate(r.nextRunDate, locale)}
                    </td>
                    <td className="px-3 py-2 text-right font-mono text-slate-700 dark:text-slate-300">
                      {r.occurrencesGenerated}
                    </td>
                    <td className="px-3 py-2 text-center">
                      <Badge variant={STATUS_VARIANT[r.status]}>{statusLabel(r.status)}</Badge>
                    </td>
                    <td className="px-3 py-2 text-right">
                      <div className="inline-flex items-center gap-1">
                        {active && (
                          <button
                            type="button"
                            onClick={() => onRunNow(r.id)}
                            disabled={busy}
                            className="rounded p-1 text-success-600 hover:bg-success-50 disabled:opacity-40 dark:hover:bg-success-500/10"
                            title={t('RecurringInvoices.actions.runNow', {
                              defaultValue: 'Şimdi üret',
                            })}
                          >
                            <Send size={13} />
                          </button>
                        )}
                        {active && (
                          <button
                            type="button"
                            onClick={() => onPause(r.id)}
                            disabled={busy}
                            className="rounded p-1 text-warning-600 hover:bg-warning-50 disabled:opacity-40 dark:hover:bg-warning-500/10"
                            title={t('RecurringInvoices.actions.pause', {
                              defaultValue: 'Duraklat',
                            })}
                          >
                            <CircleStop size={13} />
                          </button>
                        )}
                        {paused && (
                          <button
                            type="button"
                            onClick={() => onResume(r.id)}
                            disabled={busy}
                            className="rounded p-1 text-primary-600 hover:bg-primary-50 disabled:opacity-40 dark:hover:bg-primary-500/10"
                            title={t('RecurringInvoices.actions.resume', {
                              defaultValue: 'Devam et',
                            })}
                          >
                            <Play size={13} />
                          </button>
                        )}
                        {editable && (
                          <button
                            type="button"
                            onClick={() => setEditId(r.id)}
                            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-700 dark:hover:text-slate-200"
                            title={t('RecurringInvoices.actions.edit', { defaultValue: 'Düzenle' })}
                          >
                            <Pencil size={13} />
                          </button>
                        )}
                        {editable && (
                          <button
                            type="button"
                            onClick={() => onCancel(r.id, r.name)}
                            disabled={busy}
                            className="rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 disabled:opacity-40 dark:hover:bg-danger-500/10"
                            title={t('RecurringInvoices.actions.cancel', { defaultValue: 'İptal' })}
                          >
                            <XCircle size={13} />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      {createOpen && (
        <RecurringInvoiceFormModal open={createOpen} onClose={() => setCreateOpen(false)} />
      )}
      {editId && editQuery.data?.data && (
        <RecurringInvoiceFormModal
          open={editId !== null}
          template={editQuery.data.data}
          onClose={() => setEditId(null)}
          onSaved={() => setEditId(null)}
        />
      )}
    </ListPageTemplate>
  );
};

export default RecurringInvoicesPage;
