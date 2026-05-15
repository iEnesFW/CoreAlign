import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  AlertCircle,
  Banknote,
  CalendarClock,
  CheckCircle2,
  CircleDot,
  Coins,
  CreditCard,
  ExternalLink,
  FileBadge,
  FileText,
  Hash,
  MapPin,
  Printer,
  Receipt,
  ShoppingCart,
  User as UserIcon,
  Wallet,
  XCircle,
} from 'lucide-react';
import { Badge } from '@/shared/ui/Badge/Badge';
import type {
  Invoice,
  InvoiceStatus,
  TaxBreakdownItem,
} from '@/features/invoices/model/invoice.types';
import type { AddressSnapshot, CustomerSnapshot } from '@/features/orders/model/order.types';

interface Props {
  invoice: Invoice;
  locale: string;
  onMarkPaid?: () => void;
  onCancel?: () => void;
  onRecordPayment?: () => void;
}

const statusStyles: Record<InvoiceStatus, string> = {
  Draft: 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
  Issued: 'bg-blue-100 text-blue-700 dark:bg-blue-500/20 dark:text-blue-300',
  Sent: 'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300',
  PartiallyPaid: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  Paid: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Overdue: 'bg-red-100 text-red-800 dark:bg-red-500/20 dark:text-red-300',
  Void: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
  Cancelled: 'bg-red-100 text-red-700 dark:bg-red-500/20 dark:text-red-300',
};

const fmtCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

const fmtDate = (iso: string | null, locale: string) => {
  if (!iso) return '—';
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(new Date(iso));
  } catch {
    return iso.slice(0, 10);
  }
};

const daysFromNow = (iso: string | null) => {
  if (!iso) return null;
  const target = new Date(iso).getTime();
  if (Number.isNaN(target)) return null;
  const dayMs = 1000 * 60 * 60 * 24;
  return Math.round((target - Date.now()) / dayMs);
};

export const InvoiceOverviewTab = ({
  invoice,
  locale,
  onMarkPaid,
  onCancel,
  onRecordPayment,
}: Props) => {
  const { t } = useTranslation();
  const dueIn = daysFromNow(invoice.dueDate);
  const paidPct = invoice.total > 0 ? (invoice.amountPaid / invoice.total) * 100 : 0;
  const showRecordPayment =
    !!onRecordPayment &&
    (invoice.status === 'Issued' ||
      invoice.status === 'Sent' ||
      invoice.status === 'PartiallyPaid' ||
      invoice.status === 'Overdue');

  return (
    <div className="space-y-3">
      <KpiRow invoice={invoice} locale={locale} paidPct={paidPct} dueIn={dueIn} />

      <MetaChips invoice={invoice} dueIn={dueIn} locale={locale} />

      <PaymentProgressBar
        total={invoice.total}
        paid={invoice.amountPaid}
        due={invoice.amountDue}
        currency={invoice.currency}
        locale={locale}
        status={invoice.status}
      />

      <FinancialBreakdown invoice={invoice} locale={locale} />

      {invoice.taxBreakdown && invoice.taxBreakdown.length > 0 && (
        <TaxBreakdownCard
          items={invoice.taxBreakdown}
          currency={invoice.currency}
          locale={locale}
        />
      )}

      <EInvoicePanel invoice={invoice} locale={locale} />

      {invoice.customerSnapshot && <CustomerSnapshotCard snapshot={invoice.customerSnapshot} />}

      <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
        <AddressSnapshotCard
          icon={<Receipt size={12} />}
          title={t('orders.detail.billingAddress')}
          snapshot={invoice.billingAddressSnapshot}
          empty={t('orders.detail.noBillingAddress')}
        />
        <AddressSnapshotCard
          icon={<MapPin size={12} />}
          title={t('orders.detail.shippingAddress')}
          snapshot={invoice.shippingAddressSnapshot}
          empty={t('orders.detail.noShippingAddress')}
        />
      </div>

      <div className="flex items-center gap-2 rounded-lg border border-slate-200 bg-white p-2.5 text-[11px] dark:border-slate-800 dark:bg-slate-900">
        <span className="text-slate-500 dark:text-slate-400">{t('orders.fields.status')}</span>
        <span
          className={`inline-flex rounded-full px-2 py-0.5 text-[10px] font-medium ${statusStyles[invoice.status]}`}
        >
          {t(`invoices.status.${invoice.status}` as never)}
        </span>
        {invoice.orderId && (
          <Link
            to={`/dashboard/orders?selected=${invoice.orderId}`}
            className="ml-auto inline-flex items-center gap-1 text-[10px] text-indigo-600 hover:underline dark:text-indigo-400"
          >
            <ShoppingCart size={11} />
            {t('invoices.detail.linkedOrder')}
            <ExternalLink size={9} />
          </Link>
        )}
      </div>

      <ActionsBar
        invoice={invoice}
        showRecordPayment={showRecordPayment}
        onRecordPayment={onRecordPayment}
        onMarkPaid={onMarkPaid}
        onCancel={onCancel}
      />
    </div>
  );
};

