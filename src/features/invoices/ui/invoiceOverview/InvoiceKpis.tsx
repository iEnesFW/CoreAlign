import { type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertCircle, Banknote, CalendarClock, Coins, Wallet, FileText, Hash } from 'lucide-react';
import { Badge } from '@/shared/ui/Badge/Badge';
import type { Invoice, InvoiceStatus } from '@/features/invoices/model/invoice.types';
import { fmtCurrency, fmtDate } from './format';

export const KpiRow = ({
  invoice,
  locale,
  paidPct,
  dueIn,
}: {
  invoice: Invoice;
  locale: string;
  paidPct: number;
  dueIn: number | null;
}) => {
  const { t } = useTranslation();
  const dueTone =
    invoice.amountDue <= 0
      ? 'emerald'
      : dueIn !== null && dueIn < 0
        ? 'red'
        : dueIn !== null && dueIn <= 7
          ? 'amber'
          : 'slate';
  return (
    <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
      <Kpi
        icon={<Banknote size={11} />}
        label={t('invoices.fields.total')}
        value={fmtCurrency(invoice.total, invoice.currency, locale)}
        tone="indigo"
      />
      <Kpi
        icon={<Coins size={11} />}
        label={t('invoices.detail.metrics.paid')}
        value={fmtCurrency(invoice.amountPaid, invoice.currency, locale)}
        sub={`${paidPct.toFixed(0)}%`}
        tone={invoice.amountPaid > 0 ? 'emerald' : 'slate'}
      />
      <Kpi
        icon={<AlertCircle size={11} />}
        label={t('invoices.detail.metrics.due')}
        value={fmtCurrency(invoice.amountDue, invoice.currency, locale)}
        tone={dueTone}
      />
      <Kpi
        icon={<CalendarClock size={11} />}
        label={t('invoices.fields.dueDate')}
        value={fmtDate(invoice.dueDate, locale)}
        sub={
          dueIn === null
            ? undefined
            : dueIn === 0
              ? t('orders.dueToday')
              : dueIn > 0
                ? t('orders.dueIn', { count: dueIn })
                : t('orders.overdueBy', { count: -dueIn })
        }
        tone={
          dueIn !== null && dueIn < 0 ? 'red' : dueIn !== null && dueIn <= 7 ? 'amber' : 'slate'
        }
      />
    </div>
  );
};

const kpiTones: Record<'slate' | 'indigo' | 'emerald' | 'amber' | 'red', string> = {
  slate: 'border-slate-200 dark:border-slate-800',
  indigo: 'border-primary-200 dark:border-primary-500/30',
  emerald: 'border-success-200 dark:border-success-500/30',
  amber: 'border-warning-200 dark:border-warning-500/30',
  red: 'border-danger-200 dark:border-danger-500/30',
};

const Kpi = ({
  icon,
  label,
  value,
  sub,
  tone,
}: {
  icon: ReactNode;
  label: string;
  value: string;
  sub?: string;
  tone: keyof typeof kpiTones;
}) => (
  <div className={`rounded-lg border bg-white p-2 dark:bg-slate-900 ${kpiTones[tone]}`}>
    <div className="flex items-center gap-1 text-[9px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {icon}
      <span>{label}</span>
    </div>
    <div className="mt-0.5 text-sm font-bold tabular-nums text-slate-900 dark:text-slate-100">
      {value}
    </div>
    {sub && <div className="text-[9px] text-slate-500 dark:text-slate-400">{sub}</div>}
  </div>
);

export const MetaChips = ({
  invoice,
  dueIn,
  locale,
}: {
  invoice: Invoice;
  dueIn: number | null;
  locale: string;
}) => {
  const { t } = useTranslation();
  const chips: { icon: ReactNode; label: string; value: string }[] = [];
  chips.push({
    icon: <Wallet size={11} />,
    label: t('invoices.fields.currency'),
    value: `${invoice.currency}${invoice.exchangeRate && invoice.exchangeRate !== 1 ? ` · ${invoice.exchangeRate.toFixed(4)}` : ''}`,
  });
  if (invoice.paymentTermsNetDaysSnapshot !== null) {
    chips.push({
      icon: <CalendarClock size={11} />,
      label: t('orders.fields.terms'),
      value: t('customers.detail.meta.netDays', { count: invoice.paymentTermsNetDaysSnapshot }),
    });
  }
  chips.push({
    icon: <FileText size={11} />,
    label: t('invoices.fields.type'),
    value: t(`invoices.type.${invoice.type}`, { defaultValue: invoice.type }),
  });
  if (invoice.postingDate) {
    chips.push({
      icon: <Hash size={11} />,
      label: t('invoices.fields.postingDate'),
      value: fmtDate(invoice.postingDate, locale),
    });
  }
  return (
    <div className="flex flex-wrap items-center gap-1.5">
      {chips.map((chip) => (
        <span
          key={`${chip.label}-${chip.value}`}
          className="inline-flex items-center gap-1 rounded-full border border-slate-200 bg-slate-50 px-2 py-0.5 text-[10px] text-slate-700 dark:border-slate-800 dark:bg-slate-800/60 dark:text-slate-200"
        >
          {chip.icon}
          <span className="font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
            {chip.label}
          </span>
          <span className="font-medium">{chip.value}</span>
        </span>
      ))}
      {invoice.isOverdue && (
        <Badge variant="error" pill>
          <AlertCircle size={9} className="mr-1" />
          {t('orders.overdue')}
        </Badge>
      )}
      {dueIn !== null && dueIn >= 0 && dueIn <= 7 && !invoice.isOverdue && (
        <Badge variant="warning" pill>
          <CalendarClock size={9} className="mr-1" />
          {t('invoices.dueSoon', { defaultValue: 'Due soon' })}
        </Badge>
      )}
    </div>
  );
};

export const PaymentProgressBar = ({
  total,
  paid,
  due,
  currency,
  locale,
  status,
}: {
  total: number;
  paid: number;
  due: number;
  currency: string;
  locale: string;
  status: InvoiceStatus;
}) => {
  const { t } = useTranslation();
  const pct = total > 0 ? Math.min(100, (paid / total) * 100) : 0;
  const color =
    status === 'Paid'
      ? 'bg-success-500'
      : status === 'Overdue'
        ? 'bg-danger-500'
        : status === 'PartiallyPaid'
          ? 'bg-warning-500'
          : 'bg-primary-500';
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <Coins size={12} />
          {t('invoices.detail.paymentProgress')}
        </span>
        <span className="tabular-nums">{pct.toFixed(0)}%</span>
      </header>
      <div className="mt-2 h-2 w-full overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
        <div
          className={`h-full rounded-full transition-all ${color}`}
          style={{ width: `${pct}%` }}
        />
      </div>
      <div className="mt-1.5 flex items-center justify-between text-[10px] text-slate-500 dark:text-slate-400">
        <span>
          {t('invoices.detail.metrics.paid')}:{' '}
          <span className="font-semibold text-slate-900 dark:text-slate-100">
            {fmtCurrency(paid, currency, locale)}
          </span>
        </span>
        <span>
          {t('invoices.detail.metrics.due')}:{' '}
          <span
            className={`font-semibold tabular-nums ${due > 0 ? 'text-warning-600 dark:text-warning-400' : 'text-success-600 dark:text-success-400'}`}
          >
            {fmtCurrency(due, currency, locale)}
          </span>
        </span>
      </div>
    </section>
  );
};
