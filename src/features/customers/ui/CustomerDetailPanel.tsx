import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Activity,
  BarChart3,
  Contact,
  Edit2,
  FileText,
  MapPin,
  NotebookPen,
  Receipt,
  ShoppingCart,
  User,
} from 'lucide-react';
import { DetailPanel, PanelTabs } from '@/shared/ui/DetailPanel/DetailPanel';
import {
  useCustomerQuery,
  useCustomerSummaryQuery,
  useCustomerTransactionsQuery,
} from '@/features/customers/hooks/useCustomerQueries';
import { useOrdersQuery } from '@/features/orders/hooks/useOrderQueries';
import { useInvoicesQuery } from '@/features/invoices/hooks/useInvoiceQueries';
import { CustomerAddressesTab } from '@/features/customers/ui/CustomerAddressesTab';
import { CustomerAnalyticsTab } from '@/features/customers/ui/CustomerAnalyticsTab';
import { CustomerContactsTab } from '@/features/customers/ui/CustomerContactsTab';
import { CustomerOverviewTab } from '@/features/customers/ui/CustomerOverviewTab';
import { CustomerLedgerTab } from '@/features/payments/ui/CustomerLedgerTab';
import type {
  Customer,
  CustomerTransaction,
  CustomerTransactionType,
} from '@/features/customers/model/customer.types';

interface Props {
  customerId: string | null;
  onClose: () => void;
  onEdit: (customer: Customer) => void;
  onCreateOrder?: (customerId: string) => void;
  onCreateInvoice?: (customerId: string) => void;
  onRecordPayment?: (customerId: string) => void;
  onOpenOrder?: (orderId: string) => void;
  onOpenInvoice?: (invoiceId: string) => void;
  onOpenPayment?: (paymentId: string) => void;
}

type Tab =
  | 'overview'
  | 'analytics'
  | 'ledger'
  | 'transactions'
  | 'addresses'
  | 'contacts'
  | 'orders'
  | 'invoices'
  | 'notes';

const txnStyles: Record<CustomerTransactionType, string> = {
  InvoiceIssued: 'bg-primary-100 text-primary-700 dark:bg-primary-500/20 dark:text-primary-300',
  Payment: 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300',
  Refund: 'bg-warning-100 text-warning-800 dark:bg-warning-500/20 dark:text-warning-300',
  Adjustment: 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
};

const fmtCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

const fmtDate = (iso: string, locale: string) => {
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(new Date(iso));
  } catch {
    return iso.slice(0, 10);
  }
};

const fmtDateTime = (iso: string, locale: string) => {
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'short', timeStyle: 'short' }).format(
      new Date(iso),
    );
  } catch {
    return iso;
  }
};

