import { useTranslation } from 'react-i18next';
import { Factory, Gauge, PackageX } from 'lucide-react';
import { formatDate, formatNumber } from '@/shared/lib/format';
import type {
  MrpCapacityBucket,
  MrpCapacityLoadResult,
  MrpWorkCenterLoad,
} from '../model/mrp-planning.types';

interface Props {
  result: MrpCapacityLoadResult | null;
  locale: string;
  isLoading?: boolean;
}

const loadRatio = (bucket: MrpCapacityBucket): number => {
  if (bucket.capacityMinutes <= 0) return bucket.loadMinutes > 0 ? 1 : 0;
  return bucket.loadMinutes / bucket.capacityMinutes;
};

const barWidth = (bucket: MrpCapacityBucket): string => {
  const ratio = loadRatio(bucket);
  const clamped = Math.max(0, Math.min(1, ratio));
  return `${Math.round(clamped * 100)}%`;
};

const emptyState = (text: string) => (
  <div
    data-testid="capacity-load-empty"
    className="rounded-lg border border-dashed border-slate-300 bg-white p-6 text-center text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-900"
  >
    {text}
  </div>
);

export const CapacityLoadView = ({ result, locale, isLoading = false }: Props) => {
  const { t } = useTranslation();

  if (isLoading) {
    return <p className="text-sm text-slate-500 dark:text-slate-400">{t('Common.Loading')}</p>;
  }

  if (!result || result.workCenters.length === 0) {
    return emptyState(t('Mrp.Workbench.Capacity.NoData'));
  }

  return (
    <div className="space-y-4" data-testid="capacity-load-view">
      <div className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-600 dark:border-slate-700 dark:bg-slate-800/60 dark:text-slate-300">
        <span>{t('Mrp.Workbench.Capacity.Summary', { count: result.workCenters.length })}</span>
        <span className="flex items-center gap-3">
          <span className="inline-flex items-center gap-1.5">
            <span className="h-2.5 w-2.5 rounded-sm bg-success-500 dark:bg-success-400" />
            {t('Mrp.Workbench.Capacity.WithinCapacity')}
          </span>
          <span className="inline-flex items-center gap-1.5">
            <span className="h-2.5 w-2.5 rounded-sm bg-danger-500 dark:bg-danger-400" />
            {t('Mrp.Workbench.Capacity.Overloaded')}
          </span>
        </span>
      </div>

      <div className="space-y-3">
        {result.workCenters.map((wc) => (
          <WorkCenterCard key={wc.workCenterId} workCenter={wc} locale={locale} />
        ))}
      </div>

      {result.unroutedProductionOrderCount > 0 && (
        <div
          data-testid="capacity-unrouted-note"
          className="flex items-center gap-2 rounded-lg border border-warning-200 bg-warning-50 px-3 py-2 text-xs text-warning-800 dark:border-warning-700 dark:bg-warning-500/10 dark:text-warning-200"
        >
          <PackageX className="h-4 w-4 shrink-0" />
          {t('Mrp.Workbench.Capacity.UnroutedNote', {
            count: result.unroutedProductionOrderCount,
          })}
        </div>
      )}
    </div>
  );
};

const WorkCenterCard = ({
  workCenter,
  locale,
}: {
  workCenter: MrpWorkCenterLoad;
  locale: string;
}) => {
  const { t } = useTranslation();
  const overloadedCount = workCenter.buckets.filter((b) => b.isOverloaded).length;

  return (
    <div
      data-testid="capacity-work-center"
      data-work-center-id={workCenter.workCenterId}
      data-overloaded={overloadedCount > 0 ? 'true' : 'false'}
      className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-900"
    >
      <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <Factory className="h-4 w-4 text-primary-500 dark:text-primary-300" />
          <span className="font-mono text-sm font-semibold text-slate-800 dark:text-slate-100">
            {workCenter.code}
          </span>
          <span className="truncate text-xs text-slate-500 dark:text-slate-400">
            {workCenter.name}
          </span>
        </div>
        <span className="inline-flex items-center gap-1.5 text-xs text-slate-500 dark:text-slate-400">
          <Gauge className="h-3.5 w-3.5" />
          {t('Mrp.Workbench.Capacity.DailyCapacity', {
            minutes: formatNumber(workCenter.dailyCapacityMinutes, locale, 0),
          })}
          {overloadedCount > 0 && (
            <span className="ml-1 rounded bg-danger-100 px-1.5 py-0.5 font-semibold text-danger-700 dark:bg-danger-500/20 dark:text-danger-300">
              {t('Mrp.Workbench.Capacity.OverloadedBuckets', { count: overloadedCount })}
            </span>
          )}
        </span>
      </div>

      <div className="overflow-x-auto">
        <table className="min-w-full text-xs">
          <thead className="text-left text-slate-500 dark:text-slate-400">
            <tr>
              <th scope="col" className="px-2 py-1 font-medium">
                {t('Mrp.Workbench.Capacity.Bucket')}
              </th>
              <th scope="col" className="px-2 py-1 font-medium">
                {t('Mrp.Workbench.Capacity.LoadVsCapacity')}
              </th>
              <th scope="col" className="px-2 py-1 text-right font-medium">
                {t('Mrp.Workbench.Capacity.Load')}
              </th>
              <th scope="col" className="px-2 py-1 text-right font-medium">
                {t('Mrp.Workbench.Capacity.Capacity')}
              </th>
            </tr>
          </thead>
          <tbody>
            {workCenter.buckets.map((bucket) => (
              <BucketRow key={bucket.startUtc} bucket={bucket} locale={locale} />
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

const BucketRow = ({ bucket, locale }: { bucket: MrpCapacityBucket; locale: string }) => {
  const { t } = useTranslation();
  const barColor = bucket.isOverloaded
    ? 'bg-danger-500 dark:bg-danger-400'
    : 'bg-success-500 dark:bg-success-400';
  const loadColor = bucket.isOverloaded
    ? 'text-danger-600 dark:text-danger-300'
    : 'text-slate-700 dark:text-slate-200';

  return (
    <tr
      data-testid="capacity-bucket-row"
      data-overloaded={bucket.isOverloaded ? 'true' : 'false'}
      className="border-t border-slate-100 dark:border-slate-800"
    >
      <td className="whitespace-nowrap px-2 py-1.5 text-slate-600 dark:text-slate-300">
        {formatDate(bucket.startUtc, locale)}
      </td>
      <td className="px-2 py-1.5">
        <div className="h-2.5 w-full min-w-[80px] overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
          <div
            data-testid="capacity-bucket-bar"
            className={`h-full rounded-full ${barColor}`}
            style={{ width: barWidth(bucket) }}
            role="img"
            aria-label={t('Mrp.Workbench.Capacity.BarAria', {
              load: formatNumber(bucket.loadMinutes, locale, 0),
              capacity: formatNumber(bucket.capacityMinutes, locale, 0),
            })}
          />
        </div>
      </td>
      <td className={`px-2 py-1.5 text-right font-semibold tabular-nums ${loadColor}`}>
        {formatNumber(bucket.loadMinutes, locale, 0)}
      </td>
      <td className="px-2 py-1.5 text-right tabular-nums text-slate-500 dark:text-slate-400">
        {formatNumber(bucket.capacityMinutes, locale, 0)}
      </td>
    </tr>
  );
};
