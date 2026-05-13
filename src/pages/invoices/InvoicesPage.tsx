import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronLeft, ChevronRight, Search } from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { InvoiceDetailPanel } from '@/features/invoices/ui/InvoiceDetailPanel';
import { InvoiceList } from '@/features/invoices/ui/InvoiceList';
import {
  useCancelInvoice,
  useInvoicesQuery,
  useMarkInvoicePaid,
} from '@/features/invoices/hooks/useInvoiceQueries';
import type { InvoiceSummary } from '@/features/invoices/model/invoice.types';

const PAGE_SIZE = 20;

export const InvoicesPage = () => {
  const { t } = useTranslation();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [viewingId, setViewingId] = useState<string | null>(null);

  const params = useMemo(
    () => ({ page, pageSize: PAGE_SIZE, search: search.trim() || undefined }),
    [page, search],
  );

  const invoicesQuery = useInvoicesQuery(params);
  const markPaidMutation = useMarkInvoicePaid();
  const cancelMutation = useCancelInvoice();
  const confirm = useConfirm();

  const result = invoicesQuery.data?.data;
  const invoices = result?.items ?? [];
  const total = result?.total ?? 0;
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  const handleMarkPaid = async (invoice: InvoiceSummary) => {
    const confirmed = await confirm({
      title: t('invoices.actions.markPaid'),
      message: t('invoices.confirmMarkPaid', { number: invoice.invoiceNumber }),
      confirmLabel: t('common.confirm'),
    });
    if (!confirmed) return;

    markPaidMutation.mutate(invoice.id, {
      onSuccess: (response) => {
        if (response.isSuccess) {
          toast.success(t('invoices.toast.paid'));
          return;
        }
        toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
      },
      onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
    });
  };

  const handleCancel = async (invoice: InvoiceSummary) => {
    const confirmed = await confirm({
      title: t('invoices.actions.cancel'),
      message: t('invoices.confirmCancel', { number: invoice.invoiceNumber }),
      confirmLabel: t('common.confirm'),
      tone: 'danger',
    });
    if (!confirmed) return;

    cancelMutation.mutate(invoice.id, {
      onSuccess: (response) => {
        if (response.isSuccess) {
          toast.success(t('invoices.toast.cancelled'));
          return;
        }
        toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
      },
      onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
    });
  };

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
            {t('invoices.title')}
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400">{t('invoices.subtitle')}</p>
        </div>

        <div className="relative">
          <Search size={14} className="absolute left-2 top-1/2 -translate-y-1/2 text-slate-400" />
          <input
            type="search"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            placeholder={t('invoices.searchPlaceholder')}
            className="w-56 rounded border border-slate-200 bg-white py-1.5 pl-7 pr-3 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          />
        </div>
      </div>

      <InvoiceList
        invoices={invoices}
        isLoading={invoicesQuery.isPending}
        selectedId={viewingId}
        onView={(invoice) => setViewingId(invoice.id)}
        onMarkPaid={handleMarkPaid}
        onCancel={handleCancel}
      />

      {total > PAGE_SIZE && (
        <div className="flex items-center justify-between text-xs text-slate-600 dark:text-slate-400">
          <div>
            {t('invoices.pagination.summary', {
              from: (page - 1) * PAGE_SIZE + 1,
              to: Math.min(page * PAGE_SIZE, total),
              total,
              defaultValue: `${(page - 1) * PAGE_SIZE + 1}-${Math.min(page * PAGE_SIZE, total)} / ${total}`,
            })}
          </div>
          <div className="flex items-center gap-1">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              className="rounded border border-slate-200 p-1.5 text-slate-600 hover:bg-slate-100 disabled:opacity-40 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800"
              aria-label={t('invoices.pagination.previous')}
            >
              <ChevronLeft size={14} />
            </button>
            <span className="px-2">
              {page} / {totalPages}
            </span>
            <button
              type="button"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              className="rounded border border-slate-200 p-1.5 text-slate-600 hover:bg-slate-100 disabled:opacity-40 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800"
              aria-label={t('invoices.pagination.next')}
            >
              <ChevronRight size={14} />
            </button>
          </div>
        </div>
      )}

      <InvoiceDetailPanel
        invoiceId={viewingId}
        onClose={() => setViewingId(null)}
        onMarkPaid={(id) => {
          const found = invoices.find((inv) => inv.id === id);
          if (found) handleMarkPaid(found);
        }}
        onCancel={(id) => {
          const found = invoices.find((inv) => inv.id === id);
          if (found) handleCancel(found);
        }}
      />
    </div>
  );
};
