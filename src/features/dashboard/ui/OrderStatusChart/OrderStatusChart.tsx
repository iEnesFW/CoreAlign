import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts';
import { useTheme } from '@/app/providers/themeContext';

interface Props {
  counts: Record<string, number>;
}

const STATUS_ORDER = ['Draft', 'Confirmed', 'Shipped', 'Closed', 'Cancelled'] as const;
const COLORS: Record<(typeof STATUS_ORDER)[number], string> = {
  Draft: '#94a3b8',
  Confirmed: '#3b82f6',
  Shipped: '#f59e0b',
  Closed: '#10b981',
  Cancelled: '#ef4444',
};

export const OrderStatusChart = ({ counts }: Props) => {
  const { t } = useTranslation();
  const { theme } = useTheme();
  const isDark = theme === 'dark';

  const data = useMemo(
    () =>
      STATUS_ORDER.filter((status) => (counts[status] ?? 0) > 0).map((status) => ({
        name: t(`orders.status.${status}`),
        status,
        value: counts[status] ?? 0,
      })),
    [counts, t],
  );

  if (data.length === 0) {
    return (
      <div className="flex h-[220px] items-center justify-center text-xs text-slate-500 dark:text-slate-400">
        {t('dashboard.statusChart.empty')}
      </div>
    );
  }

  return (
    <ResponsiveContainer width="100%" height={220}>
      <PieChart>
        <Pie
          data={data}
          dataKey="value"
          nameKey="name"
          innerRadius={48}
          outerRadius={72}
          paddingAngle={2}
        >
          {data.map((entry) => (
            <Cell key={entry.status} fill={COLORS[entry.status as keyof typeof COLORS]} />
          ))}
        </Pie>
        <Tooltip
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
        <Legend
          verticalAlign="bottom"
          height={28}
          iconType="circle"
          wrapperStyle={{ fontSize: 11, color: isDark ? 'rgb(203 213 225)' : 'rgb(71 85 105)' }}
        />
      </PieChart>
    </ResponsiveContainer>
  );
};
