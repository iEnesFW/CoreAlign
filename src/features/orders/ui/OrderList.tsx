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
  onOpenDetails?: (order: OrderSummary) => void;
  onEdit: (order: OrderSummary) => void;
  onDelete: (order: OrderSummary) => void;
  onGenerateInvoice: (order: OrderSummary) => void;
  onCreate?: () => void;
  onStatusTransition?: (order: OrderSummary, action: string) => void;
  statusBusyId?: string | null;
  selectable?: boolean;
  selectedIds?: string[];
  onSelectionChange?: (ids: string[]) => void;
}

const statusTone: Record<OrderStatus, string> = {
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
  onOpenDetails,
  onEdit,
  onDelete,
  onGenerateInvoice,
  onCreate,
  onStatusTransition,
  statusBusyId,
  selectable,
  selectedIds,
  onSelectionChange,
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
      selectable={selectable}
      selectedIds={selectedIds}
      onSelectionChange={onSelectionChange}
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
            className="rounded-lg bg-primary-600 px-3 py-1.5 text-xs font-medium text-white shadow-sm transition hover:bg-primary-700"
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
              <span className="inline-flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-gradient-to-br from-primary-500/15 to-purple-500/15 text-primary-600 ring-1 ring-primary-200/40 dark:text-primary-300 dark:ring-primary-500/30">
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
          {onOpenDetails && (
            <RowActionButton
              icon={<PanelRightOpen size={14} />}
              label={t('common.details', { defaultValue: 'Details' })}
              onClick={() => onOpenDetails(o)}
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
