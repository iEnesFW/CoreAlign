import type { FieldErrors, UseFormRegister } from 'react-hook-form';
import { Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { formatCurrency } from '@/shared/lib/format';
import { ProductPicker } from '@/shared/ui/ProductPicker';
import type { Product } from '@/shared/model/product.types';
import type {
  PurchaseOrderFormValues,
  PurchaseOrderLineFormValues,
} from '../model/purchaseOrderSchema';

const cellCls =
  'min-w-0 w-full rounded-md border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-[#2a3143] dark:bg-[#0f111a] dark:text-slate-200';

interface Props {
  index: number;
  register: UseFormRegister<PurchaseOrderFormValues>;
  errors?: FieldErrors<PurchaseOrderLineFormValues>;
  line?: PurchaseOrderLineFormValues;
  products: Product[];
  canRemove: boolean;
  locale: string;
  currency: string;
  onProductSelect: (index: number, productId: string) => void;
  onRemove: (index: number) => void;
}

export const PurchaseOrderLineEditor = ({
  index,
  register,
  errors,
  line,
  products,
  canRemove,
  locale,
  currency,
  onProductSelect,
  onRemove,
}: Props) => {
  const { t } = useTranslation();

  const net = (Number(line?.quantity) || 0) * (Number(line?.unitCost) || 0);
  const lineTotal = net + net * ((Number(line?.taxRatePercent) || 0) / 100);

  const firstError =
    errors?.productId?.message ?? errors?.quantity?.message ?? errors?.unitCost?.message;

  return (
    <div className="min-w-0 px-4 py-3 lg:grid lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)_3.75rem_minmax(5.5rem,0.9fr)] lg:items-center lg:gap-3">
      <div className="min-w-0">
        <ProductPicker
          products={products}
          value={line?.productId ?? ''}
          invalid={Boolean(errors?.productId)}
          onSelect={(productId) => onProductSelect(index, productId)}
        />
      </div>

      <div className="mt-2 grid min-w-0 grid-cols-[minmax(0,0.7fr)_minmax(0,1.2fr)_minmax(0,0.75fr)] gap-2 lg:mt-0">
        <input
          className={`${cellCls} text-right`}
          type="number"
          step="any"
          min="0"
          aria-label={t('po.form.qty')}
          {...register(`lines.${index}.quantity`, { valueAsNumber: true })}
        />
        <input
          className={`${cellCls} text-right`}
          type="number"
          step="any"
          min="0"
          aria-label={t('po.form.unitCost')}
          {...register(`lines.${index}.unitCost`, { valueAsNumber: true })}
        />
        <input
          className={`${cellCls} text-right`}
          type="number"
          step="any"
          min="0"
          max="100"
          aria-label={t('po.form.tax')}
          {...register(`lines.${index}.taxRatePercent`)}
        />
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

      <div className="mt-2 text-right text-sm font-medium tabular-nums text-slate-900 lg:mt-0 dark:text-slate-200">
        {formatCurrency(lineTotal, locale, currency)}
      </div>

      <div className="mt-2 lg:col-span-4">
        <input
          className={cellCls}
          placeholder={t('po.form.lineNotes')}
          aria-label={t('po.form.lineNotes')}
          {...register(`lines.${index}.lineNotes`)}
        />
        {firstError && (
          <span className="mt-1 block text-[10px] text-danger-500">
            {t(firstError, { defaultValue: firstError })}
          </span>
        )}
      </div>
    </div>
  );
};
