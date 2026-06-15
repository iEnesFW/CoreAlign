import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { ShieldCheck, X } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { EmptyState } from '@/shared/ui/EmptyState/EmptyState';
import {
  usePendingSubmissionsQuery,
  usePublishTemplateMutation,
  useRejectTemplateMutation,
} from '@/features/marketplace/hooks/useMarketplace';
import type { MarketplaceSubmissionDto } from '@/features/marketplace/api/marketplaceApi';

const formatDate = (iso: string | null): string => {
  if (!iso) return '-';
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleDateString();
};

interface RejectModalState {
  templateId: string;
  reason: string;
}

export const AdminReviewQueuePage = () => {
  const { t } = useTranslation();
  const query = usePendingSubmissionsQuery();
  const publishMutation = usePublishTemplateMutation();
  const rejectMutation = useRejectTemplateMutation();
  const [rejectModal, setRejectModal] = useState<RejectModalState | null>(null);

  const handlePublish = async (submission: MarketplaceSubmissionDto) => {
    try {
      await publishMutation.mutateAsync({ templateId: submission.id });
      toast.success(t('Marketplace.Admin.Published', 'Template published'));
    } catch {
      toast.error(t('Marketplace.Admin.PublishFailed', 'Failed to publish template'));
    }
  };

  const handleReject = async () => {
    if (!rejectModal) return;
    if (!rejectModal.reason.trim()) {
      toast.error(t('Marketplace.Admin.RejectReasonRequired', 'Rejection reason is required'));
      return;
    }
    try {
      await rejectMutation.mutateAsync({
        templateId: rejectModal.templateId,
        reason: rejectModal.reason.trim(),
      });
      toast.success(t('Marketplace.Admin.Rejected', 'Submission rejected'));
      setRejectModal(null);
    } catch {
      toast.error(t('Marketplace.Admin.RejectFailed', 'Failed to reject submission'));
    }
  };

  return (
    <main className="space-y-4 p-4">
      <PageHeader
        icon={<ShieldCheck size={20} />}
        eyebrow={t('Marketplace.Admin.Eyebrow', 'Platform admin')}
        title={t('Marketplace.Admin.Title', 'Marketplace review queue')}
        subtitle={t(
          'Marketplace.Admin.Subtitle',
          'Approve or reject community submissions before they go public.',
        )}
      />

      {query.isError ? (
        <QueryError
          description={t('Marketplace.Admin.LoadFailed', 'Failed to load pending submissions')}
          onRetry={() => query.refetch()}
        />
      ) : query.isLoading ? (
        <EmptyState title={t('common.loading', 'Loading...')} variant="plain" />
      ) : (query.data ?? []).length === 0 ? (
        <EmptyState
          title={t('Marketplace.Admin.EmptyTitle', 'Queue is empty')}
          description={t(
            'Marketplace.Admin.EmptyDescription',
            'There are no pending marketplace submissions.',
          )}
        />
      ) : (
        <div className="overflow-x-auto rounded-md border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-900">
          <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
            <thead className="bg-slate-50 dark:bg-slate-800">
              <tr className="text-left text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">
                <th className="px-3 py-2">{t('Marketplace.Admin.Code', 'Code')}</th>
                <th className="px-3 py-2">{t('Marketplace.Admin.Name', 'Name')}</th>
                <th className="px-3 py-2">{t('Marketplace.Admin.SubmittedAt', 'Submitted')}</th>
                <th className="px-3 py-2 text-right">
                  {t('Marketplace.Admin.Actions', 'Actions')}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {(query.data ?? []).map((submission) => {
                const name = t(submission.displayNameKey, { defaultValue: submission.code });
                return (
                  <tr key={submission.id} className="text-sm">
                    <td className="px-3 py-2 font-mono text-xs text-slate-600 dark:text-slate-300">
                      {submission.code}
                    </td>
                    <td className="px-3 py-2 text-slate-800 dark:text-slate-100">{name}</td>
                    <td className="px-3 py-2 text-slate-600 dark:text-slate-300">
                      {formatDate(submission.submittedAtUtc)}
                    </td>
                    <td className="px-3 py-2 text-right">
                      <div className="inline-flex gap-2">
                        <button
                          type="button"
                          onClick={() => handlePublish(submission)}
                          disabled={publishMutation.isPending}
                          className="rounded-md bg-emerald-600 px-3 py-1 text-xs font-semibold text-white hover:bg-emerald-700 disabled:opacity-50"
                        >
                          {t('Marketplace.Admin.Publish', 'Publish')}
                        </button>
                        <button
                          type="button"
                          onClick={() => setRejectModal({ templateId: submission.id, reason: '' })}
                          className="rounded-md border border-rose-300 px-3 py-1 text-xs font-semibold text-rose-700 hover:bg-rose-50 dark:border-rose-700 dark:text-rose-300 dark:hover:bg-rose-900/30"
                        >
                          {t('Marketplace.Admin.Reject', 'Reject')}
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {rejectModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
          <div className="w-full max-w-md rounded-lg border border-slate-200 bg-white shadow-xl dark:border-slate-700 dark:bg-slate-900">
            <header className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-700">
              <h2 className="text-sm font-semibold text-slate-800 dark:text-slate-100">
                {t('Marketplace.Admin.RejectTitle', 'Reject submission')}
              </h2>
              <button
                type="button"
                onClick={() => setRejectModal(null)}
                className="rounded p-1 text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800"
              >
                <X size={14} />
              </button>
            </header>
            <div className="space-y-3 p-4">
              <label className="block text-xs">
                <span className="text-slate-600 dark:text-slate-300">
                  {t('Marketplace.Admin.RejectReason', 'Rejection reason')}
                </span>
                <textarea
                  value={rejectModal.reason}
                  onChange={(event) =>
                    setRejectModal((prev) =>
                      prev ? { ...prev, reason: event.target.value } : prev,
                    )
                  }
                  rows={3}
                  maxLength={1000}
                  className="mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm focus:border-rose-500 focus:outline-none dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
                />
              </label>
              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setRejectModal(null)}
                  className="rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-800"
                >
                  {t('Marketplace.Admin.Cancel', 'Cancel')}
                </button>
                <button
                  type="button"
                  onClick={handleReject}
                  disabled={rejectMutation.isPending}
                  className="rounded-md bg-rose-600 px-3 py-1.5 text-sm font-semibold text-white hover:bg-rose-700 disabled:opacity-50"
                >
                  {rejectMutation.isPending
                    ? t('Marketplace.Admin.Rejecting', 'Rejecting...')
                    : t('Marketplace.Admin.ConfirmReject', 'Reject')}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </main>
  );
};

export default AdminReviewQueuePage;
