import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Check, X, RefreshCw } from 'lucide-react';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useAdminRequestsQuery, useProcessPrivacyRequest } from '../hooks/usePrivacyRequests';
import type {
  DataSubjectRequestDto,
  DataSubjectRequestStatus,
  ProcessAction,
} from '../model/privacy.types';

const STATUSES: DataSubjectRequestStatus[] = ['Submitted', 'InProgress', 'Completed', 'Rejected'];

const actionForType: Record<string, ProcessAction> = {
  Access: 'Access',
  Erasure: 'Erasure',
  Portability: 'Portability',
  Rectification: 'Rectification',
};

export const AdminRequestQueue = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const [status, setStatus] = useState<DataSubjectRequestStatus | undefined>('Submitted');
  const [page, setPage] = useState(1);
  const pageSize = 25;
  const query = useAdminRequestsQuery(status, page, pageSize);
  const processor = useProcessPrivacyRequest();

  const items = useMemo(() => query.data?.data?.items ?? [], [query.data]);
  const total = query.data?.data?.total ?? 0;

  const handleProcess = async (request: DataSubjectRequestDto) => {
    const action = actionForType[request.type];
    if (!action) return;
    await safeRequestWithNotify(
      processor.mutateAsync({
        id: request.id,
        body: { action, keepFinancialTrail: true },
      }),
      { successMessage: t('Privacy.Admin.ProcessSuccess') },
    );
  };

  const handleReject = async (request: DataSubjectRequestDto) => {
    const reason = window.prompt(t('Privacy.Admin.RejectionPrompt'));
    if (!reason) return;
    await safeRequestWithNotify(
      processor.mutateAsync({
        id: request.id,
        body: { action: 'Reject', rejectionReason: reason },
      }),
      { successMessage: t('Privacy.Admin.RejectSuccess') },
    );
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <label
          htmlFor="status-filter"
          className="text-sm font-medium text-slate-700 dark:text-slate-200"
        >
          {t('Privacy.Admin.StatusFilter')}
        </label>
        <select
          id="status-filter"
          value={status ?? ''}
          onChange={(e) => {
            setStatus((e.target.value as DataSubjectRequestStatus) || undefined);
            setPage(1);
          }}
          className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        >
          <option value="">{t('Privacy.Admin.AllStatuses')}</option>
          {STATUSES.map((s) => (
            <option key={s} value={s}>
              {t(`Privacy.Status.${s}`)}
            </option>
          ))}
        </select>
        <button
          type="button"
          onClick={() => query.refetch()}
          className="ml-auto inline-flex items-center gap-1 rounded-md border border-slate-300 px-3 py-1.5 text-sm text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800"
        >
          <RefreshCw size={14} />
          {t('Common.Refresh')}
        </button>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-slate-700 dark:bg-slate-900 dark:text-slate-200">
            <tr>
              <th className="px-3 py-2 text-left font-semibold">{t('Privacy.Admin.Type')}</th>
              <th className="px-3 py-2 text-left font-semibold">{t('Privacy.Admin.Status')}</th>
              <th className="px-3 py-2 text-left font-semibold">
                {t('Privacy.Admin.SubmittedAt')}
              </th>
              <th className="px-3 py-2 text-left font-semibold">{t('Privacy.Admin.Notes')}</th>
              <th className="px-3 py-2 text-right font-semibold">{t('Privacy.Admin.Actions')}</th>
            </tr>
          </thead>
          <tbody>
            {items.length === 0 && (
              <tr>
                <td
                  colSpan={5}
                  className="px-3 py-6 text-center text-slate-500 dark:text-slate-400"
                >
                  {t('Privacy.Admin.Empty')}
                </td>
              </tr>
            )}
            {items.map((req) => (
              <tr key={req.id} className="border-t border-slate-100 dark:border-slate-700">
                <td className="px-3 py-2">{t(`Privacy.Request.TypeOption.${req.type}`)}</td>
                <td className="px-3 py-2">{t(`Privacy.Status.${req.status}`)}</td>
                <td className="px-3 py-2 tabular-nums">{formatDate(req.submittedAtUtc, locale)}</td>
                <td className="px-3 py-2 text-slate-600 dark:text-slate-300">{req.notes ?? '—'}</td>
                <td className="px-3 py-2 text-right">
                  {req.status === 'Submitted' || req.status === 'InProgress' ? (
                    <div className="inline-flex gap-2">
                      <button
                        type="button"
                        onClick={() => handleProcess(req)}
                        disabled={processor.isPending}
                        className="inline-flex items-center gap-1 rounded-md bg-emerald-600 px-2 py-1 text-xs font-medium text-white hover:bg-emerald-700 disabled:opacity-50"
                      >
                        <Check size={12} />
                        {t('Privacy.Admin.Process')}
                      </button>
                      <button
                        type="button"
                        onClick={() => handleReject(req)}
                        disabled={processor.isPending}
                        className="inline-flex items-center gap-1 rounded-md bg-red-600 px-2 py-1 text-xs font-medium text-white hover:bg-red-700 disabled:opacity-50"
                      >
                        <X size={12} />
                        {t('Privacy.Admin.Reject')}
                      </button>
                    </div>
                  ) : (
                    <span className="text-xs text-slate-400">—</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {total > pageSize && (
        <div className="flex items-center justify-end gap-2 text-sm text-slate-600 dark:text-slate-300">
          <button
            type="button"
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page <= 1}
            className="rounded-md border border-slate-300 px-3 py-1 disabled:opacity-50 dark:border-slate-700"
          >
            {t('Common.Previous')}
          </button>
          <span>
            {page} / {Math.max(1, Math.ceil(total / pageSize))}
          </span>
          <button
            type="button"
            onClick={() => setPage((p) => p + 1)}
            disabled={page * pageSize >= total}
            className="rounded-md border border-slate-300 px-3 py-1 disabled:opacity-50 dark:border-slate-700"
          >
            {t('Common.Next')}
          </button>
        </div>
      )}
    </div>
  );
};
