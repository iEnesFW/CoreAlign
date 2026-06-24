import { Suspense, lazy } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { AlertTriangle, DollarSign, FileText, Package, ShoppingCart, Users } from 'lucide-react';
import { useDashboardStats } from '@/features/dashboard/hooks/useDashboardStats';
import { StatStrip, type StatStripItem } from '@/shared/ui/StatStrip/StatStrip';

const SalesTrendChart = lazy(() =>
  import('@/features/dashboard/ui/SalesTrendChart/SalesTrendChart').then((m) => ({
    default: m.SalesTrendChart,
  })),
);
const OrderStatusChart = lazy(() =>
  import('@/features/dashboard/ui/OrderStatusChart/OrderStatusChart').then((m) => ({
    default: m.OrderStatusChart,
  })),
);

const ChartFallback = () => (
  <div className="h-72 animate-pulse rounded-xl bg-slate-100/60 dark:bg-slate-800/40" />
);

const numberFormatters = new Map<string, Intl.NumberFormat>();
const dateFormatters = new Map<string, Intl.DateTimeFormat>();
const currencyFormatters = new Map<string, Intl.NumberFormat>();

const getNumberFormatter = (locale: string) => {
  let f = numberFormatters.get(locale);
  if (!f) {
    f = new Intl.NumberFormat(locale);
    numberFormatters.set(locale, f);
  }
  return f;
};

const getCurrencyFormatter = (locale: string, currency: string) => {
  const key = `${locale}|${currency}`;
  let f = currencyFormatters.get(key);
  if (!f) {
    try {
      f = new Intl.NumberFormat(locale, { style: 'currency', currency });
    } catch {
      f = new Intl.NumberFormat(locale);
    }
    currencyFormatters.set(key, f);
  }
  return f;
};

const getDateFormatter = (locale: string) => {
  let f = dateFormatters.get(locale);
  if (!f) {
    f = new Intl.DateTimeFormat(locale, { dateStyle: 'medium' });
    dateFormatters.set(locale, f);
  }
  return f;
};

const formatNumber = (value: number, locale: string) => getNumberFormatter(locale).format(value);

const formatCurrency = (value: number, locale: string, currency = 'USD') =>
  getCurrencyFormatter(locale, currency).format(value);

const formatDate = (iso: string, locale: string) => {
  try {
    return getDateFormatter(locale).format(new Date(iso));
  } catch {
    return iso.slice(0, 10);
  }
};