export const CustomerDetailPanel = ({
  customerId,
  onClose,
  onEdit,
  onCreateOrder,
  onCreateInvoice,
  onRecordPayment,
  onOpenOrder,
  onOpenInvoice,
  onOpenPayment,
}: Props) => {
  const { t, i18n } = useTranslation();
  const [tab, setTab] = useState<Tab>('overview');

  const customerQuery = useCustomerQuery(customerId);
  useCustomerSummaryQuery(customerId);
  const transactionsQuery = useCustomerTransactionsQuery(
    tab === 'transactions' ? customerId : null,
  );
  const ordersQuery = useOrdersQuery(
    { page: 1, pageSize: 50, customerId: customerId ?? undefined },
    { enabled: tab === 'orders' && customerId !== null },
  );
  const invoicesQuery = useInvoicesQuery(
    { page: 1, pageSize: 50, customerId: customerId ?? undefined },
    { enabled: tab === 'invoices' && customerId !== null },
  );

  const customer = customerQuery.data?.data ?? null;
  const transactions = transactionsQuery.data?.data?.items ?? [];
  const orders = ordersQuery.data?.data?.items ?? [];
  const invoices = invoicesQuery.data?.data?.items ?? [];

  const tabs = useMemo<{ id: Tab; label: string; icon: React.ReactNode }[]>(
    () => [
      { id: 'overview', label: t('customers.detail.tabs.overview'), icon: <User size={12} /> },
      {
        id: 'analytics',
        label: t('customers.detail.tabs.analytics', { defaultValue: 'Analytics' }),
        icon: <BarChart3 size={12} />,
      },
      {
        id: 'ledger',
        label: t('payments.ledger.title', { defaultValue: 'Ledger' }),
        icon: <Receipt size={12} />,
      },
      {
        id: 'transactions',
        label: t('customers.detail.tabs.transactions'),
        icon: <Activity size={12} />,
      },
      {
        id: 'addresses',
        label: t('customers.detail.tabs.addresses'),
        icon: <MapPin size={12} />,
      },
      {
        id: 'contacts',
        label: t('customers.detail.tabs.contacts'),
        icon: <Contact size={12} />,
      },
      { id: 'orders', label: t('customers.detail.tabs.orders'), icon: <ShoppingCart size={12} /> },
      {
        id: 'invoices',
        label: t('customers.detail.tabs.invoices'),
        icon: <FileText size={12} />,
      },
      { id: 'notes', label: t('customers.detail.tabs.notes'), icon: <NotebookPen size={12} /> },
    ],
    [t],
  );

  return (
    <DetailPanel
      open={customerId !== null}
      title={customer?.name ?? t('common.loading')}
      subtitle={customer?.email ?? undefined}
      onClose={onClose}
    >
      <PanelTabs tabs={tabs} active={tab} onSelect={setTab} />

      <div className="space-y-4 p-4">
        {tab === 'overview' && customer && (
          <>
            <CustomerOverviewTab
              customer={customer}
              locale={i18n.language}
              onEdit={() => onEdit(customer)}
              onCreateOrder={onCreateOrder}
              onCreateInvoice={onCreateInvoice}
              onRecordPayment={onRecordPayment}
              onOpenOrder={onOpenOrder}
              onOpenInvoice={onOpenInvoice}
              onOpenPayment={onOpenPayment}
            />
            <button
              type="button"
              onClick={() => onEdit(customer)}
              className="inline-flex w-full items-center justify-center gap-2 rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              <Edit2 size={14} />
              {t('common.edit')}
            </button>
          </>
        )}
        {tab === 'overview' && !customer && (
          <div className="text-sm text-slate-500">{t('common.loading')}</div>
        )}
        {tab === 'analytics' && customer && (
          <CustomerAnalyticsTab customerId={customer.id} locale={i18n.language} />
        )}
        {tab === 'ledger' && customer && (
          <CustomerLedgerTab
            customerId={customer.id}
            customerName={customer.name}
            currency={customer.defaultCurrency}
          />
        )}
        {tab === 'transactions' && (
          <TransactionsTab
            transactions={transactions}
            loading={transactionsQuery.isPending}
            locale={i18n.language}
          />
        )}
        {tab === 'addresses' && customerId && <CustomerAddressesTab customerId={customerId} />}
        {tab === 'contacts' && customerId && <CustomerContactsTab customerId={customerId} />}
        {tab === 'orders' && (
          <SimpleListTab
            empty={ordersQuery.isPending ? t('common.loading') : t('customers.detail.noOrders')}
            items={orders.map((o) => ({
              id: o.id,
              left: o.orderNumber,
              middle: `${t(`orders.status.${o.status}`)} · ${fmtDate(o.orderDate, i18n.language)}`,
              right: fmtCurrency(o.total, o.currency, i18n.language),
            }))}
          />
        )}
        {tab === 'invoices' && (
          <SimpleListTab
            empty={invoicesQuery.isPending ? t('common.loading') : t('customers.detail.noInvoices')}
            items={invoices.map((inv) => ({
              id: inv.id,
              left: inv.invoiceNumber,
              middle: `${t(`invoices.status.${inv.status}` as never)} · ${fmtDate(inv.issueDate, i18n.language)}`,
              right: fmtCurrency(inv.total, inv.currency, i18n.language),
            }))}
          />
        )}
        {tab === 'notes' && (
          <div className="rounded border border-slate-200 bg-slate-50/50 p-3 text-sm text-slate-700 dark:border-slate-800 dark:bg-slate-800/30 dark:text-slate-300">
            {customer?.notes || (
              <span className="italic text-slate-500">{t('customers.detail.noNotes')}</span>
            )}
          </div>
        )}
      </div>
    </DetailPanel>
  );
};

