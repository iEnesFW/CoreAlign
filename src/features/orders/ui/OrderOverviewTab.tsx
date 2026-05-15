import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  AlertCircle,
  Banknote,
  CalendarClock,
  Edit2,
  FileText,
  Hash,
  Layers,
  MapPin,
  Package,
  Receipt,
  Truck,
  User as UserIcon,
  Wallet,
} from 'lucide-react';
import { Badge } from '@/shared/ui/Badge/Badge';
import type {
  AddressSnapshot,
  CustomerSnapshot,
  Order,
  OrderLine,
  OrderStatus,
} from '@/features/orders/model/order.types';

interface Props {
  order: Order;
  locale: string;
  onEdit: () => void;
  onGenerateInvoice?: () => void;
}

const statusStyles: Record<OrderStatus, string> = {
  Draft: 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
  Submitted: 'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300',
  Approved: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-300',
  Allocated: 'bg-violet-100 text-violet-700 dark:bg-violet-500/20 dark:text-violet-300',
  Picking: 'bg-fuchsia-100 text-fuchsia-700 dark:bg-fuchsia-500/20 dark:text-fuchsia-300',
  Packed: 'bg-purple-100 text-purple-700 dark:bg-purple-500/20 dark:text-purple-300',
  PartiallyShipped: 'bg-amber-100 text-amber-700 dark:bg-amber-500/20 dark:text-amber-300',
  Shipped: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  Delivered: 'bg-teal-100 text-teal-700 dark:bg-teal-500/20 dark:text-teal-300',
  Closed: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Cancelled: 'bg-red-100 text-red-700 dark:bg-red-500/20 dark:text-red-300',
  Returned: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
  Confirmed: 'bg-blue-100 text-blue-700 dark:bg-blue-500/20 dark:text-blue-300',
};

const fmtCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

