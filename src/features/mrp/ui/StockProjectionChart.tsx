import { useTranslation } from 'react-i18next';
import type { StockProjection } from '../model/mrp.types';
import { formatNumber } from '@/shared/lib/format';

interface Props {
  projection: StockProjection;
}

export const StockProjectionChart = ({ projection }: Props) => {
  const { t, i18n } = useTranslation();
  const points = projection.points;
  if (points.length === 0) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-4 text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-900">
        {t('Mrp.Projection.NoData')}
      </div>
    );
  }

  const values = points.map((p) => p.projectedQuantity);
  const max = Math.max(...values, projection.reorderPoint, 1);
  const min = Math.min(...values, 0);
  const range = Math.max(max - min, 1);

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-900">
      <header className="mb-3 flex items-center justify-between">
        <div>
          <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-200">
            {t('Mrp.Projection.Title')}
          </h3>
          <p className="text-xs text-slate-500 dark:text-slate-400">
            {t('Mrp.Projection.Subtitle', { days: projection.daysAhead })}
          </p>
        </div>
        <div className="text-right text-xs">
          <p className="text-slate-500 dark:text-slate-400">
            {t('Mrp.Projection.ReorderPoint')}:{' '}
            <span className="font-semibold text-slate-700 dark:text-slate-200">
              {formatNumber(projection.reorderPoint, i18n.language)}
            </span>
          </p>
          {projection.shouldReorder && (
            <p className="font-semibold text-amber-600 dark:text-amber-400">
              {t('Mrp.Projection.ShouldReorder')}
            </p>
          )}
        </div>
      </header>

      <div className="flex h-32 items-end gap-1">
        {points.map((p, idx) => {
          const heightPct = Math.max(2, ((p.projectedQuantity - min) / range) * 100);
          const belowRop = p.projectedQuantity < projection.reorderPoint;
          return (
            <div
              key={idx}
              className="flex-1"
              title={`${p.date.slice(0, 10)}: ${formatNumber(p.projectedQuantity, i18n.language)}`}
            >
              <div
                className={`w-full rounded-t ${belowRop ? 'bg-rose-500/60' : 'bg-indigo-500/60'}`}
                style={{ height: `${heightPct}%` }}
              />
            </div>
          );
        })}
      </div>

      <footer className="mt-3 grid grid-cols-2 gap-2 text-xs sm:grid-cols-4">
        <SummaryItem
          label={t('Mrp.Projection.OnHand')}
          value={projection.currentOnHand}
          locale={i18n.language}
        />
        <SummaryItem
          label={t('Mrp.Projection.Reserved')}
          value={projection.currentReserved}
          locale={i18n.language}
        />
        <SummaryItem
          label={t('Mrp.Projection.OnOrder')}
          value={projection.totalOnOrder}
          locale={i18n.language}
        />
        <SummaryItem
          label={t('Mrp.Projection.Suggested')}
          value={projection.suggestedOrderQuantity}
          locale={i18n.language}
        />
      </footer>
    </div>
  );
};

interface SummaryItemProps {
  label: string;
  value: number;
  locale: string;
}

const SummaryItem = ({ label, value, locale }: SummaryItemProps) => (
  <div className="rounded border border-slate-100 bg-slate-50 p-2 dark:border-slate-800 dark:bg-slate-800/50">
    <p className="text-slate-500 dark:text-slate-400">{label}</p>
    <p className="font-semibold text-slate-700 dark:text-slate-100">
      {formatNumber(value, locale)}
    </p>
  </div>
);