export const DashboardOverview = () => {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const { data, isPending } = useDashboardStats();

  const stats = data?.data;
  const draftCount = stats?.orderCountByStatus.Draft ?? 0;
  const confirmedCount = stats?.orderCountByStatus.Confirmed ?? 0;

  const overviewStatItems: StatStripItem[] = [
    {
      id: 'customers',
      label: t('dashboard.stats.customers'),
      value: stats?.customerCount ?? 0,
      format: (v) => (isPending ? '…' : formatNumber(v, i18n.language)),
      icon: <Users size={16} />,
      tone: 'indigo',
      onClick: () => navigate('/dashboard/customers'),
    },
    {
      id: 'products',
      label: t('dashboard.stats.products'),
      value: stats?.activeProductCount ?? 0,
      format: (v) => (isPending ? '…' : formatNumber(v, i18n.language)),
      icon: <Package size={16} />,
      tone: 'emerald',
      onClick: () => navigate('/dashboard/products'),
    },
    {
      id: 'orders',
      label: t('dashboard.stats.orders'),
      value: stats?.totalOrderCount ?? 0,
      format: (v) => (isPending ? '…' : formatNumber(v, i18n.language)),
      sub: `${draftCount} ${t('orders.status.Draft').toLowerCase()} · ${confirmedCount} ${t('orders.status.Confirmed').toLowerCase()}`,
      icon: <ShoppingCart size={16} />,
      tone: 'amber',
      onClick: () => navigate('/dashboard/orders'),
    },
    {
      id: 'totalSales',
      label: t('dashboard.stats.totalSales'),
      value: stats?.totalSales ?? 0,
      format: (v) => (isPending ? '…' : formatCurrency(v, i18n.language)),
      icon: <DollarSign size={16} />,
      tone: 'violet',
    },
  ];

  const financialStatItems: StatStripItem[] = [
    {
      id: 'outstanding',
      label: t('dashboard.stats.outstandingReceivables'),
      value: stats?.outstandingReceivables ?? 0,
      format: (v) => (isPending ? '…' : formatCurrency(v, i18n.language)),
      sub: `${stats?.openInvoiceCount ?? 0} ${t('dashboard.stats.openInvoices')}`,
      icon: <FileText size={16} />,
      tone: 'amber',
      onClick: () => navigate('/dashboard/invoices'),
    },
    {
      id: 'collected',
      label: t('dashboard.stats.collectedThisMonth'),
      value: stats?.collectedThisMonth ?? 0,
      format: (v) => (isPending ? '…' : formatCurrency(v, i18n.language)),
      icon: <DollarSign size={16} />,
      tone: 'emerald',
    },
    {
      id: 'invoices',
      label: t('dashboard.stats.totalInvoices'),
      value:
        (stats?.openInvoiceCount ?? 0) +
        ((stats?.orderCountByStatus?.Closed ?? 0) + (stats?.orderCountByStatus?.Shipped ?? 0)),
      format: (v) => (isPending ? '…' : formatNumber(v, i18n.language)),
      sub: t('dashboard.stats.openAndIssued'),
      icon: <FileText size={16} />,
      tone: 'indigo',
      onClick: () => navigate('/dashboard/invoices'),
    },
  ];

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <div>
        <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
          {t('dashboard.title')}
        </h1>
        <p className="text-xs text-slate-500 dark:text-slate-400">{t('dashboard.subtitle')}</p>
      </div>

      <StatStrip items={overviewStatItems} />
      <StatStrip items={financialStatItems} columnsClassName="grid-cols-1 sm:grid-cols-3" />

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <PanelCard
            title={t('dashboard.salesTrend.title')}
            icon={<DollarSign size={14} className="text-violet-500" />}
            empty={!stats || stats.salesTrend.length === 0}
            emptyText={t('dashboard.salesTrend.empty')}
            loading={isPending}
          >
            <div className="px-3 py-3">
              <Suspense fallback={<ChartFallback />}>
                <SalesTrendChart points={stats?.salesTrend ?? []} />
              </Suspense>
            </div>
          </PanelCard>
        </div>
        <PanelCard
          title={t('dashboard.statusChart.title')}
          icon={<ShoppingCart size={14} className="text-warning-500" />}
          empty={!stats || stats.totalOrderCount === 0}
          emptyText={t('dashboard.statusChart.empty')}
          loading={isPending}
        >
          <div className="px-3 py-3">
            <Suspense fallback={<ChartFallback />}>
              <OrderStatusChart counts={stats?.orderCountByStatus ?? {}} />
            </Suspense>
          </div>
        </PanelCard>
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <PanelCard
          title={t('dashboard.lowStock.title')}
          icon={<AlertTriangle size={14} className="text-warning-500" />}
          empty={!stats || stats.lowStockProducts.length === 0}
          emptyText={t('dashboard.lowStock.empty')}
          loading={isPending}
        >
          <ul className="divide-y divide-slate-100 dark:divide-white/5">
            {stats?.lowStockProducts.map((product) => (
              <li key={product.id}>
                <button
                  type="button"
                  onClick={() => navigate(`/dashboard/products?selected=${product.id}&tab=stock`)}
                  className="flex w-full items-center justify-between gap-2 px-3.5 py-2.5 text-sm transition-colors hover:bg-primary-50/50 dark:hover:bg-primary-500/[0.06]"
                >
                  <div className="min-w-0 text-left">
                    <div className="truncate font-medium text-slate-900 dark:text-slate-100">
                      {product.name}
                    </div>
                    <div className="font-mono text-[10px] text-slate-500">{product.sku}</div>
                  </div>
                  <div
                    className={
                      product.stockQuantity <= 0
                        ? 'rounded bg-danger-100 px-2 py-0.5 text-xs font-semibold text-danger-700 dark:bg-danger-500/20 dark:text-danger-300'
                        : 'rounded bg-warning-100 px-2 py-0.5 text-xs font-semibold text-warning-700 dark:bg-warning-500/20 dark:text-warning-300'
                    }
                  >
                    {formatNumber(product.stockQuantity, i18n.language)} {product.unit}
                  </div>
                </button>
              </li>
            ))}
          </ul>
        </PanelCard>

        <PanelCard
          title={t('dashboard.recentOrders.title')}
          icon={<FileText size={14} className="text-primary-500" />}
          empty={!stats || stats.recentOrders.length === 0}
          emptyText={t('dashboard.recentOrders.empty')}
          loading={isPending}
        >
          <ul className="divide-y divide-slate-100 dark:divide-white/5">
            {stats?.recentOrders.map((order) => (
              <li key={order.id}>
                <button
                  type="button"
                  onClick={() => navigate(`/dashboard/orders?selected=${order.id}`)}
                  className="flex w-full items-center justify-between gap-2 px-3.5 py-2.5 text-sm transition-colors hover:bg-primary-50/50 dark:hover:bg-primary-500/[0.06]"
                >
                  <div className="min-w-0 text-left">
                    <div className="truncate font-medium text-slate-900 dark:text-slate-100">
                      {order.customerName}
                    </div>
                    <div className="font-mono text-[10px] text-slate-500">
                      {order.orderNumber} · {formatDate(order.orderDate, i18n.language)}
                    </div>
                  </div>
                  <div className="text-sm font-semibold text-slate-900 dark:text-slate-100">
                    {formatCurrency(order.total, i18n.language, order.currency)}
                  </div>
                </button>
              </li>
            ))}
          </ul>
        </PanelCard>
      </div>
    </div>
  );
};

interface PanelCardProps {
  title: string;
  icon: React.ReactNode;
  empty: boolean;
  emptyText: string;
  loading: boolean;
  children: React.ReactNode;
}

const PanelCard = ({ title, icon, empty, emptyText, loading, children }: PanelCardProps) => (
  <div className="overflow-hidden rounded-xl border border-slate-200/70 bg-white shadow-[0_1px_3px_rgba(15,23,42,0.05)] dark:border-white/10 dark:bg-slate-900">
    <div className="flex items-center gap-2 border-b border-slate-200/70 px-3.5 py-2.5 dark:border-white/5">
      {icon}
      <h2 className="text-xs font-semibold uppercase tracking-wider text-slate-700 dark:text-slate-200">
        {title}
      </h2>
    </div>
    {loading ? (
      <div className="px-3 py-6 text-center text-xs text-slate-500 dark:text-slate-400">…</div>
    ) : empty ? (
      <div className="px-3 py-6 text-center text-xs text-slate-500 dark:text-slate-400">
        {emptyText}
      </div>
    ) : (
      children
    )}
  </div>
);
