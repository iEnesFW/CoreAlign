import { useTranslation } from 'react-i18next';
import { Trophy } from 'lucide-react';
import type { TopProduct } from '@/features/customers/model/customer.types';
import { fmtCurrency, fmtNumber } from './format';

export const TopProductsCard = ({
  products,
  currency,
  locale,
  onOpenProduct,
}: {
  products: TopProduct[];
  currency: string;
  locale: string;
  onOpenProduct?: (productId: string) => void;
}) => {
  const { t } = useTranslation();
  if (products.length === 0) return null;
  const maxRev = Math.max(1, ...products.map((p) => p.revenue));
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <Trophy size={12} />
          {t('customers.analytics.topProducts')}
        </span>
        <span className="text-slate-400">{products.length}</span>
      </header>
      <ol className="mt-2 space-y-1.5">
        {products.map((p, idx) => {
          const pct = (p.revenue / maxRev) * 100;
          const clickable = !!onOpenProduct && !!p.productId;
          return (
            <li key={`${p.productSku}-${idx}`}>
              <button
                type="button"
                onClick={clickable ? () => onOpenProduct?.(p.productId as string) : undefined}
                disabled={!clickable}
                className={`flex w-full items-center gap-2 rounded border border-slate-200 p-1.5 text-left text-[11px] transition dark:border-slate-800 ${clickable ? 'hover:bg-slate-50 dark:hover:bg-slate-800/50' : 'cursor-default'}`}
              >
                <span className="inline-flex h-5 w-5 shrink-0 items-center justify-center rounded bg-primary-100 text-[10px] font-bold text-primary-700 dark:bg-primary-500/20 dark:text-primary-300">
                  {idx + 1}
                </span>
                <div className="min-w-0 flex-1">
                  <div className="flex items-center justify-between gap-2">
                    <div className="min-w-0 truncate font-medium text-slate-900 dark:text-slate-100">
                      {p.productName}
                    </div>
                    <div className="shrink-0 font-mono tabular-nums text-slate-900 dark:text-slate-100">
                      {fmtCurrency(p.revenue, currency, locale)}
                    </div>
                  </div>
                  <div className="mt-0.5 flex items-center justify-between gap-2 text-[9px] text-slate-500 dark:text-slate-400">
                    <span className="font-mono">{p.productSku}</span>
                    <span>
                      {fmtNumber(p.quantity, locale)} {t('inventory.fields.onHand').toLowerCase()} ·{' '}
                      {p.invoiceCount} {t('customers.detail.metrics.invoiceCount')}
                    </span>
                  </div>
                  <div className="mt-1 h-0.5 w-full overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
                    <div
                      className="h-full rounded-full bg-primary-500"
                      style={{ width: `${pct}%` }}
                    />
                  </div>
                </div>
              </button>
            </li>
          );
        })}
      </ol>
    </section>
  );
};
