import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  AlertTriangle,
  Building2,
  CreditCard,
  FileText,
  Mail,
  MapPin,
  Phone,
  Plus,
  Receipt,
  ShoppingCart,
  Star,
  Tag,
  User,
  UserCircle2,
  Users,
  Wallet,
} from 'lucide-react';
import { Badge } from '@/shared/ui/Badge/Badge';
import { useCustomerOverviewQuery } from '@/features/customers/hooks/useCustomerQueries';
import { useCustomerAging } from '@/features/payments/hooks/usePaymentQueries';
import type {
  Customer,
  CustomerActivityItem,
  CustomerOverview,
} from '@/features/customers/model/customer.types';
import type { CustomerAging } from '@/features/payments/model/payment.types';

interface Props {
  customer: Customer;
  locale: string;
  onEdit: () => void;
  onCreateOrder?: (customerId: string) => void;
  onCreateInvoice?: (customerId: string) => void;
  onRecordPayment?: (customerId: string) => void;
  onOpenOrder?: (orderId: string) => void;
  onOpenInvoice?: (invoiceId: string) => void;
  onOpenPayment?: (paymentId: string) => void;
}

const fmtCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

const fmtPercent = (value: number, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { maximumFractionDigits: 1 }).format(value) + '%';
  } catch {
    return `${value.toFixed(1)}%`;
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

const fmtRelative = (iso: string | null, locale: string) => {
  if (!iso) return null;
  try {
    const target = new Date(iso).getTime();
    const diffMs = Date.now() - target;
    const dayMs = 1000 * 60 * 60 * 24;
    const days = Math.floor(diffMs / dayMs);
    const rtf = new Intl.RelativeTimeFormat(locale, { numeric: 'auto' });
    if (days < 1) {
      const hours = Math.floor(diffMs / (1000 * 60 * 60));
      if (hours < 1) return rtf.format(-Math.max(1, Math.floor(diffMs / (1000 * 60))), 'minute');
      return rtf.format(-hours, 'hour');
    }
    if (days < 30) return rtf.format(-days, 'day');
    if (days < 365) return rtf.format(-Math.floor(days / 30), 'month');
    return rtf.format(-Math.floor(days / 365), 'year');
  } catch {
    return null;
  }
};

export const CustomerOverviewTab = ({
  customer,
  locale,
  onCreateOrder,
  onCreateInvoice,
  onRecordPayment,
  onOpenOrder,
  onOpenInvoice,
  onOpenPayment,
}: Props) => {
  const { t } = useTranslation();
  const overviewQuery = useCustomerOverviewQuery(customer.id);
  const overview = overviewQuery.data?.data ?? null;
  const agingQuery = useCustomerAging(customer.id);
  const aging = agingQuery.data?.data ?? null;
  const currency = customer.defaultCurrency || 'TRY';

  const blocked = customer.status === 'Blocked';
  const overdue = customer.overdueAmount > 0;

  return (
    <div className="space-y-3">
      {blocked && (
        <div className="flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 p-3 text-xs text-red-700 dark:border-red-500/30 dark:bg-red-500/10 dark:text-red-300">
          <AlertTriangle size={14} className="mt-0.5 shrink-0" />
          <div>
            <div className="font-semibold">{t('customers.detail.blockedTitle')}</div>
            {customer.blockReason && (
              <div className="mt-0.5 text-[11px] opacity-90">{customer.blockReason}</div>
            )}
          </div>
        </div>
      )}

      <CreditGaugeCard
        currentBalance={overview?.currentBalance ?? customer.currentBalance}
        creditLimit={overview?.creditLimit ?? customer.creditLimit}
        outstanding={overview?.outstanding ?? 0}
        overdue={customer.overdueAmount}
        creditUsedPercent={overview?.creditUsedPercent ?? 0}
        isOverCreditLimit={overview?.isOverCreditLimit ?? false}
        currency={currency}
        locale={locale}
        loading={overviewQuery.isPending}
      />

      {aging && aging.totalOutstanding > 0 && <AgingMiniCard aging={aging} locale={locale} />}

      <QuickActionsBar
        customerId={customer.id}
        blocked={blocked}
        onCreateOrder={onCreateOrder}
        onCreateInvoice={onCreateInvoice}
        onRecordPayment={onRecordPayment}
      />

      <SnapshotCard customer={customer} />

      <MetaChips overview={overview} loading={overviewQuery.isPending} />

      <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
        <PrimaryAddressPreview overview={overview} loading={overviewQuery.isPending} />
        <PrimaryContactPreview overview={overview} loading={overviewQuery.isPending} />
      </div>

      <RecentActivityFeed
        activity={overview?.recentActivity ?? []}
        loading={overviewQuery.isPending}
        locale={locale}
        onOpenOrder={onOpenOrder}
        onOpenInvoice={onOpenInvoice}
        onOpenPayment={onOpenPayment}
      />

      {overdue && !blocked && (
        <div className="flex items-start gap-2 rounded-lg border border-amber-200 bg-amber-50/70 p-2.5 text-xs text-amber-800 dark:border-amber-500/30 dark:bg-amber-500/10 dark:text-amber-300">
          <AlertTriangle size={14} className="mt-0.5 shrink-0" />
          <span>
            {t('customers.detail.overdueWarning', {
              amount: fmtCurrency(customer.overdueAmount, currency, locale),
            })}
          </span>
        </div>
      )}
    </div>
  );
};

const AgingMiniCard = ({ aging, locale }: { aging: CustomerAging; locale: string }) => {
  const { t } = useTranslation();
  const segments: { label: string; amount: number; color: string }[] = [
    {
      label: t('payments.aging.current', { defaultValue: 'Current' }),
      amount: aging.current,
      color: 'bg-emerald-500',
    },
    { label: '1-30', amount: aging.days1To30, color: 'bg-yellow-500' },
    { label: '31-60', amount: aging.days31To60, color: 'bg-amber-500' },
    { label: '61-90', amount: aging.days61To90, color: 'bg-orange-500' },
    { label: '90+', amount: aging.daysOver90, color: 'bg-red-500' },
  ];
  const total = aging.totalOutstanding || 1;
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <Wallet size={12} />
          {t('payments.aging.title', { defaultValue: 'Aging analysis' })}
        </span>
        <span className="font-bold tabular-nums text-slate-900 dark:text-slate-100">
          {fmtCurrency(aging.totalOutstanding, aging.currency, locale)}
        </span>
      </header>
      <div className="mt-2 flex h-1.5 overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
        {segments.map((seg) => {
          const pct = (seg.amount / total) * 100;
          if (pct <= 0) return null;
          return (
            <div
              key={seg.label}
              className={seg.color}
              style={{ width: `${pct}%` }}
              title={`${seg.label}: ${fmtCurrency(seg.amount, aging.currency, locale)}`}
            />
          );
        })}
      </div>
      <div className="mt-1.5 grid grid-cols-5 gap-1 text-[9px]">
        {segments.map((seg) => (
          <div
            key={`legend-${seg.label}`}
            className="rounded border border-slate-200 px-1 py-0.5 text-center dark:border-slate-800"
          >
            <div className="flex items-center justify-center gap-0.5">
              <span className={`h-1 w-1 rounded-full ${seg.color}`} />
              <span className="font-semibold text-slate-600 dark:text-slate-300">{seg.label}</span>
            </div>
            <div className="text-[10px] tabular-nums text-slate-700 dark:text-slate-200">
              {fmtCurrency(seg.amount, aging.currency, locale)}
            </div>
          </div>
        ))}
      </div>
    </section>
  );
};

