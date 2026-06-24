import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { BarChart3, Clock, FileText, GitMerge, Receipt, ShoppingCart, User } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { DetailPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { useCustomerQuery } from '@/features/customers/hooks/useCustomerQueries';
import { useOrdersQuery } from '@/features/orders/hooks/useOrderQueries';
import { useInvoicesQuery } from '@/features/invoices/hooks/useInvoiceQueries';
import { CustomerAnalyticsTab } from '@/features/customers/ui/CustomerAnalyticsTab';
import { CustomerOverviewTab } from '@/features/customers/ui/CustomerOverviewTab';
import { CustomerLedgerTab } from '@/features/payments/ui/CustomerLedgerTab';
import { AuditTimeline } from '@/features/audit';
import { CustomerStatementButton } from './components/CustomerStatementButton';
import { CustomerTagsEditor } from './components/CustomerTagsEditor';
import { MergeCustomersModal } from './components/MergeCustomersModal';
import type { OrderStatus } from '@/features/orders/model/order.types';
import type { InvoiceStatus } from '@/features/invoices/model/invoice.types';

type Tab = 'overview' | 'analytics' | 'ledger' | 'orders' | 'invoices' | 'audit';

const PAGE_SIZE = 20;

const orderStatusStyles: Record<OrderStatus, string> = {
  Draft: 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
  Submitted: 'bg-info-100 text-info-700 dark:bg-info-500/20 dark:text-info-300',
  Approved: 'bg-primary-100 text-primary-700 dark:bg-primary-500/20 dark:text-primary-300',
  Allocated: 'bg-violet-100 text-violet-700 dark:bg-violet-500/20 dark:text-violet-300',
  Picking: 'bg-fuchsia-100 text-fuchsia-700 dark:bg-fuchsia-500/20 dark:text-fuchsia-300',
  Packed: 'bg-purple-100 text-purple-700 dark:bg-purple-500/20 dark:text-purple-300',
  PartiallyShipped: 'bg-warning-100 text-warning-700 dark:bg-warning-500/20 dark:text-warning-300',
  Shipped: 'bg-warning-100 text-warning-800 dark:bg-warning-500/20 dark:text-warning-300',
  Delivered: 'bg-teal-100 text-teal-700 dark:bg-teal-500/20 dark:text-teal-300',
  Closed: 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300',
  Cancelled: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
  Returned: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
  Confirmed: 'bg-primary-100 text-primary-700 dark:bg-primary-500/20 dark:text-primary-300',
};

const invoiceStatusStyles: Record<InvoiceStatus, string> = {
  Draft: 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
  Issued: 'bg-primary-100 text-primary-700 dark:bg-primary-500/20 dark:text-primary-300',
  Sent: 'bg-info-100 text-info-700 dark:bg-info-500/20 dark:text-info-300',
  PartiallyPaid: 'bg-warning-100 text-warning-800 dark:bg-warning-500/20 dark:text-warning-300',
  Paid: 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300',
  Overdue: 'bg-danger-100 text-danger-800 dark:bg-danger-500/20 dark:text-danger-300',
  Void: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
  Cancelled: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
};

const formatCurrency = (value: number, currency: string, locale: string) => {
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

export const CustomerDetailPage = () => {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [tab, setTab] = useState<Tab>('overview');
  const [mergeOpen, setMergeOpen] = useState(false);

  const customerQuery = useCustomerQuery(id ?? null);
  const customer = customerQuery.data?.data;

  return (
    <DetailPageTemplate
      header={
        <PageHeader
          icon={<User size={20} />}
          title={customer?.name ?? t('common.loading')}
          subtitle={t('customers.detail.subtitle')}
          crumbs={[
            {
              label: t('customers.title', { defaultValue: 'Customers' }),
              to: '/dashboard/customers',
            },
            { label: customer?.name ?? t('common.loading') },
          ]}
          actions={
            customer ? (
              <>
                <CustomerStatementButton customerId={customer.id} customerName={customer.name} />
                <Button variant="outline" size="sm" onClick={() => setMergeOpen(true)}>
                  <GitMerge size={14} />
                  {t('customers.merge.title')}
                </Button>
              </>
            ) : undefined
          }
        />
      }
    >
      {customer && <CustomerTagsEditor customerId={customer.id} />}

      {customer && (
        <MergeCustomersModal
          open={mergeOpen}
          onClose={() => setMergeOpen(false)}
          initialSource={customer}
        />
      )}

      <div className="flex gap-1 border-b border-slate-200 dark:border-slate-800">
        <TabButton active={tab === 'overview'} onClick={() => setTab('overview')}>
          <User size={14} />
          {t('customers.detail.tabs.overview')}
        </TabButton>
        <TabButton active={tab === 'analytics'} onClick={() => setTab('analytics')}>
          <BarChart3 size={14} />
          {t('customers.detail.tabs.analytics', { defaultValue: 'Analytics' })}
        </TabButton>
        <TabButton active={tab === 'ledger'} onClick={() => setTab('ledger')}>
          <Receipt size={14} />
          {t('payments.ledger.title', { defaultValue: 'Ledger' })}
        </TabButton>
        <TabButton active={tab === 'orders'} onClick={() => setTab('orders')}>
          <ShoppingCart size={14} />
          {t('customers.detail.tabs.orders')}
        </TabButton>
        <TabButton active={tab === 'invoices'} onClick={() => setTab('invoices')}>
          <FileText size={14} />
          {t('customers.detail.tabs.invoices')}
        </TabButton>
        <TabButton active={tab === 'audit'} onClick={() => setTab('audit')}>
          <Clock size={14} />
          {t('Common.AuditTab.Title', { defaultValue: 'Audit' })}
        </TabButton>
      </div>

      {tab === 'overview' && customer && (
        <CustomerOverviewTab
          customer={customer}
          locale={i18n.language}
          onEdit={() => navigate('/dashboard/customers')}
          onCreateOrder={(cid) => navigate(`/dashboard/orders?new=1&customerId=${cid}`)}
          onCreateInvoice={(cid) => navigate(`/dashboard/invoices?new=1&customerId=${cid}`)}
          onRecordPayment={(cid) => navigate(`/dashboard/invoices?customerId=${cid}&payment=1`)}
          onOpenOrder={(orderId) => navigate(`/dashboard/orders?selected=${orderId}`)}
          onOpenInvoice={(invoiceId) => navigate(`/dashboard/invoices?selected=${invoiceId}`)}
        />
      )}

      {tab === 'analytics' && customer && (
        <CustomerAnalyticsTab
          customerId={customer.id}
          locale={i18n.language}
          onOpenProduct={(productId) => navigate(`/dashboard/products?selected=${productId}`)}
        />
      )}

      {tab === 'ledger' && customer && (
        <CustomerLedgerTab
          customerId={customer.id}
          customerName={customer.name}
          currency={customer.defaultCurrency}
        />
      )}

      {tab === 'orders' && id && <OrdersTab customerId={id} locale={i18n.language} />}
      {tab === 'invoices' && id && <InvoicesTab customerId={id} locale={i18n.language} />}
      {tab === 'audit' && id && <AuditTimeline entityType="Customer" entityId={id} />}
    </DetailPageTemplate>
  );
};

interface TabButtonProps {
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
}

const TabButton = ({ active, onClick, children }: TabButtonProps) => (
  <button
    type="button"
    onClick={onClick}
    className={`-mb-px flex items-center gap-2 border-b-2 px-3 py-2 text-sm font-medium transition ${
      active
        ? 'border-primary-500 text-primary-600 dark:text-primary-400'
        : 'border-transparent text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200'
    }`}
  >
    {children}
  </button>
);

const OrdersTab = ({ customerId, locale }: { customerId: string; locale: string }) => {
  const { t } = useTranslation();
  const query = useOrdersQuery({ page: 1, pageSize: PAGE_SIZE, customerId });
  const orders = query.data?.data?.items ?? [];

  if (query.isPending) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-400">
        {t('common.loading')}
      </div>
    );
  }

  if (orders.length === 0) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-400">
        {t('customers.detail.noOrders')}
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
      <table className="w-full text-left text-sm">
        <thead className="bg-slate-50 dark:bg-slate-800/50">
          <tr>
            <Th>{t('orders.columns.orderNumber')}</Th>
            <Th>{t('orders.columns.orderDate')}</Th>
            <Th>{t('orders.columns.status')}</Th>
            <Th>{t('orders.columns.total')}</Th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
          {orders.map((order) => (
            <tr key={order.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
              <td className="px-3 py-2">
                <Link
                  to="/dashboard/orders"
                  className="font-mono text-xs text-primary-600 hover:underline dark:text-primary-400"
                >
                  {order.orderNumber}
                </Link>
              </td>
              <td className="px-3 py-2 text-slate-600 dark:text-slate-400">
                {formatDate(order.orderDate, locale)}
              </td>
              <td className="px-3 py-2">
                <span
                  className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${orderStatusStyles[order.status]}`}
                >
                  {t(`orders.status.${order.status}` as never)}
                </span>
              </td>
              <td className="px-3 py-2 font-semibold text-slate-900 dark:text-slate-100">
                {formatCurrency(order.total, order.currency, locale)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

const InvoicesTab = ({ customerId, locale }: { customerId: string; locale: string }) => {
  const { t } = useTranslation();
  const query = useInvoicesQuery({ page: 1, pageSize: PAGE_SIZE, customerId });
  const invoices = query.data?.data?.items ?? [];

  if (query.isPending) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-400">
        {t('common.loading')}
      </div>
    );
  }

  if (invoices.length === 0) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-400">
        {t('customers.detail.noInvoices')}
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
      <table className="w-full text-left text-sm">
        <thead className="bg-slate-50 dark:bg-slate-800/50">
          <tr>
            <Th>{t('invoices.columns.invoiceNumber')}</Th>
            <Th>{t('invoices.columns.issueDate')}</Th>
            <Th>{t('invoices.columns.dueDate')}</Th>
            <Th>{t('invoices.columns.status')}</Th>
            <Th>{t('invoices.columns.total')}</Th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
          {invoices.map((invoice) => (
            <tr key={invoice.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
              <td className="px-3 py-2">
                <Link
                  to="/dashboard/invoices"
                  className="font-mono text-xs text-primary-600 hover:underline dark:text-primary-400"
                >
                  {invoice.invoiceNumber}
                </Link>
              </td>
              <td className="px-3 py-2 text-slate-600 dark:text-slate-400">
                {formatDate(invoice.issueDate, locale)}
              </td>
              <td className="px-3 py-2 text-slate-600 dark:text-slate-400">
                {formatDate(invoice.dueDate, locale)}
              </td>
              <td className="px-3 py-2">
                <span
                  className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${invoiceStatusStyles[invoice.status]}`}
                >
                  {t(`invoices.status.${invoice.status}` as never)}
                </span>
              </td>
              <td className="px-3 py-2 font-semibold text-slate-900 dark:text-slate-100">
                {formatCurrency(invoice.total, invoice.currency, locale)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

const Th = ({ children }: { children: React.ReactNode }) => (
  <th className="px-3 py-2 text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
    {children}
  </th>
);
