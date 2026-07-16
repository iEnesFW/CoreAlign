import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router-dom';
import {
  Activity,
  Boxes,
  Clock,
  Copy,
  ExternalLink,
  FileText,
  Flame,
  History,
  ListOrdered,
  NotebookPen,
  Percent,
  RotateCcw,
  ShoppingCart,
  Truck,
  User,
  Workflow,
} from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { DetailPanel, PanelTabs } from '@/shared/ui/DetailPanel/DetailPanel';
import { useOrderQuery, useReorderOrder } from '@/features/orders/hooks/useOrderQueries';
import { useCustomerQuery } from '@/features/customers/hooks/useCustomerQueries';
import { useInvoicesByOrderQuery } from '@/features/invoices/hooks/useInvoiceQueries';
import { CreateReturnModal } from '@/features/returns/ui/CreateReturnModal';
import { OrderScrapModal } from '@/features/orders/ui/OrderScrapModal';
import { DocumentChain } from '@/widgets/DocumentChain';
import { NextBestAction } from '@/widgets/NextBestAction';
import { AuditTimeline } from '@/features/audit';
import type { Customer } from '@/features/customers/model/customer.types';
import type { Order, OrderLine, OrderStatus } from '@/features/orders/model/order.types';
import { OrderAllocationsTab } from './OrderAllocationsTab';
import { OrderAuditTab } from './OrderAuditTab';
import { OrderMarginTab } from './OrderMarginTab';
import { OrderStatusTimeline } from './OrderStatusTimeline';
import { OrderActionsBar } from './OrderActionsBar';
import { OrderOverviewTab } from './OrderOverviewTab';
import { ShipmentsTab } from './ShipmentsTab';

interface Props {
  orderId: string | null;
  onClose: () => void;
  onEdit: (orderId: string) => void;
  onGenerateInvoice?: (orderId: string) => void;
}

type Tab =
  | 'overview'
  | 'chain'
  | 'lines'
  | 'margin'
  | 'allocations'
  | 'shipments'
  | 'customer'
  | 'invoices'
  | 'audit'
  | 'changes'
  | 'notes';

const INVOICEABLE: OrderStatus[] = [
  'Confirmed',
  'Shipped',
  'Delivered',
  'Closed',
  'PartiallyShipped',
];

