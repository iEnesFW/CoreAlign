import type { FieldErrors, UseFormRegister } from 'react-hook-form';
import { Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { ProductPicker } from '@/shared/ui/ProductPicker';
import type { Product } from '@/shared/model/product.types';
import type {
  StockVoucherFormValues,
  StockVoucherLineFormValues,
  StockVoucherType,
} from '../model/stockVoucherSchema';

const cellCls =
  'min-w-0 w-full rounded-md border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-[#2a3143] dark:bg-[#0f111a] dark:text-slate-200';

interface Props {
  index: number;
  type: StockVoucherType;
  register: UseFormRegister<StockVoucherFormValues>;
  errors?: FieldErrors<StockVoucherLineFormValues>;
  line?: StockVoucherLineFormValues;
  products: Product[];
  onHand: number | null;
  canRemove: boolean;
  onProductSelect: (index: number, productId: string) => void;
  onRemove: (index: number) => void;
}

export const StockVoucherLineEditor = ({
  index,
  type,
  register,
  errors,
  line,
  products,
  onHand,
  canRemove,
  onProductSelect,
  onRemove,
}: Props) => {
  const { t } = useTranslation();
  const firstError = errors?.productId?.message ?? errors?.quantity?.message;

  return (
    <div className="min-w-0 px-4 py-3 lg:grid lg:grid-cols-[minmax(0,3fr)_minmax(0,2fr)_3.75rem] lg:items-center lg:gap-3">
      <div className="min-w-0">
        <ProductPicker
          products={products}
          value={line?.productId ?? ''}
          invalid={Boolean(errors?.productId)}
          onSelect={(productId) => onProductSelect(index, productId)}
        />
      </div>

      <div className="mt-2 grid min-w-0 grid-cols-2 gap-2 lg:mt-0">
        <input
          className={`${cellCls} text-right`}
          type="number"
          step="any"
          min="0"
          aria-label={
            type === 'count' ? t('inventory.voucher.counted') : t('inventory.voucher.quantity')
          }
          {...register(`lines.${index}.quantity`, { valueAsNumber: true })}
        />
        {type === 'receive' ? (
          <input
            className={`${cellCls} text-right`}
            type="number"
            step="any"
            min="0"
            aria-label={t('inventory.voucher.unitCost')}
            {...register(`lines.${index}.unitCost`, { valueAsNumber: true })}
          />
        ) : type === 'count' ? (
          <div className="self-center text-right font-mono text-xs text-slate-500 dark:text-slate-400">
            {line?.productId && onHand !== null ? onHand : '—'}
          </div>
        ) : (
          <div aria-hidden="true" />
        )}
      </div>

      <div className="mt-2 flex items-center justify-end lg:mt-0">
        <button
          type="button"
          disabled={!canRemove}
          onClick={() => onRemove(index)}
          aria-label={t('common.delete')}
          className="rounded-md p-1.5 text-danger-600 transition-colors hover:bg-danger-50 disabled:opacity-40 dark:text-danger-300 dark:hover:bg-danger-900/40"
        >
          <Trash2 size={14} />
        </button>
      </div>

      {firstError && (
        <div className="mt-1 lg:col-span-3">
          <span className="block text-[10px] text-danger-500">
            {t(firstError, { defaultValue: firstError })}
          </span>
        </div>
      )}
    </div>
  );
};
