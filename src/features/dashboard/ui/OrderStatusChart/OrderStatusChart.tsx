import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts';

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
            border: '1px solid rgb(226 232 240)',
            borderRadius: 6,
            background: 'rgb(255 255 255 / 0.95)',
          }}
        />
        <Legend
          verticalAlign="bottom"
          height={28}
          iconType="circle"
          wrapperStyle={{ fontSize: 11 }}
        />
      </PieChart>
    </ResponsiveContainer>
  );
};
