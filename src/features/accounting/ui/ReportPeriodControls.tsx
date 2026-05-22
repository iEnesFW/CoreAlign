import type { ReactNode } from 'react';

interface Props {
  year: number;
  fromDate: string;
  toDate: string;
  onYearChange: (year: number) => void;
  onFromChange: (value: string) => void;
  onToChange: (value: string) => void;
  right?: ReactNode;
}

const yearStart = (year: number) => `${year}-01-01`;
const yearEnd = (year: number) => `${year}-12-31`;

export const ReportPeriodControls = ({
  year,
  fromDate,
  toDate,
  onYearChange,
  onFromChange,
  onToChange,
  right,
}: Props) => {
  const setYear = (y: number) => {
    onYearChange(y);
    onFromChange(yearStart(y));
    onToChange(yearEnd(y));
  };

  return (
    <div className="flex flex-wrap items-end gap-3">
      <div>
        <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">Yıl</label>
        <div className="mt-1 inline-flex items-center gap-1">
          <button
            type="button"
            onClick={() => setYear(year - 1)}
            className="rounded border border-slate-200 bg-white px-2 py-1 text-xs hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900"
          >
            ←
          </button>
          <span className="px-2 font-semibold text-slate-900 dark:text-slate-100">{year}</span>
          <button
            type="button"
            onClick={() => setYear(year + 1)}
            className="rounded border border-slate-200 bg-white px-2 py-1 text-xs hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900"
          >
            →
          </button>
        </div>
      </div>
      <div>
        <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
          Başlangıç
        </label>
        <input
          type="date"
          value={fromDate}
          onChange={(e) => onFromChange(e.target.value)}
          className="mt-1 rounded border border-slate-300 bg-white px-2 py-1 text-sm dark:border-slate-700 dark:bg-slate-800"
        />
      </div>
      <div>
        <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
          Bitiş
        </label>
        <input
          type="date"
          value={toDate}
          onChange={(e) => onToChange(e.target.value)}
          className="mt-1 rounded border border-slate-300 bg-white px-2 py-1 text-sm dark:border-slate-700 dark:bg-slate-800"
        />
      </div>
      {right && <div className="ml-auto">{right}</div>}
    </div>
  );
};
