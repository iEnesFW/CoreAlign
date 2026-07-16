import { useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, Check, X } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { useDesignerUxMode } from '@/shared/lib/persona';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import {
  useApproveWorkOrderRevisionMutation,
  useRejectWorkOrderRevisionMutation,
  useWorkOrderRevisionsQuery,
} from '@/features/glass-enclosure/hooks/useWorkOrderRevisions';
import type {
  WorkOrderRevisionDto,
  WorkOrderRevisionStatus,
} from '@/features/glass-enclosure/model/workOrder.types';

interface RevisionsPanelProps {
  workOrderId: string | null | undefined;
  className?: string;
}

const STATUS_BADGE_CLASSES: Record<WorkOrderRevisionStatus, string> = {
  PendingApproval: 'bg-warning-100 text-warning-800 dark:bg-warning-900/40 dark:text-warning-300',
  Blocked: 'bg-danger-100 text-danger-800 dark:bg-danger-900/40 dark:text-danger-300',
  Approved: 'bg-success-100 text-success-800 dark:bg-success-900/40 dark:text-success-300',
  Rejected: 'bg-slate-200 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
  SilentSnapshot: 'bg-primary-100 text-primary-800 dark:bg-primary-900/40 dark:text-primary-300',
};

const formatPercent = (value: number): string => `${value >= 0 ? '+' : ''}${value.toFixed(1)}%`;

const formatDate = (utc: string): string => {
  const date = new Date(utc);
  if (Number.isNaN(date.getTime())) return utc;
  return date.toLocaleString();
};

