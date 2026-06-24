import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronLeft, ChevronRight, Activity } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { useActivityLogsQuery } from '@/features/activity/hooks/useActivityQueries';
import type { ActivityLog } from '@/features/activity/model/activity.types';

const PAGE_SIZE = 30;

const methodStyles: Record<string, string> = {
  POST: 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300',
  PUT: 'bg-warning-100 text-warning-800 dark:bg-warning-500/20 dark:text-warning-300',
  PATCH: 'bg-warning-100 text-warning-800 dark:bg-warning-500/20 dark:text-warning-300',
  DELETE: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
};

const statusStyles = (status: number): string => {
  if (status >= 500)
    return 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300';
  if (status >= 400)
    return 'bg-warning-100 text-warning-800 dark:bg-warning-500/20 dark:text-warning-300';
  if (status >= 300)
    return 'bg-primary-100 text-primary-700 dark:bg-primary-500/20 dark:text-primary-300';
  return 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300';
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
    <ListPageTemplate
      header={
        <PageHeader
          icon={<Activity size={20} />}
          title={t('activity.title')}
          subtitle={t('activity.subtitle')}
        />
      }
      pagination={
        total > PAGE_SIZE ? (
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
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                aria-label={t('activity.pagination.previous')}
              >
                <ChevronLeft size={14} />
              </Button>
              <span className="px-2">
                {page} / {totalPages}
              </span>
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                aria-label={t('activity.pagination.next')}
              >
                <ChevronRight size={14} />
              </Button>
            </div>
          </div>
        ) : undefined
      }
    >
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
    </ListPageTemplate>
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
