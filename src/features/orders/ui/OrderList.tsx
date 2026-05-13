import { useTranslation } from 'react-i18next';
import { Edit2, FileText, Trash2 } from 'lucide-react';
import type { OrderStatus, OrderSummary } from '../model/order.types';

const INVOICEABLE_STATUSES: OrderStatus[] = ['Confirmed', 'Shipped', 'Closed'];

interface Props {
  orders: OrderSummary[];
  isLoading: boolean;
  selectedId?: string | null;
  onSelect?: (order: OrderSummary) => void;
  onEdit: (order: OrderSummary) => void;
  onDelete: (order: OrderSummary) => void;
  onGenerateInvoice: (order: OrderSummary) => void;
}

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

const formatTotal = (value: number, currency: string, locale: string) => {
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

export const OrderList = ({
  orders,
  isLoading,
  selectedId,
  onSelect,
  onEdit,
  onDelete,
  onGenerateInvoice,
}: Props) => {
  const { t, i18n } = useTranslation();

  if (isLoading && orders.length === 0) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-400">
        {t('common.loading')}
      </div>
    );
  }

  if (orders.length === 0) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-400">
        {t('orders.empty')}
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead className="bg-slate-50 dark:bg-slate-800/50">
            <tr>
              <Th>{t('orders.columns.orderNumber')}</Th>
              <Th>{t('orders.columns.customer')}</Th>
              <Th>{t('orders.columns.orderDate')}</Th>
              <Th>{t('orders.columns.status')}</Th>
              <Th>{t('orders.columns.total')}</Th>
              <th className="px-3 py-2 text-right text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('orders.columns.actions')}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
            {orders.map((order) => {
              const isSelected = selectedId === order.id;
              return (
                <tr
                  key={order.id}
                  onClick={() => onSelect?.(order)}
                  onKeyDown={(e) => {
                    if (!onSelect) return;
                    if (e.key === 'Enter' || e.key === ' ') {
                      e.preventDefault();
                      onSelect(order);
                    }
                  }}
                  tabIndex={onSelect ? 0 : -1}
                  role={onSelect ? 'button' : undefined}
                  aria-selected={onSelect ? isSelected : undefined}
                  className={`${onSelect ? 'cursor-pointer focus:outline-none focus:ring-2 focus:ring-indigo-500' : ''} ${
                    isSelected
                      ? 'bg-indigo-50 dark:bg-indigo-500/10'
                      : 'hover:bg-slate-50 dark:hover:bg-slate-800/50'
                  }`}
                >
                  <Td className="font-mono text-xs">{order.orderNumber}</Td>
                  <Td className="font-medium text-slate-900 dark:text-slate-100">
                    {order.customerName}
                  </Td>
                  <Td>{formatDate(order.orderDate, i18n.language)}</Td>
                  <Td>
                    <span
                      className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${statusStyles[order.status]}`}
                    >
                      {t(`orders.status.${order.status}` as never)}
                    </span>
                  </Td>
                  <Td>{formatTotal(order.total, order.currency, i18n.language)}</Td>
                  <td className="px-3 py-2 text-right" onClick={(e) => e.stopPropagation()}>
                    <div className="inline-flex items-center gap-1">
                      {INVOICEABLE_STATUSES.includes(order.status) && (
                        <button
                          type="button"
                          onClick={() => onGenerateInvoice(order)}
                          className="rounded p-1.5 text-slate-500 hover:bg-violet-50 hover:text-violet-600 dark:text-slate-400 dark:hover:bg-violet-500/10 dark:hover:text-violet-400"
                          aria-label={t('orders.actions.generateInvoice')}
                          title={t('orders.actions.generateInvoice')}
                        >
                          <FileText size={14} />
                        </button>
                      )}
                      <button
                        type="button"
                        onClick={() => onEdit(order)}
                        className="rounded p-1.5 text-slate-500 hover:bg-slate-100 hover:text-indigo-600 dark:text-slate-400 dark:hover:bg-slate-800 dark:hover:text-indigo-400"
                        aria-label={t('common.edit')}
                      >
                        <Edit2 size={14} />
                      </button>
                      <button
                        type="button"
                        onClick={() => onDelete(order)}
                        className="rounded p-1.5 text-slate-500 hover:bg-red-50 hover:text-red-600 dark:text-slate-400 dark:hover:bg-red-500/10 dark:hover:text-red-400"
                        aria-label={t('common.delete')}
                      >
                        <Trash2 size={14} />
                      </button>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
};

const Th = ({ children }: { children: React.ReactNode }) => (
  <th className="px-3 py-2 text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
    {children}
  </th>
);

const Td = ({ children, className }: { children: React.ReactNode; className?: string }) => (
  <td className={`px-3 py-2 text-slate-700 dark:text-slate-200 ${className ?? ''}`}>{children}</td>
);
