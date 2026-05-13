import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { AlertTriangle, DollarSign, FileText, Package, ShoppingCart, Users } from 'lucide-react';
import { useDashboardStats } from '@/features/dashboard/hooks/useDashboardStats';
import { SalesTrendChart } from '@/features/dashboard/ui/SalesTrendChart/SalesTrendChart';
import { OrderStatusChart } from '@/features/dashboard/ui/OrderStatusChart/OrderStatusChart';

const formatNumber = (value: number, locale: string) => new Intl.NumberFormat(locale).format(value);

const formatCurrency = (value: number, locale: string, currency = 'USD') => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

const formatDate = (iso: string, locale: string) => {
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(new Date(iso));
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

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <div>
        <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
          {t('dashboard.title')}
        </h1>
        <p className="text-xs text-slate-500 dark:text-slate-400">{t('dashboard.subtitle')}</p>
      </div>

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <StatCard
          label={t('dashboard.stats.customers')}
          value={formatNumber(stats?.customerCount ?? 0, i18n.language)}
          icon={<Users size={18} />}
          accent="indigo"
          loading={isPending}
          onClick={() => navigate('/dashboard/customers')}
        />
        <StatCard
          label={t('dashboard.stats.products')}
          value={formatNumber(stats?.activeProductCount ?? 0, i18n.language)}
          icon={<Package size={18} />}
          accent="emerald"
          loading={isPending}
          onClick={() => navigate('/dashboard/products')}
        />
        <StatCard
          label={t('dashboard.stats.orders')}
          value={formatNumber(stats?.totalOrderCount ?? 0, i18n.language)}
          subtitle={`${draftCount} ${t('orders.status.Draft').toLowerCase()} · ${confirmedCount} ${t('orders.status.Confirmed').toLowerCase()}`}
          icon={<ShoppingCart size={18} />}
          accent="amber"
          loading={isPending}
          onClick={() => navigate('/dashboard/orders')}
        />
        <StatCard
          label={t('dashboard.stats.totalSales')}
          value={formatCurrency(stats?.totalSales ?? 0, i18n.language)}
          icon={<DollarSign size={18} />}
          accent="violet"
          loading={isPending}
        />
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        <StatCard
          label={t('dashboard.stats.outstandingReceivables')}
          value={formatCurrency(stats?.outstandingReceivables ?? 0, i18n.language)}
          subtitle={`${stats?.openInvoiceCount ?? 0} ${t('dashboard.stats.openInvoices')}`}
          icon={<FileText size={18} />}
          accent="amber"
          loading={isPending}
          onClick={() => navigate('/dashboard/invoices')}
        />
        <StatCard
          label={t('dashboard.stats.collectedThisMonth')}
          value={formatCurrency(stats?.collectedThisMonth ?? 0, i18n.language)}
          icon={<DollarSign size={18} />}
          accent="emerald"
          loading={isPending}
        />
        <StatCard
          label={t('dashboard.stats.totalInvoices')}
          value={formatNumber(
            (stats?.openInvoiceCount ?? 0) +
              ((stats?.orderCountByStatus?.Closed ?? 0) +
                (stats?.orderCountByStatus?.Shipped ?? 0)),
            i18n.language,
          )}
          subtitle={t('dashboard.stats.openAndIssued')}
          icon={<FileText size={18} />}
          accent="indigo"
          loading={isPending}
          onClick={() => navigate('/dashboard/invoices')}
        />
      </div>

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
              <SalesTrendChart points={stats?.salesTrend ?? []} />
            </div>
          </PanelCard>
        </div>
        <PanelCard
          title={t('dashboard.statusChart.title')}
          icon={<ShoppingCart size={14} className="text-amber-500" />}
          empty={!stats || stats.totalOrderCount === 0}
          emptyText={t('dashboard.statusChart.empty')}
          loading={isPending}
        >
          <div className="px-3 py-3">
            <OrderStatusChart counts={stats?.orderCountByStatus ?? {}} />
          </div>
        </PanelCard>
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <PanelCard
          title={t('dashboard.lowStock.title')}
          icon={<AlertTriangle size={14} className="text-amber-500" />}
          empty={!stats || stats.lowStockProducts.length === 0}
          emptyText={t('dashboard.lowStock.empty')}
          loading={isPending}
        >
          <ul className="divide-y divide-slate-200 dark:divide-slate-800">
            {stats?.lowStockProducts.map((product) => (
              <li key={product.id} className="flex items-center justify-between px-3 py-2 text-sm">
                <div className="min-w-0">
                  <div className="truncate font-medium text-slate-900 dark:text-slate-100">
                    {product.name}
                  </div>
                  <div className="font-mono text-[10px] text-slate-500">{product.sku}</div>
                </div>
                <div
                  className={
                    product.stockQuantity <= 0
                      ? 'rounded bg-red-100 px-2 py-0.5 text-xs font-semibold text-red-700 dark:bg-red-500/20 dark:text-red-300'
                      : 'rounded bg-amber-100 px-2 py-0.5 text-xs font-semibold text-amber-700 dark:bg-amber-500/20 dark:text-amber-300'
                  }
                >
                  {formatNumber(product.stockQuantity, i18n.language)} {product.unit}
                </div>
              </li>
            ))}
          </ul>
        </PanelCard>

        <PanelCard
          title={t('dashboard.recentOrders.title')}
          icon={<FileText size={14} className="text-indigo-500" />}
          empty={!stats || stats.recentOrders.length === 0}
          emptyText={t('dashboard.recentOrders.empty')}
          loading={isPending}
        >
          <ul className="divide-y divide-slate-200 dark:divide-slate-800">
            {stats?.recentOrders.map((order) => (
              <li key={order.id} className="flex items-center justify-between px-3 py-2 text-sm">
                <div className="min-w-0">
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
              </li>
            ))}
          </ul>
        </PanelCard>
      </div>
    </div>
  );
};

