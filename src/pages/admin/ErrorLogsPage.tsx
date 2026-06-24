import { useState } from 'react';
import { Navigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Bug } from 'lucide-react';
import { toast } from 'sonner';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { EmptyState } from '@/shared/ui/EmptyState/EmptyState';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useIsTenantAdmin } from '@/features/billing/hooks/useIsTenantAdmin';
import {
  useErrorLogsQuery,
  useErrorLogDetailQuery,
  useResolveErrorLogMutation,
} from '@/features/admin/error-logs/hooks/useErrorLogs';
import type { ErrorLogFilters, ErrorSeverity } from '@/features/admin/error-logs/errorLogs.types';

const severityClasses: Record<ErrorSeverity, string> = {
  Error: 'bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-300',
  Warning: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
  Info: 'bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-300',
};

export const ErrorLogsPage = () => {
  const { t } = useTranslation();
  const isAdmin = useIsTenantAdmin();
  const [filters, setFilters] = useState<ErrorLogFilters>({
    onlyUnresolved: true,
    page: 1,
    pageSize: 25,
  });
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [notes, setNotes] = useState('');

  const listQuery = useErrorLogsQuery(filters);
  const detailQuery = useErrorLogDetailQuery(selectedId);
  const resolveMutation = useResolveErrorLogMutation();

  if (!isAdmin) return <Navigate to="/dashboard" replace />;

  const patch = (next: Partial<ErrorLogFilters>) =>
    setFilters((prev) => ({ ...prev, page: 1, ...next }));

  const onResolve = async () => {
    if (!selectedId) return;
    try {
      await resolveMutation.mutateAsync({ id: selectedId, notes: notes.trim() || null });
      toast.success(t('Admin.ErrorLogs.Toast.Resolved'));
      setSelectedId(null);
      setNotes('');
    } catch (err) {
      toastApiError(err, t('Admin.ErrorLogs.Toast.ResolveFailed'));
    }
  };

  const page = listQuery.data;

  return (
    <main className="space-y-4 p-4">
      <PageHeader
        icon={<Bug size={20} />}
        eyebrow={t('Admin.ErrorLogs.Eyebrow')}
        title={t('Admin.ErrorLogs.Title')}
        subtitle={t('Admin.ErrorLogs.Description')}
      />

      <div className="flex flex-wrap items-end gap-2 rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-700 dark:bg-slate-900">
        <Input
          placeholder={t('Admin.ErrorLogs.Filter.Search')}
          className="w-56"
          onChange={(e) => patch({ search: e.target.value || undefined })}
        />
        <select
          className="h-9 rounded border border-slate-300 px-2 text-sm dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
          onChange={(e) =>
            patch({ severity: (e.target.value || undefined) as ErrorSeverity | undefined })
          }
        >
          <option value="">{t('Admin.ErrorLogs.Filter.AllSeverities')}</option>
          <option value="Error">Error</option>
          <option value="Warning">Warning</option>
          <option value="Info">Info</option>
        </select>
        <select
          className="h-9 rounded border border-slate-300 px-2 text-sm dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
          onChange={(e) =>
            patch({ source: (e.target.value || undefined) as 'Backend' | 'Frontend' | undefined })
          }
        >
          <option value="">{t('Admin.ErrorLogs.Filter.AllSources')}</option>
          <option value="Backend">Backend</option>
          <option value="Frontend">Frontend</option>
        </select>
        <Input
          placeholder={t('Admin.ErrorLogs.Filter.CorrelationId')}
          className="w-48"
          onChange={(e) => patch({ correlationId: e.target.value || undefined })}
        />
        <label className="flex items-center gap-1.5 text-sm text-slate-600 dark:text-slate-300">
          <input
            type="checkbox"
            checked={filters.onlyUnresolved ?? false}
            onChange={(e) => patch({ onlyUnresolved: e.target.checked || undefined })}
          />
          {t('Admin.ErrorLogs.Filter.OnlyUnresolved')}
        </label>
      </div>

      {listQuery.isError ? (
        <QueryError
          description={t('Admin.ErrorLogs.LoadFailed')}
          onRetry={() => listQuery.refetch()}
        />
      ) : listQuery.isLoading ? (
        <EmptyState title={t('common.loading')} variant="plain" />
      ) : !page || page.items.length === 0 ? (
        <EmptyState title={t('Admin.ErrorLogs.Empty')} variant="plain" />
      ) : (
        <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-700">
          <table className="min-w-full divide-y divide-slate-100 text-sm dark:divide-slate-800">
            <thead className="bg-slate-50 text-left text-xs uppercase text-slate-500 dark:bg-slate-900 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2">{t('Admin.ErrorLogs.Col.When')}</th>
                <th className="px-3 py-2">{t('Admin.ErrorLogs.Col.Severity')}</th>
                <th className="px-3 py-2">{t('Admin.ErrorLogs.Col.Source')}</th>
                <th className="px-3 py-2">{t('Admin.ErrorLogs.Col.Status')}</th>
                <th className="px-3 py-2">{t('Admin.ErrorLogs.Col.Where')}</th>
                <th className="px-3 py-2">{t('Admin.ErrorLogs.Col.Message')}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 bg-white dark:divide-slate-800 dark:bg-slate-950">
              {page.items.map((row) => (
                <tr
                  key={row.id}
                  onClick={() => setSelectedId(row.id)}
                  className="cursor-pointer hover:bg-slate-50 dark:hover:bg-slate-900"
                >
                  <td className="whitespace-nowrap px-3 py-2 text-slate-500">
                    {new Date(row.occurredAtUtc).toLocaleString()}
                  </td>
                  <td className="px-3 py-2">
                    <span
                      className={`rounded px-1.5 py-0.5 text-xs font-medium ${severityClasses[row.severity]}`}
                    >
                      {row.severity}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-slate-500">{row.source}</td>
                  <td className="px-3 py-2 text-slate-500">{row.statusCode ?? '—'}</td>
                  <td className="max-w-[220px] truncate px-3 py-2 text-slate-600 dark:text-slate-300">
                    {row.clientPage ?? row.path ?? '—'}
                  </td>
                  <td className="max-w-[320px] truncate px-3 py-2 text-slate-800 dark:text-slate-100">
                    {row.message}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {page && page.total > page.pageSize ? (
        <div className="flex items-center justify-end gap-2 text-sm">
          <Button
            variant="ghost"
            size="sm"
            disabled={(filters.page ?? 1) <= 1}
            onClick={() => setFilters((p) => ({ ...p, page: (p.page ?? 1) - 1 }))}
          >
            {t('Admin.ErrorLogs.Prev')}
          </Button>
          <span className="text-slate-500">{filters.page ?? 1}</span>
          <Button
            variant="ghost"
            size="sm"
            disabled={(filters.page ?? 1) * page.pageSize >= page.total}
            onClick={() => setFilters((p) => ({ ...p, page: (p.page ?? 1) + 1 }))}
          >
            {t('Admin.ErrorLogs.Next')}
          </Button>
        </div>
      ) : null}

      <Modal
        open={!!selectedId}
        onClose={() => {
          setSelectedId(null);
          setNotes('');
        }}
        title={t('Admin.ErrorLogs.Detail.Title')}
        size="lg"
        footer={
          detailQuery.data && !detailQuery.data.isResolved ? (
            <Button onClick={onResolve} isLoading={resolveMutation.isPending}>
              {t('Admin.ErrorLogs.Detail.MarkResolved')}
            </Button>
          ) : null
        }
      >
        {detailQuery.isLoading ? (
          <p className="text-sm text-slate-500">{t('common.loading')}</p>
        ) : detailQuery.data ? (
          <div className="space-y-3 text-sm">
            <DetailRow
              label={t('Admin.ErrorLogs.Detail.CorrelationId')}
              value={detailQuery.data.correlationId}
              mono
            />
            <DetailRow
              label={t('Admin.ErrorLogs.Detail.When')}
              value={new Date(detailQuery.data.occurredAtUtc).toLocaleString()}
            />
            <DetailRow
              label={t('Admin.ErrorLogs.Detail.Severity')}
              value={`${detailQuery.data.severity} • ${detailQuery.data.source}`}
            />
            <DetailRow
              label={t('Admin.ErrorLogs.Detail.Where')}
              value={detailQuery.data.clientPage ?? detailQuery.data.path ?? '—'}
            />
            <DetailRow
              label={t('Admin.ErrorLogs.Detail.Status')}
              value={
                `${detailQuery.data.httpMethod ?? ''} ${detailQuery.data.statusCode ?? ''}`.trim() ||
                '—'
              }
            />
            <DetailRow
              label={t('Admin.ErrorLogs.Detail.ExceptionType')}
              value={detailQuery.data.exceptionType ?? '—'}
              mono
            />
            <DetailRow
              label={t('Admin.ErrorLogs.Detail.User')}
              value={detailQuery.data.userName ?? detailQuery.data.userId ?? '—'}
            />
            <div>
              <p className="mb-1 text-xs font-medium text-slate-500">
                {t('Admin.ErrorLogs.Detail.Message')}
              </p>
              <p className="rounded bg-slate-50 p-2 text-slate-800 dark:bg-slate-800 dark:text-slate-100">
                {detailQuery.data.message}
              </p>
            </div>
            {detailQuery.data.stackTrace ? (
              <div>
                <p className="mb-1 text-xs font-medium text-slate-500">
                  {t('Admin.ErrorLogs.Detail.StackTrace')}
                </p>
                <pre className="max-h-64 overflow-auto rounded bg-slate-900 p-2 text-[11px] text-slate-200">
                  {detailQuery.data.stackTrace}
                </pre>
              </div>
            ) : null}
            {detailQuery.data.isResolved ? (
              <p className="text-xs text-emerald-600 dark:text-emerald-400">
                {t('Admin.ErrorLogs.Detail.ResolvedNote', {
                  notes: detailQuery.data.resolutionNotes ?? '',
                })}
              </p>
            ) : (
              <Input
                label={t('Admin.ErrorLogs.Detail.ResolutionNotes')}
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
              />
            )}
          </div>
        ) : (
          <p className="text-sm text-slate-500">{t('common.noData')}</p>
        )}
      </Modal>
    </main>
  );
};

const DetailRow = ({ label, value, mono }: { label: string; value: string; mono?: boolean }) => (
  <div className="flex gap-3">
    <span className="w-32 shrink-0 text-xs font-medium text-slate-500">{label}</span>
    <span className={`text-slate-800 dark:text-slate-100 ${mono ? 'font-mono text-xs' : ''}`}>
      {value}
    </span>
  </div>
);

export default ErrorLogsPage;
