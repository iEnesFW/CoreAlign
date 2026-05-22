import { useTranslation } from 'react-i18next';
import { Edit2, FileText, Hash, PanelRightOpen, ShoppingCart, Trash2 } from 'lucide-react';
import { DataTable, RowActionButton } from '@/shared/ui/DataTable/DataTable';
import type { OrderStatus, OrderSummary } from '../model/order.types';
import { OrderStatusCell } from './OrderStatusCell';

const INVOICEABLE_STATUSES: OrderStatus[] = ['Confirmed', 'Shipped', 'Closed'];

interface Props {
  orders: OrderSummary[];
  isLoading: boolean;
  selectedId?: string | null;
  onSelect?: (order: OrderSummary) => void;
  onEdit: (order: OrderSummary) => void;
  onDelete: (order: OrderSummary) => void;
  onGenerateInvoice: (order: OrderSummary) => void;
  onCreate?: () => void;
  onStatusTransition?: (order: OrderSummary, action: string) => void;
  statusBusyId?: string | null;
}

const statusTone: Record<OrderStatus, string> = {
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

const fmtRelative = (iso: string, locale: string) => {
  try {
    const target = new Date(iso).getTime();
    const diffMs = Date.now() - target;
    const dayMs = 1000 * 60 * 60 * 24;
    const days = Math.floor(diffMs / dayMs);
    const rtf = new Intl.RelativeTimeFormat(locale, { numeric: 'auto' });
    if (days < 1) return rtf.format(-Math.max(0, Math.floor(diffMs / (1000 * 60 * 60))), 'hour');
    if (days < 30) return rtf.format(-days, 'day');
    if (days < 365) return rtf.format(-Math.floor(days / 30), 'month');
    return rtf.format(-Math.floor(days / 365), 'year');
  } catch {
    return iso.slice(0, 10);
  }
};

export const OrderList = ({
  orders,
  isLoading,
  selectedId,
  onSelect,
  onEdit,
  onDelete,
  onGenerateInvoice,
  onCreate,
  onStatusTransition,
  statusBusyId,
}: Props) => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;

  return (
    <DataTable
      rows={orders}
      getRowId={(o) => o.id}
      isLoading={isLoading}
      selectedId={selectedId ?? null}
      onRowClick={onSelect}
      emptyIcon={<ShoppingCart size={20} />}
      emptyTitle={t('orders.empty')}
      emptyDescription={t('orders.emptyHint', {
        defaultValue: 'Create your first order to begin the sales pipeline.',
      })}
      emptyAction={
        onCreate && (
          <button
            type="button"
            onClick={onCreate}
            className="rounded-lg bg-indigo-600 px-3 py-1.5 text-xs font-medium text-white shadow-sm transition hover:bg-indigo-700"
          >
            {t('orders.addNew')}
          </button>
        )
      }
      columns={[
        {
          key: 'orderNumber',
          label: t('orders.columns.orderNumber'),
          sortable: true,
          sortValue: (o) => o.orderNumber,
          cell: (o) => (
            <div className="flex items-center gap-2">
              <span className="inline-flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-gradient-to-br from-indigo-500/15 to-purple-500/15 text-indigo-600 ring-1 ring-indigo-200/40 dark:text-indigo-300 dark:ring-indigo-500/30">
                <Hash size={11} />
              </span>
              <span className="font-mono text-xs font-semibold text-slate-900 dark:text-slate-100">
                {o.orderNumber}
              </span>
            </div>
          ),
        },
        {
          key: 'customer',
          label: t('orders.columns.customer'),
          sortable: true,
          sortValue: (o) => o.customerName.toLowerCase(),
          cell: (o) => (
            <div className="min-w-0">
              <div className="truncate font-medium text-slate-900 dark:text-slate-100">
                {o.customerName}
              </div>
            </div>
          ),
        },
        {
          key: 'orderDate',
          label: t('orders.columns.orderDate'),
          sortable: true,
          sortValue: (o) => o.orderDate,
          hideOnMobile: true,
          cell: (o) => (
            <div className="text-[11px]">
              <div className="text-slate-700 dark:text-slate-200">
                {fmtDate(o.orderDate, locale)}
              </div>
              <div className="text-[10px] text-slate-500 dark:text-slate-400">
                {fmtRelative(o.orderDate, locale)}
              </div>
            </div>
          ),
        },
        {
          key: 'status',
          label: t('orders.columns.status'),
          sortable: true,
          sortValue: (o) => o.status,
          cell: (o) =>
            onStatusTransition ? (
              <OrderStatusCell
                status={o.status}
                toneClass={statusTone[o.status]}
                busy={statusBusyId === o.id}
                onTransition={(action) => onStatusTransition(o, action)}
              />
            ) : (
              <span
                className={`inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider ${statusTone[o.status]}`}
              >
                {t(`orders.status.${o.status}` as never)}
              </span>
            ),
        },
        {
          key: 'total',
          label: t('orders.columns.total'),
          align: 'right',
          sortable: true,
          sortValue: (o) => o.total,
          cell: (o) => (
            <span className="font-mono text-xs font-semibold tabular-nums text-slate-900 dark:text-slate-100">
              {fmtCurrency(o.total, o.currency, locale)}
            </span>
          ),
        },
      ]}
      rowActionsHeader={
        <span className="text-[10px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
          {t('orders.columns.actions')}
        </span>
      }
      rowActions={(o) => (
        <>
          {onSelect && (
            <RowActionButton
              icon={<PanelRightOpen size={14} />}
              label={t('common.details', { defaultValue: 'Details' })}
              onClick={() => onSelect(o)}
            />
          )}
          {INVOICEABLE_STATUSES.includes(o.status) && (
            <button
              type="button"
              onClick={() => onGenerateInvoice(o)}
              className="rounded-md p-1.5 text-slate-500 transition-colors hover:bg-violet-50 hover:text-violet-600 dark:text-slate-400 dark:hover:bg-violet-500/10 dark:hover:text-violet-300"
              aria-label={t('orders.actions.generateInvoice')}
              title={t('orders.actions.generateInvoice')}
            >
              <FileText size={14} />
            </button>
          )}
          <RowActionButton
            icon={<Edit2 size={14} />}
            label={t('common.edit')}
            onClick={() => onEdit(o)}
          />
          <RowActionButton
            icon={<Trash2 size={14} />}
            label={t('common.delete')}
            tone="danger"
            onClick={() => onDelete(o)}
          />
        </>
      )}
    />
  );
};
