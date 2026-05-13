import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Building2, FileText, Mail, Phone, ShoppingCart, User } from 'lucide-react';
import {
  useCustomerQuery,
  useCustomerSummaryQuery,
} from '@/features/customers/hooks/useCustomerQueries';
import { useOrdersQuery } from '@/features/orders/hooks/useOrderQueries';
import { useInvoicesQuery } from '@/features/invoices/hooks/useInvoiceQueries';
import type { OrderStatus } from '@/features/orders/model/order.types';
import type { InvoiceStatus } from '@/features/invoices/model/invoice.types';

type Tab = 'overview' | 'orders' | 'invoices';

const PAGE_SIZE = 20;

const orderStatusStyles: Record<OrderStatus, string> = {
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

const invoiceStatusStyles: Record<InvoiceStatus, string> = {
  Draft: 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
  Issued: 'bg-blue-100 text-blue-700 dark:bg-blue-500/20 dark:text-blue-300',
  Sent: 'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300',
  PartiallyPaid: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  Paid: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Overdue: 'bg-red-100 text-red-800 dark:bg-red-500/20 dark:text-red-300',
  Void: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
  Cancelled: 'bg-red-100 text-red-700 dark:bg-red-500/20 dark:text-red-300',
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

  const customerQuery = useCustomerQuery(id ?? null);
  const summaryQuery = useCustomerSummaryQuery(id ?? null);
  const customer = customerQuery.data?.data;
  const summary = summaryQuery.data?.data;

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={() => navigate('/dashboard/customers')}
          className="rounded p-1.5 text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800"
          aria-label={t('common.back')}
        >
          <ArrowLeft size={16} />
        </button>
        <div>
          <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
            {customer?.name ?? t('common.loading')}
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400">
            {t('customers.detail.subtitle')}
          </p>
        </div>
      </div>

      <div className="flex gap-1 border-b border-slate-200 dark:border-slate-800">
        <TabButton active={tab === 'overview'} onClick={() => setTab('overview')}>
          <User size={14} />
          {t('customers.detail.tabs.overview')}
        </TabButton>
        <TabButton active={tab === 'orders'} onClick={() => setTab('orders')}>
          <ShoppingCart size={14} />
          {t('customers.detail.tabs.orders')}
        </TabButton>
        <TabButton active={tab === 'invoices'} onClick={() => setTab('invoices')}>
          <FileText size={14} />
          {t('customers.detail.tabs.invoices')}
        </TabButton>
      </div>

      {tab === 'overview' && summary && (
        <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
          <MetricCard
            label={t('customers.detail.metrics.orders')}
            value={`${summary.orderCount}`}
            sub={formatCurrency(summary.totalOrderAmount, summary.currency, i18n.language)}
            tone="indigo"
          />
          <MetricCard
            label={t('customers.detail.metrics.invoiced')}
            value={formatCurrency(summary.totalInvoiced, summary.currency, i18n.language)}
            sub={`${summary.invoiceCount} ${t('customers.detail.metrics.invoiceCount')}`}
            tone="blue"
          />
          <MetricCard
            label={t('customers.detail.metrics.paid')}
            value={formatCurrency(summary.totalPaid, summary.currency, i18n.language)}
            tone="emerald"
          />
          <MetricCard
            label={t('customers.detail.metrics.outstanding')}
            value={formatCurrency(summary.outstanding, summary.currency, i18n.language)}
            tone={summary.outstanding > 0 ? 'amber' : 'slate'}
          />
        </div>
      )}

      {tab === 'overview' && (
        <div className="rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
          {customer ? (
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <Field icon={<Mail size={14} />} label={t('customers.fields.email')}>
                {customer.email ?? '—'}
              </Field>
              <Field icon={<Phone size={14} />} label={t('customers.fields.phone')}>
                {customer.phone ?? '—'}
              </Field>
              <Field icon={<Building2 size={14} />} label={t('customers.fields.taxNumber')}>
                {customer.taxNumber ?? '—'}
              </Field>
              {customer.notes && (
                <div className="sm:col-span-2">
                  <div className="text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                    {t('customers.fields.notes')}
                  </div>
                  <div className="mt-1 rounded border border-slate-200 bg-slate-50/50 px-3 py-2 text-sm text-slate-700 dark:border-slate-800 dark:bg-slate-800/30 dark:text-slate-300">
                    {customer.notes}
                  </div>
                </div>
              )}
            </div>
          ) : (
            <div className="py-4 text-center text-sm text-slate-500 dark:text-slate-400">
              {t('common.loading')}
            </div>
          )}
        </div>
      )}

      {tab === 'orders' && id && <OrdersTab customerId={id} locale={i18n.language} />}
      {tab === 'invoices' && id && <InvoicesTab customerId={id} locale={i18n.language} />}
    </div>
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
        ? 'border-indigo-500 text-indigo-600 dark:text-indigo-400'
        : 'border-transparent text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200'
    }`}
  >
    {children}
  </button>
);

type MetricTone = 'indigo' | 'blue' | 'emerald' | 'amber' | 'slate';

const toneClasses: Record<MetricTone, string> = {
  indigo: 'border-indigo-200 dark:border-indigo-500/30',
  blue: 'border-blue-200 dark:border-blue-500/30',
  emerald: 'border-emerald-200 dark:border-emerald-500/30',
  amber: 'border-amber-200 dark:border-amber-500/30',
  slate: 'border-slate-200 dark:border-slate-800',
};

interface MetricCardProps {
  label: string;
  value: string;
  sub?: string;
  tone: MetricTone;
}

const MetricCard = ({ label, value, sub, tone }: MetricCardProps) => (
  <div className={`rounded-lg border bg-white p-3 dark:bg-slate-900 ${toneClasses[tone]}`}>
    <div className="text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {label}
    </div>
    <div className="mt-1 text-lg font-bold text-slate-900 dark:text-slate-100">{value}</div>
    {sub && <div className="mt-0.5 text-[10px] text-slate-500 dark:text-slate-400">{sub}</div>}
  </div>
);

const Field = ({
  icon,
  label,
  children,
}: {
  icon: React.ReactNode;
  label: string;
  children: React.ReactNode;
}) => (
  <div>
    <div className="flex items-center gap-1 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {icon}
      <span>{label}</span>
    </div>
    <div className="mt-1 text-sm text-slate-700 dark:text-slate-200">{children}</div>
  </div>
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
                  className="font-mono text-xs text-indigo-600 hover:underline dark:text-indigo-400"
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
                  className="font-mono text-xs text-indigo-600 hover:underline dark:text-indigo-400"
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
