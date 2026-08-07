import { useEffect, useMemo, useState } from 'react';
import { useActiveFiscalYear } from '@/shared/lib/store/fiscalYearStore';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  AlertTriangle,
  CalendarClock,
  CircleDollarSign,
  Coins,
  Download,
  FileText,
  Plus,
} from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { downloadCsv } from '@/shared/lib/exportCsv';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { StatStrip, type StatStripItem } from '@/shared/ui/StatStrip/StatStrip';
import { DataToolbar } from '@/shared/ui/DataToolbar/DataToolbar';
import { FilterChip } from '@/shared/ui/FilterChip/FilterChip';
import { SegmentedControl } from '@/shared/ui/SegmentedControl/SegmentedControl';
import { CollapsibleSection } from '@/shared/ui/CollapsibleSection/CollapsibleSection';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { Pagination } from '@/shared/ui/Pagination/Pagination';
import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue';
import { InvoiceDetailPanel } from '@/features/invoices/ui/InvoiceDetailPanel';
import { InvoiceInlineCard } from '@/features/invoices/ui/InvoiceInlineCard';
import { InvoiceList } from '@/features/invoices/ui/InvoiceList';
import {
  useCancelInvoice,
  useInvoiceAggregatesQuery,
  useInvoiceQuery,
  useInvoicesQuery,
  useMarkInvoicePaid,
} from '@/features/invoices/hooks/useInvoiceQueries';
import { PaymentCreateModal } from '@/features/payments/ui/PaymentCreateModal';
import type { InvoiceSummary } from '@/features/invoices/model/invoice.types';

const PAGE_SIZE = 10;

type StatusBucket = 'all' | 'open' | 'partiallyPaid' | 'overdue' | 'paid' | 'cancelled';

const exportInvoicesCsv = (rows: InvoiceSummary[]) =>
  downloadCsv({
    filename: 'invoices',
    rows,
    columns: [
      { header: 'InvoiceNumber', value: (i) => i.invoiceNumber },
      { header: 'OrderNumber', value: (i) => i.orderNumber ?? '' },
      { header: 'Customer', value: (i) => i.customerName },
      { header: 'IssueDate', value: (i) => i.issueDate },
      { header: 'DueDate', value: (i) => i.dueDate },
      { header: 'Status', value: (i) => i.status },
      { header: 'Currency', value: (i) => i.currency },
      { header: 'Total', value: (i) => i.total },
      { header: 'AmountPaid', value: (i) => i.amountPaid },
      { header: 'AmountDue', value: (i) => i.amountDue },
    ],
  });

