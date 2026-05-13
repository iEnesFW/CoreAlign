import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  Activity,
  Edit2,
  ExternalLink,
  FileText,
  ListOrdered,
  NotebookPen,
  ShoppingCart,
  User,
} from 'lucide-react';
import { DetailPanel, PanelTabs } from '@/shared/ui/DetailPanel/DetailPanel';
import { Truck } from 'lucide-react';
import { useOrderQuery } from '@/features/orders/hooks/useOrderQueries';
import { useCustomerQuery } from '@/features/customers/hooks/useCustomerQueries';
import { useInvoicesByOrderQuery } from '@/features/invoices/hooks/useInvoiceQueries';
import type { Customer } from '@/features/customers/model/customer.types';
import type { Order, OrderLine, OrderStatus } from '@/features/orders/model/order.types';
import { OrderStatusTimeline } from './OrderStatusTimeline';
import { OrderActionsBar } from './OrderActionsBar';
import { ShipmentsTab } from './ShipmentsTab';

interface Props {
  orderId: string | null;
  onClose: () => void;
  onEdit: (orderId: string) => void;
  onGenerateInvoice?: (orderId: string) => void;
}

type Tab = 'overview' | 'lines' | 'shipments' | 'customer' | 'invoices' | 'notes';

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

const INVOICEABLE: OrderStatus[] = [
  'Confirmed',
  'Shipped',
  'Delivered',
  'Closed',
  'PartiallyShipped',
];

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

const fmtNumber = (value: number, locale: string) => new Intl.NumberFormat(locale).format(value);

export const OrderDetailPanel = ({ orderId, onClose, onEdit, onGenerateInvoice }: Props) => {
  const { t, i18n } = useTranslation();
  const [tab, setTab] = useState<Tab>('overview');
  const [shipmentCreateOpen, setShipmentCreateOpen] = useState(false);

  const orderQuery = useOrderQuery(orderId);
  const order = orderQuery.data?.data ?? null;

  const customerQuery = useCustomerQuery(tab === 'customer' && order ? order.customerId : null);
  const invoicesQuery = useInvoicesByOrderQuery(tab === 'invoices' && order ? order.id : null);

  const customer = customerQuery.data?.data ?? null;
  const linkedInvoices = invoicesQuery.data?.data ?? [];

  const tabs: { id: Tab; label: string; icon: React.ReactNode }[] = [
    { id: 'overview', label: t('orders.detail.tabs.overview'), icon: <ShoppingCart size={12} /> },
    { id: 'lines', label: t('orders.detail.tabs.lines'), icon: <ListOrdered size={12} /> },
    {
      id: 'shipments',
      label: t('orders.shipments.title'),
      icon: <Truck size={12} />,
    },
    { id: 'customer', label: t('orders.detail.tabs.customer'), icon: <User size={12} /> },
    { id: 'invoices', label: t('orders.detail.tabs.invoices'), icon: <FileText size={12} /> },
    { id: 'notes', label: t('orders.detail.tabs.notes'), icon: <NotebookPen size={12} /> },
  ];

  return (
    <DetailPanel
      open={orderId !== null}
      title={order?.orderNumber ?? t('common.loading')}
      subtitle={order?.customerName}
      onClose={onClose}
    >
      <PanelTabs tabs={tabs} active={tab} onSelect={setTab} />

      <div className="space-y-4 p-4">
        {order && (
          <OrderActionsBar
            order={order}
            onShipmentRequested={() => {
              setTab('shipments');
              setShipmentCreateOpen(true);
            }}
          />
        )}
        {tab === 'overview' && order && (
          <>
            <OrderStatusTimeline order={order} locale={i18n.language} />
            <OverviewTab
              order={order}
              locale={i18n.language}
              onEdit={() => onEdit(order.id)}
              onGenerateInvoice={
                onGenerateInvoice && INVOICEABLE.includes(order.status)
                  ? () => onGenerateInvoice(order.id)
                  : undefined
              }
            />
          </>
        )}
        {tab === 'lines' && order && <LinesTab order={order} locale={i18n.language} />}
        {tab === 'shipments' && order && (
          <ShipmentsTab
            order={order}
            showCreateModal={shipmentCreateOpen}
            onCloseCreateModal={() => setShipmentCreateOpen(false)}
          />
        )}
        {tab === 'customer' && (
          <CustomerTab customer={customer} loading={customerQuery.isPending} />
        )}
        {tab === 'invoices' && (
          <InvoicesTab
            invoices={linkedInvoices.map((inv) => ({
              id: inv.id,
              number: inv.invoiceNumber,
              status: t(`invoices.status.${inv.status}` as never),
              date: fmtDate(inv.issueDate, i18n.language),
              amount: fmtCurrency(inv.total, inv.currency, i18n.language),
            }))}
            loading={invoicesQuery.isPending}
          />
        )}
        {tab === 'notes' && (
          <div className="rounded border border-slate-200 bg-slate-50/50 p-3 text-sm text-slate-700 dark:border-slate-800 dark:bg-slate-800/30 dark:text-slate-300">
            {order?.notes || (
              <span className="italic text-slate-500">{t('orders.detail.noNotes')}</span>
            )}
          </div>
        )}
      </div>
    </DetailPanel>
  );
};

