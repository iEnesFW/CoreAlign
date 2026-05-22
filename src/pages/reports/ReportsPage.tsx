import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  AlertTriangle,
  BarChart3,
  Calendar,
  CalendarDays,
  CalendarRange,
  Coins,
  DollarSign,
  Package,
  TrendingUp,
  Trophy,
  Users,
} from 'lucide-react';
import {
  useAgingSummaryQuery,
  useSalesByPeriodQuery,
  useTopCustomersQuery,
  useTopProductsQuery,
} from '@/features/reports/hooks/useReportQueries';
import type { SalesBucket, SalesPeriodPoint } from '@/features/reports/model/reports.types';

const fmtCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

const fmtNumber = (value: number, locale: string, decimals = 0) =>
  new Intl.NumberFormat(locale, {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(value);

const isoDate = (d: Date) => d.toISOString().slice(0, 10);
const toIsoStartOfDay = (d: string) => new Date(`${d}T00:00:00Z`).toISOString();
const toIsoEndOfDay = (d: string) => new Date(`${d}T23:59:59Z`).toISOString();

type DatePreset = '30d' | '90d' | '6m' | '12m' | 'ytd' | 'custom';

const defaultRange = (preset: DatePreset): { from: string; to: string } => {
  const now = new Date();
  const to = isoDate(now);
  const from = new Date(now);
  switch (preset) {
    case '30d':
      from.setDate(from.getDate() - 30);
      break;
    case '90d':
      from.setDate(from.getDate() - 90);
      break;
    case '6m':
      from.setMonth(from.getMonth() - 6);
      break;
    case 'ytd':
      from.setMonth(0);
      from.setDate(1);
      break;
    case '12m':
    case 'custom':
    default:
      from.setMonth(from.getMonth() - 12);
      break;
  }
  return { from: isoDate(from), to };
};

export const ReportsPage = () => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const navigate = useNavigate();

  const [preset, setPreset] = useState<DatePreset>('12m');
  const [bucket, setBucket] = useState<SalesBucket>('Month');
  const initial = useMemo(() => defaultRange(preset), [preset]);
  const [fromDate, setFromDate] = useState(initial.from);
  const [toDate, setToDate] = useState(initial.to);

  const applyPreset = (p: DatePreset) => {
    setPreset(p);
    const r = defaultRange(p);
    setFromDate(r.from);
    setToDate(r.to);
  };

  const salesParams = useMemo(
    () => ({
      fromUtc: toIsoStartOfDay(fromDate),
      toUtc: toIsoEndOfDay(toDate),
      bucket,
    }),
    [fromDate, toDate, bucket],
  );
  const dateRange = useMemo(
    () => ({ fromUtc: toIsoStartOfDay(fromDate), toUtc: toIsoEndOfDay(toDate) }),
    [fromDate, toDate],
  );

  // Wrap each query param object in useMemo so React Query sees a stable key
  // across renders — otherwise the inline object literals re-create the key
  // on every parent state change and force a refetch.
  const topCustomersParams = useMemo(() => ({ limit: 10, ...dateRange }), [dateRange]);
  const topProductsParams = useMemo(() => ({ limit: 10, ...dateRange }), [dateRange]);

  const salesQuery = useSalesByPeriodQuery(salesParams);
  const topCustomersQuery = useTopCustomersQuery(topCustomersParams);
  const topProductsQuery = useTopProductsQuery(topProductsParams);
  const agingQuery = useAgingSummaryQuery();

  const sales = salesQuery.data?.data ?? null;
  const topCustomers = topCustomersQuery.data?.data ?? [];
  const topProducts = topProductsQuery.data?.data ?? [];
  const aging = agingQuery.data?.data ?? null;

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <header className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
            {t('reports.title')}
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400">{t('reports.subtitle')}</p>
        </div>
        <div className="flex flex-wrap items-center gap-1.5">
          <PresetToggle current={preset} value="30d" onClick={() => applyPreset('30d')}>
            30d
          </PresetToggle>
          <PresetToggle current={preset} value="90d" onClick={() => applyPreset('90d')}>
            90d
          </PresetToggle>
          <PresetToggle current={preset} value="6m" onClick={() => applyPreset('6m')}>
            6m
          </PresetToggle>
          <PresetToggle current={preset} value="12m" onClick={() => applyPreset('12m')}>
            12m
          </PresetToggle>
          <PresetToggle current={preset} value="ytd" onClick={() => applyPreset('ytd')}>
            YTD
          </PresetToggle>
          <span className="mx-1 h-5 w-px bg-slate-200 dark:bg-slate-800" />
          <input
            type="date"
            value={fromDate}
            onChange={(e) => {
              setPreset('custom');
              setFromDate(e.target.value);
            }}
            className="rounded border border-slate-200 bg-white px-2 py-1 text-[11px] dark:border-slate-700 dark:bg-slate-900"
          />
          <span className="text-[11px] text-slate-500">→</span>
          <input
            type="date"
            value={toDate}
            onChange={(e) => {
              setPreset('custom');
              setToDate(e.target.value);
            }}
            className="rounded border border-slate-200 bg-white px-2 py-1 text-[11px] dark:border-slate-700 dark:bg-slate-900"
          />
        </div>
      </header>

      <SalesKpiRow sales={sales} locale={locale} />

      <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
        <header className="flex items-center justify-between gap-2">
          <div className="flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
            <BarChart3 size={13} />
            {t('reports.salesTrend.title')}
          </div>
          <div className="flex gap-1">
            <BucketToggle current={bucket} value="Day" onClick={() => setBucket('Day')}>
              <Calendar size={11} /> {t('reports.bucket.day')}
            </BucketToggle>
            <BucketToggle current={bucket} value="Week" onClick={() => setBucket('Week')}>
              <CalendarDays size={11} /> {t('reports.bucket.week')}
            </BucketToggle>
            <BucketToggle current={bucket} value="Month" onClick={() => setBucket('Month')}>
              <CalendarRange size={11} /> {t('reports.bucket.month')}
            </BucketToggle>
          </div>
        </header>
        <SalesTrendChart
          points={sales?.points ?? []}
          currency={sales?.currency ?? 'TRY'}
          locale={locale}
          loading={salesQuery.isPending}
        />
      </section>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <AgingPanel
          aging={aging}
          loading={agingQuery.isPending}
          locale={locale}
          onOpenCustomer={(cid) => navigate(`/dashboard/customers/${cid}`)}
        />
        <TopCustomersPanel
          rows={topCustomers}
          locale={locale}
          loading={topCustomersQuery.isPending}
          onOpen={(cid) => navigate(`/dashboard/customers/${cid}`)}
        />
      </div>

      <TopProductsPanel rows={topProducts} locale={locale} loading={topProductsQuery.isPending} />
    </div>
  );
};

