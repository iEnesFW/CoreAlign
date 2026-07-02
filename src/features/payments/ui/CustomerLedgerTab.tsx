import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  AlertCircle,
  ArrowDownRight,
  ArrowUpRight,
  Download,
  Plus,
  Receipt,
  Wallet,
} from 'lucide-react';
import { toast } from 'sonner';
import { downloadCsv } from '@/shared/lib/exportCsv';
import { toastApiError } from '@/shared/lib/mutationToast';
import {
  useApplyPaymentFifo,
  useCustomerAging,
  useCustomerLedger,
  usePaymentsByCustomer,
} from '../hooks/usePaymentQueries';
import type { CustomerLedgerEntry, LedgerEntryType } from '../model/payment.types';
import { PaymentCreateModal } from './PaymentCreateModal';
import { AdvanceOffsetModal } from './AdvanceOffsetModal';

interface Props {
  customerId: string;
  customerName: string;
  currency: string;
}

const fmtCurrency = (n: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(n);
  } catch {
    return `${n.toFixed(2)} ${currency}`;
  }
};

const fmtDateTime = (iso: string, locale: string) => {
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'short', timeStyle: 'short' }).format(
      new Date(iso),
    );
  } catch {
    return iso;
  }
};

const ENTRY_ICON: Record<LedgerEntryType, React.ReactNode> = {
  Debit: <ArrowUpRight size={12} className="text-danger-600 dark:text-danger-400" />,
  Credit: <ArrowDownRight size={12} className="text-success-600 dark:text-success-400" />,
};