const OverviewTab = ({
  order,
  locale,
  onEdit,
  onGenerateInvoice,
}: {
  order: Order;
  locale: string;
  onEdit: () => void;
  onGenerateInvoice?: () => void;
}) => {
  const { t } = useTranslation();
  return (
    <>
      <div className="grid grid-cols-2 gap-2">
        <Stat
          label={t('orders.detail.metrics.total')}
          value={fmtCurrency(order.total, order.currency, locale)}
          highlight="indigo"
        />
        <Stat
          label={t('orders.detail.metrics.lines')}
          value={String(order.lines.length)}
          sub={`${fmtNumber(
            order.lines.reduce((s, l) => s + l.quantity, 0),
            locale,
          )} ${t('orders.detail.metrics.units')}`}
          highlight="blue"
        />
      </div>
      <div className="space-y-2 rounded-lg border border-slate-200 p-3 text-sm dark:border-slate-800">
        <Row label={t('orders.fields.status')}>
          <span
            className={`inline-flex rounded-full px-2 py-0.5 text-[10px] font-medium ${statusStyles[order.status]}`}
          >
            {t(`orders.status.${order.status}` as never)}
          </span>
        </Row>
        <Row label={t('orders.fields.orderDate')}>{fmtDate(order.orderDate, locale)}</Row>
        <Row label={t('orders.fields.customer')}>{order.customerName}</Row>
        <Row label={t('orders.fields.currency')}>{order.currency}</Row>
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
        {onGenerateInvoice && (
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
    </>
  );
};

const LinesTab = ({ order, locale }: { order: Order; locale: string }) => {
  const { t } = useTranslation();
  if (order.lines.length === 0) {
    return (
      <div className="rounded border border-slate-200 p-4 text-center text-sm text-slate-500 dark:border-slate-800">
        {t('orders.detail.noLines')}
      </div>
    );
  }
  return (
    <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
      <table className="w-full text-left text-xs">
        <thead className="bg-slate-50 dark:bg-slate-800/50">
          <tr>
            <th className="px-2 py-1.5 font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              {t('orders.lines.product')}
            </th>
            <th className="px-2 py-1.5 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              {t('orders.lines.quantity')}
            </th>
            <th className="px-2 py-1.5 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              {t('orders.lines.unitPrice')}
            </th>
            <th className="px-2 py-1.5 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              {t('invoices.fields.lineTotal')}
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
          {order.lines.map((line: OrderLine) => (
            <tr key={line.id}>
              <td className="px-2 py-1.5">
                <div className="font-medium text-slate-900 dark:text-slate-100">
                  {line.productName}
                </div>
                <div className="font-mono text-[10px] text-slate-500">{line.productSku}</div>
              </td>
              <td className="px-2 py-1.5 text-right tabular-nums text-slate-700 dark:text-slate-300">
                {fmtNumber(line.quantity, locale)}
              </td>
              <td className="px-2 py-1.5 text-right tabular-nums text-slate-700 dark:text-slate-300">
                {fmtCurrency(line.unitPrice, order.currency, locale)}
              </td>
              <td className="px-2 py-1.5 text-right font-medium tabular-nums text-slate-900 dark:text-slate-100">
                {fmtCurrency(line.lineTotal, order.currency, locale)}
              </td>
            </tr>
          ))}
        </tbody>
        <tfoot className="bg-slate-50 dark:bg-slate-800/50">
          <tr>
            <td
              colSpan={3}
              className="px-2 py-2 text-right text-[10px] font-semibold uppercase text-slate-500 dark:text-slate-400"
            >
              {t('orders.fields.total')}
            </td>
            <td className="px-2 py-2 text-right text-sm font-bold tabular-nums text-slate-900 dark:text-slate-100">
              {fmtCurrency(order.total, order.currency, locale)}
            </td>
          </tr>
        </tfoot>
      </table>
    </div>
  );
};

const CustomerTab = ({ customer, loading }: { customer: Customer | null; loading: boolean }) => {
  const { t } = useTranslation();
  if (loading && !customer) {
    return <div className="text-sm text-slate-500">{t('common.loading')}</div>;
  }
  if (!customer) {
    return (
      <div className="rounded border border-slate-200 p-4 text-center text-sm text-slate-500 dark:border-slate-800">
        {t('orders.detail.noCustomer')}
      </div>
    );
  }
  return (
    <div className="space-y-2 rounded-lg border border-slate-200 p-3 text-sm dark:border-slate-800">
      <Row label={t('customers.fields.name')}>
        <Link
          to={`/dashboard/customers/${customer.id}`}
          className="inline-flex items-center gap-1 text-indigo-600 hover:underline dark:text-indigo-400"
        >
          {customer.name}
          <ExternalLink size={10} />
        </Link>
      </Row>
      <Row label={t('customers.fields.email')}>{customer.email ?? '—'}</Row>
      <Row label={t('customers.fields.phone')}>{customer.phone ?? '—'}</Row>
      <Row label={t('customers.fields.taxNumber')}>{customer.taxNumber ?? '—'}</Row>
    </div>
  );
};

const InvoicesTab = ({
  invoices,
  loading,
}: {
  invoices: { id: string; number: string; status: string; date: string; amount: string }[];
  loading: boolean;
}) => {
  const { t } = useTranslation();
  if (loading && invoices.length === 0) {
    return <div className="text-sm text-slate-500">{t('common.loading')}</div>;
  }
  if (invoices.length === 0) {
    return (
      <div className="rounded border border-slate-200 p-4 text-center text-sm text-slate-500 dark:border-slate-800">
        {t('orders.detail.noInvoices')}
      </div>
    );
  }
  return (
    <ul className="divide-y divide-slate-200 overflow-hidden rounded-lg border border-slate-200 dark:divide-slate-800 dark:border-slate-800">
      {invoices.map((inv) => (
        <li key={inv.id} className="flex items-center justify-between gap-2 px-3 py-2 text-sm">
          <div className="min-w-0">
            <div className="font-mono text-xs text-slate-900 dark:text-slate-100">{inv.number}</div>
            <div className="flex items-center gap-1.5 text-[10px] text-slate-500">
              <Activity size={10} />
              {inv.status} · {inv.date}
            </div>
          </div>
          <div className="shrink-0 text-sm font-semibold tabular-nums text-slate-900 dark:text-slate-100">
            {inv.amount}
          </div>
        </li>
      ))}
    </ul>
  );
};

const Row = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <div className="flex items-center justify-between gap-2">
    <span className="text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {label}
    </span>
    <span className="truncate text-sm text-slate-700 dark:text-slate-200">{children}</span>
  </div>
);

const highlightClass: Record<'indigo' | 'blue', string> = {
  indigo: 'border-indigo-200 dark:border-indigo-500/30',
  blue: 'border-blue-200 dark:border-blue-500/30',
};

const Stat = ({
  label,
  value,
  sub,
  highlight,
}: {
  label: string;
  value: string;
  sub?: string;
  highlight: keyof typeof highlightClass;
}) => (
  <div className={`rounded border bg-white p-2.5 dark:bg-slate-900 ${highlightClass[highlight]}`}>
    <div className="text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {label}
    </div>
    <div className="mt-0.5 text-base font-bold text-slate-900 dark:text-slate-100">{value}</div>
    {sub && <div className="mt-0.5 text-[10px] text-slate-500 dark:text-slate-400">{sub}</div>}
  </div>
);