const PresetToggle = ({
  current,
  value,
  onClick,
  children,
}: {
  current: DatePreset;
  value: DatePreset;
  onClick: () => void;
  children: React.ReactNode;
}) => (
  <button
    type="button"
    onClick={onClick}
    className={`rounded border px-2 py-1 text-[10px] font-medium transition ${
      current === value
        ? 'border-indigo-300 bg-indigo-50 text-indigo-700 dark:border-indigo-500/40 dark:bg-indigo-500/10 dark:text-indigo-300'
        : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800'
    }`}
  >
    {children}
  </button>
);

const BucketToggle = ({
  current,
  value,
  onClick,
  children,
}: {
  current: SalesBucket;
  value: SalesBucket;
  onClick: () => void;
  children: React.ReactNode;
}) => (
  <button
    type="button"
    onClick={onClick}
    className={`inline-flex items-center gap-1 rounded border px-2 py-1 text-[10px] font-medium transition ${
      current === value
        ? 'border-indigo-300 bg-indigo-50 text-indigo-700 dark:border-indigo-500/40 dark:bg-indigo-500/10 dark:text-indigo-300'
        : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800'
    }`}
  >
    {children}
  </button>
);

const SalesKpiRow = ({
  sales,
  locale,
}: {
  sales: NonNullable<ReturnType<typeof useSalesByPeriodQuery>['data']>['data'] | null;
  locale: string;
}) => {
  const { t } = useTranslation();
  const currency = sales?.currency ?? 'TRY';
  const collected = sales?.totalPaid ?? 0;
  const collectionRate =
    sales && sales.totalRevenue > 0 ? (collected / sales.totalRevenue) * 100 : 0;
  return (
    <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
      <KpiCard
        icon={<DollarSign size={13} />}
        label={t('reports.kpi.revenue')}
        value={fmtCurrency(sales?.totalRevenue ?? 0, currency, locale)}
        tone="indigo"
      />
      <KpiCard
        icon={<Coins size={13} />}
        label={t('reports.kpi.collected')}
        value={fmtCurrency(collected, currency, locale)}
        sub={`${collectionRate.toFixed(1)}% ${t('reports.kpi.collectionRate')}`}
        tone="emerald"
      />
      <KpiCard
        icon={<TrendingUp size={13} />}
        label={t('reports.kpi.invoices')}
        value={fmtNumber(sales?.invoiceCount ?? 0, locale)}
        tone="blue"
      />
      <KpiCard
        icon={<Users size={13} />}
        label={t('reports.kpi.activeCustomers')}
        value={fmtNumber(sales?.customerCount ?? 0, locale)}
        tone="violet"
      />
    </div>
  );
};

