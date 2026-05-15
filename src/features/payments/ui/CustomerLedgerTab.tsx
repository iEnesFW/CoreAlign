import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertCircle, ArrowDownRight, ArrowUpRight, Download, Plus, Receipt } from 'lucide-react';
import {
  useCustomerAging,
  useCustomerLedger,
  usePaymentsByCustomer,
} from '../hooks/usePaymentQueries';
import type { CustomerLedgerEntry, LedgerEntryType } from '../model/payment.types';
import { PaymentCreateModal } from './PaymentCreateModal';

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
  Debit: <ArrowUpRight size={12} className="text-red-600 dark:text-red-400" />,
  Credit: <ArrowDownRight size={12} className="text-emerald-600 dark:text-emerald-400" />,
};

export const CustomerLedgerTab = ({ customerId, customerName, currency }: Props) => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const [paymentModalOpen, setPaymentModalOpen] = useState(false);
  const [page, setPage] = useState(1);
  const [fromUtc, setFromUtc] = useState<string | undefined>(undefined);
  const [toUtc, setToUtc] = useState<string | undefined>(undefined);

  const ledgerQuery = useCustomerLedger(customerId, fromUtc, toUtc, page, 25);
  const agingQuery = useCustomerAging(customerId);
  const paymentsQuery = usePaymentsByCustomer(customerId);

  const ledger = useMemo(
    () => ledgerQuery.data?.data?.items ?? [],
    [ledgerQuery.data?.data?.items],
  );
  const ledgerTotal = ledgerQuery.data?.data?.total ?? 0;
  const totalPages = ledgerQuery.data?.data?.totalPages ?? 0;
  const aging = agingQuery.data?.data;
  const payments = paymentsQuery.data?.data ?? [];

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
          <button
            type="button"
            onClick={() => setPaymentModalOpen(true)}
            className="inline-flex items-center gap-1.5 rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-emerald-700"
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
              color="bg-emerald-400"
            />
            <AgingSegment
              value={aging.days1To30}
              total={aging.totalOutstanding}
              color="bg-amber-400"
            />
            <AgingSegment
              value={aging.days31To60}
              total={aging.totalOutstanding}
              color="bg-orange-500"
            />
            <AgingSegment
              value={aging.days61To90}
              total={aging.totalOutstanding}
              color="bg-red-500"
            />
            <AgingSegment
              value={aging.daysOver90}
              total={aging.totalOutstanding}
              color="bg-rose-700"
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
            <div className="mt-2 flex items-center gap-1.5 text-[11px] text-rose-700 dark:text-rose-300">
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
          <span className="text-[11px] text-slate-500">{ledgerTotal} entries</span>
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
                  <td className="px-3 py-2 text-right font-mono text-red-700 dark:text-red-300">
                    {e.entryType === 'Debit' ? fmtCurrency(e.amount, e.currency, locale) : '—'}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-emerald-700 dark:text-emerald-300">
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
                  <div className="font-mono text-slate-800 dark:text-slate-100">
                    {p.paymentNumber}
                  </div>
                  <div className="text-[11px] text-slate-500">
                    {fmtDateTime(p.paymentDate, locale)} ·{' '}
                    {t(`invoices.paymentMethod.${p.method}` as never)}
                    {p.unappliedAmount > 0 && (
                      <span className="ml-2 inline-flex rounded bg-amber-100 px-1.5 text-[10px] font-medium text-amber-800 dark:bg-amber-500/20 dark:text-amber-300">
                        {fmtCurrency(p.unappliedAmount, p.currency, locale)}{' '}
                        {t('payments.list.unapplied', { defaultValue: 'unapplied' })}
                      </span>
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
  emerald: 'bg-emerald-400',
  amber: 'bg-amber-400',
  orange: 'bg-orange-500',
  red: 'bg-red-500',
  rose: 'bg-rose-700',
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
      className="rounded border border-slate-200 bg-white px-1.5 py-1 text-[11px] text-slate-900 focus:border-indigo-500 focus:outline-none dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
    />
  </label>
);

const csvEscape = (value: string | number | null | undefined): string => {
  if (value === null || value === undefined) return '';
  const s = String(value);
  if (s.includes(',') || s.includes('"') || s.includes('\n')) {
    return `"${s.replace(/"/g, '""')}"`;
  }
  return s;
};

const buildCsvDownloader = (entries: CustomerLedgerEntry[], customerName: string) => () => {
  if (entries.length === 0) return;
  const headers = [
    'OccurredAt',
    'PostingDate',
    'Type',
    'Source',
    'Reference',
    'Description',
    'Currency',
    'Amount',
    'AmountBase',
    'RunningBalance',
  ];
  const rows = entries.map((e) => [
    e.occurredAtUtc,
    e.postingDate,
    e.entryType,
    e.sourceType,
    e.sourceDocumentNumber ?? '',
    e.description ?? '',
    e.currency,
    e.entryType === 'Debit' ? e.amount : -e.amount,
    e.amountInBase,
    e.runningBalanceAfter,
  ]);
  const csv = [headers, ...rows].map((row) => row.map(csvEscape).join(',')).join('\r\n');
  const blob = new Blob(['﻿', csv], { type: 'text/csv;charset=utf-8' });
  const safeName = customerName.replace(/[^a-z0-9-_]+/gi, '_').slice(0, 64) || 'customer';
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = `ledger_${safeName}_${new Date().toISOString().slice(0, 10)}.csv`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(link.href);
};
