import type { FieldErrors, UseFormRegister } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Trash2 } from 'lucide-react';
import { Input } from '@/shared/ui/Input/Input';
import { formatCurrency, formatNumber } from '@/shared/lib/format';
import type { Product } from '@/shared/model/product.types';
import type {
  TaxRate,
  Warehouse,
  WithholdingTaxCode,
} from '@/shared/master-data/model/masterData.types';
import { glassLineArea, isAreaUnit } from '../model/orderSchema';
import type { OrderFormValues, OrderLineFormValues } from '../model/orderSchema';
import { ProductPicker } from '@/shared/ui/ProductPicker';

const selectCls =
  'w-full rounded border border-slate-200 bg-white px-2 py-1.5 text-sm text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 disabled:opacity-60 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100';

interface Props {
  index: number;
  isLast: boolean;
  register: UseFormRegister<OrderFormValues>;
  errors?: FieldErrors<OrderLineFormValues>;
  line?: Partial<OrderLineFormValues>;
  products: Product[];
  taxRates: TaxRate[];
  warehouses: Warehouse[];
  withholdingCodes: WithholdingTaxCode[];
  disabled: boolean;
  canRemove: boolean;
  locale: string;
  currency: string;
  decimals: number;
  setProductRef: (el: HTMLInputElement | null) => void;
  onProductSelect: (index: number, productId: string) => void;
  onTaxRateSelect: (index: number, taxRateId: string) => void;
  onRemove: (index: number) => void;
  onAddLine: () => void;
}

const truncateName = (name: string, max = 40): string =>
  name.length > max ? `${name.slice(0, max)}…` : name;

const computeLine = (line?: Partial<OrderLineFormValues>, withholdingCode?: WithholdingTaxCode) => {
  const qty = Number(line?.quantity) || 0;
  const price = Number(line?.unitPrice) || 0;
  const discountPct = Number(line?.lineDiscountPercent) || 0;
  const taxPct = Number(line?.taxRatePercent) || 0;
  const whtPct = Number(line?.withholdingRatePercent) || 0;
  const gross = qty * price;
  const discount = gross * (discountPct / 100);
  const net = gross - discount;
  const tax = net * (taxPct / 100);
  const withholding =
    withholdingCode && withholdingCode.denominator > 0
      ? tax * (withholdingCode.numerator / withholdingCode.denominator)
      : net * (whtPct / 100);
  return {
    qty,
    price,
    discountPct,
    taxPct,
    whtPct,
    gross,
    discount,
    net,
    total: net + tax - withholding,
  };
};