export const CustomerLedgerTab = ({ customerId, customerName, currency }: Props) => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const [paymentModalOpen, setPaymentModalOpen] = useState(false);
  const [offsetModalOpen, setOffsetModalOpen] = useState(false);
  const [page, setPage] = useState(1);
  const [fromUtc, setFromUtc] = useState<string | undefined>(undefined);
  const [toUtc, setToUtc] = useState<string | undefined>(undefined);

  const ledgerQuery = useCustomerLedger(customerId, fromUtc, toUtc, page, 25);
  const agingQuery = useCustomerAging(customerId);
  const paymentsQuery = usePaymentsByCustomer(customerId);
  const applyFifo = useApplyPaymentFifo();

  const handleApplyFifo = async (id: string) => {
    try {
      await applyFifo.mutateAsync(id);
      toast.success(
        t('payments.list.fifoApplied', {
          defaultValue: 'Ödeme en eski açık faturalardan kapatıldı.',
        }),
      );
    } catch (err) {
      toastApiError(err);
    }
  };

  const ledger = useMemo(
    () => ledgerQuery.data?.data?.items ?? [],
    [ledgerQuery.data?.data?.items],
  );
  const ledgerTotal = ledgerQuery.data?.data?.total ?? 0;
  const totalPages = ledgerQuery.data?.data?.totalPages ?? 0;
  const aging = agingQuery.data?.data;
  const payments = paymentsQuery.data?.data ?? [];
  const hasAdvances = payments.some((p) => p.isAdvance && p.unappliedAmount > 0);

  const exportCsv = useMemo(() => buildCsvDownloader(ledger, customerName), [ledger, customerName]);

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-200">
          {t('payments.ledger.title', { defaultValue: 'Customer ledger' })}
        </h3>
        <div className="flex flex-wrap items-center gap-1.5">
          <DateInput
            value={fromUtc?.slice(0, 10) ?? ''}
            onChange={(v) => {
              setPage(1);
              setFromUtc(v ? new Date(v + 'T00:00:00Z').toISOString() : undefined);
            }}
            label={t('payments.ledger.from', { defaultValue: 'From' })}
          />
          <DateInput
            value={toUtc?.slice(0, 10) ?? ''}
            onChange={(v) => {
              setPage(1);
              setToUtc(v ? new Date(v + 'T23:59:59Z').toISOString() : undefined);
            }}
            label={t('payments.ledger.to', { defaultValue: 'To' })}
          />
          <button
            type="button"
            onClick={exportCsv}
            disabled={ledger.length === 0}
            className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-[11px] font-medium text-slate-700 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            title={t('payments.ledger.exportCsv', { defaultValue: 'Export CSV' })}
          >
            <Download size={11} />
            CSV
          </button>
          {hasAdvances && (
            <button
              type="button"
              onClick={() => setOffsetModalOpen(true)}
              className="inline-flex items-center gap-1.5 rounded-lg border border-amber-300 bg-amber-50 px-3 py-1.5 text-xs font-medium text-amber-800 hover:bg-amber-100 dark:border-amber-500/40 dark:bg-amber-500/10 dark:text-amber-300 dark:hover:bg-amber-500/20"
            >
              <Wallet size={12} />
              {t('Payments.offset.action', { defaultValue: 'Avans Mahsup Et' })}
            </button>
          )}
          <button
            type="button"
            onClick={() => setPaymentModalOpen(true)}
            className="inline-flex items-center gap-1.5 rounded-lg bg-success-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-success-700"
          >
            <Plus size={12} />
            {t('payments.ledger.recordPayment', { defaultValue: 'Record payment' })}
          </button>
        </div>
      </div>

      {aging && aging.totalOutstanding > 0 && (
        <div className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
          <div className="mb-2 flex items-center justify-between">
            <div className="text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              {t('payments.aging.title', { defaultValue: 'Aging analysis' })}
            </div>
            <div className="text-sm font-bold text-slate-900 dark:text-slate-100">
              {fmtCurrency(aging.totalOutstanding, aging.currency, locale)}
            </div>
          </div>
          <div className="flex h-2 overflow-hidden rounded">
            <AgingSegment
              value={aging.current}
              total={aging.totalOutstanding}
              color="bg-success-400"
            />
            <AgingSegment
              value={aging.days1To30}
              total={aging.totalOutstanding}
              color="bg-warning-400"
            />
            <AgingSegment
              value={aging.days31To60}
              total={aging.totalOutstanding}
              color="bg-warning-500"
            />
            <AgingSegment
              value={aging.days61To90}
              total={aging.totalOutstanding}
              color="bg-danger-500"
            />
            <AgingSegment
              value={aging.daysOver90}
              total={aging.totalOutstanding}
              color="bg-danger-700"
            />
          </div>
          <div className="mt-2 grid grid-cols-5 gap-1 text-[10px]">
            <AgingLegend
              label={t('payments.aging.current', { defaultValue: 'Current' })}
              amount={aging.current}
              currency={aging.currency}
              locale={locale}
              color="emerald"
            />
            <AgingLegend
              label="1-30"
              amount={aging.days1To30}
              currency={aging.currency}
              locale={locale}
              color="amber"
            />
            <AgingLegend
              label="31-60"
              amount={aging.days31To60}
              currency={aging.currency}
              locale={locale}
              color="orange"
            />
            <AgingLegend
              label="61-90"
              amount={aging.days61To90}
              currency={aging.currency}
              locale={locale}
              color="red"
            />
            <AgingLegend
              label="90+"
              amount={aging.daysOver90}
              currency={aging.currency}
              locale={locale}
              color="rose"
            />
          </div>
          {aging.daysOver90 > 0 && (
            <div className="mt-2 flex items-center gap-1.5 text-[11px] text-danger-700 dark:text-danger-300">
              <AlertCircle size={12} />
              {t('payments.aging.overdueWarning', {
                defaultValue: '{{amount}} is overdue by more than 90 days.',
                amount: fmtCurrency(aging.daysOver90, aging.currency, locale),
              })}
            </div>
          )}
        </div>
      )}

      <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
        <div className="flex items-center justify-between bg-slate-50 px-3 py-2 dark:bg-slate-900/40">
          <span className="text-xs font-semibold text-slate-700 dark:text-slate-200">
            {t('payments.ledger.entries', { defaultValue: 'Ledger entries' })}
          </span>
          <span className="text-[11px] text-slate-500 dark:text-slate-400">
            {t('payments.ledger.entryCount', {
              count: ledgerTotal,
              defaultValue: '{{count}} entries',
            })}
          </span>
        </div>
        {ledger.length === 0 ? (
          <div className="px-3 py-6 text-center text-sm text-slate-500">
            {t('payments.ledger.empty', { defaultValue: 'No ledger entries yet.' })}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2 text-left">{t('inventory.movements.when')}</th>
                <th className="px-3 py-2 text-left">
                  {t('payments.ledger.description', { defaultValue: 'Description' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('payments.ledger.debit', { defaultValue: 'Debit' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('payments.ledger.credit', { defaultValue: 'Credit' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('payments.ledger.balance', { defaultValue: 'Balance' })}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {ledger.map((e) => (
                <tr key={e.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
                  <td className="px-3 py-2 text-[11px] text-slate-500 dark:text-slate-400">
                    {fmtDateTime(e.occurredAtUtc, locale)}
                  </td>
                  <td className="px-3 py-2">
                    <div className="flex items-center gap-1.5">
                      {ENTRY_ICON[e.entryType]}
                      <span className="text-slate-800 dark:text-slate-200">{e.description}</span>
                    </div>
                    {e.sourceDocumentNumber && (
                      <div className="ml-5 font-mono text-[10px] text-slate-500">
                        {e.sourceDocumentNumber}
                      </div>
                    )}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-danger-700 dark:text-danger-300">
                    {e.entryType === 'Debit' ? fmtCurrency(e.amount, e.currency, locale) : '—'}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-success-700 dark:text-success-300">
                    {e.entryType === 'Credit' ? fmtCurrency(e.amount, e.currency, locale) : '—'}
                  </td>
                  <td className="px-3 py-2 text-right font-mono font-semibold text-slate-800 dark:text-slate-200">
                    {fmtCurrency(e.runningBalanceAfter, e.currency, locale)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        {totalPages > 1 && (
          <div className="flex items-center justify-between border-t border-slate-200 px-3 py-2 text-[11px] dark:border-slate-800">
            <span>{t('common.pagination', { page, totalPages })}</span>
            <div className="flex gap-1">
              <button
                type="button"
                disabled={page === 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                className="rounded border border-slate-200 bg-white px-2 py-1 disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900"
              >
                {t('common.prev')}
              </button>
              <button
                type="button"
                disabled={page === totalPages}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                className="rounded border border-slate-200 bg-white px-2 py-1 disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900"
              >
                {t('common.next')}
              </button>
            </div>
          </div>
        )}
      </div>

      {payments.length > 0 && (
        <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
          <div className="bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-700 dark:bg-slate-900/40 dark:text-slate-200">
            <Receipt size={12} className="mr-1 inline" />
            {t('payments.list.title', { defaultValue: 'Recent payments' })}
          </div>
          <ul className="divide-y divide-slate-200 dark:divide-slate-800">
            {payments.slice(0, 5).map((p) => (
              <li key={p.id} className="flex items-center justify-between px-3 py-2 text-sm">
                <div>
                  <div className="flex items-center gap-1.5 font-mono text-slate-800 dark:text-slate-100">
                    {p.paymentNumber}
                    {p.isAdvance && (
                      <span className="inline-flex rounded bg-amber-100 px-1.5 text-[10px] font-medium text-amber-800 dark:bg-amber-500/20 dark:text-amber-300">
                        {t('Payments.offset.advanceBadge', { defaultValue: 'Avans' })}
                      </span>
                    )}
                  </div>
                  <div className="text-[11px] text-slate-500">
                    {fmtDateTime(p.paymentDate, locale)} ·{' '}
                    {t(`invoices.paymentMethod.${p.method}` as never)}
                    {p.unappliedAmount > 0 && (
                      <span className="ml-2 inline-flex rounded bg-warning-100 px-1.5 text-[10px] font-medium text-warning-800 dark:bg-warning-500/20 dark:text-warning-300">
                        {fmtCurrency(p.unappliedAmount, p.currency, locale)}{' '}
                        {t('payments.list.unapplied', { defaultValue: 'unapplied' })}
                      </span>
                    )}
                    {p.unappliedAmount > 0 && (
                      <button
                        type="button"
                        onClick={() => handleApplyFifo(p.id)}
                        disabled={applyFifo.isPending}
                        className="ml-2 rounded border border-primary-200 px-1.5 py-0.5 text-[10px] font-medium text-primary-700 hover:bg-primary-50 disabled:opacity-50 dark:border-primary-500/30 dark:text-primary-300 dark:hover:bg-primary-500/10"
                      >
                        {t('payments.list.autoApplyOldest', { defaultValue: 'Otomatik kapat' })}
                      </button>
                    )}
                  </div>
                </div>
                <div className="text-right">
                  <div className="font-mono font-semibold text-slate-800 dark:text-slate-100">
                    {fmtCurrency(p.amount, p.currency, locale)}
                  </div>
                  <div className="text-[10px] text-slate-500">
                    {t(`invoices.paymentStatus.${p.status}` as never)}
                  </div>
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}

      {paymentModalOpen && (
        <PaymentCreateModal
          customerId={customerId}
          customerName={customerName}
          currency={currency}
          onClose={() => setPaymentModalOpen(false)}
        />
      )}

      {offsetModalOpen && (
        <AdvanceOffsetModal
          customerId={customerId}
          customerName={customerName}
          currency={currency}
          onClose={() => setOffsetModalOpen(false)}
        />
      )}
    </div>
  );
};

interface AgingSegmentProps {
  value: number;
  total: number;
  color: string;
}

const AgingSegment = ({ value, total, color }: AgingSegmentProps) => {
  const pct = total > 0 ? (value / total) * 100 : 0;
  if (pct <= 0) return null;
  return <div className={color} style={{ width: `${pct}%` }} />;
};

interface AgingLegendProps {
  label: string;
  amount: number;
  currency: string;
  locale: string;
  color: 'emerald' | 'amber' | 'orange' | 'red' | 'rose';
}

const COLOR_DOT: Record<AgingLegendProps['color'], string> = {
  emerald: 'bg-success-400',
  amber: 'bg-warning-400',
  orange: 'bg-warning-500',
  red: 'bg-danger-500',
  rose: 'bg-danger-700',
};

const AgingLegend = ({ label, amount, currency, locale, color }: AgingLegendProps) => (
  <div className="rounded border border-slate-200 bg-slate-50 p-1.5 text-center dark:border-slate-800 dark:bg-slate-800/30">
    <div className="flex items-center justify-center gap-1">
      <span className={`h-1.5 w-1.5 rounded-full ${COLOR_DOT[color]}`} />
      <span className="font-semibold text-slate-700 dark:text-slate-300">{label}</span>
    </div>
    <div className="mt-0.5 font-mono text-slate-800 dark:text-slate-200">
      {fmtCurrency(amount, currency, locale)}
    </div>
  </div>
);

const DateInput = ({
  value,
  onChange,
  label,
}: {
  value: string;
  onChange: (v: string) => void;
  label: string;
}) => (
  <label className="inline-flex items-center gap-1 text-[10px] text-slate-500 dark:text-slate-400">
    {label}
    <input
      type="date"
      value={value}
      onChange={(e) => onChange(e.target.value)}
      className="rounded border border-slate-200 bg-white px-1.5 py-1 text-[11px] text-slate-900 focus:border-primary-500 focus:outline-none dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
    />
  </label>
);

const buildCsvDownloader = (entries: CustomerLedgerEntry[], customerName: string) => () => {
  downloadCsv({
    filename: `ledger_${customerName}`,
    rows: entries,
    columns: [
      { header: 'OccurredAt', value: (e) => e.occurredAtUtc },
      { header: 'PostingDate', value: (e) => e.postingDate },
      { header: 'Type', value: (e) => e.entryType },
      { header: 'Source', value: (e) => e.sourceType },
      { header: 'Reference', value: (e) => e.sourceDocumentNumber },
      { header: 'Description', value: (e) => e.description },
      { header: 'Currency', value: (e) => e.currency },
      { header: 'Amount', value: (e) => (e.entryType === 'Debit' ? e.amount : -e.amount) },
      { header: 'AmountBase', value: (e) => e.amountInBase },
      { header: 'RunningBalance', value: (e) => e.runningBalanceAfter },
    ],
  });
};