const kpiTone: Record<'indigo' | 'emerald' | 'blue' | 'violet' | 'red' | 'slate', string> = {
  indigo: 'border-indigo-200 bg-indigo-50/30 dark:border-indigo-500/30 dark:bg-indigo-500/10',
  emerald: 'border-emerald-200 bg-emerald-50/30 dark:border-emerald-500/30 dark:bg-emerald-500/10',
  blue: 'border-blue-200 bg-blue-50/30 dark:border-blue-500/30 dark:bg-blue-500/10',
  violet: 'border-violet-200 bg-violet-50/30 dark:border-violet-500/30 dark:bg-violet-500/10',
  red: 'border-red-200 bg-red-50/30 dark:border-red-500/30 dark:bg-red-500/10',
  slate: 'border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900',
};

const KpiCard = ({
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
  tone: keyof typeof kpiTone;
}) => (
  <div className={`rounded-lg border p-3 ${kpiTone[tone]}`}>
    <div className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {icon}
      <span>{label}</span>
    </div>
    <div className="mt-1 text-lg font-bold tabular-nums text-slate-900 dark:text-slate-100">
      {value}
    </div>
    {sub && <div className="mt-0.5 text-[10px] text-slate-500 dark:text-slate-400">{sub}</div>}
  </div>
);

const SalesTrendChart = ({
  points,
  currency,
  locale,
  loading,
}: {
  points: SalesPeriodPoint[];
  currency: string;
  locale: string;
  loading: boolean;
}) => {
  const { t } = useTranslation();
  if (loading && points.length === 0) {
    return (
      <div className="mt-3 flex h-32 items-center justify-center text-[11px] text-slate-400">
        {t('common.loading')}
      </div>
    );
  }
  if (points.length === 0) {
    return (
      <div className="mt-3 flex h-32 items-center justify-center text-[11px] italic text-slate-400">
        {t('reports.salesTrend.empty')}
      </div>
    );
  }
  const maxRevenue = Math.max(1, ...points.map((p) => p.revenue));
  const maxPaid = Math.max(1, ...points.map((p) => p.paid));
  const max = Math.max(maxRevenue, maxPaid);
  return (
    <>
      <div className="mt-3 flex h-40 items-end gap-px">
        {points.map((p) => {
          const revH = (p.revenue / max) * 100;
          const paidH = (p.paid / max) * 100;
          return (
            <div
              key={p.periodKey}
              className="group relative flex flex-1 items-end gap-px"
              title={`${p.label}: ${fmtCurrency(p.revenue, currency, locale)} (${p.invoiceCount})`}
            >
              <div
                className="w-full rounded-t-sm bg-indigo-500 transition-all group-hover:bg-indigo-600"
                style={{ height: `${Math.max(2, revH)}%` }}
              />
              <div
                className="absolute bottom-0 left-0 w-full rounded-t-sm bg-emerald-500/70"
                style={{ height: `${Math.max(0, paidH)}%`, opacity: 0.85 }}
              />
            </div>
          );
        })}
      </div>
      <div className="mt-1 flex items-center justify-between text-[9px] text-slate-500 dark:text-slate-400">
        <span>{points[0]?.label}</span>
        <span>{points[points.length - 1]?.label}</span>
      </div>
      <div className="mt-2 flex items-center gap-3 text-[10px] text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1">
          <span className="inline-block h-2 w-2 rounded-full bg-indigo-500" />
          {t('reports.kpi.revenue')}
        </span>
        <span className="inline-flex items-center gap-1">
          <span className="inline-block h-2 w-2 rounded-full bg-emerald-500/70" />
          {t('reports.kpi.collected')}
        </span>
      </div>
    </>
  );
};