export const OrderLineEditor = ({
  index,
  isLast,
  register,
  errors,
  line,
  products,
  taxRates,
  warehouses,
  withholdingCodes,
  disabled,
  canRemove,
  locale,
  currency,
  decimals,
  setProductRef,
  onProductSelect,
  onTaxRateSelect,
  onRemove,
  onAddLine,
}: Props) => {
  const { t } = useTranslation();
  const translateError = (key?: string): string | undefined =>
    key ? t(key, { defaultValue: key }) : undefined;

  const step = (1 / Math.pow(10, decimals)).toString();
  const selectedWithholding = line?.withholdingTaxCodeId
    ? withholdingCodes.find((c) => c.id === line.withholdingTaxCodeId)
    : undefined;
  const selectedProduct = line?.productId
    ? products.find((p) => p.id === line.productId)
    : undefined;
  const unitCode = selectedProduct?.unit;
  const areaMode = isAreaUnit(unitCode);
  const glassArea = areaMode
    ? glassLineArea(unitCode, line?.widthMm, line?.heightMm, line?.pieces)
    : null;
  // For an area-unit line the quantity is DERIVED from the cut size, so the summary/pricing use the
  // computed area rather than the (hidden) plain quantity field.
  const calc = computeLine(
    glassArea !== null ? { ...line, quantity: glassArea } : line,
    selectedWithholding,
  );

  const handleLastFieldKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (disabled || !isLast) return;
    if (e.key === 'Enter' || (e.key === 'Tab' && !e.shiftKey)) {
      e.preventDefault();
      onAddLine();
    }
  };

  return (
    <div className="rounded border border-slate-200 bg-slate-50/50 p-2 dark:border-slate-800 dark:bg-slate-800/30">
      <div className="flex items-start gap-2">
        <div className="min-w-0 flex-1">
          <ProductPicker
            ref={setProductRef}
            products={products}
            value={line?.productId ?? ''}
            disabled={disabled}
            invalid={!!errors?.productId}
            onSelect={(id) => onProductSelect(index, id)}
          />
          {errors?.productId?.message && (
            <span className="mt-1 block text-xs text-danger-500">
              {translateError(errors.productId.message)}
            </span>
          )}
        </div>
        <button
          type="button"
          onClick={() => onRemove(index)}
          disabled={disabled || !canRemove}
          className="rounded p-2 text-slate-500 hover:bg-danger-50 hover:text-danger-600 disabled:opacity-30 dark:text-slate-400 dark:hover:bg-danger-500/10"
          aria-label={t('common.delete')}
        >
          <Trash2 size={14} />
        </button>
      </div>

      <div className="mt-2 grid grid-cols-12 gap-2">
        <div className="col-span-6 sm:col-span-3">
          {areaMode ? (
            <div className="flex h-full min-h-[38px] flex-col justify-center rounded border border-dashed border-emerald-300 px-2 py-1 dark:border-emerald-800">
              <span className="text-[10px] uppercase tracking-wide text-slate-400">
                {t('orders.lines.quantity')}
              </span>
              <span className="text-sm font-medium text-emerald-600 dark:text-emerald-400">
                {glassArea !== null ? `${formatNumber(glassArea, locale, 4)} ${unitCode}` : '—'}
              </span>
              <input
                type="hidden"
                {...register(`lines.${index}.quantity`, { valueAsNumber: true })}
              />
            </div>
          ) : (
            <div className="relative">
              <Input
                type="number"
                step={step}
                placeholder={t('orders.lines.quantity')}
                disabled={disabled}
                error={translateError(errors?.quantity?.message)}
                {...register(`lines.${index}.quantity`, { valueAsNumber: true })}
              />
              {line?.uomCode && (
                <span className="pointer-events-none absolute right-2 top-2 text-xs text-slate-400">
                  {line.uomCode}
                </span>
              )}
            </div>
          )}
        </div>
        <div className="col-span-6 sm:col-span-3">
          <Input
            type="number"
            step={step}
            placeholder={t('orders.lines.unitPrice')}
            disabled={disabled}
            error={translateError(errors?.unitPrice?.message)}
            {...register(`lines.${index}.unitPrice`, { valueAsNumber: true })}
          />
        </div>
        <div className="col-span-6 sm:col-span-3">
          <Input
            type="number"
            step="0.01"
            min="0"
            max="100"
            placeholder={t('orders.lines.discountPercent')}
            disabled={disabled}
            {...register(`lines.${index}.lineDiscountPercent`)}
          />
        </div>
        <div className="col-span-6 sm:col-span-3">
          <select
            disabled={disabled}
            className={selectCls}
            value={line?.taxRateId ?? ''}
            onChange={(e) => onTaxRateSelect(index, e.target.value)}
            aria-label={t('orders.lines.taxRate')}
          >
            <option value="">{t('orders.lines.taxRatePlaceholder')}</option>
            {taxRates.map((r) => (
              <option key={r.id} value={r.id}>
                {r.name} ({r.ratePercent}%)
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="mt-2 grid grid-cols-12 gap-2">
        <div className="col-span-6 sm:col-span-3">
          <select
            disabled={disabled}
            className={selectCls}
            {...register(`lines.${index}.withholdingTaxCodeId`)}
            aria-label={t('orders.lines.withholdingCode')}
          >
            <option value="">{t('orders.lines.withholdingCodePlaceholder')}</option>
            {withholdingCodes.map((c) => (
              <option key={c.id} value={c.id}>
                {c.code} — {truncateName(c.name)} ({c.numerator}/{c.denominator})
              </option>
            ))}
          </select>
        </div>
        <div className="col-span-6 sm:col-span-3">
          <select
            disabled={disabled}
            className={selectCls}
            {...register(`lines.${index}.warehouseId`)}
            aria-label={t('orders.lines.warehouse')}
          >
            <option value="">{t('orders.lines.warehousePlaceholder')}</option>
            {warehouses.map((w) => (
              <option key={w.id} value={w.id}>
                {w.name}
              </option>
            ))}
          </select>
        </div>
        <div className="col-span-12 sm:col-span-6">
          <Input
            type="text"
            placeholder={t('orders.lines.lineNotes')}
            disabled={disabled}
            onKeyDown={handleLastFieldKeyDown}
            {...register(`lines.${index}.lineNotes`)}
          />
        </div>
      </div>

      {areaMode && (
        <div className="mt-2 grid grid-cols-12 items-center gap-2">
          <div className="col-span-4 sm:col-span-3">
            <Input
              type="number"
              step="0.1"
              placeholder={t('orders.lines.widthMm')}
              disabled={disabled}
              {...register(`lines.${index}.widthMm`)}
            />
          </div>
          <div className="col-span-4 sm:col-span-3">
            <Input
              type="number"
              step="0.1"
              placeholder={t('orders.lines.heightMm')}
              disabled={disabled}
              {...register(`lines.${index}.heightMm`)}
            />
          </div>
          <div className="col-span-4 sm:col-span-2">
            <Input
              type="number"
              step="1"
              placeholder={t('orders.lines.pieces')}
              disabled={disabled}
              {...register(`lines.${index}.pieces`)}
            />
          </div>
          {glassArea !== null && (
            <div className="col-span-12 flex items-center text-xs font-medium text-emerald-600 dark:text-emerald-400 sm:col-span-4">
              = {formatNumber(glassArea, locale, 4)} {unitCode}
            </div>
          )}
        </div>
      )}

      <div className="mt-2 flex flex-wrap items-center justify-end gap-x-3 gap-y-1 text-xs text-slate-500 dark:text-slate-400">
        <span>
          {formatNumber(calc.qty, locale, decimals)} × {formatNumber(calc.price, locale, decimals)}
        </span>
        {calc.discountPct > 0 && (
          <span className="text-warning-600 dark:text-warning-400">
            {t('orders.lines.discountPercent')} {calc.discountPct}% (−
            {formatCurrency(calc.discount, locale, currency, decimals)})
          </span>
        )}
        {calc.taxPct > 0 && (
          <span className="rounded bg-slate-200 px-1.5 py-0.5 text-slate-600 dark:bg-slate-700 dark:text-slate-300">
            {t('orders.lines.taxRate')} {calc.taxPct}%
          </span>
        )}
        <span className="font-semibold text-slate-900 dark:text-slate-100">
          {t('orders.lines.lineTotal')}: {formatCurrency(calc.total, locale, currency, decimals)}
        </span>
      </div>
    </div>
  );
};
