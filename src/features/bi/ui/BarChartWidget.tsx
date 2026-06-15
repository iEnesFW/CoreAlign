import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import type { BIResult } from '../model/bi.types';

interface Props {
  title: string;
  result: BIResult;
  xKey?: string;
  yKey?: string;
}

export const BarChartWidget = ({ title, result, xKey, yKey }: Props) => {
  const x = xKey ?? result.columns[0]?.key ?? 'x';
  const y = yKey ?? result.columns[1]?.key ?? 'y';
  return (
    <div className="flex h-full flex-col rounded-lg border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-900">
      <h3 className="mb-2 text-sm font-semibold text-slate-700 dark:text-slate-200">{title}</h3>
      <div className="min-h-0 flex-1">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={result.rows as Array<Record<string, unknown>>}>
            <CartesianGrid strokeDasharray="3 3" className="opacity-30" />
            <XAxis dataKey={x} fontSize={11} />
            <YAxis fontSize={11} />
            <Tooltip />
            <Bar dataKey={y} fill="#2563eb" radius={[4, 4, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
};
