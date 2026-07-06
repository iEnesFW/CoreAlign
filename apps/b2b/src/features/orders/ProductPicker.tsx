import { Search } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Input } from '@/shared/ui/Input';
import { Spinner } from '@/shared/ui/Spinner';
import { useCatalogProducts } from '@/features/portal/hooks';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import type { ProductSummary } from '@/features/portal/types';

interface ProductPickerProps {
  customerId: string | null;
  onPick: (product: ProductSummary) => void;
}

const DEBOUNCE_MS = 250;

export const ProductPicker = ({ customerId, onPick }: ProductPickerProps) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const [search, setSearch] = useState('');
  const [debounced, setDebounced] = useState('');

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), DEBOUNCE_MS);
    return () => window.clearTimeout(handle);
  }, [search]);

  const { data, isFetching } = useCatalogProducts(
    { search: debounced || undefined, customerId: customerId ?? undefined, page: 1, pageSize: 20 },
    { enabled: debounced.length >= 2 },
  );

  return (
    <div className="space-y-3">
      <div className="relative">
        <Search
          size={14}
          className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"
        />
        <Input
          type="search"
          placeholder={t('b2b.newOrder.productSearch')}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="pl-9"
        />
      </div>

      {debounced.length < 2 ? (
        <p className="rounded-xl border border-dashed border-slate-200 px-4 py-6 text-center text-xs text-slate-500 dark:border-slate-700">
          {t('b2b.newOrder.searchPrompt')}
        </p>
      ) : isFetching ? (
        <p className="flex items-center gap-2 px-2 py-2 text-xs text-slate-500">
          <Spinner size={12} /> {t('b2b.common.loading')}
        </p>
      ) : (data?.items.length ?? 0) === 0 ? (
        <p className="rounded-xl border border-dashed border-slate-200 px-4 py-6 text-center text-xs text-slate-500 dark:border-slate-700">
          {t('b2b.newOrder.noProducts')}
        </p>
      ) : (
        <ul className="max-h-64 divide-y divide-slate-100 overflow-y-auto rounded-xl border border-slate-200 dark:divide-slate-800 dark:border-slate-700">
          {data!.items.map((p) => (
            <li key={p.id}>
              <button
                type="button"
                onClick={() => onPick(p)}
                className="flex w-full items-center justify-between gap-3 px-4 py-2.5 text-left text-sm transition hover:bg-amber-50 dark:hover:bg-amber-900/20"
              >
                <div className="min-w-0">
                  <p className="truncate font-medium text-slate-900 dark:text-slate-100">
                    {p.name}
                  </p>
                  <p className="truncate text-xs text-slate-500">{p.sku}</p>
                  {p.isStockTracked ? (
                    (p.stockQuantity ?? 0) > 0 ? (
                      <p className="text-[11px] font-medium text-emerald-600 dark:text-emerald-400">
                        {t('b2b.newOrder.inStock', { qty: p.stockQuantity, unit: p.unit })}
                      </p>
                    ) : (
                      <p className="text-[11px] font-medium text-red-500 dark:text-red-400">
                        {t('b2b.newOrder.outOfStock')}
                      </p>
                    )
                  ) : null}
                </div>
                <span className="text-sm font-semibold text-slate-700 dark:text-slate-200">
                  {formatCurrency(p.price, locale, p.currency || 'TRY')}
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};
