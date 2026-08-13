import { Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';

export interface DraftOrderLine {
  productId: string;
  productSku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  currency: string;
  lineNotes: string;
  minOrderQuantity?: number | null;
}

interface OrderLineEditorProps {
  line: DraftOrderLine;
  index: number;
  pricedUnitPrice?: number | null;
  onChange: (next: DraftOrderLine) => void;
  onRemove: () => void;
}

export const OrderLineEditor = ({
  line,
  index,
  pricedUnitPrice,
  onChange,
  onRemove,
}: OrderLineEditorProps) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  // WHY the server price wins when it differs: the catalogue quotes a single unit, the order is
  // booked at the tier for the quantity ordered, and the dealer must see the one they will pay.
  const effectiveUnitPrice =
    pricedUnitPrice !== null && pricedUnitPrice !== undefined ? pricedUnitPrice : line.unitPrice;
  const catalogPriceSuperseded =
    pricedUnitPrice !== null &&
    pricedUnitPrice !== undefined &&
    Math.abs(pricedUnitPrice - line.unitPrice) > 0.0001;
  const lineTotal = (line.quantity || 0) * (effectiveUnitPrice || 0);

  return (
    <tr className="border-b border-slate-100 last:border-b-0 dark:border-slate-800">
      <td className="px-3 py-3 text-center text-xs text-slate-500">{index + 1}</td>
      <td className="px-3 py-3">
        <p className="font-medium text-slate-900 dark:text-slate-100">{line.productName}</p>
        <p className="text-xs text-slate-500">{line.productSku}</p>
      </td>
      <td className="px-3 py-3">
        <div className="flex flex-col items-end gap-1">
          <Input
            type="number"
            inputMode="decimal"
            step="0.01"
            min="0"
            value={line.quantity}
            onChange={(e) =>
              onChange({ ...line, quantity: Math.max(0, Number(e.target.value) || 0) })
            }
            className="h-9 max-w-[7rem] text-right"
          />
          {line.minOrderQuantity && line.minOrderQuantity > 0 ? (
            <span
              className={`text-[11px] ${
                line.quantity < line.minOrderQuantity
                  ? 'text-rose-600 dark:text-rose-400'
                  : 'text-slate-500 dark:text-slate-400'
              }`}
            >
              {t('b2b.newOrder.minQtyHint', { count: line.minOrderQuantity })}
            </span>
          ) : null}
        </div>
      </td>
      <td className="px-3 py-3">
        <div className="flex flex-col items-end gap-1">
          <Input
            type="number"
            inputMode="decimal"
            step="0.01"
            min="0"
            readOnly
            title={t('b2b.newOrder.priceLockedTooltip')}
            value={effectiveUnitPrice}
            className="h-9 max-w-[8rem] cursor-not-allowed bg-slate-50 text-right dark:bg-slate-800"
          />
          {catalogPriceSuperseded ? (
            <span className="text-[11px] text-emerald-700 dark:text-emerald-400">
              {t('b2b.newOrder.tierPriceApplied', {
                catalog: formatCurrency(line.unitPrice, locale, line.currency || 'TRY'),
              })}
            </span>
          ) : null}
        </div>
      </td>
      <td className="px-3 py-3 text-right text-sm font-semibold text-slate-900 dark:text-slate-100">
        {formatCurrency(lineTotal, locale, line.currency || 'TRY')}
      </td>
      <td className="px-3 py-3 text-right">
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={onRemove}
          aria-label={t('b2b.common.remove')}
        >
          <Trash2 size={14} className="text-rose-500" />
        </Button>
      </td>
    </tr>
  );
};
