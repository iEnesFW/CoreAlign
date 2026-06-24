import { useTranslation } from 'react-i18next';
import { FileText, ShoppingCart } from 'lucide-react';
import { useCustomerAnalyticsQuery } from '@/features/customers/hooks/useCustomerQueries';
import { KpiStrip } from './customerAnalytics/AnalyticsKpis';
import { PaymentBehaviorCard } from './customerAnalytics/PaymentBehavior';
import { MonthlyRevenueChart } from './customerAnalytics/RevenueChart';
import { TopProductsCard } from './customerAnalytics/TopProducts';
import { StatusBreakdownCard } from './customerAnalytics/StatusBreakdown';

interface Props {
  customerId: string;
  locale: string;
  monthsBack?: number;
  onOpenProduct?: (productId: string) => void;
}

export const CustomerAnalyticsTab = ({
  customerId,
  locale,
  monthsBack = 12,
  onOpenProduct,
}: Props) => {
  const { t } = useTranslation();
  const query = useCustomerAnalyticsQuery(customerId, monthsBack);
  const analytics = query.data?.data ?? null;

  if (query.isPending && !analytics) {
    return <div className="text-sm italic text-slate-500">{t('common.loading')}</div>;
  }
  if (!analytics) {
    return (
      <div className="rounded border border-slate-200 p-4 text-center text-sm text-slate-500 dark:border-slate-800">
        {t('customers.analytics.noData')}
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <KpiStrip analytics={analytics} locale={locale} />
      <PaymentBehaviorCard analytics={analytics} locale={locale} />
      <MonthlyRevenueChart
        points={analytics.monthlyRevenue}
        currency={analytics.currency}
        locale={locale}
      />
      <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
        <StatusBreakdownCard
          title={t('customers.analytics.orderStatusBreakdown')}
          icon={<ShoppingCart size={12} />}
          items={analytics.orderStatusBreakdown}
          currency={analytics.currency}
          locale={locale}
          statusPrefix="orders.status"
        />
        <StatusBreakdownCard
          title={t('customers.analytics.invoiceStatusBreakdown')}
          icon={<FileText size={12} />}
          items={analytics.invoiceStatusBreakdown}
          currency={analytics.currency}
          locale={locale}
          statusPrefix="invoices.status"
        />
      </div>
      <TopProductsCard
        products={analytics.topProducts}
        currency={analytics.currency}
        locale={locale}
        onOpenProduct={onOpenProduct}
      />
    </div>
  );
};