const CreditGaugeCard = ({
  currentBalance,
  creditLimit,
  outstanding,
  overdue,
  creditUsedPercent,
  isOverCreditLimit,
  currency,
  locale,
  loading,
}: {
  currentBalance: number;
  creditLimit: number;
  outstanding: number;
  overdue: number;
  creditUsedPercent: number;
  isOverCreditLimit: boolean;
  currency: string;
  locale: string;
  loading: boolean;
}) => {
  const { t } = useTranslation();
  const noLimit = creditLimit <= 0;
  const pct = noLimit ? 0 : Math.min(Math.max(creditUsedPercent, 0), 120);
  const barColor = isOverCreditLimit
    ? 'bg-red-500'
    : pct >= 85
      ? 'bg-amber-500'
      : pct >= 60
        ? 'bg-yellow-500'
        : 'bg-emerald-500';
  const available = Math.max(0, creditLimit - currentBalance);

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between">
        <div className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
          <CreditCard size={12} />
          {t('customers.detail.metrics.creditLine')}
        </div>
        {isOverCreditLimit && (
          <Badge variant="error" pill>
            {t('customers.detail.metrics.overLimit')}
          </Badge>
        )}
      </header>

      <div className="mt-2 grid grid-cols-3 gap-2">
        <GaugeStat
          label={t('customers.detail.metrics.balance')}
          value={fmtCurrency(currentBalance, currency, locale)}
          tone={currentBalance > 0 ? 'amber' : currentBalance < 0 ? 'emerald' : 'slate'}
        />
        <GaugeStat
          label={t('customers.detail.metrics.creditLimit')}
          value={noLimit ? '—' : fmtCurrency(creditLimit, currency, locale)}
          tone="slate"
        />
        <GaugeStat
          label={t('customers.detail.metrics.available')}
          value={noLimit ? '—' : fmtCurrency(available, currency, locale)}
          tone={noLimit ? 'slate' : available > 0 ? 'emerald' : 'red'}
        />
      </div>

      <div className="mt-3">
        <div className="flex items-center justify-between text-[10px] text-slate-500 dark:text-slate-400">
          <span>{t('customers.detail.metrics.creditUsed')}</span>
          <span className="tabular-nums">
            {noLimit ? '—' : fmtPercent(Math.min(creditUsedPercent, 999.9), locale)}
          </span>
        </div>
        <div className="mt-1 h-1.5 w-full overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
          <div
            className={`h-full rounded-full transition-all ${barColor}`}
            style={{ width: `${Math.min(pct, 100)}%` }}
          />
        </div>
      </div>

      <div className="mt-2 grid grid-cols-2 gap-2 text-[11px]">
        <div className="flex items-center justify-between rounded border border-slate-200 px-2 py-1 dark:border-slate-800">
          <span className="text-slate-500 dark:text-slate-400">
            {t('customers.detail.metrics.outstanding')}
          </span>
          <span className="font-semibold tabular-nums text-slate-900 dark:text-slate-100">
            {fmtCurrency(outstanding, currency, locale)}
          </span>
        </div>
        <div className="flex items-center justify-between rounded border border-slate-200 px-2 py-1 dark:border-slate-800">
          <span className="text-slate-500 dark:text-slate-400">
            {t('customers.detail.metrics.overdue')}
          </span>
          <span
            className={`font-semibold tabular-nums ${
              overdue > 0 ? 'text-red-600 dark:text-red-400' : 'text-slate-900 dark:text-slate-100'
            }`}
          >
            {fmtCurrency(overdue, currency, locale)}
          </span>
        </div>
      </div>

      {loading && (
        <div className="mt-2 text-[10px] italic text-slate-400 dark:text-slate-500">
          {t('common.loading')}
        </div>
      )}
    </section>
  );
};

