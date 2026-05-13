import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronLeft, ChevronRight, Activity } from 'lucide-react';
import { useActivityLogsQuery } from '@/features/activity/hooks/useActivityQueries';
import type { ActivityLog } from '@/features/activity/model/activity.types';

const PAGE_SIZE = 30;

const methodStyles: Record<string, string> = {
  POST: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  PUT: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  PATCH: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  DELETE: 'bg-red-100 text-red-700 dark:bg-red-500/20 dark:text-red-300',
};

const statusStyles = (status: number): string => {
  if (status >= 500) return 'bg-red-100 text-red-700 dark:bg-red-500/20 dark:text-red-300';
  if (status >= 400) return 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300';
  if (status >= 300) return 'bg-blue-100 text-blue-700 dark:bg-blue-500/20 dark:text-blue-300';
  return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300';
};

const formatDateTime = (iso: string, locale: string) => {
  try {
    return new Intl.DateTimeFormat(locale, {
      dateStyle: 'short',
      timeStyle: 'medium',
    }).format(new Date(iso));
  } catch {
    return iso;
  }
};

export const ActivityPage = () => {
  const { t, i18n } = useTranslation();
  const [page, setPage] = useState(1);

  const query = useActivityLogsQuery({ page, pageSize: PAGE_SIZE });
  const result = query.data?.data;
  const logs = result?.items ?? [];
  const total = result?.total ?? 0;
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <div>
        <h1 className="flex items-center gap-2 text-xl font-semibold text-slate-900 dark:text-slate-100">
          <Activity size={18} className="text-indigo-500" />
          {t('activity.title')}
        </h1>
        <p className="text-xs text-slate-500 dark:text-slate-400">{t('activity.subtitle')}</p>
      </div>

      {query.isPending && logs.length === 0 ? (
        <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-400">
          {t('common.loading')}
        </div>
      ) : logs.length === 0 ? (
        <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-400">
          {t('activity.empty')}
        </div>
      ) : (
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-50 dark:bg-slate-800/50">
                <tr>
                  <Th>{t('activity.columns.time')}</Th>
                  <Th>{t('activity.columns.method')}</Th>
                  <Th>{t('activity.columns.path')}</Th>
                  <Th>{t('activity.columns.status')}</Th>
                  <Th>{t('activity.columns.duration')}</Th>
                  <Th>{t('activity.columns.user')}</Th>
                  <Th>{t('activity.columns.ip')}</Th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
                {logs.map((log) => (
                  <Row key={log.id} log={log} locale={i18n.language} />
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {total > PAGE_SIZE && (
        <div className="flex items-center justify-between text-xs text-slate-600 dark:text-slate-400">
          <div>
            {t('activity.pagination.summary', {
              from: (page - 1) * PAGE_SIZE + 1,
              to: Math.min(page * PAGE_SIZE, total),
              total,
              defaultValue: `${(page - 1) * PAGE_SIZE + 1}-${Math.min(page * PAGE_SIZE, total)} / ${total}`,
            })}
          </div>
          <div className="flex items-center gap-1">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              className="rounded border border-slate-200 p-1.5 text-slate-600 hover:bg-slate-100 disabled:opacity-40 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800"
              aria-label={t('activity.pagination.previous')}
            >
              <ChevronLeft size={14} />
            </button>
            <span className="px-2">
              {page} / {totalPages}
            </span>
            <button
              type="button"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              className="rounded border border-slate-200 p-1.5 text-slate-600 hover:bg-slate-100 disabled:opacity-40 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800"
              aria-label={t('activity.pagination.next')}
            >
              <ChevronRight size={14} />
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

const Th = ({ children }: { children: React.ReactNode }) => (
  <th className="px-3 py-2 text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
    {children}
  </th>
);

const Row = ({ log, locale }: { log: ActivityLog; locale: string }) => (
  <tr className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
    <td className="whitespace-nowrap px-3 py-2 text-xs text-slate-600 dark:text-slate-400">
      {formatDateTime(log.createdAtUtc, locale)}
    </td>
    <td className="px-3 py-2">
      <span
        className={`inline-flex rounded px-1.5 py-0.5 font-mono text-[10px] font-semibold ${methodStyles[log.method] ?? 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300'}`}
      >
        {log.method}
      </span>
    </td>
    <td className="px-3 py-2 font-mono text-xs text-slate-700 dark:text-slate-200">{log.path}</td>
    <td className="px-3 py-2">
      <span
        className={`inline-flex rounded px-1.5 py-0.5 font-mono text-[10px] font-semibold ${statusStyles(log.statusCode)}`}
      >
        {log.statusCode}
      </span>
    </td>
    <td className="px-3 py-2 text-xs text-slate-600 dark:text-slate-400">{log.durationMs} ms</td>
    <td className="px-3 py-2 font-mono text-[10px] text-slate-500 dark:text-slate-500">
      {log.userId ? log.userId.slice(0, 8) : '—'}
    </td>
    <td className="px-3 py-2 font-mono text-[10px] text-slate-500 dark:text-slate-500">
      {log.ipAddress ?? '—'}
    </td>
  </tr>
);
