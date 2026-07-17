import { forwardRef, useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { Product } from '@/shared/model/product.types';

interface Props {
  products: Product[];
  value: string;
  disabled?: boolean;
  invalid?: boolean;
  onSelect: (productId: string) => void;
  onKeyDown?: React.KeyboardEventHandler<HTMLInputElement>;
}

const MAX_RESULTS = 50;

const labelOf = (p: Product) => `${p.sku} — ${p.name}`;

export const ProductPicker = forwardRef<HTMLInputElement, Props>(
  ({ products, value, disabled, invalid, onSelect, onKeyDown: externalOnKeyDown }, ref) => {
    const { t } = useTranslation();
    const selected = useMemo(() => products.find((p) => p.id === value), [products, value]);
    const [query, setQuery] = useState('');
    const [open, setOpen] = useState(false);
    const [highlight, setHighlight] = useState(0);
    const blurTimer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

    const syncedId = useRef<string | undefined>(undefined);
    if (selected?.id !== syncedId.current) {
      syncedId.current = selected?.id;
      setQuery(selected ? labelOf(selected) : '');
    }

    useEffect(
      () => () => {
        if (blurTimer.current) clearTimeout(blurTimer.current);
      },
      [],
    );

    const filtered = useMemo(() => {
      const q = query.trim().toLowerCase();
      const showAll = q === '' || (selected && q === labelOf(selected).toLowerCase());
      const list = showAll
        ? products
        : products.filter(
            (p) =>
              p.sku.toLowerCase().includes(q) ||
              p.name.toLowerCase().includes(q) ||
              (p.barcode?.toLowerCase().includes(q) ?? false),
          );
      return list.slice(0, MAX_RESULTS);
    }, [products, query, selected]);

    const commit = (product: Product) => {
      onSelect(product.id);
      setQuery(labelOf(product));
      setOpen(false);
    };

    const revert = () => setQuery(selected ? labelOf(selected) : '');

    const onKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === 'ArrowDown') {
        e.preventDefault();
        if (!open) setOpen(true);
        setHighlight((h) => Math.min(h + 1, filtered.length - 1));
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        setHighlight((h) => Math.max(h - 1, 0));
      } else if (e.key === 'Enter') {
        if (open && filtered[highlight]) {
          e.preventDefault();
          commit(filtered[highlight]);
        }
      } else if (e.key === 'Escape') {
        setOpen(false);
        revert();
      }

      if (!e.defaultPrevented) externalOnKeyDown?.(e);
    };

    return (
      <div className="relative">
        <input
          ref={ref}
          type="text"
          role="combobox"
          aria-expanded={open}
          aria-autocomplete="list"
          autoComplete="off"
          disabled={disabled}
          value={query}
          placeholder={t('orders.lines.productPlaceholder')}
          onFocus={(e) => {
            setOpen(true);
            setHighlight(0);
            e.currentTarget.select();
          }}
          onChange={(e) => {
            setQuery(e.target.value);
            setOpen(true);
            setHighlight(0);
          }}
          onBlur={() => {
            blurTimer.current = setTimeout(() => {
              setOpen(false);
              revert();
            }, 120);
          }}
          onKeyDown={onKeyDown}
          className={`w-full rounded-xl border bg-white/60 backdrop-blur-md px-3 py-2 text-sm text-slate-900 focus:bg-white focus:outline-none focus:ring-2 disabled:opacity-60 transition-all dark:bg-slate-900/60 dark:text-slate-100 dark:focus:bg-slate-900 ${
            invalid
              ? 'border-danger-400 focus:border-danger-500 focus:ring-danger-500/20'
              : 'border-slate-200 focus:border-indigo-500 focus:ring-indigo-500/20 dark:border-slate-700/50'
          }`}
        />
        {open && filtered.length > 0 && (
          <ul
            className="absolute z-20 mt-1 max-h-60 w-full overflow-auto rounded border border-slate-200 bg-white py-1 shadow-lg dark:border-slate-700 dark:bg-slate-800"
            role="listbox"
            onMouseDown={(e) => {
              e.preventDefault();
              if (blurTimer.current) clearTimeout(blurTimer.current);
            }}
          >
            {filtered.map((p, i) => (
              <li key={p.id} role="option" aria-selected={i === highlight}>
                <button
                  type="button"
                  onClick={() => commit(p)}
                  onMouseEnter={() => setHighlight(i)}
                  className={`flex w-full items-center justify-between gap-2 px-2 py-1.5 text-left text-sm ${
                    i === highlight
                      ? 'bg-primary-50 dark:bg-primary-500/20'
                      : 'hover:bg-slate-50 dark:hover:bg-slate-700/50'
                  }`}
                >
                  <span className="min-w-0">
                    <span className="font-mono text-xs font-semibold text-slate-900 dark:text-slate-100">
                      {p.sku}
                    </span>
                    <span className="ml-2 truncate text-slate-600 dark:text-slate-300">
                      {p.name}
                    </span>
                  </span>
                  <span className="shrink-0 text-xs text-slate-400">
                    {p.stockQuantity} {p.unit}
                  </span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    );
  },
);

ProductPicker.displayName = 'ProductPicker';