const toneClasses: Record<'slate' | 'amber' | 'emerald' | 'red', string> = {
  slate: 'text-slate-900 dark:text-slate-100',
  amber: 'text-amber-600 dark:text-amber-400',
  emerald: 'text-emerald-600 dark:text-emerald-400',
  red: 'text-red-600 dark:text-red-400',
};

const GaugeStat = ({
  label,
  value,
  tone,
}: {
  label: string;
  value: string;
  tone: keyof typeof toneClasses;
}) => (
  <div className="rounded border border-slate-200 px-2 py-1.5 dark:border-slate-800">
    <div className="text-[9px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {label}
    </div>
    <div className={`mt-0.5 text-sm font-bold tabular-nums ${toneClasses[tone]}`}>{value}</div>
  </div>
);

const QuickActionsBar = ({
  customerId,
  blocked,
  onCreateOrder,
  onCreateInvoice,
  onRecordPayment,
}: {
  customerId: string;
  blocked: boolean;
  onCreateOrder?: (customerId: string) => void;
  onCreateInvoice?: (customerId: string) => void;
  onRecordPayment?: (customerId: string) => void;
}) => {
  const { t } = useTranslation();
  const actions: { label: string; icon: React.ReactNode; onClick?: () => void }[] = [
    {
      label: t('customers.detail.actions.newOrder'),
      icon: <ShoppingCart size={13} />,
      onClick: onCreateOrder ? () => onCreateOrder(customerId) : undefined,
    },
    {
      label: t('customers.detail.actions.newInvoice'),
      icon: <FileText size={13} />,
      onClick: onCreateInvoice ? () => onCreateInvoice(customerId) : undefined,
    },
    {
      label: t('customers.detail.actions.recordPayment'),
      icon: <Receipt size={13} />,
      onClick: onRecordPayment ? () => onRecordPayment(customerId) : undefined,
    },
  ];
  return (
    <div className="grid grid-cols-3 gap-1.5">
      {actions.map((action) => (
        <button
          key={action.label}
          type="button"
          onClick={action.onClick}
          disabled={blocked || !action.onClick}
          className="inline-flex items-center justify-center gap-1 rounded-md border border-slate-200 bg-white px-2 py-1.5 text-[11px] font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
        >
          {action.icon}
          <span className="truncate">{action.label}</span>
        </button>
      ))}
    </div>
  );
};

