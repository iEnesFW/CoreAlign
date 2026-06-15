import { useTranslation } from 'react-i18next';
import { formatNumber } from '@/shared/lib/format';
import type { MrpReorderCandidate } from '../model/mrp.types';

interface Props {
  candidates: MrpReorderCandidate[];
  onSelect?: (productId: string) => void;
}

export const RequisitionSuggestionsTable = ({ candidates, onSelect }: Props) => {
  const { t, i18n } = useTranslation();

  if (candidates.length === 0) {
    return (
      <div className="rounded-lg border border-dashed border-slate-300 bg-white p-6 text-center text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-900">
        {t('Mrp.Suggestions.Empty')}
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-900">
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-800">
          <thead className="bg-slate-50 dark:bg-slate-800/50">
            <tr>
              <Th>{t('Mrp.Suggestions.Sku')}</Th>
              <Th>{t('Mrp.Suggestions.Product')}</Th>
              <Th align="right">{t('Mrp.Suggestions.OnHand')}</Th>
              <Th align="right">{t('Mrp.Suggestions.OnOrder')}</Th>
              <Th align="right">{t('Mrp.Suggestions.Available')}</Th>
              <Th align="right">{t('Mrp.Suggestions.ReorderPoint')}</Th>
              <Th align="right">{t('Mrp.Suggestions.Suggested')}</Th>
              <Th align="right">{t('Mrp.Suggestions.DaysUntilStockOut')}</Th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {candidates.map((c) => (
              <tr
                key={c.productId}
                onClick={() => onSelect?.(c.productId)}
                className="cursor-pointer hover:bg-slate-50 dark:hover:bg-slate-800/50"
              >
                <Td className="font-mono text-xs">{c.productSku}</Td>
                <Td>{c.productName}</Td>
                <Td align="right">{formatNumber(c.onHand, i18n.language)}</Td>
                <Td align="right">{formatNumber(c.onOrder, i18n.language)}</Td>
                <Td align="right">{formatNumber(c.projectedAvailable, i18n.language)}</Td>
                <Td align="right">{formatNumber(c.reorderPoint, i18n.language)}</Td>
                <Td align="right" className="font-semibold text-indigo-600 dark:text-indigo-300">
                  {formatNumber(c.suggestedOrderQuantity, i18n.language)}
                </Td>
                <Td align="right">
                  <span
                    className={
                      c.daysUntilStockOut <= 7
                        ? 'rounded bg-rose-100 px-2 py-0.5 text-xs font-semibold text-rose-700 dark:bg-rose-500/20 dark:text-rose-300'
                        : c.daysUntilStockOut <= 30
                          ? 'rounded bg-amber-100 px-2 py-0.5 text-xs font-semibold text-amber-700 dark:bg-amber-500/20 dark:text-amber-300'
                          : 'text-slate-600 dark:text-slate-300'
                    }
                  >
                    {c.daysUntilStockOut >= 9999 ? '—' : c.daysUntilStockOut}
                  </span>
                </Td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

const Th = ({
  children,
  align = 'left',
}: {
  children: React.ReactNode;
  align?: 'left' | 'right';
}) => (
  <th
    className={`px-3 py-2 text-${align} text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400`}
  >
    {children}
  </th>
);

const Td = ({
  children,
  align = 'left',
  className = '',
}: {
  children: React.ReactNode;
  align?: 'left' | 'right';
  className?: string;
}) => (
  <td className={`px-3 py-2 text-${align} text-sm text-slate-700 dark:text-slate-200 ${className}`}>
    {children}
  </td>
);
