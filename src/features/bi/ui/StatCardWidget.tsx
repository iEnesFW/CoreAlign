import type { BIResult } from '../model/bi.types';

interface Props {
  title: string;
  result: BIResult;
  valueKey?: string;
  format?: 'number' | 'currency';
}

export const StatCardWidget = ({ title, result, valueKey, format = 'number' }: Props) => {
  const key =
    valueKey ??
    result.columns.find((c) => c.dataType !== 'string')?.key ??
    result.columns[0]?.key ??
    'value';
  const firstRow = result.rows[0];
  const raw = firstRow ? firstRow[key] : null;
  const numeric = typeof raw === 'number' ? raw : Number(raw ?? 0);
  const formatted =
    format === 'currency'
      ? new Intl.NumberFormat(undefined, {
          style: 'currency',
          currency: 'TRY',
          maximumFractionDigits: 2,
        }).format(numeric)
      : new Intl.NumberFormat(undefined, { maximumFractionDigits: 2 }).format(numeric);
  return (
    <div className="flex h-full flex-col justify-between rounded-lg border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-900">
      <h3 className="text-sm font-medium text-slate-500 dark:text-slate-400">{title}</h3>
      <div className="text-3xl font-semibold text-slate-900 dark:text-slate-50">{formatted}</div>
    </div>
  );
};
