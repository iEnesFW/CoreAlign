import { useTranslation } from 'react-i18next';
import { Check, Circle, X } from 'lucide-react';
import type { Order, OrderStatus } from '../model/order.types';

interface Props {
  order: Order;
  locale: string;
}

interface Step {
  status: OrderStatus | 'Cancelled';
  labelKey: string;
  timestamp: string | null;
}

const STEPS: { status: OrderStatus; labelKey: string }[] = [
  { status: 'Draft', labelKey: 'orders.status.Draft' },
  { status: 'Submitted', labelKey: 'orders.status.Submitted' },
  { status: 'Approved', labelKey: 'orders.status.Approved' },
  { status: 'Allocated', labelKey: 'orders.status.Allocated' },
  { status: 'Shipped', labelKey: 'orders.status.Shipped' },
  { status: 'Delivered', labelKey: 'orders.status.Delivered' },
  { status: 'Closed', labelKey: 'orders.status.Closed' },
];

const STATUS_RANK: Record<OrderStatus, number> = {
  Draft: 0,
  Submitted: 1,
  Approved: 2,
  Allocated: 3,
  Picking: 3,
  Packed: 3,
  PartiallyShipped: 4,
  Shipped: 4,
  Delivered: 5,
  Closed: 6,
  Returned: 6,
  Cancelled: -1,
  Confirmed: 3,
};

const fmtDateTime = (iso: string | null, locale: string) => {
  if (!iso) return null;
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'short', timeStyle: 'short' }).format(
      new Date(iso),
    );
  } catch {
    return iso;
  }
};

export const OrderStatusTimeline = ({ order, locale }: Props) => {
  const { t } = useTranslation();
  const currentRank = STATUS_RANK[order.status] ?? 0;
  const isCancelled = order.status === 'Cancelled';

  const steps: Step[] = STEPS.map((s, idx) => ({
    status: s.status,
    labelKey: s.labelKey,
    timestamp:
      idx === 0
        ? order.createdAtUtc
        : s.status === 'Submitted'
          ? order.submittedAtUtc
          : s.status === 'Approved'
            ? order.approvedAtUtc
            : s.status === 'Delivered'
              ? order.actualDeliveryDate
              : null,
  }));

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
      <h4 className="mb-3 text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        {t('orders.timeline.title')}
      </h4>
      {isCancelled ? (
        <div className="flex items-center gap-2 rounded border border-danger-200 bg-danger-50/60 p-3 text-sm dark:border-danger-500/30 dark:bg-danger-500/10">
          <X size={14} className="text-danger-600 dark:text-danger-400" />
          <div className="flex-1">
            <div className="font-medium text-danger-700 dark:text-danger-300">
              {t('orders.timeline.cancelledAt')}
            </div>
            {order.cancelledAtUtc && (
              <div className="text-[11px] text-danger-600/80 dark:text-danger-400/80">
                {fmtDateTime(order.cancelledAtUtc, locale)}
                {order.cancelReason && ` · ${order.cancelReason}`}
              </div>
            )}
          </div>
        </div>
      ) : (
        <ol className="space-y-3">
          {steps.map((s, idx) => {
            const stepRank = idx;
            const isComplete = currentRank > stepRank || (currentRank === stepRank && idx > 0);
            const isCurrent =
              STATUS_RANK[order.status] === stepRank || (idx === 0 && order.status === 'Draft');
            const ts = fmtDateTime(s.timestamp, locale);
            return (
              <li key={s.status} className="flex items-start gap-3">
                <div
                  className={`mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-[10px] ${
                    isComplete
                      ? 'bg-success-500 text-white'
                      : isCurrent
                        ? 'bg-primary-500 text-white'
                        : 'bg-slate-200 text-slate-400 dark:bg-slate-700 dark:text-slate-500'
                  }`}
                >
                  {isComplete ? <Check size={11} /> : <Circle size={9} />}
                </div>
                <div className="flex-1">
                  <div
                    className={`text-xs font-medium ${
                      isCurrent
                        ? 'text-primary-700 dark:text-primary-300'
                        : isComplete
                          ? 'text-slate-700 dark:text-slate-300'
                          : 'text-slate-400 dark:text-slate-500'
                    }`}
                  >
                    {t(s.labelKey as never)}
                  </div>
                  {ts && <div className="text-[10px] text-slate-500 dark:text-slate-400">{ts}</div>}
                </div>
              </li>
            );
          })}
        </ol>
      )}
    </div>
  );
};
