import { useTranslation } from 'react-i18next';
import { Edit2, Trash2 } from 'lucide-react';
import type { Product } from '../model/product.types';

interface Props {
  products: Product[];
  isLoading: boolean;
  selectedId?: string | null;
  onSelect?: (product: Product) => void;
  onEdit: (product: Product) => void;
  onDelete: (product: Product) => void;
}

const formatPrice = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

export const ProductList = ({
  products,
  isLoading,
  selectedId,
  onSelect,
  onEdit,
  onDelete,
}: Props) => {
  const { t, i18n } = useTranslation();

  if (isLoading && products.length === 0) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-400">
        {t('common.loading')}
      </div>
    );
  }

  if (products.length === 0) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-400">
        {t('products.empty')}
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead className="bg-slate-50 dark:bg-slate-800/50">
            <tr>
              <Th>{t('products.columns.sku')}</Th>
              <Th>{t('products.columns.name')}</Th>
              <Th>{t('products.columns.price')}</Th>
              <Th>{t('products.columns.stock')}</Th>
              <Th>{t('products.columns.status')}</Th>
              <th className="px-3 py-2 text-right text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('products.columns.actions')}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
            {products.map((product) => {
              const isSelected = selectedId === product.id;
              return (
                <tr
                  key={product.id}
                  onClick={() => onSelect?.(product)}
                  onKeyDown={(e) => {
                    if (!onSelect) return;
                    if (e.key === 'Enter' || e.key === ' ') {
                      e.preventDefault();
                      onSelect(product);
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
                  <Td className="font-mono text-xs">{product.sku}</Td>
                  <Td className="font-medium text-slate-900 dark:text-slate-100">{product.name}</Td>
                  <Td>{formatPrice(product.price, product.currency, i18n.language)}</Td>
                  <Td>
                    {product.stockQuantity} {product.unit}
                  </Td>
                  <Td>
                    <span
                      className={
                        product.isActive
                          ? 'inline-flex items-center rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300'
                          : 'inline-flex items-center rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600 dark:bg-slate-700/40 dark:text-slate-300'
                      }
                    >
                      {product.isActive ? t('common.active') : t('common.inactive')}
                    </span>
                  </Td>
                  <td className="px-3 py-2 text-right" onClick={(e) => e.stopPropagation()}>
                    <div className="inline-flex items-center gap-1">
                      <button
                        type="button"
                        onClick={() => onEdit(product)}
                        className="rounded p-1.5 text-slate-500 hover:bg-slate-100 hover:text-indigo-600 dark:text-slate-400 dark:hover:bg-slate-800 dark:hover:text-indigo-400"
                        aria-label={t('common.edit')}
                      >
                        <Edit2 size={14} />
                      </button>
                      <button
                        type="button"
                        onClick={() => onDelete(product)}
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
