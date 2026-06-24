import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { ShieldCheck, Ban } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { EmptyState } from '@/shared/ui/EmptyState/EmptyState';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Textarea } from '@/shared/ui/Textarea/Textarea';
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
                          className="rounded-md bg-success-600 px-3 py-1 text-xs font-semibold text-white hover:bg-success-700 disabled:opacity-50"
                        >
                          {t('Marketplace.Admin.Publish', 'Publish')}
                        </button>
                        <button
                          type="button"
                          onClick={() => setRejectModal({ templateId: submission.id, reason: '' })}
                          className="rounded-md border border-danger-300 px-3 py-1 text-xs font-semibold text-danger-700 hover:bg-danger-50 dark:border-danger-700 dark:text-danger-300 dark:hover:bg-danger-900/30"
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

      <Modal
        open={rejectModal !== null}
        title={t('Marketplace.Admin.RejectTitle', 'Reject submission')}
        icon={<Ban size={18} />}
        onClose={() => setRejectModal(null)}
        size="md"
        footer={
          <>
            <Button type="button" variant="ghost" onClick={() => setRejectModal(null)}>
              {t('Marketplace.Admin.Cancel', 'Cancel')}
            </Button>
            <Button
              type="button"
              variant="danger"
              onClick={handleReject}
              isLoading={rejectMutation.isPending}
            >
              {rejectMutation.isPending
                ? t('Marketplace.Admin.Rejecting', 'Rejecting...')
                : t('Marketplace.Admin.ConfirmReject', 'Reject')}
            </Button>
          </>
        }
      >
        <Textarea
          label={t('Marketplace.Admin.RejectReason', 'Rejection reason')}
          value={rejectModal?.reason ?? ''}
          onChange={(event) =>
            setRejectModal((prev) => (prev ? { ...prev, reason: event.target.value } : prev))
          }
          rows={3}
          maxLength={1000}
        />
      </Modal>
    </main>
  );
};

export default AdminReviewQueuePage;
