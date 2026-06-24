import { useTranslation } from 'react-i18next';
import { AlertTriangle } from 'lucide-react';
import { useCustomerOverviewQuery } from '@/features/customers/hooks/useCustomerQueries';
import { useCustomerAging } from '@/features/payments/hooks/usePaymentQueries';
import type { Customer } from '@/features/customers/model/customer.types';
import { fmtCurrency } from './customerOverview/format';
import { AgingMiniCard, CreditGaugeCard } from './customerOverview/CustomerCredit';
import { MetaChips, QuickActionsBar, SnapshotCard } from './customerOverview/CustomerSnapshot';
import { PrimaryAddressPreview, PrimaryContactPreview } from './customerOverview/CustomerPreviews';
import { RecentActivityFeed } from './customerOverview/CustomerActivity';

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
        <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 p-3 text-xs text-danger-700 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-300">
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
        <div className="flex items-start gap-2 rounded-lg border border-warning-200 bg-warning-50/70 p-2.5 text-xs text-warning-800 dark:border-warning-500/30 dark:bg-warning-500/10 dark:text-warning-300">
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
