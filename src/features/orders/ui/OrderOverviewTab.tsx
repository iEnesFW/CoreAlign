import { useTranslation } from 'react-i18next';
import { Edit2, FileText, MapPin, Receipt } from 'lucide-react';
import type { Order, OrderStatus } from '@/features/orders/model/order.types';
import { daysFromNow, fmtDate } from './orderOverview/format';
import { KpiRow, MetaChips } from './orderOverview/OrderKpis';
import { FinancialBreakdown } from './orderOverview/OrderFinancial';
import { AddressSnapshotCard, CustomerSnapshotCard } from './orderOverview/OrderSnapshots';
import { LineProgressList } from './orderOverview/OrderLineProgress';

interface Props {
  order: Order;
  locale: string;
  onEdit: () => void;
  onGenerateInvoice?: () => void;
}

const statusStyles: Record<OrderStatus, string> = {
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

export const OrderOverviewTab = ({ order, locale, onEdit, onGenerateInvoice }: Props) => {
  const { t } = useTranslation();
  const totalQty = order.lines.reduce((s, l) => s + l.quantity, 0);
  const shippedQty = order.lines.reduce((s, l) => s + l.quantityShipped, 0);
  const invoicedQty = order.lines.reduce((s, l) => s + l.quantityInvoiced, 0);
  const dueIn = daysFromNow(order.dueDate);
  const showInvoiceCta = !!onGenerateInvoice;

  return (
    <div className="space-y-3">
      <KpiRow
        order={order}
        locale={locale}
        totalQty={totalQty}
        shippedQty={shippedQty}
        invoicedQty={invoicedQty}
      />

      <MetaChips order={order} dueIn={dueIn} />

      <FinancialBreakdown order={order} locale={locale} />

      {order.customerSnapshot && <CustomerSnapshotCard snapshot={order.customerSnapshot} />}

      <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
        <AddressSnapshotCard
          icon={<Receipt size={12} />}
          title={t('orders.detail.billingAddress')}
          snapshot={order.billingAddressSnapshot}
          empty={t('orders.detail.noBillingAddress')}
        />
        <AddressSnapshotCard
          icon={<MapPin size={12} />}
          title={t('orders.detail.shippingAddress')}
          snapshot={order.shippingAddressSnapshot}
          empty={t('orders.detail.noShippingAddress')}
        />
      </div>

      <LineProgressList lines={order.lines} locale={locale} />

      <div className="flex items-center gap-2 rounded-lg border border-slate-200 bg-white p-2.5 text-[11px] dark:border-slate-800 dark:bg-slate-900">
        <span className="text-slate-500 dark:text-slate-400">{t('orders.fields.status')}</span>
        <span
          className={`inline-flex rounded-full px-2 py-0.5 text-[10px] font-medium ${statusStyles[order.status]}`}
        >
          {t(`orders.status.${order.status}` as never)}
        </span>
        <span className="ml-auto text-[10px] text-slate-500 dark:text-slate-400">
          {t('orders.fields.orderDate')}: {fmtDate(order.orderDate, locale)}
        </span>
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
        {showInvoiceCta && (
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
    </div>
  );
};