const KpiRow = ({
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
  indigo: 'border-indigo-200 dark:border-indigo-500/30',
  emerald: 'border-emerald-200 dark:border-emerald-500/30',
  amber: 'border-amber-200 dark:border-amber-500/30',
  red: 'border-red-200 dark:border-red-500/30',
};

const Kpi = ({
  icon,
  label,
  value,
  sub,
  tone,
}: {
  icon: React.ReactNode;
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

const MetaChips = ({
  invoice,
  dueIn,
  locale,
}: {
  invoice: Invoice;
  dueIn: number | null;
  locale: string;
}) => {
  const { t } = useTranslation();
  const chips: { icon: React.ReactNode; label: string; value: string }[] = [];
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

const PaymentProgressBar = ({
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
      ? 'bg-emerald-500'
      : status === 'Overdue'
        ? 'bg-red-500'
        : status === 'PartiallyPaid'
          ? 'bg-amber-500'
          : 'bg-indigo-500';
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
            className={`font-semibold tabular-nums ${due > 0 ? 'text-amber-600 dark:text-amber-400' : 'text-emerald-600 dark:text-emerald-400'}`}
          >
            {fmtCurrency(due, currency, locale)}
          </span>
        </span>
      </div>
    </section>
  );
};

const FinancialBreakdown = ({ invoice, locale }: { invoice: Invoice; locale: string }) => {
  const { t } = useTranslation();
  const rows = useMemo(
    () =>
      [
        { label: t('orders.detail.financial.subtotal'), value: invoice.subtotal, bold: false },
        invoice.lineDiscountTotal > 0 && {
          label: t('orders.detail.financial.lineDiscount'),
          value: -invoice.lineDiscountTotal,
          bold: false,
          tone: 'discount' as const,
        },
        invoice.headerDiscountAmount > 0 && {
          label: t('orders.detail.financial.headerDiscount', {
            pct: invoice.headerDiscountPercent,
          }),
          value: -invoice.headerDiscountAmount,
          bold: false,
          tone: 'discount' as const,
        },
        invoice.taxableTotal !== invoice.subtotal && {
          label: t('orders.detail.financial.taxable'),
          value: invoice.taxableTotal,
          bold: false,
        },
        invoice.taxTotal > 0 && {
          label: t('orders.detail.financial.tax'),
          value: invoice.taxTotal,
          bold: false,
        },
        invoice.withholdingTotal > 0 && {
          label: t('orders.detail.financial.withholding'),
          value: -invoice.withholdingTotal,
          bold: false,
          tone: 'discount' as const,
        },
        invoice.shippingCost > 0 && {
          label: t('orders.detail.financial.shipping'),
          value: invoice.shippingCost,
          bold: false,
        },
        invoice.roundingAdjustment !== 0 && {
          label: t('orders.detail.financial.rounding'),
          value: invoice.roundingAdjustment,
          bold: false,
        },
        { label: t('orders.detail.financial.total'), value: invoice.total, bold: true },
      ].filter(Boolean) as {
        label: string;
        value: number;
        bold: boolean;
        tone?: 'discount';
      }[],
    [invoice, t],
  );

  return (
    <section className="rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center gap-1.5 px-3 py-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <Banknote size={12} />
        {t('orders.detail.financial.title')}
      </header>
      <dl className="divide-y divide-slate-100 dark:divide-slate-800">
        {rows.map((row, i) => (
          <div key={`${row.label}-${i}`} className="flex items-center justify-between px-3 py-1.5">
            <dt
              className={`text-[11px] ${row.bold ? 'font-semibold text-slate-900 dark:text-slate-100' : 'text-slate-600 dark:text-slate-300'}`}
            >
              {row.label}
            </dt>
            <dd
              className={`text-[11px] tabular-nums ${
                row.bold
                  ? 'text-base font-bold text-slate-900 dark:text-slate-100'
                  : row.tone === 'discount'
                    ? 'font-medium text-emerald-600 dark:text-emerald-400'
                    : 'font-medium text-slate-700 dark:text-slate-200'
              }`}
            >
              {fmtCurrency(row.value, invoice.currency, locale)}
            </dd>
          </div>
        ))}
      </dl>
    </section>
  );
};

const TaxBreakdownCard = ({
  items,
  currency,
  locale,
}: {
  items: TaxBreakdownItem[];
  currency: string;
  locale: string;
}) => {
  const { t } = useTranslation();
  const totalBase = items.reduce((s, i) => s + i.base, 0);
  const totalTax = items.reduce((s, i) => s + i.amount, 0);
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <CircleDot size={12} />
        {t('invoices.detail.taxBreakdown')}
      </header>
      <div className="mt-2 overflow-hidden rounded border border-slate-100 dark:border-slate-800">
        <table className="w-full text-left text-[11px]">
          <thead className="bg-slate-50 dark:bg-slate-800/50">
            <tr>
              <th className="px-2 py-1 font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('invoices.detail.tax.rate')}
              </th>
              <th className="px-2 py-1 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('invoices.detail.tax.base')}
              </th>
              <th className="px-2 py-1 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('invoices.detail.tax.amount')}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {items.map((item, idx) => (
              <tr key={`${item.rate}-${idx}`}>
                <td className="px-2 py-1 font-mono">{item.rate}%</td>
                <td className="px-2 py-1 text-right tabular-nums">
                  {fmtCurrency(item.base, currency, locale)}
                </td>
                <td className="px-2 py-1 text-right font-medium tabular-nums">
                  {fmtCurrency(item.amount, currency, locale)}
                </td>
              </tr>
            ))}
          </tbody>
          <tfoot className="bg-slate-50 dark:bg-slate-800/50">
            <tr>
              <td className="px-2 py-1 text-right text-[10px] font-semibold uppercase text-slate-500 dark:text-slate-400">
                {t('orders.detail.financial.total')}
              </td>
              <td className="px-2 py-1 text-right font-semibold tabular-nums">
                {fmtCurrency(totalBase, currency, locale)}
              </td>
              <td className="px-2 py-1 text-right font-semibold tabular-nums">
                {fmtCurrency(totalTax, currency, locale)}
              </td>
            </tr>
          </tfoot>
        </table>
      </div>
    </section>
  );
};