const SnapshotCard = ({ customer }: { customer: Customer }) => {
  const { t } = useTranslation();
  const rows = useMemo(
    () =>
      [
        { label: t('customers.fields.type'), value: t(`customers.type.${customer.type}`) },
        { label: t('customers.fields.code'), value: customer.code ?? '—', mono: true },
        { label: t('customers.fields.legalName'), value: customer.legalName },
        { label: t('customers.fields.tradeName'), value: customer.tradeName },
        { label: t('customers.fields.taxNumber'), value: customer.taxNumber, mono: true },
        { label: t('customers.fields.taxOffice'), value: customer.taxOffice },
        { label: t('customers.fields.nationalId'), value: customer.nationalId, mono: true },
        { label: t('customers.fields.currency'), value: customer.defaultCurrency },
      ].filter((row) => row.value),
    [customer, t],
  );

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <UserCircle2 size={12} />
        {t('customers.detail.snapshot')}
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
        <div className="col-span-2 flex items-center justify-between gap-2 border-t border-slate-100 pt-1.5 text-[11px] dark:border-slate-800">
          <dt className="flex items-center gap-1 text-slate-500 dark:text-slate-400">
            <Mail size={11} />
            {customer.email ?? '—'}
          </dt>
          <dd className="flex items-center gap-1 text-slate-500 dark:text-slate-400">
            <Phone size={11} />
            {customer.phone ?? '—'}
          </dd>
        </div>
      </dl>
    </section>
  );
};

