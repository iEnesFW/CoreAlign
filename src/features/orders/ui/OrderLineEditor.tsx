import { useState } from 'react';
import type { FieldErrors, UseFormRegister } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Trash2 } from 'lucide-react';
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

const truncateName = (name: string, max = 20): string =>
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
  const [noteOpen, setNoteOpen] = useState(false);
  const { t } = useTranslation();

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

  const calc = computeLine(
    glassArea !== null ? { ...line, quantity: glassArea } : line,
    selectedWithholding,
  );

  const handleLastFieldKeyDown = (e: React.KeyboardEvent<HTMLInputElement | HTMLSelectElement>) => {
    if (disabled || !isLast) return;
    if (e.key === 'Enter' || (e.key === 'Tab' && !e.shiftKey)) {
      e.preventDefault();
      onAddLine();
    }
  };

  return (
    <div
      className={`p-4 transition-colors group ${noteOpen ? 'bg-slate-50 dark:bg-[#1e2332]' : 'bg-white dark:bg-[#1b202e] hover:bg-slate-50 dark:hover:bg-[#1f2536] border-b border-slate-100 dark:border-[#2a3143]/50 last:border-0'}`}
    >
      <div className="grid min-w-0 grid-cols-1 gap-3 lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)_3.75rem_minmax(5.5rem,0.9fr)] lg:items-center">
        {/* Product Column */}
        <div className="flex min-w-0 items-center gap-3">
          <div className="min-w-0 flex-1">
            <ProductPicker
              ref={(el) => setProductRef(el)}
              products={products}
              value={line?.productId ?? ''}
              disabled={disabled}
              invalid={!!errors?.productId}
              onSelect={(pid) => onProductSelect(index, pid)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') {
                  e.preventDefault();
                  onAddLine();
                }
              }}
            />
            {selectedProduct && (
              <div className="mt-1 flex items-center gap-1.5 text-[10px] font-medium text-slate-400">
                <span className="truncate">{truncateName(selectedProduct.name, 35)}</span>
                <span className="text-slate-500">|</span>
                <span className="font-semibold text-slate-300">
                  {t('orders.lines.stock')}:{' '}
                  {formatNumber(selectedProduct.stockQuantity, locale, 0)}{' '}
                  {line?.uomCode ?? selectedProduct.unit}
                </span>
              </div>
            )}
          </div>
        </div>

        {/* Quantities & Prices */}
        <div className="grid min-w-0 grid-cols-2 gap-2 sm:grid-cols-4 lg:grid-cols-[minmax(0,0.7fr)_minmax(0,1.2fr)_minmax(0,0.6fr)_minmax(0,0.75fr)]">
          <div className="relative min-w-0">
            <label className="text-[10px] text-slate-500 uppercase font-semibold mb-1 block lg:hidden">
              {t('orders.lines.quantity')}
            </label>
            <input
              type="number"
              step="0.01"
              min="0"
              disabled={disabled}
              className="w-full text-left lg:text-right bg-white dark:bg-[#0f111a] border border-slate-200 dark:border-[#2a3143] rounded-md px-2 py-1.5 text-sm text-slate-900 dark:text-slate-200 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all disabled:opacity-50 appearance-none"
              {...register(`lines.${index}.quantity`, { valueAsNumber: true })}
            />
            {areaMode && (
              <div className="absolute right-2 top-8 lg:top-1.5 flex items-center justify-end">
                <span
                  className="text-[10px] font-bold text-indigo-400 opacity-80"
                  title={t('orders.lines.areaUnit')}
                >
                  m²
                </span>
              </div>
            )}
          </div>

          <div className="min-w-0">
            <label className="text-[10px] text-slate-500 uppercase font-semibold mb-1 block lg:hidden">
              {t('orders.lines.unitPrice')}
            </label>
            <input
              type="number"
              step="0.0001"
              disabled={disabled}
              className="w-full text-left lg:text-right bg-white dark:bg-[#0f111a] border border-slate-200 dark:border-[#2a3143] rounded-md px-2 py-1.5 text-sm text-slate-900 dark:text-slate-200 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all disabled:opacity-50 appearance-none"
              {...register(`lines.${index}.unitPrice`, { valueAsNumber: true })}
            />
          </div>

          <div className="min-w-0">
            <label className="text-[10px] text-slate-500 uppercase font-semibold mb-1 block lg:hidden">
              {t('orders.lines.discountPercent')}
            </label>
            <input
              type="number"
              step="0.01"
              min="0"
              max="100"
              disabled={disabled}
              className="w-full text-left lg:text-right bg-white dark:bg-[#0f111a] border border-slate-200 dark:border-[#2a3143] rounded-md px-2 py-1.5 text-sm text-slate-900 dark:text-slate-200 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all disabled:opacity-50 appearance-none"
              {...register(`lines.${index}.lineDiscountPercent`)}
            />
          </div>

          <div className="min-w-0">
            <label className="text-[10px] text-slate-500 uppercase font-semibold mb-1 block lg:hidden">
              {t('orders.lines.taxRate')}
            </label>
            <select
              disabled={disabled}
              className="w-full bg-white dark:bg-[#0f111a] border border-slate-200 dark:border-[#2a3143] rounded-md px-2 py-1.5 text-sm text-slate-900 dark:text-slate-200 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all appearance-none cursor-pointer disabled:opacity-50 lg:text-right"
              {...register(`lines.${index}.taxRateId`)}
              onChange={(e) => onTaxRateSelect(index, e.target.value)}
              onKeyDown={handleLastFieldKeyDown}
            >
              <option value="">--</option>
              {taxRates.map((tr) => (
                <option key={tr.id} value={tr.id}>
                  {tr.ratePercent}%
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="mt-1 flex items-center justify-between gap-3 lg:contents">
          {/* Actions */}
          <div className="flex shrink-0 items-center gap-1 lg:justify-center">
            <button
              type="button"
              onClick={() => setNoteOpen(!noteOpen)}
              className={`transition-colors p-1.5 rounded-md ${noteOpen ? 'bg-indigo-500/10 text-indigo-400' : 'text-slate-500 hover:text-indigo-400 hover:bg-indigo-500/10'}`}
              title={t('orders.lines.advancedOptions')}
            >
              <svg
                xmlns="http://www.w3.org/2000/svg"
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z"></path>
                <circle cx="12" cy="12" r="3"></circle>
              </svg>
            </button>
            <button
              type="button"
              onClick={() => onRemove(index)}
              disabled={!canRemove || disabled}
              className="text-slate-500 hover:text-red-400 hover:bg-red-500/10 disabled:opacity-50 transition-colors p-1.5 rounded-md"
              title={t('common.delete')}
            >
              <Trash2 size={16} />
            </button>
          </div>

          <div className="min-w-0 text-right">
            <output
              className="whitespace-nowrap text-sm font-bold tabular-nums text-slate-900 dark:text-slate-100"
              title={t('orders.lines.lineTotal')}
              aria-label={t('orders.lines.lineTotal')}
            >
              {formatCurrency(calc.total, locale, currency, decimals)}
            </output>
          </div>
        </div>
      </div>

      {/* Advanced Options */}
      {noteOpen && (
        <div className="overflow-hidden">
          <div className="mt-4 lg:ml-7 grid grid-cols-1 sm:grid-cols-3 gap-4 p-4 bg-slate-50 dark:bg-[#141824] rounded-lg border border-slate-200 dark:border-[#2a3143] shadow-inner">
            <div className="flex flex-col gap-1.5">
              <label className="text-[10px] uppercase text-slate-500 font-semibold tracking-wider">
                {t('orders.lines.warehouse')}
              </label>
              <select
                disabled={disabled}
                className="w-full bg-white dark:bg-[#0f111a] border border-slate-200 dark:border-[#2a3143] rounded-md px-3 py-1.5 text-sm text-slate-900 dark:text-slate-200 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all appearance-none cursor-pointer disabled:opacity-50 "
                {...register(`lines.${index}.warehouseId`)}
              >
                <option value="">--</option>
                {warehouses.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex flex-col gap-1.5">
              <label className="text-[10px] uppercase text-slate-500 font-semibold tracking-wider">
                {t('orders.lines.withholdingCode')}
              </label>
              <select
                disabled={disabled}
                className="w-full bg-white dark:bg-[#0f111a] border border-slate-200 dark:border-[#2a3143] rounded-md px-3 py-1.5 text-sm text-slate-900 dark:text-slate-200 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all appearance-none cursor-pointer disabled:opacity-50 "
                {...register(`lines.${index}.withholdingTaxCodeId`)}
              >
                <option value="">{t('orders.lines.none')}</option>
                {withholdingCodes.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.code} - {w.numerator}/{w.denominator}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex flex-col gap-1.5">
              <label className="text-[10px] uppercase text-slate-500 font-semibold tracking-wider">
                {t('orders.lines.lineNotes')}
              </label>
              <input
                type="text"
                placeholder={t('orders.lines.lineNotesPlaceholder')}
                disabled={disabled}
                className="w-full text-left  bg-white dark:bg-[#0f111a] border border-slate-200 dark:border-[#2a3143] rounded-md px-3 py-1.5 text-sm text-slate-900 dark:text-slate-200 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all disabled:opacity-50 appearance-none"
                {...register(`lines.${index}.lineNotes`)}
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
