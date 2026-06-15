import { Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';

export interface DraftDirectOrderLine {
  productId: string;
  productSku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  currency: string;
  unit: string;
  lineNotes: string;
  minOrderQuantity?: number | null;
}

interface LineEditorProps {
  line: DraftDirectOrderLine;
  index: number;
  onChange: (next: DraftDirectOrderLine) => void;
  onRemove: () => void;
}

export const LineEditor = ({ line, index, onChange, onRemove }: LineEditorProps) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const lineTotal = (line.quantity || 0) * (line.unitPrice || 0);

  return (
    <tr className="border-b border-slate-100 last:border-b-0 dark:border-slate-800">
      <td className="px-3 py-3 text-center text-xs text-slate-500">{index + 1}</td>
      <td className="px-3 py-3">
        <p className="font-medium text-slate-900 dark:text-slate-100">{line.productName}</p>
        <p className="text-xs text-slate-500">{line.productSku}</p>
      </td>
      <td className="px-3 py-3">
        <div className="flex flex-col items-end gap-1">
          <div className="flex items-center justify-end gap-1">
            <Input
              type="number"
              inputMode="decimal"
              step="0.01"
              min="0"
              value={line.quantity}
              onChange={(event) =>
                onChange({ ...line, quantity: Math.max(0, Number(event.target.value) || 0) })
              }
              className="h-9 max-w-[7rem] text-right"
            />
            <span className="min-w-[2.5rem] text-xs text-slate-500">{line.unit}</span>
          </div>
          {line.minOrderQuantity && line.minOrderQuantity > 0 ? (
            <span
              className={`text-[11px] ${
                line.quantity < line.minOrderQuantity
                  ? 'text-rose-600 dark:text-rose-400'
                  : 'text-slate-500 dark:text-slate-400'
              }`}
            >
              {t('orders.create.minQtyHint', { count: line.minOrderQuantity })}
            </span>
          ) : null}
        </div>
      </td>
      <td className="px-3 py-3 text-right text-sm text-slate-700 dark:text-slate-200">
        {formatCurrency(line.unitPrice, locale, line.currency || 'TRY')}
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
          aria-label={t('common.remove', 'Remove')}
        >
          <Trash2 size={14} className="text-rose-500" />
        </Button>
      </td>
    </tr>
  );
};
