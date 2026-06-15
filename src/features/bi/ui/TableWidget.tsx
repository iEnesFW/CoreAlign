import type { BIResult } from '../model/bi.types';

interface Props {
  title: string;
  result: BIResult;
}

export const TableWidget = ({ title, result }: Props) => {
  return (
    <div className="flex h-full flex-col rounded-lg border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-900">
      <h3 className="mb-2 text-sm font-semibold text-slate-700 dark:text-slate-200">{title}</h3>
      <div className="min-h-0 flex-1 overflow-auto">
        <table className="w-full text-left text-sm">
          <thead className="sticky top-0 bg-slate-50 text-xs uppercase text-slate-500 dark:bg-slate-800 dark:text-slate-400">
            <tr>
              {result.columns.map((c) => (
                <th key={c.key} className="px-2 py-1.5 font-medium">
                  {c.label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {result.rows.map((row, idx) => (
              <tr key={idx} className="hover:bg-slate-50 dark:hover:bg-slate-800/60">
                {result.columns.map((c) => (
                  <td key={c.key} className="px-2 py-1.5 text-slate-700 dark:text-slate-300">
                    {String(row[c.key] ?? '')}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
