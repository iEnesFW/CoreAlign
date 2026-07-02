import { type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import type { StatusBreakdown } from '@/features/customers/model/customer.types';
import { fmtCurrency } from './format';

const statusToneByKind: Record<string, string> = {
  Draft: 'bg-slate-200 dark:bg-slate-700',
  Submitted: 'bg-info-400',
  Approved: 'bg-primary-500',
  Confirmed: 'bg-primary-500',
  Allocated: 'bg-violet-500',
  Picking: 'bg-fuchsia-500',
  Packed: 'bg-purple-500',
  PartiallyShipped: 'bg-warning-400',
  Shipped: 'bg-warning-500',
  Delivered: 'bg-teal-500',
  Closed: 'bg-success-500',
  Cancelled: 'bg-danger-500',
  Returned: 'bg-danger-500',
  Issued: 'bg-primary-500',
  Sent: 'bg-info-500',
  PartiallyPaid: 'bg-warning-500',
  Paid: 'bg-success-500',
  Overdue: 'bg-danger-500',
  Void: 'bg-danger-500',
};

export const StatusBreakdownCard = ({
  title,
  icon,
  items,
  currency,
  locale,
  statusPrefix,
}: {
  title: string;
  icon: ReactNode;
  items: StatusBreakdown[];
  currency: string;
  locale: string;
  statusPrefix: string;
}) => {
  const { t } = useTranslation();
  if (items.length === 0) return null;
  const totalCount = items.reduce((s, i) => s + i.count, 0);
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          {icon}
          {title}
        </span>
        <span className="text-slate-400">{totalCount}</span>
      </header>
      <div className="mt-2 flex h-1.5 overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
        {items.map((item) => {
          const pct = (item.count / Math.max(1, totalCount)) * 100;
          if (pct <= 0) return null;
          return (
            <div
              key={item.status}
              className={statusToneByKind[item.status] ?? 'bg-slate-400'}
              style={{ width: `${pct}%` }}
              title={`${t(`${statusPrefix}.${item.status}` as never, { defaultValue: item.status })}: ${item.count}`}
            />
          );
        })}
      </div>
      <ul className="mt-2 space-y-1 text-[11px]">
        {items.map((item) => (
          <li key={item.status} className="flex items-center justify-between gap-2">
            <div className="flex min-w-0 items-center gap-1.5">
              <span
                className={`inline-block h-2 w-2 shrink-0 rounded-full ${statusToneByKind[item.status] ?? 'bg-slate-400'}`}
              />
              <span className="truncate text-slate-700 dark:text-slate-200">
                {t(`${statusPrefix}.${item.status}` as never, { defaultValue: item.status })}
              </span>
            </div>
            <div className="shrink-0 text-right text-slate-500 dark:text-slate-400">
              <span className="tabular-nums">{item.count}</span>
              <span className="ml-1 font-mono text-[10px]">
                {fmtCurrency(item.total, currency, locale)}
              </span>
            </div>
          </li>
        ))}
      </ul>
    </section>
  );
};
