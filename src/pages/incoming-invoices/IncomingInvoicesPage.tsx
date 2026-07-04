import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Inbox } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Select } from '@/shared/ui/Select/Select';
import {
  useIgnoreIncomingInvoice,
  useIncomingInvoicesQuery,
} from '@/features/incoming-invoices/hooks/useIncomingInvoiceQueries';
import { IncomingInvoiceList } from '@/features/incoming-invoices/ui/IncomingInvoiceList';
import { ProcessIncomingInvoiceModal } from '@/features/incoming-invoices/ui/ProcessIncomingInvoiceModal';
import type {
  IncomingInvoiceDto,
  IncomingInvoiceStatus,
} from '@/features/incoming-invoices/model/incomingInvoice.types';

const STATUSES: IncomingInvoiceStatus[] = ['New', 'Reviewed', 'Processed', 'Ignored'];
const PAGE_SIZE = 20;

export const IncomingInvoicesPage = () => {
  const { t } = useTranslation();
  const confirm = useConfirm();

  const [status, setStatus] = useState<IncomingInvoiceStatus | ''>('');
  const [page, setPage] = useState(1);
  const [processing, setProcessing] = useState<IncomingInvoiceDto | null>(null);

  const listQuery = useIncomingInvoicesQuery({
    status: status || undefined,
    page,
    pageSize: PAGE_SIZE,
  });
  const ignoreMutation = useIgnoreIncomingInvoice();

  const items = listQuery.data?.data?.items ?? [];
  const total = listQuery.data?.data?.total ?? 0;
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  const statusLabel = (s: IncomingInvoiceStatus) =>
    t(`incomingInvoices.status.${s}` as const, { defaultValue: s });

  const busy = ignoreMutation.isPending;

  const onIgnore = async (invoice: IncomingInvoiceDto) => {
    const ok = await confirm({
      title: t('incomingInvoices.ignore.title', { defaultValue: 'Faturayı Yoksay' }),
      message: t('incomingInvoices.ignore.confirm', {
        defaultValue: '{{n}} numaralı fatura yoksayılsın mı?',
        n: invoice.invoiceNumber,
      }),
      confirmLabel: t('common.confirm', { defaultValue: 'Onayla' }),
      tone: 'danger',
    });
    if (!ok) return;
    ignoreMutation.mutate(
      { id: invoice.id, input: { reason: null } },
      {
        onSuccess: (response) => {
          if (!response.isSuccess) {
            toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
            return;
          }
          toast.success(
            t('incomingInvoices.ignore.success', { defaultValue: 'Fatura yoksayıldı.' }),
          );
        },
        onError: (err: unknown) => toastApiError(err),
      },
    );
  };

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<Inbox size={20} />}
          title={t('incomingInvoices.title', { defaultValue: 'Gelen Faturalar' })}
          subtitle={t('incomingInvoices.subtitle', {
            defaultValue: 'e-Fatura sağlayıcısından gelen faturaları inceleyin ve sisteme işleyin.',
          })}
        />
      }
      toolbar={
        <div className="flex flex-wrap items-center gap-2">
          <Select
            value={status}
            onChange={(e) => {
              setStatus(e.target.value as IncomingInvoiceStatus | '');
              setPage(1);
            }}
            className="w-full sm:w-44"
          >
            <option value="">
              {t('incomingInvoices.filter.all', { defaultValue: 'Tüm durumlar' })}
            </option>
            {STATUSES.map((s) => (
              <option key={s} value={s}>
                {statusLabel(s)}
              </option>
            ))}
          </Select>
          <span className="ml-auto text-[11px] text-slate-500 dark:text-slate-400">
            {t('incomingInvoices.count', { defaultValue: '{{count}} fatura', count: total })}
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
            <span className="px-2 text-slate-500 dark:text-slate-400">
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
      <IncomingInvoiceList
        items={items}
        isLoading={listQuery.isPending}
        busy={busy}
        onProcess={(invoice) => setProcessing(invoice)}
        onIgnore={onIgnore}
      />

      {processing && (
        <ProcessIncomingInvoiceModal
          invoice={processing}
          onClose={() => setProcessing(null)}
          onProcessed={() => setProcessing(null)}
        />
      )}
    </ListPageTemplate>
  );
};

export default IncomingInvoicesPage;
