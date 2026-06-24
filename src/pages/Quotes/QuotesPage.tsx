import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { FileText, Plus } from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { DataToolbar } from '@/shared/ui/DataToolbar/DataToolbar';
import { SegmentedControl } from '@/shared/ui/SegmentedControl/SegmentedControl';
import { Pagination } from '@/shared/ui/Pagination/Pagination';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue';
import {
  useQuotesQuery,
  useDeleteQuote,
  useSendQuote,
  useAcceptQuote,
  useRejectQuote,
  useConvertQuoteToOrder,
} from '@/features/quotes/hooks/useQuoteQueries';
import {
  QUOTE_STATUSES,
  type QuoteStatus,
  type QuoteSummary,
} from '@/features/quotes/model/quote.types';
import { CreateQuoteModal } from './components/CreateQuoteModal';
import { QuoteStatusBadge } from './components/QuoteStatusBadge';

const PAGE_SIZE = 20;

export const QuotesPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const navigate = useNavigate();
  const confirm = useConfirm();

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE);
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebouncedValue(search, 300);
  const [status, setStatus] = useState<QuoteStatus | 'all'>('all');
  const [createOpen, setCreateOpen] = useState(false);

  const params = useMemo(
    () => ({
      page,
      pageSize,
      search: debouncedSearch.trim() || undefined,
      status: status === 'all' ? undefined : status,
    }),
    [page, pageSize, debouncedSearch, status],
  );

  const quotesQuery = useQuotesQuery(params);
  const deleteMutation = useDeleteQuote();
  const sendMutation = useSendQuote();
  const acceptMutation = useAcceptQuote();
  const rejectMutation = useRejectQuote();
  const convertMutation = useConvertQuoteToOrder();

  const result = quotesQuery.data?.data;
  const quotes = result?.items ?? [];
  const total = result?.total ?? 0;

  const handleDelete = async (quote: QuoteSummary) => {
    const confirmed = await confirm({
      title: t('quotes.actions.delete'),
      message: t('quotes.confirm.delete', { number: quote.quoteNumber }),
      confirmLabel: t('common.confirm'),
      tone: 'danger',
    });
    if (!confirmed) return;
    deleteMutation.mutate(quote.id, {
      onSuccess: (response) => {
        if (response.isSuccess) {
          toast.success(t('quotes.toast.deleted'));
          return;
        }
        toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
      },
      onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
    });
  };

  const handleSend = async (quote: QuoteSummary) => {
    const confirmed = await confirm({
      title: t('quotes.actions.send'),
      message: t('quotes.confirm.send', { number: quote.quoteNumber }),
      confirmLabel: t('common.confirm'),
    });
    if (!confirmed) return;
    sendMutation.mutate(quote.id, {
      onSuccess: (response) => {
        if (response.isSuccess) {
          toast.success(t('quotes.toast.sent'));
          return;
        }
        toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
      },
      onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
    });
  };

  const handleAccept = async (quote: QuoteSummary) => {
    const confirmed = await confirm({
      title: t('quotes.actions.accept'),
      message: t('quotes.confirm.accept', { number: quote.quoteNumber }),
      confirmLabel: t('common.confirm'),
    });
    if (!confirmed) return;
    acceptMutation.mutate(quote.id, {
      onSuccess: (response) => {
        if (response.isSuccess) {
          toast.success(t('quotes.toast.accepted'));
          return;
        }
        toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
      },
      onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
    });
  };

  const handleReject = async (quote: QuoteSummary) => {
    const confirmed = await confirm({
      title: t('quotes.actions.reject'),
      message: t('quotes.confirm.reject', { number: quote.quoteNumber }),
      confirmLabel: t('common.confirm'),
      tone: 'danger',
    });
    if (!confirmed) return;
    rejectMutation.mutate(
      { id: quote.id, reason: null },
      {
        onSuccess: (response) => {
          if (response.isSuccess) {
            toast.success(t('quotes.toast.rejected'));
            return;
          }
          toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
        },
        onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
      },
    );
  };

  const handleConvert = async (quote: QuoteSummary) => {
    const confirmed = await confirm({
      title: t('quotes.actions.convertToOrder'),
      message: t('quotes.confirm.convert', { number: quote.quoteNumber }),
      confirmLabel: t('common.confirm'),
    });
    if (!confirmed) return;
    convertMutation.mutate(quote.id, {
      onSuccess: (response) => {
        if (response.isSuccess && response.data) {
          toast.success(t('quotes.toast.convertedToOrder'));
          navigate(`/dashboard/orders?focus=${response.data.id}`);
          return;
        }
        toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
      },
      onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
    });
  };

  const fmtCurrency = (value: number, currency = 'TRY') =>
    formatCurrency(value, locale, currency, 2);

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <PageHeader
        icon={<FileText size={20} />}
        eyebrow={t('quotes.eyebrow')}
        title={t('quotes.title')}
        subtitle={t('quotes.subtitle')}
        crumbs={[
          { label: t('navigation.dashboard', { defaultValue: 'Dashboard' }), to: '/dashboard' },
          { label: t('quotes.title') },
        ]}
        tone="violet"
        actions={
          <button
            type="button"
            onClick={() => setCreateOpen(true)}
            className="inline-flex items-center gap-1.5 rounded-lg bg-primary-600 px-3 py-1.5 text-xs font-medium text-white transition hover:bg-primary-500"
          >
            <Plus size={13} />
            {t('quotes.newButton')}
          </button>
        }
      />

      <DataToolbar
        search={{
          value: search,
          onChange: (v) => {
            setPage(1);
            setSearch(v);
          },
          placeholder: t('quotes.searchPlaceholder'),
        }}
        viewMode={
          <SegmentedControl
            value={status}
            onChange={(v) => {
              setPage(1);
              setStatus(v);
            }}
            options={[
              { value: 'all', label: t('common.all', { defaultValue: 'All' }) },
              ...QUOTE_STATUSES.map((s) => ({ value: s, label: t(`quotes.status.${s}`) })),
            ]}
          />
        }
        resultCount={{
          count: quotes.length,
          label: t('quotes.resultCountLabel'),
        }}
      />

      {quotesQuery.isError ? (
        <QueryError onRetry={() => quotesQuery.refetch()} isRetrying={quotesQuery.isFetching} />
      ) : (
        <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-800">
              <thead className="bg-slate-50 dark:bg-slate-800/40">
                <tr>
                  <Th>{t('quotes.columns.quoteNumber')}</Th>
                  <Th>{t('quotes.columns.customer')}</Th>
                  <Th>{t('quotes.columns.quoteDate')}</Th>
                  <Th>{t('quotes.columns.validUntil')}</Th>
                  <Th>{t('quotes.columns.status')}</Th>
                  <Th className="text-right">{t('quotes.columns.total')}</Th>
                  <Th className="text-right">{t('quotes.columns.actions')}</Th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 bg-white dark:divide-slate-800 dark:bg-slate-900">
                {quotes.length === 0 && !quotesQuery.isPending && (
                  <tr>
                    <td
                      colSpan={7}
                      className="px-4 py-12 text-center text-sm text-slate-500 dark:text-slate-400"
                    >
                      {t('quotes.empty')}
                    </td>
                  </tr>
                )}
                {quotes.map((q) => (
                  <tr key={q.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/40">
                    <Td className="font-medium text-slate-900 dark:text-slate-100">
                      {q.quoteNumber}
                    </Td>
                    <Td>{q.customerName}</Td>
                    <Td>{new Date(q.quoteDate).toLocaleDateString(locale)}</Td>
                    <Td>{new Date(q.validUntilUtc).toLocaleDateString(locale)}</Td>
                    <Td>
                      <QuoteStatusBadge status={q.status} />
                    </Td>
                    <Td className="text-right tabular-nums">{fmtCurrency(q.total, q.currency)}</Td>
                    <Td className="text-right">
                      <div className="flex flex-wrap justify-end gap-1.5">
                        {q.status === 'Draft' && (
                          <ActionButton onClick={() => handleSend(q)}>
                            {t('quotes.actions.send')}
                          </ActionButton>
                        )}
                        {q.status === 'Sent' && (
                          <>
                            <ActionButton onClick={() => handleAccept(q)} tone="success">
                              {t('quotes.actions.accept')}
                            </ActionButton>
                            <ActionButton onClick={() => handleReject(q)} tone="danger">
                              {t('quotes.actions.reject')}
                            </ActionButton>
                          </>
                        )}
                        {q.status === 'Accepted' && !q.convertedOrderId && (
                          <ActionButton onClick={() => handleConvert(q)} tone="primary">
                            {t('quotes.actions.convertToOrder')}
                          </ActionButton>
                        )}
                        {q.status === 'Draft' && (
                          <ActionButton onClick={() => handleDelete(q)} tone="danger">
                            {t('quotes.actions.delete')}
                          </ActionButton>
                        )}
                      </div>
                    </Td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {!quotesQuery.isError && total > 0 && (
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

      <CreateQuoteModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onCreated={() => {
          setCreateOpen(false);
          toast.success(t('quotes.toast.created'));
        }}
      />
    </div>
  );
};

const Th = ({ children, className }: { children: React.ReactNode; className?: string }) => (
  <th
    className={`px-4 py-2.5 text-left text-[11px] font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400 ${className ?? ''}`}
  >
    {children}
  </th>
);

const Td = ({ children, className }: { children: React.ReactNode; className?: string }) => (
  <td className={`px-4 py-2.5 text-sm text-slate-700 dark:text-slate-300 ${className ?? ''}`}>
    {children}
  </td>
);

type Tone = 'primary' | 'success' | 'danger' | 'default';

const toneClasses: Record<Tone, string> = {
  primary:
    'border-primary-300 bg-primary-50 text-primary-700 hover:bg-primary-100 dark:border-primary-700 dark:bg-primary-900/40 dark:text-primary-200 dark:hover:bg-primary-900/60',
  success:
    'border-success-300 bg-success-50 text-success-700 hover:bg-success-100 dark:border-success-700 dark:bg-success-900/40 dark:text-success-200 dark:hover:bg-success-900/60',
  danger:
    'border-danger-300 bg-danger-50 text-danger-700 hover:bg-danger-100 dark:border-danger-700 dark:bg-danger-900/40 dark:text-danger-200 dark:hover:bg-danger-900/60',
  default:
    'border-slate-200 bg-white text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800',
};

const ActionButton = ({
  children,
  onClick,
  tone = 'default',
}: {
  children: React.ReactNode;
  onClick: () => void;
  tone?: Tone;
}) => (
  <button
    type="button"
    onClick={onClick}
    className={`rounded-md border px-2 py-1 text-[11px] font-medium transition ${toneClasses[tone]}`}
  >
    {children}
  </button>
);

export default QuotesPage;