const RETURNABLE: OrderStatus[] = [
  'Shipped',
  'PartiallyShipped',
  'Delivered',
  'Closed',
  'Returned',
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
  const navigate = useNavigate();
  const [tab, setTab] = useState<Tab>('overview');
  const [shipmentCreateOpen, setShipmentCreateOpen] = useState(false);
  const [returnCreateOpen, setReturnCreateOpen] = useState(false);
  const [scrapOpen, setScrapOpen] = useState(false);

  const orderQuery = useOrderQuery(orderId);
  const order = orderQuery.data?.data ?? null;
  const reorder = useReorderOrder();

  const handleReorder = async () => {
    if (!order) return;
    try {
      const res = await reorder.mutateAsync(order.id);
      if (res.isSuccess && res.data) {
        toast.success(
          t('orders.reorder.created', {
            defaultValue: 'Yeni taslak oluşturuldu: {{number}}',
            number: res.data.orderNumber,
          }),
        );
      } else {
        toast.error(res.errors[0] ?? t('auth.common.unexpectedError'));
      }
    } catch (err) {
      toastApiError(err);
    }
  };

  const customerQuery = useCustomerQuery(tab === 'customer' && order ? order.customerId : null);
  const invoicesQuery = useInvoicesByOrderQuery(tab === 'invoices' && order ? order.id : null);

  const customer = customerQuery.data?.data ?? null;
  const linkedInvoices = invoicesQuery.data?.data ?? [];

  const tabs: { id: Tab; label: string; icon: React.ReactNode }[] = [
    { id: 'overview', label: t('orders.detail.tabs.overview'), icon: <ShoppingCart size={12} /> },
    {
      id: 'chain',
      label: t('orders.detail.tabs.chain', { defaultValue: 'Belge Zinciri' }),
      icon: <Workflow size={12} />,
    },
    { id: 'lines', label: t('orders.detail.tabs.lines'), icon: <ListOrdered size={12} /> },
    {
      id: 'margin',
      label: t('orders.detail.tabs.margin', { defaultValue: 'Margin' }),
      icon: <Percent size={12} />,
    },
    {
      id: 'allocations',
      label: t('orders.detail.tabs.allocations', { defaultValue: 'Allocations' }),
      icon: <Boxes size={12} />,
    },
    {
      id: 'shipments',
      label: t('orders.shipments.title'),
      icon: <Truck size={12} />,
    },
    { id: 'customer', label: t('orders.detail.tabs.customer'), icon: <User size={12} /> },
    { id: 'invoices', label: t('orders.detail.tabs.invoices'), icon: <FileText size={12} /> },
    {
      id: 'audit',
      label: t('orders.detail.tabs.audit', { defaultValue: 'Audit' }),
      icon: <Clock size={12} />,
    },
    {
      id: 'changes',
      label: t('Common.AuditTab.ChangesTitle', { defaultValue: 'Changes' }),
      icon: <History size={12} />,
    },
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
          <NextBestAction
            entity="order"
            order={order}
            onCreateShipment={() => {
              setTab('shipments');
              setShipmentCreateOpen(true);
            }}
            onGenerateInvoice={
              onGenerateInvoice && INVOICEABLE.includes(order.status)
                ? () => onGenerateInvoice(order.id)
                : undefined
            }
          />
        )}
        {order && (
          <OrderActionsBar
            order={order}
            onShipmentRequested={() => {
              setTab('shipments');
              setShipmentCreateOpen(true);
            }}
          />
        )}
        {order && (
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              onClick={handleReorder}
              disabled={reorder.isPending}
              className="inline-flex items-center gap-1.5 rounded-md border border-slate-200 bg-white px-2.5 py-1 text-xs font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              <Copy size={13} />
              {t('orders.reorder.button', { defaultValue: 'Tekrarla' })}
            </button>
            {RETURNABLE.includes(order.status) && (
              <button
                type="button"
                onClick={() => setReturnCreateOpen(true)}
                className="inline-flex items-center gap-1.5 rounded-md border border-slate-200 bg-white px-2.5 py-1 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
              >
                <RotateCcw size={13} />
                {t('Returns.create.title')}
              </button>
            )}
            {order.lines.some((l) => l.quantityRemainingToShip > 0) && (
              <button
                type="button"
                onClick={() => setScrapOpen(true)}
                className="inline-flex items-center gap-1.5 rounded-md border border-slate-200 bg-white px-2.5 py-1 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
              >
                <Flame size={13} />
                {t('orders.scrap.button')}
              </button>
            )}
          </div>
        )}
        {tab === 'overview' && order && (
          <>
            <OrderStatusTimeline order={order} locale={i18n.language} />
            <OrderOverviewTab
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
        {tab === 'chain' && order && <DocumentChain entity="order" id={order.id} />}
        {tab === 'lines' && order && <LinesTab order={order} locale={i18n.language} />}
        {tab === 'margin' && order && <OrderMarginTab order={order} locale={i18n.language} />}
        {tab === 'allocations' && order && (
          <OrderAllocationsTab orderId={order.id} locale={i18n.language} />
        )}
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
        {tab === 'audit' && order && <OrderAuditTab order={order} locale={i18n.language} />}
        {tab === 'changes' && order && <AuditTimeline entityType="Order" entityId={order.id} />}
        {tab === 'notes' && (
          <div className="rounded border border-slate-200 bg-slate-50/50 p-3 text-sm text-slate-700 dark:border-slate-800 dark:bg-slate-800/30 dark:text-slate-300">
            {order?.notes || (
              <span className="italic text-slate-500 dark:text-slate-400">
                {t('orders.detail.noNotes')}
              </span>
            )}
          </div>
        )}
      </div>
      <CreateReturnModal
        order={order}
        open={returnCreateOpen}
        onClose={() => setReturnCreateOpen(false)}
        onCreated={(returnId) => navigate(`/dashboard/returns/${returnId}`)}
      />
      {scrapOpen && order && <OrderScrapModal order={order} onClose={() => setScrapOpen(false)} />}
    </DetailPanel>
  );
};

const LinesTab = ({ order, locale }: { order: Order; locale: string }) => {
  const { t } = useTranslation();
  if (order.lines.length === 0) {
    return (
      <div className="rounded border border-slate-200 p-4 text-center text-sm text-slate-500 dark:border-slate-800 dark:text-slate-400">
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
                <div className="font-mono text-[10px] text-slate-500 dark:text-slate-400">
                  {line.productSku}
                </div>
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
    return <div className="text-sm text-slate-500 dark:text-slate-400">{t('common.loading')}</div>;
  }
  if (!customer) {
    return (
      <div className="rounded border border-slate-200 p-4 text-center text-sm text-slate-500 dark:border-slate-800 dark:text-slate-400">
        {t('orders.detail.noCustomer')}
      </div>
    );
  }
  return (
    <div className="space-y-2 rounded-lg border border-slate-200 p-3 text-sm dark:border-slate-800">
      <Row label={t('customers.fields.name')}>
        <Link
          to={`/dashboard/customers/${customer.id}`}
          className="inline-flex items-center gap-1 text-primary-600 hover:underline dark:text-primary-400"
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
    return <div className="text-sm text-slate-500 dark:text-slate-400">{t('common.loading')}</div>;
  }
  if (invoices.length === 0) {
    return (
      <div className="rounded border border-slate-200 p-4 text-center text-sm text-slate-500 dark:border-slate-800 dark:text-slate-400">
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
            <div className="flex items-center gap-1.5 text-[10px] text-slate-500 dark:text-slate-400">
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
