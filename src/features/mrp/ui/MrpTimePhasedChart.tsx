import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ReferenceLine,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { formatNumber } from '@/shared/lib/format';
import type { MrpItemPlan } from '../model/mrp-planning.types';

interface Props {
  item: MrpItemPlan;
}

export const MrpTimePhasedChart = ({ item }: Props) => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;

  const data = useMemo(
    () =>
      (item.buckets ?? []).map((b) => ({
        label: b.startUtc.slice(5, 10),
        projectedOnHand: b.projectedOnHand,
        scheduledReceipts: b.scheduledReceipts,
        grossRequirements: b.grossRequirements,
        plannedReleases: b.plannedReleases,
      })),
    [item.buckets],
  );

  if (data.length === 0) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-4 text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-900">
        {t('Mrp.Workbench.Chart.NoData')}
      </div>
    );
  }

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-3 shadow-sm dark:border-slate-700 dark:bg-slate-900">
      <ResponsiveContainer width="100%" height={240}>
        <LineChart data={data} margin={{ top: 8, right: 12, left: -8, bottom: 0 }}>
          <CartesianGrid
            strokeDasharray="3 3"
            stroke="currentColor"
            className="text-slate-200 dark:text-slate-800"
          />
          <XAxis
            dataKey="label"
            tick={{ fontSize: 10 }}
            stroke="currentColor"
            className="text-slate-500"
            interval="preserveStartEnd"
            minTickGap={20}
          />
          <YAxis
            tick={{ fontSize: 10 }}
            stroke="currentColor"
            className="text-slate-500"
            width={56}
            tickFormatter={(v) => formatNumber(Number(v), locale, 0)}
          />
          <Tooltip
            formatter={(value) => formatNumber(Number(value), locale)}
            contentStyle={{
              fontSize: 12,
              borderRadius: 6,
            }}
          />
          <Legend wrapperStyle={{ fontSize: 11 }} />
          {item.reorderPoint > 0 && (
            <ReferenceLine
              y={item.reorderPoint}
              stroke="#f59e0b"
              strokeDasharray="4 4"
              label={{ value: t('Mrp.Workbench.Chart.Rop'), fontSize: 10, fill: '#f59e0b' }}
            />
          )}
          {item.safetyStock > 0 && (
            <ReferenceLine y={item.safetyStock} stroke="#ef4444" strokeDasharray="2 2" />
          )}
          <Line
            type="monotone"
            dataKey="projectedOnHand"
            name={t('Mrp.Workbench.Chart.ProjectedOnHand')}
            stroke="#6366f1"
            strokeWidth={2}
            dot={false}
          />
          <Line
            type="monotone"
            dataKey="scheduledReceipts"
            name={t('Mrp.Workbench.Chart.ScheduledReceipts')}
            stroke="#10b981"
            strokeWidth={1.5}
            dot={false}
          />
          <Line
            type="monotone"
            dataKey="grossRequirements"
            name={t('Mrp.Workbench.Chart.Demand')}
            stroke="#ef4444"
            strokeWidth={1.5}
            dot={false}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
};