const fmtNumber = (value: number, locale: string) => {
  try {
    return new Intl.NumberFormat(locale).format(value);
  } catch {
    return `${value}`;
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

export const OrderOverviewTab = ({ order, locale, onEdit, onGenerateInvoice }: Props) => {
  const { t } = useTranslation();
  const totalQty = order.lines.reduce((s, l) => s + l.quantity, 0);
  const shippedQty = order.lines.reduce((s, l) => s + l.quantityShipped, 0);
  const invoicedQty = order.lines.reduce((s, l) => s + l.quantityInvoiced, 0);
  const dueIn = daysFromNow(order.dueDate);
  const showInvoiceCta = !!onGenerateInvoice;

  return (
    <div className="space-y-3">
      <KpiRow
        order={order}
        locale={locale}
        totalQty={totalQty}
        shippedQty={shippedQty}
        invoicedQty={invoicedQty}
      />

      <MetaChips order={order} dueIn={dueIn} />

      <FinancialBreakdown order={order} locale={locale} />

      {order.customerSnapshot && <CustomerSnapshotCard snapshot={order.customerSnapshot} />}

      <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
        <AddressSnapshotCard
          icon={<Receipt size={12} />}
          title={t('orders.detail.billingAddress')}
          snapshot={order.billingAddressSnapshot}
          empty={t('orders.detail.noBillingAddress')}
        />
        <AddressSnapshotCard
          icon={<MapPin size={12} />}
          title={t('orders.detail.shippingAddress')}
          snapshot={order.shippingAddressSnapshot}
          empty={t('orders.detail.noShippingAddress')}
        />
      </div>

      <LineProgressList lines={order.lines} locale={locale} />

      <div className="flex items-center gap-2 rounded-lg border border-slate-200 bg-white p-2.5 text-[11px] dark:border-slate-800 dark:bg-slate-900">
        <span className="text-slate-500 dark:text-slate-400">{t('orders.fields.status')}</span>
        <span
          className={`inline-flex rounded-full px-2 py-0.5 text-[10px] font-medium ${statusStyles[order.status]}`}
        >
          {t(`orders.status.${order.status}` as never)}
        </span>
        <span className="ml-auto text-[10px] text-slate-500 dark:text-slate-400">
          {t('orders.fields.orderDate')}: {fmtDate(order.orderDate, locale)}
        </span>
      </div>

      <div className="flex flex-col gap-2 sm:flex-row">
        <button
          type="button"
          onClick={onEdit}
          className="inline-flex flex-1 items-center justify-center gap-2 rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
        >
          <Edit2 size={14} />
          {t('common.edit')}
        </button>
        {showInvoiceCta && (
          <button
            type="button"
            onClick={onGenerateInvoice}
            className="inline-flex flex-1 items-center justify-center gap-2 rounded-lg border border-violet-300 bg-violet-50 px-3 py-2 text-sm font-medium text-violet-700 hover:bg-violet-100 dark:border-violet-500/40 dark:bg-violet-500/10 dark:text-violet-300 dark:hover:bg-violet-500/20"
          >
            <FileText size={14} />
            {t('orders.actions.generateInvoice')}
          </button>
        )}
      </div>
    </div>
  );
};

const KpiRow = ({
  order,
  locale,
  totalQty,
  shippedQty,
  invoicedQty,
}: {
  order: Order;
  locale: string;
  totalQty: number;
  shippedQty: number;
  invoicedQty: number;
}) => {
  const { t } = useTranslation();
  return (
    <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
      <Kpi
        icon={<Banknote size={11} />}
        label={t('orders.detail.metrics.total')}
        value={fmtCurrency(order.total, order.currency, locale)}
        tone="indigo"
      />
      <Kpi
        icon={<Layers size={11} />}
        label={t('orders.detail.metrics.lines')}
        value={String(order.lines.length)}
        sub={`${fmtNumber(totalQty, locale)} ${t('orders.detail.metrics.units')}`}
        tone="blue"
      />
      <Kpi
        icon={<Truck size={11} />}
        label={t('orders.detail.metrics.shipped')}
        value={`${fmtNumber(shippedQty, locale)} / ${fmtNumber(totalQty, locale)}`}
        sub={t('orders.detail.metrics.units')}
        tone={shippedQty >= totalQty ? 'emerald' : shippedQty > 0 ? 'amber' : 'slate'}
      />
      <Kpi
        icon={<FileText size={11} />}
        label={t('orders.detail.metrics.invoiced')}
        value={`${fmtNumber(invoicedQty, locale)} / ${fmtNumber(totalQty, locale)}`}
        sub={t('orders.detail.metrics.units')}
        tone={invoicedQty >= totalQty ? 'emerald' : invoicedQty > 0 ? 'amber' : 'slate'}
      />
    </div>
  );
};

const kpiTones: Record<'slate' | 'indigo' | 'blue' | 'emerald' | 'amber', string> = {
  slate: 'border-slate-200 dark:border-slate-800',
  indigo: 'border-indigo-200 dark:border-indigo-500/30',
  blue: 'border-blue-200 dark:border-blue-500/30',
  emerald: 'border-emerald-200 dark:border-emerald-500/30',
  amber: 'border-amber-200 dark:border-amber-500/30',
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

const MetaChips = ({ order, dueIn }: { order: Order; dueIn: number | null }) => {
  const { t } = useTranslation();
  const chips: { icon: React.ReactNode; label: string; value: string }[] = [];
  chips.push({
    icon: <Wallet size={11} />,
    label: t('orders.fields.currency'),
    value: `${order.currency}${order.exchangeRate && order.exchangeRate !== 1 ? ` · ${order.exchangeRate.toFixed(4)}` : ''}`,
  });
  if (order.paymentTermsNetDaysSnapshot !== null) {
    chips.push({
      icon: <CalendarClock size={11} />,
      label: t('orders.fields.terms'),
      value: t('customers.detail.meta.netDays', { count: order.paymentTermsNetDaysSnapshot }),
    });
  }
  if (order.dueDate) {
    chips.push({
      icon: <CalendarClock size={11} />,
      label: t('orders.fields.dueDate'),
      value:
        dueIn !== null
          ? dueIn === 0
            ? t('orders.dueToday', { defaultValue: 'Due today' })
            : dueIn > 0
              ? t('orders.dueIn', { count: dueIn, defaultValue: `Due in ${dueIn}d` })
              : t('orders.overdueBy', { count: -dueIn, defaultValue: `Overdue ${-dueIn}d` })
          : fmtDate(order.dueDate, 'en-US'),
    });
  }
  if (order.requestedDeliveryDate) {
    chips.push({
      icon: <Truck size={11} />,
      label: t('orders.fields.requestedDelivery'),
      value: fmtDate(order.requestedDeliveryDate, 'en-US'),
    });
  }
  if (order.channel) {
    chips.push({
      icon: <Hash size={11} />,
      label: t('orders.fields.channel'),
      value: order.channel,
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
      {dueIn !== null && dueIn < 0 && (
        <Badge variant="error" pill>
          <AlertCircle size={9} className="mr-1" />
          {t('orders.overdue', { defaultValue: 'Overdue' })}
        </Badge>
      )}
    </div>
  );
};

const FinancialBreakdown = ({ order, locale }: { order: Order; locale: string }) => {
  const { t } = useTranslation();
  const rows = useMemo(
    () =>
      [
        { label: t('orders.detail.financial.subtotal'), value: order.subtotal, bold: false },
        order.lineDiscountTotal > 0 && {
          label: t('orders.detail.financial.lineDiscount'),
          value: -order.lineDiscountTotal,
          bold: false,
          tone: 'discount' as const,
        },
        order.headerDiscountAmount > 0 && {
          label: t('orders.detail.financial.headerDiscount', {
            pct: order.headerDiscountPercent,
          }),
          value: -order.headerDiscountAmount,
          bold: false,
          tone: 'discount' as const,
        },
        order.taxableTotal !== order.subtotal && {
          label: t('orders.detail.financial.taxable'),
          value: order.taxableTotal,
          bold: false,
        },
        order.taxTotal > 0 && {
          label: t('orders.detail.financial.tax'),
          value: order.taxTotal,
          bold: false,
        },
        order.withholdingTotal > 0 && {
          label: t('orders.detail.financial.withholding'),
          value: -order.withholdingTotal,
          bold: false,
          tone: 'discount' as const,
        },
        order.shippingCost > 0 && {
          label: t('orders.detail.financial.shipping'),
          value: order.shippingCost,
          bold: false,
        },
        order.roundingAdjustment !== 0 && {
          label: t('orders.detail.financial.rounding'),
          value: order.roundingAdjustment,
          bold: false,
        },
        { label: t('orders.detail.financial.total'), value: order.total, bold: true },
      ].filter(Boolean) as {
        label: string;
        value: number;
        bold: boolean;
        tone?: 'discount';
      }[],
    [order, t],
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
              {fmtCurrency(row.value, order.currency, locale)}
            </dd>
          </div>
        ))}
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
          {snapshot.phone && (
            <div className="mt-0.5 text-[10px] text-slate-500 dark:text-slate-400">
              {snapshot.phone}
            </div>
          )}
        </>
      ) : (
        <span className="italic text-slate-400 dark:text-slate-500">{empty}</span>
      )}
    </div>
  </article>
);

const LineProgressList = ({ lines, locale }: { lines: OrderLine[]; locale: string }) => {
  const { t } = useTranslation();
  if (lines.length === 0) return null;
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <Package size={12} />
          {t('orders.detail.lineProgress')}
        </span>
        <span className="text-slate-400">{lines.length}</span>
      </header>
      <ul className="mt-2 space-y-1.5">
        {lines.map((line) => {
          const shippedPct = line.quantity > 0 ? (line.quantityShipped / line.quantity) * 100 : 0;
          const invoicedPct = line.quantity > 0 ? (line.quantityInvoiced / line.quantity) * 100 : 0;
          return (
            <li
              key={line.id}
              className="space-y-1 rounded border border-slate-200 px-2 py-1.5 dark:border-slate-800"
            >
              <div className="flex items-center justify-between gap-2 text-[11px]">
                <div className="min-w-0">
                  <div className="truncate font-medium text-slate-900 dark:text-slate-100">
                    {line.productName}
                  </div>
                  <div className="font-mono text-[9px] text-slate-500 dark:text-slate-400">
                    {line.productSku}
                  </div>
                </div>
                <div className="shrink-0 text-right text-[10px] text-slate-500 dark:text-slate-400">
                  {fmtNumber(line.quantity, locale)} {line.uomCode ?? ''}
                </div>
              </div>
              <ProgressRow
                label={t('orders.detail.lineProgressShipped')}
                done={line.quantityShipped}
                total={line.quantity}
                pct={shippedPct}
                locale={locale}
                color="bg-amber-500"
              />
              <ProgressRow
                label={t('orders.detail.lineProgressInvoiced')}
                done={line.quantityInvoiced}
                total={line.quantity}
                pct={invoicedPct}
                locale={locale}
                color="bg-violet-500"
              />
            </li>
          );
        })}
      </ul>
    </section>
  );
};

const ProgressRow = ({
  label,
  done,
  total,
  pct,
  locale,
  color,
}: {
  label: string;
  done: number;
  total: number;
  pct: number;
  locale: string;
  color: string;
}) => (
  <div>
    <div className="flex items-center justify-between text-[9px] text-slate-500 dark:text-slate-400">
      <span>{label}</span>
      <span className="tabular-nums">
        {fmtNumber(done, locale)} / {fmtNumber(total, locale)}
      </span>
    </div>
    <div className="mt-0.5 h-1 w-full overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
      <div
        className={`h-full rounded-full transition-all ${color}`}
        style={{ width: `${Math.min(100, Math.max(0, pct))}%` }}
      />
    </div>
  </div>
);