export const InvoicesPage = () => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE);
  const [search, setSearch] = useState(
    () => new URLSearchParams(window.location.search).get('q') ?? '',
  );
  const debouncedSearch = useDebouncedValue(search, 300);
  const [statusBucket, setStatusBucket] = useState<StatusBucket>('all');
  const [hasDueSoonOnly, setHasDueSoonOnly] = useState(false);
  const [searchParams, setSearchParams] = useSearchParams();
  const focusFromUrl = searchParams.get('focus') ?? searchParams.get('selected');
  const [viewingId, setViewingId] = useState<string | null>(focusFromUrl);
  const [panelOpen, setPanelOpen] = useState<boolean>(!!focusFromUrl);
  const [paymentForInvoiceId, setPaymentForInvoiceId] = useState<string | null>(null);

  useEffect(() => {
    if (focusFromUrl) {
      const next = new URLSearchParams(searchParams);
      next.delete('focus');
      next.delete('selected');
      setSearchParams(next, { replace: true });
    }
  }, [focusFromUrl, searchParams, setSearchParams]);

  const searchParam = debouncedSearch.trim() || undefined;
  const fiscalYear = useActiveFiscalYear() ?? undefined;
  const params = useMemo(
    () => ({
      page,
      pageSize,
      search: searchParam,
      statusBucket: statusBucket === 'all' ? undefined : statusBucket,
      dueSoonOnly: hasDueSoonOnly || undefined,
      fiscalYear,
    }),
    [page, pageSize, searchParam, statusBucket, hasDueSoonOnly, fiscalYear],
  );

  const invoicesQuery = useInvoicesQuery(params);
  // Header KPIs + bucket counts aggregate the whole tenant result set (matching the
  // search), server-side — so they never depend on which page/bucket is visible.
  const aggregatesQuery = useInvoiceAggregatesQuery(searchParam, fiscalYear);
  const markPaidMutation = useMarkInvoicePaid();
  const cancelMutation = useCancelInvoice();
  const confirm = useConfirm();

  const result = invoicesQuery.data?.data;
  const invoices = useMemo(() => result?.items ?? [], [result?.items]);
  const total = result?.total ?? 0;

  const agg = aggregatesQuery.data?.data;
  const buckets = {
    all: agg?.totalCount ?? 0,
    open: agg?.openCount ?? 0,
    partiallyPaid: agg?.partiallyPaidCount ?? 0,
    overdue: agg?.overdueCount ?? 0,
    paid: agg?.paidCount ?? 0,
    cancelled: agg?.cancelledCount ?? 0,
  };

  const hasActiveFilters =
    statusBucket !== 'all' || hasDueSoonOnly || debouncedSearch.trim() !== '';

  const clearFilters = () => {
    setSearch('');
    setStatusBucket('all');
    setHasDueSoonOnly(false);
    setPage(1);
  };

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

  const fmtCurrency = (value: number, currency = 'TRY') => {
    try {
      return new Intl.NumberFormat(locale, {
        style: 'currency',
        currency,
        maximumFractionDigits: 0,
      }).format(value);
    } catch {
      return `${value.toFixed(0)} ${currency}`;
    }
  };

  const statItems: StatStripItem[] = [
    {
      id: 'total',
      label: t('invoices.stats.total', { defaultValue: 'Invoices' }),
      value: buckets.all,
      format: (v) => Math.round(v).toLocaleString(locale),
      icon: <FileText size={14} />,
      sub: t('invoices.stats.totalHint', {
        defaultValue: '{{count}} on this page',
        count: invoices.length,
        all: buckets.all,
      }),
      tone: 'sky',
    },
    {
      id: 'outstanding',
      label: t('invoices.stats.outstanding', { defaultValue: 'Outstanding' }),
      value: agg?.outstandingTotal ?? 0,
      format: (v) => fmtCurrency(v),
      icon: <CircleDollarSign size={14} />,
      sub: `${buckets.open + buckets.overdue} ${t('invoices.stats.openInvoices', { defaultValue: 'open' })}`,
      tone: 'amber',
    },
    {
      id: 'collected',
      label: t('invoices.stats.collected', { defaultValue: 'Collected' }),
      value: agg?.paidTotal ?? 0,
      format: (v) => fmtCurrency(v),
      icon: <Coins size={14} />,
      sub: `${buckets.paid} ${t('invoices.status.Paid').toLowerCase()}`,
      tone: 'emerald',
    },
    {
      id: 'overdue',
      label: t('invoices.stats.overdue', { defaultValue: 'Overdue' }),
      value: agg?.overdueTotal ?? 0,
      format: (v) => fmtCurrency(v),
      icon: <AlertTriangle size={14} />,
      sub: `${buckets.overdue} ${t('invoices.stats.overdueHint', { defaultValue: 'invoices past due' })}`,
      tone: (agg?.overdueTotal ?? 0) > 0 ? 'rose' : 'slate',
      onClick: () => setStatusBucket('overdue'),
    },
  ];

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <PageHeader
        icon={<FileText size={20} />}
        eyebrow={t('invoices.eyebrow', { defaultValue: 'Finance · AR' })}
        title={t('invoices.title')}
        subtitle={t('invoices.subtitle')}
        crumbs={[
          { label: t('navigation.dashboard', { defaultValue: 'Dashboard' }), to: '/dashboard' },
          { label: t('invoices.title') },
        ]}
        tone="sky"
        actions={
          <>
            <button
              type="button"
              onClick={() => exportInvoicesCsv(invoices)}
              disabled={invoices.length === 0}
              className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              <Download size={13} />
              {t('common.exportCsv', { defaultValue: 'Export CSV' })}
            </button>
            <button
              type="button"
              onClick={() => navigate('/dashboard/invoices/new')}
              className="inline-flex items-center gap-1.5 rounded-lg bg-gradient-to-r from-primary-600 to-purple-600 px-3 py-1.5 text-xs font-medium text-white shadow-md shadow-primary-500/20 transition hover:shadow-lg hover:shadow-primary-500/30 hover:-translate-y-px"
            >
              <Plus size={13} />
              {t('invoices.newInvoice')}
            </button>
          </>
        }
      />

      <CollapsibleSection
        storageKey="invoices.stats"
        label={t('Common.SummaryCards', { defaultValue: 'Özet kartları' })}
      >
        <StatStrip items={statItems} />
      </CollapsibleSection>

      <DataToolbar
        search={{
          value: search,
          onChange: (v) => {
            setPage(1);
            setSearch(v);
          },
          placeholder: t('invoices.searchPlaceholder'),
        }}
        viewMode={
          <SegmentedControl
            value={statusBucket}
            onChange={(v) => {
              setPage(1);
              setStatusBucket(v);
            }}
            options={[
              {
                value: 'all',
                label: t('invoices.filter.all', { defaultValue: 'All' }),
                count: buckets.all,
              },
              {
                value: 'open',
                label: t('invoices.filter.open', { defaultValue: 'Open' }),
                count: buckets.open,
              },
              {
                value: 'partiallyPaid',
                label: t('invoices.filter.partiallyPaid', { defaultValue: 'Partially paid' }),
                count: buckets.partiallyPaid,
              },
              {
                value: 'overdue',
                label: t('invoices.filter.overdue', { defaultValue: 'Overdue' }),
                count: buckets.overdue,
                icon: <AlertTriangle size={11} />,
              },
              {
                value: 'paid',
                label: t('invoices.filter.paid', { defaultValue: 'Paid' }),
                count: buckets.paid,
              },
              {
                value: 'cancelled',
                label: t('invoices.filter.cancelled', { defaultValue: 'Cancelled' }),
                count: buckets.cancelled,
              },
            ]}
          />
        }
        filters={
          <FilterChip
            label={t('invoices.filter.dueSoon', { defaultValue: 'Due in ≤7d' })}
            icon={<CalendarClock size={10} />}
            active={hasDueSoonOnly}
            count={agg?.dueSoonCount ?? 0}
            tone="amber"
            onClick={() => {
              setPage(1);
              setHasDueSoonOnly((v) => !v);
            }}
          />
        }
        resultCount={{
          count: total,
          label: t('invoices.resultCountLabel', { defaultValue: 'invoices' }),
        }}
        hasActiveFilters={hasActiveFilters}
        onClearFilters={clearFilters}
      />

      {invoicesQuery.isError ? (
        <QueryError onRetry={() => invoicesQuery.refetch()} isRetrying={invoicesQuery.isFetching} />
      ) : (
        <InvoiceList
          invoices={invoices}
          isLoading={invoicesQuery.isPending}
          selectedId={viewingId}
          onView={(invoice) => {
            setViewingId((curr) => (curr === invoice.id ? null : invoice.id));
            setPanelOpen(false);
          }}
          onMarkPaid={handleMarkPaid}
          onCancel={handleCancel}
        />
      )}

      {viewingId && !panelOpen && (
        <InvoiceInlineCard
          invoiceId={viewingId}
          onClose={() => setViewingId(null)}
          onOpenPanel={() => setPanelOpen(true)}
        />
      )}

      {!invoicesQuery.isError && total > 0 && (
        <div className="rounded-xl border border-slate-200/70 bg-white/60 px-3 py-2 dark:border-slate-800/70 dark:bg-slate-900/40">
          <Pagination
            page={page}
            pageSize={pageSize}
            total={total}
            onPageChange={setPage}
            pageSizeOptions={[10, 25, 50, 100]}
            onPageSizeChange={(size) => {
              setPageSize(size);
              setPage(1);
            }}
          />
        </div>
      )}

      <InvoiceDetailPanel
        invoiceId={panelOpen ? viewingId : null}
        onClose={() => setPanelOpen(false)}
        onMarkPaid={(id) => {
          const found = invoices.find((inv) => inv.id === id);
          if (found) handleMarkPaid(found);
        }}
        onCancel={(id) => {
          const found = invoices.find((inv) => inv.id === id);
          if (found) handleCancel(found);
        }}
        onRecordPayment={(id) => setPaymentForInvoiceId(id)}
      />

      {paymentForInvoiceId && (
        <PaymentModalLoader
          invoiceId={paymentForInvoiceId}
          onClose={() => setPaymentForInvoiceId(null)}
        />
      )}
    </div>
  );
};

const PaymentModalLoader = ({ invoiceId, onClose }: { invoiceId: string; onClose: () => void }) => {
  const invoiceQuery = useInvoiceQuery(invoiceId);
  const invoice = invoiceQuery.data?.data;
  if (!invoice) return null;
  return (
    <PaymentCreateModal
      customerId={invoice.customerId}
      customerName={invoice.customerName}
      currency={invoice.currency}
      onClose={onClose}
    />
  );
};