const EInvoicePanel = ({ invoice, locale }: { invoice: Invoice; locale: string }) => {
  const { t } = useTranslation();
  if (!invoice.eInvoiceUuid && !invoice.eInvoiceStatus && !invoice.isPostedToLedger) return null;
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-2.5 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <FileBadge size={12} />
        {t('invoices.detail.eInvoice')}
      </header>
      <dl className="mt-1.5 grid grid-cols-2 gap-x-3 gap-y-1 text-[11px]">
        {invoice.eInvoiceUuid && (
          <div className="col-span-2 flex items-center justify-between gap-2">
            <dt className="text-slate-500 dark:text-slate-400">
              {t('invoices.detail.eInvoiceUuid')}
            </dt>
            <dd className="min-w-0 truncate font-mono text-slate-900 dark:text-slate-100">
              {invoice.eInvoiceUuid}
            </dd>
          </div>
        )}
        {invoice.eInvoiceStatus && (
          <div className="flex items-center justify-between gap-2">
            <dt className="text-slate-500 dark:text-slate-400">
              {t('invoices.detail.eInvoiceStatus')}
            </dt>
            <dd className="font-medium text-slate-900 dark:text-slate-100">
              {invoice.eInvoiceStatus}
            </dd>
          </div>
        )}
        <div className="flex items-center justify-between gap-2">
          <dt className="text-slate-500 dark:text-slate-400">
            {t('invoices.detail.postedToLedger')}
          </dt>
          <dd className="font-medium">
            {invoice.isPostedToLedger ? (
              <span className="inline-flex items-center gap-1 text-emerald-600 dark:text-emerald-400">
                <CheckCircle2 size={11} /> {t('common.active')}
              </span>
            ) : (
              <span className="text-slate-500">—</span>
            )}
          </dd>
        </div>
        {invoice.issuedAtUtc && (
          <div className="col-span-2 flex items-center justify-between gap-2">
            <dt className="text-slate-500 dark:text-slate-400">{t('invoices.detail.issuedAt')}</dt>
            <dd className="text-slate-700 dark:text-slate-200">
              {fmtDate(invoice.issuedAtUtc, locale)}
            </dd>
          </div>
        )}
      </dl>
    </section>
  );
};

