import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { useTheme } from '@/app/providers/themeContext';
import type { SalesTrendPoint } from '@/features/dashboard/model/dashboard.types';

interface Props {
  points: SalesTrendPoint[];
}

export const SalesTrendChart = ({ points }: Props) => {
  const { i18n } = useTranslation();
  const { theme } = useTheme();
  const isDark = theme === 'dark';

  const data = useMemo(
    () =>
      points.map((p) => ({
        date: p.date,
        total: p.total,
        label: new Intl.DateTimeFormat(i18n.language, { month: 'short', day: 'numeric' }).format(
          new Date(p.date),
        ),
      })),
    [points, i18n.language],
  );

  const formatCurrency = (value: number) => {
    try {
      return new Intl.NumberFormat(i18n.language, {
        style: 'currency',
        currency: 'USD',
        maximumFractionDigits: 0,
      }).format(value);
    } catch {
      return value.toFixed(0);
    }
  };

  return (
    <ResponsiveContainer width="100%" height={220}>
      <AreaChart data={data} margin={{ top: 8, right: 12, left: -10, bottom: 0 }}>
        <defs>
          <linearGradient id="salesGradient" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#6366f1" stopOpacity={0.45} />
            <stop offset="100%" stopColor="#6366f1" stopOpacity={0} />
          </linearGradient>
        </defs>
        <CartesianGrid
          strokeDasharray="3 3"
          stroke="currentColor"
          className="text-slate-200 dark:text-slate-800"
        />
        <XAxis
          dataKey="label"
          tick={{ fontSize: 10, fill: 'currentColor' }}
          stroke="currentColor"
          className="text-slate-500 dark:text-slate-400"
          interval="preserveStartEnd"
          minTickGap={24}
        />
        <YAxis
          tick={{ fontSize: 10, fill: 'currentColor' }}
          stroke="currentColor"
          className="text-slate-500 dark:text-slate-400"
          tickFormatter={formatCurrency}
          width={64}
        />
        <Tooltip
          formatter={(value) => formatCurrency(Number(value))}
          cursor={{ stroke: isDark ? 'rgb(71 85 105)' : 'rgb(203 213 225)' }}
          contentStyle={{
            fontSize: 12,
            borderRadius: 6,
            border: `1px solid ${isDark ? 'rgb(51 65 85)' : 'rgb(226 232 240)'}`,
            background: isDark ? 'rgb(15 23 42 / 0.97)' : 'rgb(255 255 255 / 0.97)',
            color: isDark ? 'rgb(226 232 240)' : 'rgb(15 23 42)',
          }}
          itemStyle={{ color: isDark ? 'rgb(226 232 240)' : 'rgb(15 23 42)' }}
          labelStyle={{ color: isDark ? 'rgb(148 163 184)' : 'rgb(100 116 139)' }}
        />
        <Area
          type="monotone"
          dataKey="total"
          stroke="#6366f1"
          strokeWidth={2}
          fill="url(#salesGradient)"
        />
      </AreaChart>
    </ResponsiveContainer>
  );
};
