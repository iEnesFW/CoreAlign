import { type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { FileText, Plus, Receipt, ShoppingCart } from 'lucide-react';
import type { CustomerActivityItem } from '@/features/customers/model/customer.types';
import { fmtCurrency, fmtDate, fmtRelative } from './format';

const activityKindStyles: Record<string, { tone: string; icon: ReactNode }> = {
  Order: {
    tone: 'bg-primary-100 text-primary-700 dark:bg-primary-500/20 dark:text-primary-300',
    icon: <ShoppingCart size={11} />,
  },
  Invoice: {
    tone: 'bg-primary-100 text-primary-700 dark:bg-primary-500/20 dark:text-primary-300',
    icon: <FileText size={11} />,
  },
  Payment: {
    tone: 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300',
    icon: <Receipt size={11} />,
  },
};

export const RecentActivityFeed = ({
  activity,
  loading,
  locale,
  onOpenOrder,
  onOpenInvoice,
  onOpenPayment,
}: {
  activity: CustomerActivityItem[];
  loading: boolean;
  locale: string;
  onOpenOrder?: (orderId: string) => void;
  onOpenInvoice?: (invoiceId: string) => void;
  onOpenPayment?: (paymentId: string) => void;
}) => {
  const { t } = useTranslation();
  const handleOpen = (item: CustomerActivityItem) => {
    if (item.kind === 'Order') onOpenOrder?.(item.sourceId);
    else if (item.kind === 'Invoice') onOpenInvoice?.(item.sourceId);
    else if (item.kind === 'Payment') onOpenPayment?.(item.sourceId);
  };

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1">
          <Plus size={12} />
          {t('customers.detail.recentActivity')}
        </span>
        <span className="text-slate-400">{activity.length}</span>
      </header>
      {activity.length === 0 ? (
        <div className="mt-2 rounded border border-dashed border-slate-200 p-3 text-center text-[11px] italic text-slate-400 dark:border-slate-700 dark:text-slate-500">
          {loading ? t('common.loading') : t('customers.detail.noRecentActivity')}
        </div>
      ) : (
        <ul className="mt-2 divide-y divide-slate-100 dark:divide-slate-800">
          {activity.map((item) => {
            const style = activityKindStyles[item.kind] ?? {
              tone: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
              icon: <Plus size={11} />,
            };
            const relative = fmtRelative(item.occurredAtUtc, locale);
            const clickable =
              (item.kind === 'Order' && !!onOpenOrder) ||
              (item.kind === 'Invoice' && !!onOpenInvoice) ||
              (item.kind === 'Payment' && !!onOpenPayment);
            return (
              <li
                key={`${item.kind}-${item.sourceId}`}
                className={`flex items-center justify-between gap-2 py-1.5 ${clickable ? 'cursor-pointer hover:bg-slate-50 dark:hover:bg-slate-800/50' : ''}`}
                onClick={clickable ? () => handleOpen(item) : undefined}
              >
                <div className="flex min-w-0 items-center gap-2">
                  <span
                    className={`inline-flex h-5 w-5 shrink-0 items-center justify-center rounded ${style.tone}`}
                  >
                    {style.icon}
                  </span>
                  <div className="min-w-0">
                    <div className="flex items-center gap-1.5 text-[11px] font-medium text-slate-900 dark:text-slate-100">
                      <span className="font-mono">{item.sourceNumber ?? '—'}</span>
                      {item.status && (
                        <span className="rounded bg-slate-100 px-1 py-px text-[9px] font-semibold uppercase tracking-wider text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                          {item.status}
                        </span>
                      )}
                    </div>
                    <div className="text-[10px] text-slate-500 dark:text-slate-400">
                      {fmtDate(item.occurredAtUtc, locale)}
                      {relative ? ` · ${relative}` : ''}
                    </div>
                  </div>
                </div>
                <div className="shrink-0 text-right">
                  <div className="text-[11px] font-semibold tabular-nums text-slate-900 dark:text-slate-100">
                    {fmtCurrency(item.amount, item.currency, locale)}
                  </div>
                  <div className="text-[9px] uppercase tracking-wider text-slate-400">
                    {t(`customers.detail.activity.${item.kind}`, { defaultValue: item.kind })}
                  </div>
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
};