const MetaChips = ({
  overview,
  loading,
}: {
  overview: CustomerOverview | null;
  loading: boolean;
}) => {
  const { t } = useTranslation();
  const chips = [
    {
      icon: <Users size={11} />,
      label: t('customers.detail.meta.group'),
      value: overview?.groupName,
    },
    {
      icon: <User size={11} />,
      label: t('customers.detail.meta.salesRep'),
      value: overview?.salesRepName,
    },
    {
      icon: <Tag size={11} />,
      label: t('customers.detail.meta.priceList'),
      value: overview?.priceListName,
    },
    {
      icon: <Wallet size={11} />,
      label: t('customers.detail.meta.paymentTerms'),
      value: overview?.paymentTermsName
        ? overview.paymentTermsNetDays !== null && overview.paymentTermsNetDays !== undefined
          ? `${overview.paymentTermsName} · ${t('customers.detail.meta.netDays', { count: overview.paymentTermsNetDays })}`
          : overview.paymentTermsName
        : null,
    },
  ];
  const visible = chips.filter((c) => c.value);
  if (visible.length === 0 && !loading) return null;
  return (
    <section className="flex flex-wrap items-center gap-1.5">
      {visible.map((chip) => (
        <span
          key={chip.label}
          className="inline-flex items-center gap-1 rounded-full border border-slate-200 bg-slate-50 px-2 py-0.5 text-[10px] text-slate-700 dark:border-slate-800 dark:bg-slate-800/60 dark:text-slate-200"
        >
          {chip.icon}
          <span className="font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
            {chip.label}
          </span>
          <span className="font-medium">{chip.value}</span>
        </span>
      ))}
      {loading && visible.length === 0 && (
        <span className="text-[10px] italic text-slate-400 dark:text-slate-500">
          {t('common.loading')}
        </span>
      )}
    </section>
  );
};

const PrimaryAddressPreview = ({
  overview,
  loading,
}: {
  overview: CustomerOverview | null;
  loading: boolean;
}) => {
  const { t } = useTranslation();
  const address = overview?.primaryShippingAddress ?? overview?.primaryBillingAddress ?? null;
  return (
    <article className="rounded-lg border border-slate-200 bg-white p-2.5 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1">
          <MapPin size={12} />
          {t('customers.detail.primaryAddress')}
        </span>
        {address?.isPrimary && (
          <span className="inline-flex items-center gap-0.5 text-amber-500">
            <Star size={10} fill="currentColor" />
          </span>
        )}
      </header>
      <div className="mt-1.5 min-h-[42px] text-[11px] leading-tight text-slate-700 dark:text-slate-200">
        {address ? (
          <>
            <div className="font-semibold">{address.label}</div>
            <div className="mt-0.5 text-slate-600 dark:text-slate-300">{address.line1}</div>
            {address.line2 && (
              <div className="text-slate-600 dark:text-slate-300">{address.line2}</div>
            )}
            <div className="mt-0.5 text-slate-500 dark:text-slate-400">
              {[address.postalCode, address.city, address.state, address.country]
                .filter(Boolean)
                .join(', ')}
            </div>
          </>
        ) : (
          <span className="italic text-slate-400 dark:text-slate-500">
            {loading ? t('common.loading') : t('customers.detail.noPrimaryAddress')}
          </span>
        )}
      </div>
    </article>
  );
};

const PrimaryContactPreview = ({
  overview,
  loading,
}: {
  overview: CustomerOverview | null;
  loading: boolean;
}) => {
  const { t } = useTranslation();
  const contact = overview?.primaryContact ?? null;
  return (
    <article className="rounded-lg border border-slate-200 bg-white p-2.5 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1">
          <Building2 size={12} />
          {t('customers.detail.primaryContact')}
        </span>
        {contact?.isPrimary && (
          <span className="inline-flex items-center gap-0.5 text-amber-500">
            <Star size={10} fill="currentColor" />
          </span>
        )}
      </header>
      <div className="mt-1.5 min-h-[42px] text-[11px] leading-tight text-slate-700 dark:text-slate-200">
        {contact ? (
          <>
            <div className="font-semibold">{contact.name}</div>
            {contact.role && (
              <div className="mt-0.5 text-slate-500 dark:text-slate-400">{contact.role}</div>
            )}
            <div className="mt-0.5 flex flex-wrap gap-x-2 gap-y-0.5 text-slate-600 dark:text-slate-300">
              {contact.email && (
                <span className="inline-flex items-center gap-1">
                  <Mail size={10} /> {contact.email}
                </span>
              )}
              {contact.phone && (
                <span className="inline-flex items-center gap-1">
                  <Phone size={10} /> {contact.phone}
                </span>
              )}
            </div>
          </>
        ) : (
          <span className="italic text-slate-400 dark:text-slate-500">
            {loading ? t('common.loading') : t('customers.detail.noPrimaryContact')}
          </span>
        )}
      </div>
    </article>
  );
};

