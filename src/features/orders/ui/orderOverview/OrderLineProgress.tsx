import { useTranslation } from 'react-i18next';
import { Package } from 'lucide-react';
import type { OrderLine } from '@/features/orders/model/order.types';
import { fmtNumber } from './format';

export const LineProgressList = ({ lines, locale }: { lines: OrderLine[]; locale: string }) => {
  const { t } = useTranslation();
  if (lines.length === 0) return null;
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <Package size={12} />
          {t('orders.detail.lineProgress')}
        </span>
        <span className="text-slate-400">{lines.length}</span>
      </header>
      <ul className="mt-2 space-y-1.5">
        {lines.map((line) => {
          const shippedPct = line.quantity > 0 ? (line.quantityShipped / line.quantity) * 100 : 0;
          const invoicedPct = line.quantity > 0 ? (line.quantityInvoiced / line.quantity) * 100 : 0;
          return (
            <li
              key={line.id}
              className="space-y-1 rounded border border-slate-200 px-2 py-1.5 dark:border-slate-800"
            >
              <div className="flex items-center justify-between gap-2 text-[11px]">
                <div className="min-w-0">
                  <div className="truncate font-medium text-slate-900 dark:text-slate-100">
                    {line.productName}
                  </div>
                  <div className="font-mono text-[9px] text-slate-500 dark:text-slate-400">
                    {line.productSku}
                  </div>
                </div>
                <div className="shrink-0 text-right text-[10px] text-slate-500 dark:text-slate-400">
                  {fmtNumber(line.quantity, locale)} {line.uomCode ?? ''}
                </div>
              </div>
              <ProgressRow
                label={t('orders.detail.lineProgressShipped')}
                done={line.quantityShipped}
                total={line.quantity}
                pct={shippedPct}
                locale={locale}
                color="bg-warning-500"
              />
              <ProgressRow
                label={t('orders.detail.lineProgressInvoiced')}
                done={line.quantityInvoiced}
                total={line.quantity}
                pct={invoicedPct}
                locale={locale}
                color="bg-violet-500"
              />
            </li>
          );
        })}
      </ul>
    </section>
  );
};

const ProgressRow = ({
  label,
  done,
  total,
  pct,
  locale,
  color,
}: {
  label: string;
  done: number;
  total: number;
  pct: number;
  locale: string;
  color: string;
}) => (
  <div>
    <div className="flex items-center justify-between text-[9px] text-slate-500 dark:text-slate-400">
      <span>{label}</span>
      <span className="tabular-nums">
        {fmtNumber(done, locale)} / {fmtNumber(total, locale)}
      </span>
    </div>
    <div className="mt-0.5 h-1 w-full overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
      <div
        className={`h-full rounded-full transition-all ${color}`}
        style={{ width: `${Math.min(100, Math.max(0, pct))}%` }}
      />
    </div>
  </div>
);
