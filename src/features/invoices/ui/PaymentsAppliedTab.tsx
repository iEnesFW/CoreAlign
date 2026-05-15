import { useTranslation } from 'react-i18next';
import { ArrowDownToLine, CreditCard, Receipt } from 'lucide-react';
import { usePaymentsByInvoice } from '@/features/payments/hooks/usePaymentQueries';

interface Props {
  invoiceId: string;
  currency: string;
  locale: string;
  amountPaid: number;
  amountDue: number;
  total: number;
  onRecordPayment?: () => void;
}

const fmtCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

const fmtDateTime = (iso: string, locale: string) => {
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(
      new Date(iso),
    );
  } catch {
    return iso;
  }
};

export const PaymentsAppliedTab = ({
  invoiceId,
  currency,
  locale,
  amountPaid,
  amountDue,
  total,
  onRecordPayment,
}: Props) => {
  const { t } = useTranslation();
  const query = usePaymentsByInvoice(invoiceId);
  const applications = query.data?.data ?? [];

  return (
    <div className="space-y-3">
      <div className="grid grid-cols-3 gap-2">
        <Stat
          label={t('invoices.detail.metrics.totalInvoice')}
          value={fmtCurrency(total, currency, locale)}
          tone="indigo"
        />
        <Stat
          label={t('invoices.detail.metrics.paid')}
          value={fmtCurrency(amountPaid, currency, locale)}
          tone="emerald"
        />
        <Stat
          label={t('invoices.detail.metrics.due')}
          value={fmtCurrency(amountDue, currency, locale)}
          tone={amountDue > 0 ? 'amber' : 'slate'}
        />
      </div>

      {onRecordPayment && amountDue > 0 && (
        <button
          type="button"
          onClick={onRecordPayment}
          className="inline-flex w-full items-center justify-center gap-2 rounded-lg border border-violet-300 bg-violet-50 px-3 py-2 text-sm font-medium text-violet-700 hover:bg-violet-100 dark:border-violet-500/40 dark:bg-violet-500/10 dark:text-violet-300 dark:hover:bg-violet-500/20"
        >
          <CreditCard size={14} />
          {t('invoices.actions.recordPayment')}
        </button>
      )}

      <section className="rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
        <header className="flex items-center justify-between gap-2 border-b border-slate-100 px-3 py-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:border-slate-800 dark:text-slate-400">
          <span className="inline-flex items-center gap-1.5">
            <ArrowDownToLine size={12} />
            {t('invoices.detail.paymentsApplied')}
          </span>
          <span className="text-slate-400">{applications.length}</span>
        </header>
        {query.isPending ? (
          <div className="p-4 text-center text-[11px] italic text-slate-400">
            {t('common.loading')}
          </div>
        ) : applications.length === 0 ? (
          <div className="p-4 text-center text-[11px] italic text-slate-400">
            {t('invoices.detail.noPayments')}
          </div>
        ) : (
          <ul className="divide-y divide-slate-100 dark:divide-slate-800">
            {applications.map((app) => (
              <li
                key={app.id}
                className="flex items-center justify-between gap-2 px-3 py-2 text-[11px]"
              >
                <div className="flex min-w-0 items-center gap-2">
                  <span className="inline-flex h-6 w-6 shrink-0 items-center justify-center rounded bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300">
                    <Receipt size={12} />
                  </span>
                  <div className="min-w-0">
                    <div className="font-mono text-[11px] font-medium text-slate-900 dark:text-slate-100">
                      {app.paymentNumber || '—'}
                    </div>
                    <div className="text-[10px] text-slate-500 dark:text-slate-400">
                      {fmtDateTime(app.appliedAtUtc, locale)}
                      {app.paymentMethod
                        ? ` · ${t(`invoices.paymentMethod.${app.paymentMethod}` as never, { defaultValue: app.paymentMethod })}`
                        : ''}
                    </div>
                  </div>
                </div>
                <div className="shrink-0 text-right">
                  <div className="text-[11px] font-semibold tabular-nums text-emerald-600 dark:text-emerald-400">
                    {fmtCurrency(app.appliedAmount, currency, locale)}
                  </div>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
};

const statTones: Record<'slate' | 'indigo' | 'emerald' | 'amber', string> = {
  slate: 'border-slate-200 dark:border-slate-800',
  indigo: 'border-indigo-200 dark:border-indigo-500/30',
  emerald: 'border-emerald-200 dark:border-emerald-500/30',
  amber: 'border-amber-200 dark:border-amber-500/30',
};

const Stat = ({
  label,
  value,
  tone,
}: {
  label: string;
  value: string;
  tone: keyof typeof statTones;
}) => (
  <div className={`rounded-lg border bg-white p-2 dark:bg-slate-900 ${statTones[tone]}`}>
    <div className="text-[9px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {label}
    </div>
    <div className="mt-0.5 text-sm font-bold tabular-nums text-slate-900 dark:text-slate-100">
      {value}
    </div>
  </div>
);
