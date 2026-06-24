import React from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, Edit2, Trash2, Search, Filter, ChevronLeft, ChevronRight } from 'lucide-react';

export interface DataTableCardProps {
  data: { id: string; customer: string; amount: string; status: string; date: string }[];
}

export const DataTableCard: React.FC<DataTableCardProps> = ({ data }) => {
  const { t } = useTranslation();

  return (
    <div className="bg-white dark:bg-shell rounded-[5px] shadow-[0_2px_10px_-3px_rgba(6,81,237,0.1)] dark:shadow-none border border-slate-200/60 dark:border-slate-800/60 flex flex-col">
      <div className="p-3 border-b border-slate-200/60 dark:border-slate-800/60 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div className="flex items-center gap-1.5">
          <button className="flex items-center gap-1 px-2 py-1.5 text-[10px] font-semibold text-white bg-primary-600 hover:bg-primary-700 rounded-[5px] transition-colors shadow-sm">
            <Plus size={12} /> {t('common.add_new')}
          </button>
          <button className="flex items-center gap-1 px-2 py-1.5 text-[10px] font-medium text-slate-700 dark:text-slate-200 bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 rounded-[5px] transition-colors">
            <Edit2 size={12} /> {t('common.edit')}
          </button>
          <button className="flex items-center gap-1 px-2 py-1.5 text-[10px] font-medium text-danger-600 bg-danger-50 dark:bg-danger-500/10 hover:bg-danger-100 dark:hover:bg-danger-500/20 rounded-[5px] transition-colors">
            <Trash2 size={12} /> {t('common.delete')}
          </button>
        </div>

        <div className="flex items-center gap-1.5">
          <div className="relative">
            <Search size={12} className="absolute left-2 top-1/2 -translate-y-1/2 text-slate-400" />
            <input
              type="text"
              placeholder={`${t('common.search')}...`}
              className="pl-6 pr-2 py-1.5 text-[10px] border border-slate-200 dark:border-slate-700 rounded-[5px] bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-primary-500 w-40 sm:w-48 transition-all"
            />
          </div>
          <button className="flex items-center gap-1 px-2 py-1.5 text-[10px] font-medium text-slate-700 dark:text-slate-200 bg-white dark:bg-shell border border-slate-200 dark:border-slate-700 hover:bg-slate-50 dark:hover:bg-slate-800 rounded-[5px] transition-colors">
            <Filter size={12} /> {t('common.filter')}
          </button>
        </div>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="bg-slate-50/50 dark:bg-slate-800/30 border-b border-slate-200/60 dark:border-slate-800/60">
              <th className="p-2 w-8 text-center">
                <input
                  type="checkbox"
                  className="rounded-[3px] border-slate-300 text-primary-600 focus:ring-primary-500"
                />
              </th>
              <th className="p-2 text-[10px] font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                Order ID
              </th>
              <th className="p-2 text-[10px] font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                Customer
              </th>
              <th className="p-2 text-[10px] font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                Amount
              </th>
              <th className="p-2 text-[10px] font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                Status
              </th>
              <th className="p-2 text-[10px] font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                Date
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200/60 dark:divide-slate-800/60">
            {data.map((row) => (
              <tr
                key={row.id}
                className="hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors group"
              >
                <td className="p-2 text-center">
                  <input
                    type="checkbox"
                    className="rounded-[3px] border-slate-300 text-primary-600 focus:ring-primary-500"
                  />
                </td>
                <td className="p-2 text-[11px] font-medium text-slate-900 dark:text-white">
                  {row.id}
                </td>
                <td className="p-2 text-[11px] text-slate-600 dark:text-slate-300">
                  {row.customer}
                </td>
                <td className="p-2 text-[11px] font-medium text-slate-900 dark:text-white">
                  {row.amount}
                </td>
                <td className="p-2">
                  <span
                    className={`inline-flex items-center px-1.5 py-0.5 rounded-[3px] text-[9px] font-bold uppercase tracking-wider ${
                      row.status === 'Completed'
                        ? 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-400'
                        : row.status === 'Pending'
                          ? 'bg-warning-100 text-warning-700 dark:bg-warning-500/20 dark:text-warning-400'
                          : row.status === 'Processing'
                            ? 'bg-primary-100 text-primary-700 dark:bg-primary-500/20 dark:text-primary-400'
                            : 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-400'
                    }`}
                  >
                    {row.status}
                  </span>
                </td>
                <td className="p-2 text-[11px] text-slate-500 dark:text-slate-400">{row.date}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="p-2 border-t border-slate-200/60 dark:border-slate-800/60 flex items-center justify-between bg-slate-50/30 dark:bg-slate-800/10">
        <span className="text-[10px] text-slate-500 dark:text-slate-400">
          {t('table.showing')} <span className="font-medium text-slate-900 dark:text-white">1</span>{' '}
          {t('table.to')} <span className="font-medium text-slate-900 dark:text-white">5</span>{' '}
          {t('table.of')} <span className="font-medium text-slate-900 dark:text-white">24</span>{' '}
          {t('table.results')}
        </span>
        <div className="flex items-center gap-1">
          <button className="p-1 rounded-[3px] border border-slate-200 dark:border-slate-700 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors disabled:opacity-50">
            <ChevronLeft size={12} />
          </button>
          <button className="w-5 h-5 flex items-center justify-center rounded-[3px] text-[10px] font-medium bg-primary-600 text-white">
            1
          </button>
          <button className="w-5 h-5 flex items-center justify-center rounded-[3px] text-[10px] font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors">
            2
          </button>
          <button className="w-5 h-5 flex items-center justify-center rounded-[3px] text-[10px] font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors">
            3
          </button>
          <span className="text-[10px] text-slate-400 px-1">...</span>
          <button className="p-1 rounded-[3px] border border-slate-200 dark:border-slate-700 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors">
            <ChevronRight size={12} />
          </button>
        </div>
      </div>
    </div>
  );
};