const CustomerSnapshotCard = ({ snapshot }: { snapshot: CustomerSnapshot }) => {
  const { t } = useTranslation();
  const rows = [
    { label: t('customers.fields.code'), value: snapshot.code, mono: true },
    { label: t('customers.fields.legalName'), value: snapshot.legalName },
    { label: t('customers.fields.tradeName'), value: snapshot.tradeName },
    { label: t('customers.fields.taxNumber'), value: snapshot.taxNumber, mono: true },
    { label: t('customers.fields.taxOffice'), value: snapshot.taxOffice },
    { label: t('customers.fields.nationalId'), value: snapshot.nationalId, mono: true },
    { label: t('customers.fields.email'), value: snapshot.email },
    { label: t('customers.fields.phone'), value: snapshot.phone },
  ].filter((r) => r.value);
  if (rows.length === 0) return null;
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <UserIcon size={12} />
        {t('orders.detail.customerSnapshot')}
      </header>
      <dl className="mt-2 grid grid-cols-2 gap-x-3 gap-y-1.5 text-[11px]">
        {rows.map((row) => (
          <div key={row.label} className="flex items-center justify-between gap-2">
            <dt className="text-slate-500 dark:text-slate-400">{row.label}</dt>
            <dd
              className={`min-w-0 truncate text-right text-slate-900 dark:text-slate-100 ${row.mono ? 'font-mono' : 'font-medium'}`}
            >
              {row.value}
            </dd>
          </div>
        ))}
      </dl>
    </section>
  );
};

const AddressSnapshotCard = ({
  icon,
  title,
  snapshot,
  empty,
}: {
  icon: React.ReactNode;
  title: string;
  snapshot: AddressSnapshot | null;
  empty: string;
}) => (
  <article className="rounded-lg border border-slate-200 bg-white p-2.5 dark:border-slate-800 dark:bg-slate-900">
    <header className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {icon}
      {title}
    </header>
    <div className="mt-1.5 min-h-[44px] text-[11px] leading-tight text-slate-700 dark:text-slate-200">
      {snapshot ? (
        <>
          {snapshot.recipientName && <div className="font-semibold">{snapshot.recipientName}</div>}
          {snapshot.label && (
            <div className="text-[10px] uppercase tracking-wider text-slate-400">
              {snapshot.label}
            </div>
          )}
          <div className="mt-0.5">{snapshot.line1}</div>
          {snapshot.line2 && <div>{snapshot.line2}</div>}
          <div className="mt-0.5 text-slate-500 dark:text-slate-400">
            {[snapshot.postalCode, snapshot.city, snapshot.state, snapshot.country]
              .filter(Boolean)
              .join(', ')}
          </div>
        </>
      ) : (
        <span className="italic text-slate-400 dark:text-slate-500">{empty}</span>
      )}
    </div>
  </article>
);

const ActionsBar = ({
  invoice,
  showRecordPayment,
  onRecordPayment,
  onMarkPaid,
  onCancel,
}: {
  invoice: Invoice;
  showRecordPayment: boolean;
  onRecordPayment?: () => void;
  onMarkPaid?: () => void;
  onCancel?: () => void;
}) => {
  const { t } = useTranslation();
  return (
    <div className="flex flex-col gap-2 sm:flex-row sm:flex-wrap">
      <Link
        to={`/invoices/${invoice.id}/print`}
        target="_blank"
        rel="noopener noreferrer"
        className="inline-flex flex-1 min-w-[140px] items-center justify-center gap-2 rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
      >
        <Printer size={14} />
        {t('invoices.actions.print')}
      </Link>
      {showRecordPayment && (
        <button
          type="button"
          onClick={onRecordPayment}
          className="inline-flex flex-1 min-w-[140px] items-center justify-center gap-2 rounded-lg border border-violet-300 bg-violet-50 px-3 py-2 text-sm font-medium text-violet-700 hover:bg-violet-100 dark:border-violet-500/40 dark:bg-violet-500/10 dark:text-violet-300 dark:hover:bg-violet-500/20"
        >
          <CreditCard size={14} />
          {t('invoices.actions.recordPayment')}
        </button>
      )}
      {onMarkPaid && (
        <button
          type="button"
          onClick={onMarkPaid}
          className="inline-flex flex-1 min-w-[140px] items-center justify-center gap-2 rounded-lg border border-emerald-300 bg-emerald-50 px-3 py-2 text-sm font-medium text-emerald-700 hover:bg-emerald-100 dark:border-emerald-500/40 dark:bg-emerald-500/10 dark:text-emerald-300 dark:hover:bg-emerald-500/20"
        >
          <CheckCircle2 size={14} />
          {t('invoices.actions.markPaid')}
        </button>
      )}
      {onCancel && (
        <button
          type="button"
          onClick={onCancel}
          className="inline-flex flex-1 min-w-[140px] items-center justify-center gap-2 rounded-lg border border-red-300 bg-red-50 px-3 py-2 text-sm font-medium text-red-700 hover:bg-red-100 dark:border-red-500/40 dark:bg-red-500/10 dark:text-red-300 dark:hover:bg-red-500/20"
        >
          <XCircle size={14} />
          {t('invoices.actions.cancel')}
        </button>
      )}
    </div>
  );
};