const activityKindStyles: Record<string, { tone: string; icon: React.ReactNode }> = {
  Order: {
    tone: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-300',
    icon: <ShoppingCart size={11} />,
  },
  Invoice: {
    tone: 'bg-blue-100 text-blue-700 dark:bg-blue-500/20 dark:text-blue-300',
    icon: <FileText size={11} />,
  },
  Payment: {
    tone: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
    icon: <Receipt size={11} />,
  },
};

const RecentActivityFeed = ({
  activity,
  loading,
  locale,
  onOpenOrder,
  onOpenInvoice,
  onOpenPayment,
}: {
  activity: CustomerActivityItem[];
  loading: boolean;
  locale: string;
  onOpenOrder?: (orderId: string) => void;
  onOpenInvoice?: (invoiceId: string) => void;
  onOpenPayment?: (paymentId: string) => void;
}) => {
  const { t } = useTranslation();
  const handleOpen = (item: CustomerActivityItem) => {
    if (item.kind === 'Order') onOpenOrder?.(item.sourceId);
    else if (item.kind === 'Invoice') onOpenInvoice?.(item.sourceId);
    else if (item.kind === 'Payment') onOpenPayment?.(item.sourceId);
  };

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1">
          <Plus size={12} />
          {t('customers.detail.recentActivity')}
        </span>
        <span className="text-slate-400">{activity.length}</span>
      </header>
      {activity.length === 0 ? (
        <div className="mt-2 rounded border border-dashed border-slate-200 p-3 text-center text-[11px] italic text-slate-400 dark:border-slate-700 dark:text-slate-500">
          {loading ? t('common.loading') : t('customers.detail.noRecentActivity')}
        </div>
      ) : (
        <ul className="mt-2 divide-y divide-slate-100 dark:divide-slate-800">
          {activity.map((item) => {
            const style = activityKindStyles[item.kind] ?? {
              tone: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
              icon: <Plus size={11} />,
            };
            const relative = fmtRelative(item.occurredAtUtc, locale);
            const clickable =
              (item.kind === 'Order' && !!onOpenOrder) ||
              (item.kind === 'Invoice' && !!onOpenInvoice) ||
              (item.kind === 'Payment' && !!onOpenPayment);
            return (
              <li
                key={`${item.kind}-${item.sourceId}`}
                className={`flex items-center justify-between gap-2 py-1.5 ${clickable ? 'cursor-pointer hover:bg-slate-50 dark:hover:bg-slate-800/50' : ''}`}
                onClick={clickable ? () => handleOpen(item) : undefined}
              >
                <div className="flex min-w-0 items-center gap-2">
                  <span
                    className={`inline-flex h-5 w-5 shrink-0 items-center justify-center rounded ${style.tone}`}
                  >
                    {style.icon}
                  </span>
                  <div className="min-w-0">
                    <div className="flex items-center gap-1.5 text-[11px] font-medium text-slate-900 dark:text-slate-100">
                      <span className="font-mono">{item.sourceNumber ?? '—'}</span>
                      {item.status && (
                        <span className="rounded bg-slate-100 px-1 py-px text-[9px] font-semibold uppercase tracking-wider text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                          {item.status}
                        </span>
                      )}
                    </div>
                    <div className="text-[10px] text-slate-500 dark:text-slate-400">
                      {fmtDate(item.occurredAtUtc, locale)}
                      {relative ? ` · ${relative}` : ''}
                    </div>
                  </div>
                </div>
                <div className="shrink-0 text-right">
                  <div className="text-[11px] font-semibold tabular-nums text-slate-900 dark:text-slate-100">
                    {fmtCurrency(item.amount, item.currency, locale)}
                  </div>
                  <div className="text-[9px] uppercase tracking-wider text-slate-400">
                    {t(`customers.detail.activity.${item.kind}`, { defaultValue: item.kind })}
                  </div>
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
};