type Accent = 'indigo' | 'emerald' | 'amber' | 'violet';

const accentClasses: Record<Accent, string> = {
  indigo: 'bg-indigo-50 text-indigo-700 dark:bg-indigo-500/10 dark:text-indigo-300',
  emerald: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300',
  amber: 'bg-amber-50 text-amber-700 dark:bg-amber-500/10 dark:text-amber-300',
  violet: 'bg-violet-50 text-violet-700 dark:bg-violet-500/10 dark:text-violet-300',
};

interface StatCardProps {
  label: string;
  value: string;
  subtitle?: string;
  icon: React.ReactNode;
  accent: Accent;
  loading?: boolean;
  onClick?: () => void;
}

const StatCard = ({ label, value, subtitle, icon, accent, loading, onClick }: StatCardProps) => (
  <button
    type="button"
    onClick={onClick}
    disabled={!onClick}
    className="flex flex-col items-start gap-2 rounded-lg border border-slate-200 bg-white p-3 text-left transition hover:border-indigo-300 hover:shadow-sm disabled:cursor-default disabled:hover:border-slate-200 disabled:hover:shadow-none dark:border-slate-800 dark:bg-slate-900 dark:hover:border-indigo-500/50"
  >
    <div className="flex w-full items-center justify-between">
      <span className="text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        {label}
      </span>
      <span className={`rounded p-1.5 ${accentClasses[accent]}`}>{icon}</span>
    </div>
    <div className="text-xl font-bold text-slate-900 dark:text-slate-100">
      {loading ? '…' : value}
    </div>
    {subtitle && <div className="text-[10px] text-slate-500 dark:text-slate-400">{subtitle}</div>}
  </button>
);

interface PanelCardProps {
  title: string;
  icon: React.ReactNode;
  empty: boolean;
  emptyText: string;
  loading: boolean;
  children: React.ReactNode;
}

const PanelCard = ({ title, icon, empty, emptyText, loading, children }: PanelCardProps) => (
  <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
    <div className="flex items-center gap-2 border-b border-slate-200 px-3 py-2 dark:border-slate-800">
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