const TransactionsTab = ({
  transactions,
  loading,
  locale,
}: {
  transactions: CustomerTransaction[];
  loading: boolean;
  locale: string;
}) => {
  const { t } = useTranslation();
  if (loading && transactions.length === 0) {
    return <div className="text-sm text-slate-500">{t('common.loading')}</div>;
  }
  if (transactions.length === 0) {
    return (
      <div className="rounded border border-slate-200 p-4 text-center text-sm text-slate-500 dark:border-slate-800">
        {t('customers.detail.noTransactions')}
      </div>
    );
  }
  const balance = transactions.reduce((sum, tx) => sum + tx.amount, 0);
  const currency = transactions[0]?.currency ?? 'USD';

  return (
    <div className="space-y-3">
      <div className="rounded-lg border border-slate-200 bg-slate-50/50 p-3 dark:border-slate-800 dark:bg-slate-800/30">
        <div className="text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
          {t('customers.detail.metrics.balance')}
        </div>
        <div
          className={`mt-1 text-xl font-bold ${
            balance > 0
              ? 'text-warning-600 dark:text-warning-400'
              : balance < 0
                ? 'text-success-600 dark:text-success-400'
                : 'text-slate-900 dark:text-slate-100'
          }`}
        >
          {fmtCurrency(balance, currency, locale)}
        </div>
      </div>
      <ul className="divide-y divide-slate-200 overflow-hidden rounded-lg border border-slate-200 dark:divide-slate-800 dark:border-slate-800">
        {transactions.map((tx) => (
          <li key={tx.id} className="flex items-center justify-between gap-2 px-3 py-2 text-sm">
            <div className="min-w-0">
              <div className="flex items-center gap-2">
                <span
                  className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${txnStyles[tx.type]}`}
                >
                  {t(`customers.detail.txnType.${tx.type}`)}
                </span>
                {tx.reference && (
                  <span className="font-mono text-[10px] text-slate-500">{tx.reference}</span>
                )}
              </div>
              <div className="mt-0.5 text-[10px] text-slate-500">
                {fmtDateTime(tx.occurredAtUtc, locale)}
                {tx.notes ? ` · ${tx.notes}` : ''}
              </div>
            </div>
            <div
              className={`shrink-0 text-sm font-semibold tabular-nums ${
                tx.amount > 0
                  ? 'text-warning-600 dark:text-warning-400'
                  : 'text-success-600 dark:text-success-400'
              }`}
            >
              {tx.amount > 0 ? '+' : ''}
              {fmtCurrency(tx.amount, tx.currency, locale)}
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
};

const SimpleListTab = ({
  items,
  empty,
}: {
  items: { id: string; left: string; middle: string; right: string }[];
  empty: string;
}) => {
  if (items.length === 0) {
    return (
      <div className="rounded border border-slate-200 p-4 text-center text-sm text-slate-500 dark:border-slate-800">
        {empty}
      </div>
    );
  }
  return (
    <ul className="divide-y divide-slate-200 overflow-hidden rounded-lg border border-slate-200 dark:divide-slate-800 dark:border-slate-800">
      {items.map((item) => (
        <li key={item.id} className="flex items-center justify-between gap-2 px-3 py-2 text-sm">
          <div className="min-w-0">
            <div className="font-mono text-xs text-slate-900 dark:text-slate-100">{item.left}</div>
            <div className="text-[10px] text-slate-500">{item.middle}</div>
          </div>
          <div className="shrink-0 text-sm font-semibold tabular-nums text-slate-900 dark:text-slate-100">
            {item.right}
          </div>
        </li>
      ))}
    </ul>
  );
};
