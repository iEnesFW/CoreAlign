import { useTranslation } from 'react-i18next';
import { formatNumber } from '@/shared/lib/format';
import type { MrpBucket, MrpItemPlan } from '../model/mrp-planning.types';
import { ProcurementBadge } from './ProcurementBadge';
import { AbcBadge } from './AbcBadge';

interface Props {
  items: MrpItemPlan[];
  locale: string;
  onSelectItem: (productId: string) => void;
  selectedProductId?: string | null;
}

const ROW_KEYS = [
  'Gross',
  'ScheduledReceipts',
  'ProjectedOnHand',
  'Net',
  'PlannedReleases',
] as const;

type RowKey = (typeof ROW_KEYS)[number];

const bucketValue = (bucket: MrpBucket, row: RowKey): number => {
  switch (row) {
    case 'Gross':
      return bucket.grossRequirements;
    case 'ScheduledReceipts':
      return bucket.scheduledReceipts;
    case 'ProjectedOnHand':
      return bucket.projectedOnHand;
    case 'Net':
      return bucket.netRequirements;
    case 'PlannedReleases':
      return bucket.plannedReleases;
    default:
      return 0;
  }
};

const bucketLabel = (startUtc: string): string => startUtc.slice(5, 10);

export const MrpPlanningGrid = ({ items, locale, onSelectItem, selectedProductId }: Props) => {
  const { t } = useTranslation();

  if (items.length === 0) {
    return (
      <div className="rounded-lg border border-dashed border-slate-300 bg-white p-6 text-center text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-900">
        {t('Mrp.Workbench.Grid.Empty')}
      </div>
    );
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-900">
      <table className="min-w-full border-collapse text-xs">
        {items.map((item) => {
          const buckets = item.buckets ?? [];
          const isSelected = selectedProductId === item.productId;
          return (
            <tbody key={item.productId} className="border-b border-slate-200 dark:border-slate-700">
              <tr
                className={
                  isSelected
                    ? 'bg-primary-50 dark:bg-primary-500/10'
                    : 'bg-slate-50 dark:bg-slate-800/60'
                }
              >
                <th
                  scope="col"
                  className="sticky left-0 z-10 min-w-[180px] bg-inherit px-3 py-2 text-left"
                >
                  <button
                    type="button"
                    onClick={() => onSelectItem(item.productId)}
                    className="flex flex-col text-left"
                  >
                    <span className="flex items-center gap-1.5 font-semibold text-slate-800 dark:text-slate-100">
                      {item.sku}
                      <ProcurementBadge type={item.procurementType} />
                      <AbcBadge abcClass={item.abcClass} />
                    </span>
                    <span className="text-[11px] text-slate-500 dark:text-slate-400">
                      {item.name}
                    </span>
                  </button>
                </th>
                {buckets.map((b, idx) => (
                  <th
                    key={idx}
                    scope="col"
                    className="px-2 py-2 text-right font-medium text-slate-500 dark:text-slate-400"
                  >
                    {bucketLabel(b.startUtc)}
                  </th>
                ))}
              </tr>
              {ROW_KEYS.map((row) => (
                <tr key={row} className="border-t border-slate-100 dark:border-slate-800">
                  <td className="sticky left-0 z-10 bg-white px-3 py-1.5 text-slate-600 dark:bg-slate-900 dark:text-slate-300">
                    {t(`Mrp.Workbench.Grid.Row.${row}`)}
                  </td>
                  {buckets.map((b, idx) => {
                    const value = bucketValue(b, row);
                    const isShort =
                      row === 'ProjectedOnHand' && value < Math.max(item.safetyStock, 0);
                    const isNegative = row === 'ProjectedOnHand' && value < 0;
                    const tone = isNegative
                      ? 'text-danger-600 font-semibold dark:text-danger-400'
                      : isShort
                        ? 'text-warning-600 font-semibold dark:text-warning-400'
                        : value === 0
                          ? 'text-slate-300 dark:text-slate-600'
                          : 'text-slate-700 dark:text-slate-200';
                    return (
                      <td
                        key={idx}
                        data-testid={row === 'ProjectedOnHand' ? 'proj-on-hand-cell' : undefined}
                        data-below-safety={isShort ? 'true' : undefined}
                        className={`px-2 py-1.5 text-right tabular-nums ${tone}`}
                      >
                        {formatNumber(value, locale)}
                      </td>
                    );
                  })}
                </tr>
              ))}
            </tbody>
          );
        })}
      </table>
    </div>
  );
};
