import type { FieldErrors, UseFormRegister } from 'react-hook-form';
import { Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { formatCurrency } from '@/shared/lib/format';
import type { WithholdingTaxCode } from '@/shared/master-data/model/masterData.types';
import type {
  StandaloneInvoiceFormValues,
  StandaloneInvoiceLineFormValues,
} from '@/features/invoices/model/standaloneInvoiceSchema';

const cellCls =
  'min-w-0 w-full rounded-md border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-[#2a3143] dark:bg-[#0f111a] dark:text-slate-200';

const truncate = (value: string, max = 40): string =>
  value.length > max ? `${value.slice(0, max)}…` : value;

interface Props {
  index: number;
  register: UseFormRegister<StandaloneInvoiceFormValues>;
  errors?: FieldErrors<StandaloneInvoiceLineFormValues>;
  line?: StandaloneInvoiceLineFormValues;
  withholdingCodes: readonly WithholdingTaxCode[];
  canRemove: boolean;
  locale: string;
  currency: string;
  decimals: number;
  onRemove: (index: number) => void;
}

export const StandaloneInvoiceLineEditor = ({
  index,
  register,
  errors,
  line,
  withholdingCodes,
  canRemove,
  locale,
  currency,
  decimals,
  onRemove,
}: Props) => {
  const { t } = useTranslation();

  const gross = (Number(line?.quantity) || 0) * (Number(line?.unitPrice) || 0);
  const net = gross - gross * ((Number(line?.lineDiscountPercent) || 0) / 100);
  const tax = net * ((Number(line?.taxRatePercent) || 0) / 100);
  const code = line?.withholdingTaxCodeId
    ? withholdingCodes.find((c) => c.id === line.withholdingTaxCodeId)
    : undefined;
  const withholding = code && code.denominator > 0 ? tax * (code.numerator / code.denominator) : 0;
  const lineTotal = net + tax - withholding;

  const firstError =
    errors?.productSku?.message ??
    errors?.productName?.message ??
    errors?.quantity?.message ??
    errors?.unitPrice?.message;

  return (
    <div className="min-w-0 px-4 py-3 lg:grid lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)_3.75rem_minmax(5.5rem,0.9fr)] lg:items-center lg:gap-3">
      <div className="grid min-w-0 grid-cols-2 gap-2 lg:grid-cols-1">
        <input
          className={cellCls}
          placeholder={t('invoices.standalone.lineSku')}
          aria-label={t('invoices.standalone.lineSku')}
          {...register(`lines.${index}.productSku`)}
        />
        <input
          className={cellCls}
          placeholder={t('invoices.standalone.lineName')}
          aria-label={t('invoices.standalone.lineName')}
          {...register(`lines.${index}.productName`)}
        />
      </div>

      <div className="mt-2 grid min-w-0 grid-cols-[minmax(0,0.7fr)_minmax(0,1.2fr)_minmax(0,0.6fr)_minmax(0,0.75fr)] gap-2 lg:mt-0">
        <input
          className={`${cellCls} text-right`}
          type="number"
          step="0.01"
          min="0"
          aria-label={t('invoices.standalone.lineQuantity')}
          {...register(`lines.${index}.quantity`, { valueAsNumber: true })}
        />
        <input
          className={`${cellCls} text-right`}
          type="number"
          step="0.01"
          min="0"
          aria-label={t('invoices.standalone.lineUnitPrice')}
          {...register(`lines.${index}.unitPrice`, { valueAsNumber: true })}
        />
        <input
          className={`${cellCls} text-right`}
          type="number"
          step="0.01"
          min="0"
          max="100"
          placeholder="0"
          aria-label={t('invoices.standalone.lineDiscountPercent')}
          {...register(`lines.${index}.lineDiscountPercent`)}
        />
        <input
          className={`${cellCls} text-right`}
          type="number"
          step="0.1"
          min="0"
          max="100"
          aria-label={t('invoices.standalone.lineTaxRate')}
          {...register(`lines.${index}.taxRatePercent`)}
        />
      </div>

      <div className="mt-2 flex items-center justify-end lg:mt-0">
        <button
          type="button"
          disabled={!canRemove}
          onClick={() => onRemove(index)}
          aria-label={t('invoices.standalone.removeLine')}
          className="rounded-md p-1.5 text-danger-600 transition-colors hover:bg-danger-50 disabled:opacity-40 dark:text-danger-300 dark:hover:bg-danger-900/40"
        >
          <Trash2 size={14} />
        </button>
      </div>

      <div className="mt-2 text-right text-sm font-medium tabular-nums text-slate-900 lg:mt-0 dark:text-slate-200">
        {formatCurrency(lineTotal, locale, currency, decimals)}
      </div>

      <div className="mt-2 lg:col-span-4">
        <select
          className={cellCls}
          aria-label={t('invoices.standalone.withholdingCode')}
          {...register(`lines.${index}.withholdingTaxCodeId`)}
        >
          <option value="">{t('invoices.standalone.withholdingNone')}</option>
          {withholdingCodes.map((c) => (
            <option key={c.id} value={c.id}>
              {c.code} — {truncate(c.name)} ({c.numerator}/{c.denominator})
            </option>
          ))}
        </select>
        {firstError && (
          <span className="mt-1 block text-[10px] text-danger-500">
            {t(firstError, { defaultValue: firstError })}
          </span>
        )}
      </div>
    </div>
  );
};