const AgingPanel = ({
  aging,
  loading,
  locale,
  onOpenCustomer,
}: {
  aging: NonNullable<ReturnType<typeof useAgingSummaryQuery>['data']>['data'] | null;
  loading: boolean;
  locale: string;
  onOpenCustomer: (id: string) => void;
}) => {
  const { t } = useTranslation();
  if (loading && !aging) {
    return (
      <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
        <div className="flex h-32 items-center justify-center text-[11px] text-slate-400">
          {t('common.loading')}
        </div>
      </section>
    );
  }
  if (!aging || aging.totalOutstanding <= 0) {
    return (
      <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
        <header className="flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
          <AlertTriangle size={13} />
          {t('reports.aging.title')}
        </header>
        <div className="mt-2 text-center text-[11px] italic text-slate-400">
          {t('reports.aging.empty')}
        </div>
      </section>
    );
  }
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

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[11px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <AlertTriangle size={13} />
          {t('reports.aging.title')}
        </span>
        <span className="font-bold tabular-nums text-slate-900 dark:text-slate-100">
          {fmtCurrency(aging.totalOutstanding, aging.currency, locale)}
        </span>
      </header>
      <div className="mt-2 flex h-2 overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
        {segments.map((s) => {
          const pct = (s.amount / aging.totalOutstanding) * 100;
          if (pct <= 0) return null;
          return (
            <div
              key={s.label}
              className={s.color}
              style={{ width: `${pct}%` }}
              title={`${s.label}: ${fmtCurrency(s.amount, aging.currency, locale)}`}
            />
          );
        })}
      </div>
      <div className="mt-2 grid grid-cols-5 gap-1 text-[10px]">
        {segments.map((s) => (
          <div
            key={`legend-${s.label}`}
            className="rounded border border-slate-200 px-1 py-1 text-center dark:border-slate-800"
          >
            <div className="flex items-center justify-center gap-1">
              <span className={`h-1.5 w-1.5 rounded-full ${s.color}`} />
              <span className="font-semibold text-slate-700 dark:text-slate-300">{s.label}</span>
            </div>
            <div className="mt-0.5 tabular-nums text-slate-700 dark:text-slate-200">
              {fmtCurrency(s.amount, aging.currency, locale)}
            </div>
          </div>
        ))}
      </div>
      <div className="mt-3">
        <div className="mb-1 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
          {t('reports.aging.byCustomer')}
        </div>
        <ul className="max-h-56 divide-y divide-slate-100 overflow-y-auto rounded border border-slate-200 dark:divide-slate-800 dark:border-slate-800">
          {aging.byCustomer.slice(0, 15).map((row) => (
            <li key={row.customerId}>
              <button
                type="button"
                onClick={() => onOpenCustomer(row.customerId)}
                className="flex w-full items-center justify-between gap-2 px-2 py-1.5 text-left text-[11px] transition hover:bg-slate-50 dark:hover:bg-slate-800/50"
              >
                <span className="min-w-0 truncate text-slate-700 dark:text-slate-200">
                  {row.customerName}
                </span>
                <span
                  className={`shrink-0 font-mono tabular-nums ${row.daysOver90 > 0 ? 'text-red-600 dark:text-red-400' : row.days31To60 + row.days61To90 > 0 ? 'text-amber-600 dark:text-amber-400' : 'text-slate-700 dark:text-slate-200'}`}
                >
                  {fmtCurrency(row.totalOutstanding, row.currency, locale)}
                </span>
              </button>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
};

const TopCustomersPanel = ({
  rows,
  locale,
  loading,
  onOpen,
}: {
  rows: Array<{
    customerId: string;
    name: string;
    code: string | null;
    currency: string;
    totalRevenue: number;
    totalPaid: number;
    outstanding: number;
    invoiceCount: number;
    orderCount: number;
  }>;
  locale: string;
  loading: boolean;
  onOpen: (id: string) => void;
}) => {
  const { t } = useTranslation();
  const maxRev = Math.max(1, ...rows.map((r) => r.totalRevenue));
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <Trophy size={13} />
        {t('reports.topCustomers.title')}
      </header>
      {loading && rows.length === 0 ? (
        <div className="mt-3 text-center text-[11px] text-slate-400">{t('common.loading')}</div>
      ) : rows.length === 0 ? (
        <div className="mt-3 text-center text-[11px] italic text-slate-400">
          {t('reports.topCustomers.empty')}
        </div>
      ) : (
        <ol className="mt-2 space-y-1">
          {rows.map((row, idx) => {
            const pct = (row.totalRevenue / maxRev) * 100;
            return (
              <li key={row.customerId}>
                <button
                  type="button"
                  onClick={() => onOpen(row.customerId)}
                  className="flex w-full items-center gap-2 rounded border border-slate-200 px-2 py-1.5 text-left transition hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/50"
                >
                  <span className="inline-flex h-5 w-5 shrink-0 items-center justify-center rounded bg-indigo-100 text-[10px] font-bold text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-300">
                    {idx + 1}
                  </span>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center justify-between gap-2">
                      <div className="min-w-0 truncate text-[11px] font-medium text-slate-900 dark:text-slate-100">
                        {row.name}
                      </div>
                      <div className="shrink-0 text-[11px] font-mono tabular-nums text-slate-900 dark:text-slate-100">
                        {fmtCurrency(row.totalRevenue, row.currency, locale)}
                      </div>
                    </div>
                    <div className="mt-0.5 flex items-center justify-between gap-2 text-[9px] text-slate-500 dark:text-slate-400">
                      <span>
                        {row.invoiceCount} {t('customers.detail.metrics.invoiceCount')} ·{' '}
                        {row.orderCount} {t('reports.topCustomers.orders').toLowerCase()}
                      </span>
                      <span
                        className={
                          row.outstanding > 0
                            ? 'text-amber-600 dark:text-amber-400'
                            : 'text-emerald-600 dark:text-emerald-400'
                        }
                      >
                        {t('reports.topCustomers.outstanding')}:{' '}
                        {fmtCurrency(row.outstanding, row.currency, locale)}
                      </span>
                    </div>
                    <div className="mt-1 h-0.5 w-full overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
                      <div className="h-full bg-indigo-500" style={{ width: `${pct}%` }} />
                    </div>
                  </div>
                </button>
              </li>
            );
          })}
        </ol>
      )}
    </section>
  );
};

const TopProductsPanel = ({
  rows,
  locale,
  loading,
}: {
  rows: Array<{
    productId: string | null;
    productSku: string;
    productName: string;
    quantity: number;
    revenue: number;
    invoiceCount: number;
  }>;
  locale: string;
  loading: boolean;
}) => {
  const { t } = useTranslation();
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <Package size={13} />
        {t('reports.topProducts.title')}
      </header>
      {loading && rows.length === 0 ? (
        <div className="mt-3 text-center text-[11px] text-slate-400">{t('common.loading')}</div>
      ) : rows.length === 0 ? (
        <div className="mt-3 text-center text-[11px] italic text-slate-400">
          {t('reports.topProducts.empty')}
        </div>
      ) : (
        <table className="mt-2 w-full text-left text-[11px]">
          <thead className="bg-slate-50 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/40 dark:text-slate-400">
            <tr>
              <th className="px-2 py-1.5">#</th>
              <th className="px-2 py-1.5">{t('reports.topProducts.product')}</th>
              <th className="px-2 py-1.5">{t('reports.topProducts.sku')}</th>
              <th className="px-2 py-1.5 text-right">{t('reports.topProducts.quantity')}</th>
              <th className="px-2 py-1.5 text-right">{t('reports.topProducts.revenue')}</th>
              <th className="px-2 py-1.5 text-right">{t('reports.topProducts.invoiceCount')}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {rows.map((row, idx) => (
              <tr
                key={`${row.productSku}-${idx}`}
                className="hover:bg-slate-50/60 dark:hover:bg-slate-800/40"
              >
                <td className="px-2 py-1.5 text-slate-500">{idx + 1}</td>
                <td className="px-2 py-1.5 font-medium text-slate-900 dark:text-slate-100">
                  {row.productName}
                </td>
                <td className="px-2 py-1.5 font-mono text-[10px] text-slate-500">
                  {row.productSku}
                </td>
                <td className="px-2 py-1.5 text-right tabular-nums text-slate-700 dark:text-slate-300">
                  {fmtNumber(row.quantity, locale, 2)}
                </td>
                <td className="px-2 py-1.5 text-right font-semibold tabular-nums text-slate-900 dark:text-slate-100">
                  {fmtCurrency(row.revenue, 'TRY', locale)}
                </td>
                <td className="px-2 py-1.5 text-right text-slate-700 dark:text-slate-300">
                  {row.invoiceCount}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
};