export const RevisionsPanel = ({ workOrderId, className }: RevisionsPanelProps) => {
  const { t } = useTranslation();
  const mode = useDesignerUxMode();
  const isSimple = mode === 'Simple';
  const query = useWorkOrderRevisionsQuery(workOrderId);
  const approveMutation = useApproveWorkOrderRevisionMutation();
  const rejectMutation = useRejectWorkOrderRevisionMutation();
  const [overrideReason, setOverrideReason] = useState('');
  const [rejectionReason, setRejectionReason] = useState('');
  const [activeOverrideId, setActiveOverrideId] = useState<string | null>(null);
  const [activeRejectionId, setActiveRejectionId] = useState<string | null>(null);

  const revisions = useMemo<WorkOrderRevisionDto[]>(
    () => query.data?.data ?? [],
    [query.data?.data],
  );

  const hasBlocking = useMemo(
    () => revisions.some((r) => r.status === 'PendingApproval' || r.status === 'Blocked'),
    [revisions],
  );

  const handleApprove = useCallback(
    async (revisionId: string) => {
      if (!workOrderId) return;
      await safeRequestWithNotify(
        approveMutation.mutateAsync({
          workOrderId,
          revisionId,
          input: overrideReason.trim() ? { overrideReason: overrideReason.trim() } : undefined,
        }),
        { successMessage: t('GlassEnclosure.WorkOrder.Revision.Toast.Approved') },
      );
      setOverrideReason('');
      setActiveOverrideId(null);
    },
    [approveMutation, overrideReason, t, workOrderId],
  );

  const handleReject = useCallback(
    async (revisionId: string) => {
      if (!workOrderId || rejectionReason.trim().length === 0) return;
      await safeRequestWithNotify(
        rejectMutation.mutateAsync({
          workOrderId,
          revisionId,
          input: { reason: rejectionReason.trim() },
        }),
        { successMessage: t('GlassEnclosure.WorkOrder.Revision.Toast.Rejected') },
      );
      setRejectionReason('');
      setActiveRejectionId(null);
    },
    [rejectMutation, rejectionReason, t, workOrderId],
  );

  if (!workOrderId) {
    return (
      <section
        className={cn('flex h-full flex-col items-center justify-center p-4', className)}
        aria-label={t('GlassEnclosure.WorkOrder.Revision.Title')}
      >
        <p className="text-xs text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.WorkOrder.Revision.Empty')}
        </p>
      </section>
    );
  }

  return (
    <section
      className={cn('flex h-full flex-col bg-white dark:bg-slate-900', className)}
      aria-label={t('GlassEnclosure.WorkOrder.Revision.Title')}
    >
      <header className="flex items-center justify-between border-b border-slate-200 px-3 py-2 dark:border-slate-700">
        <h2 className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.WorkOrder.Revision.Title')}
        </h2>
      </header>

      {hasBlocking && (
        <div
          role="alert"
          className="flex items-start gap-2 border-b border-warning-200 bg-warning-50 px-3 py-2 text-xs text-warning-900 dark:border-warning-900/40 dark:bg-warning-950/40 dark:text-warning-200"
        >
          <AlertTriangle size={14} className="mt-0.5 shrink-0" aria-hidden />
          <span className="font-medium">
            {isSimple
              ? t('GlassEnclosure.WorkOrder.Revision.Banner.Simple')
              : t('GlassEnclosure.WorkOrder.Revision.Banner.Pro')}
          </span>
        </div>
      )}

      <div className="flex-1 overflow-auto">
        {query.isLoading && (
          <div className="px-3 py-2 text-xs text-slate-500 dark:text-slate-400">
            {t('Common.Loading')}
          </div>
        )}
        {!query.isLoading && revisions.length === 0 && (
          <div className="px-3 py-2 text-xs text-slate-500 dark:text-slate-400">
            {t('GlassEnclosure.WorkOrder.Revision.Empty')}
          </div>
        )}
        {revisions.length > 0 && (
          <table className="w-full table-fixed text-left text-xs">
            <thead className="border-b border-slate-200 bg-slate-50 text-[10px] uppercase tracking-wide text-slate-500 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2 font-semibold">
                  {t('GlassEnclosure.WorkOrder.Revision.Number')}
                </th>
                <th className="px-3 py-2 font-semibold">
                  {t('GlassEnclosure.WorkOrder.Revision.DeltaPercent')}
                </th>
                <th className="px-3 py-2 font-semibold">
                  {t('GlassEnclosure.WorkOrder.Revision.CreatedAt')}
                </th>
                <th
                  className="px-3 py-2 font-semibold"
                  aria-label={t('GlassEnclosure.WorkOrder.Revision.StatusColumn', {
                    defaultValue: 'Status',
                  })}
                />
                <th
                  className="px-3 py-2 font-semibold"
                  aria-label={t('GlassEnclosure.WorkOrder.Revision.ActionsColumn', {
                    defaultValue: 'Actions',
                  })}
                />
              </tr>
            </thead>
            <tbody>
              {revisions.map((revision) => {
                const canAct =
                  revision.status === 'PendingApproval' || revision.status === 'Blocked';
                const isOverrideOpen = activeOverrideId === revision.id;
                const isRejectionOpen = activeRejectionId === revision.id;
                return (
                  <tr
                    key={revision.id}
                    className="border-b border-slate-100 align-top last:border-b-0 dark:border-slate-800"
                  >
                    <td className="px-3 py-2 text-slate-700 dark:text-slate-200">
                      #{revision.revisionNumber}
                    </td>
                    <td className="px-3 py-2 text-slate-700 dark:text-slate-200">
                      {formatPercent(revision.deltaPercent)}
                    </td>
                    <td className="px-3 py-2 text-slate-500 dark:text-slate-400">
                      {formatDate(revision.createdAtUtc)}
                    </td>
                    <td className="px-3 py-2">
                      <span
                        className={cn(
                          'inline-flex rounded-full px-2 py-0.5 text-[10px] font-semibold',
                          STATUS_BADGE_CLASSES[revision.status],
                        )}
                      >
                        {t(`GlassEnclosure.WorkOrder.Revision.${revision.status}`)}
                      </span>
                    </td>
                    <td className="px-3 py-2">
                      {canAct && (
                        <div className="flex flex-col gap-1">
                          {isSimple ? (
                            <button
                              type="button"
                              onClick={() => handleApprove(revision.id)}
                              disabled={approveMutation.isPending}
                              className="inline-flex items-center justify-center gap-1 rounded-md bg-primary-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-primary-700 disabled:opacity-50"
                            >
                              <Check size={12} aria-hidden />
                              {t('GlassEnclosure.WorkOrder.Revision.Banner.Action')}
                            </button>
                          ) : (
                            <div className="flex flex-col gap-1">
                              <div className="flex gap-1">
                                <button
                                  type="button"
                                  onClick={() => {
                                    setActiveOverrideId(isOverrideOpen ? null : revision.id);
                                    setActiveRejectionId(null);
                                  }}
                                  className="inline-flex items-center gap-1 rounded-md border border-success-600 px-2 py-1 text-[11px] font-semibold text-success-700 hover:bg-success-50 dark:border-success-500 dark:text-success-300 dark:hover:bg-success-900/40"
                                >
                                  <Check size={12} aria-hidden />
                                  {t('GlassEnclosure.WorkOrder.Revision.Approve')}
                                </button>
                                <button
                                  type="button"
                                  onClick={() => {
                                    setActiveRejectionId(isRejectionOpen ? null : revision.id);
                                    setActiveOverrideId(null);
                                  }}
                                  className="inline-flex items-center gap-1 rounded-md border border-danger-600 px-2 py-1 text-[11px] font-semibold text-danger-700 hover:bg-danger-50 dark:border-danger-500 dark:text-danger-300 dark:hover:bg-danger-900/40"
                                >
                                  <X size={12} aria-hidden />
                                  {t('GlassEnclosure.WorkOrder.Revision.Reject')}
                                </button>
                              </div>
                              {isOverrideOpen && (
                                <div className="flex flex-col gap-1">
                                  <label
                                    htmlFor={`override-reason-${revision.id}`}
                                    className="text-[10px] uppercase tracking-wide text-slate-500 dark:text-slate-400"
                                  >
                                    {t('GlassEnclosure.WorkOrder.Revision.OverrideReason')}
                                  </label>
                                  <input
                                    id={`override-reason-${revision.id}`}
                                    type="text"
                                    value={overrideReason}
                                    onChange={(e) => setOverrideReason(e.target.value)}
                                    placeholder={
                                      t(
                                        'GlassEnclosure.WorkOrder.Revision.OverrideReasonPlaceholder',
                                      ) as string
                                    }
                                    className="rounded-md border border-slate-200 px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
                                  />
                                  <button
                                    type="button"
                                    onClick={() => handleApprove(revision.id)}
                                    disabled={approveMutation.isPending}
                                    className="self-start rounded-md bg-success-600 px-3 py-1 text-[11px] font-semibold text-white hover:bg-success-700 disabled:opacity-50"
                                  >
                                    {t('GlassEnclosure.WorkOrder.Revision.Approve')}
                                  </button>
                                </div>
                              )}
                              {isRejectionOpen && (
                                <div className="flex flex-col gap-1">
                                  <label
                                    htmlFor={`rejection-reason-${revision.id}`}
                                    className="text-[10px] uppercase tracking-wide text-slate-500 dark:text-slate-400"
                                  >
                                    {t('GlassEnclosure.WorkOrder.Revision.RejectionReason')}
                                  </label>
                                  <input
                                    id={`rejection-reason-${revision.id}`}
                                    type="text"
                                    value={rejectionReason}
                                    onChange={(e) => setRejectionReason(e.target.value)}
                                    placeholder={
                                      t(
                                        'GlassEnclosure.WorkOrder.Revision.RejectionReasonPlaceholder',
                                      ) as string
                                    }
                                    className="rounded-md border border-slate-200 px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
                                  />
                                  <button
                                    type="button"
                                    onClick={() => handleReject(revision.id)}
                                    disabled={
                                      rejectMutation.isPending ||
                                      rejectionReason.trim().length === 0
                                    }
                                    className="self-start rounded-md bg-danger-600 px-3 py-1 text-[11px] font-semibold text-white hover:bg-danger-700 disabled:opacity-50"
                                  >
                                    {t('GlassEnclosure.WorkOrder.Revision.Reject')}
                                  </button>
                                </div>
                              )}
                            </div>
                          )}
                        </div>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>
    </section>
  );
};

export default RevisionsPanel;
