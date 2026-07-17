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
      <div className="rounded-3xl border-2 border-dashed border-slate-300 bg-white/50 backdrop-blur-md p-12 text-center text-sm font-medium text-slate-500 shadow-sm dark:border-slate-700 dark:bg-slate-900/50">
        {t('Mrp.Workbench.Grid.Empty')}
      </div>
    );
  }

  return (
    <div className="overflow-x-auto rounded-3xl border border-white/40 bg-white/70 shadow-sm backdrop-blur-xl dark:border-slate-700/50 dark:bg-slate-800/70">
      <table className="min-w-full border-collapse text-xs">
        {items.map((item) => {
          const buckets = item.buckets ?? [];
          const isSelected = selectedProductId === item.productId;
          return (
            <tbody
              key={item.productId}
              className="border-b-[4px] border-white dark:border-slate-900 transition-colors"
            >
              <tr
                className={`group cursor-pointer transition-all duration-200 ${
                  isSelected
                    ? 'bg-indigo-50/80 dark:bg-indigo-900/20 shadow-inner'
                    : 'bg-slate-100/50 hover:bg-slate-200/50 dark:bg-slate-800/40 dark:hover:bg-slate-700/50'
                }`}
                onClick={() => onSelectItem(item.productId)}
              >
                <th
                  scope="col"
                  className="sticky left-0 z-10 min-w-[220px] bg-inherit px-5 py-3 text-left shadow-[2px_0_4px_rgba(0,0,0,0.02)]"
                >
                  <div className="flex flex-col text-left">
                    <span className="flex flex-wrap items-center gap-2 font-extrabold text-slate-800 dark:text-slate-100 text-sm mb-0.5">
                      {item.sku}
                      <ProcurementBadge type={item.procurementType} />
                      <AbcBadge abcClass={item.abcClass} />
                    </span>
                    <span className="text-[11px] font-medium text-slate-500 dark:text-slate-400 line-clamp-1">
                      {item.name}
                    </span>
                  </div>
                </th>
                {buckets.map((b, idx) => (
                  <th
                    key={idx}
                    scope="col"
                    className="px-3 py-3 text-right text-[11px] font-bold tracking-wider text-slate-500 dark:text-slate-400"
                  >
                    <div className="bg-slate-200/50 dark:bg-slate-700/50 px-2 py-1 rounded-md inline-block">
                      {bucketLabel(b.startUtc)}
                    </div>
                  </th>
                ))}
              </tr>
              {ROW_KEYS.map((row, rowIndex) => (
                <tr
                  key={row}
                  className={`${rowIndex % 2 === 0 ? 'bg-transparent' : 'bg-white/40 dark:bg-slate-800/20'}`}
                >
                  <td className="sticky left-0 z-10 bg-inherit px-5 py-2.5 font-medium text-slate-600 dark:text-slate-300 shadow-[2px_0_4px_rgba(0,0,0,0.01)] border-r border-slate-100/50 dark:border-slate-800/50">
                    {t(`Mrp.Workbench.Grid.Row.${row}`)}
                  </td>
                  {buckets.map((b, idx) => {
                    const value = bucketValue(b, row);
                    const isShort =
                      row === 'ProjectedOnHand' && value < Math.max(item.safetyStock, 0);
                    const isNegative = row === 'ProjectedOnHand' && value < 0;

                    let tone = 'text-slate-700 dark:text-slate-200';
                    let bg = 'bg-transparent';

                    if (isNegative) {
                      tone = 'text-red-700 font-bold dark:text-red-300';
                      bg =
                        'bg-red-50/80 dark:bg-red-900/20 ring-1 ring-inset ring-red-100 dark:ring-red-900/30 rounded-md';
                    } else if (isShort) {
                      tone = 'text-amber-700 font-bold dark:text-amber-400';
                      bg =
                        'bg-amber-50/80 dark:bg-amber-900/20 ring-1 ring-inset ring-amber-100 dark:ring-amber-900/30 rounded-md';
                    } else if (value === 0) {
                      tone = 'text-slate-300 dark:text-slate-600';
                    } else if (row === 'PlannedReleases' && value > 0) {
                      tone = 'text-indigo-700 font-bold dark:text-indigo-400';
                      bg = 'bg-indigo-50/80 dark:bg-indigo-900/20 rounded-md';
                    }

                    return (
                      <td
                        key={idx}
                        data-testid={row === 'ProjectedOnHand' ? 'proj-on-hand-cell' : undefined}
                        data-below-safety={isShort ? 'true' : undefined}
                        className={`px-3 py-2 text-right tabular-nums`}
                      >
                        <div
                          className={`inline-block px-1.5 py-0.5 ${bg} ${tone} transition-colors`}
                        >
                          {formatNumber(value, locale)}
                        </div>
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
