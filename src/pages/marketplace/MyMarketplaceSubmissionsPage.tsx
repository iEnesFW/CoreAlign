import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ClipboardList, Upload } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { EmptyState } from '@/shared/ui/EmptyState/EmptyState';
import { SubmitTemplateModal } from '@/features/marketplace/ui/SubmitTemplateModal';
import { useMySubmissionsQuery } from '@/features/marketplace/hooks/useMarketplace';
import type { ProjectTemplateVisibility } from '@/features/marketplace/api/marketplaceApi';

const visibilityBadge: Record<ProjectTemplateVisibility, string> = {
  Private: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
  Pending: 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-300',
  Public: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-300',
  Rejected: 'bg-rose-100 text-rose-800 dark:bg-rose-900/30 dark:text-rose-300',
};

const formatDate = (iso: string | null): string => {
  if (!iso) return '-';
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleDateString();
};

export const MyMarketplaceSubmissionsPage = () => {
  const { t } = useTranslation();
  const query = useMySubmissionsQuery();
  const [submitOpen, setSubmitOpen] = useState(false);

  return (
    <main className="space-y-4 p-4">
      <PageHeader
        icon={<ClipboardList size={20} />}
        eyebrow={t('Marketplace.MySubmissions.Eyebrow', 'Marketplace')}
        title={t('Marketplace.MySubmissions.Title', 'My submissions')}
        subtitle={t(
          'Marketplace.MySubmissions.Subtitle',
          'Track templates you submitted to the community marketplace.',
        )}
        actions={
          <button
            type="button"
            onClick={() => setSubmitOpen(true)}
            className="inline-flex items-center gap-2 rounded-md bg-emerald-600 px-3 py-1.5 text-sm font-semibold text-white hover:bg-emerald-700"
          >
            <Upload size={14} />
            {t('Marketplace.MySubmissions.New', 'Submit another')}
          </button>
        }
      />

      {query.isError ? (
        <QueryError
          description={t('Marketplace.MySubmissions.LoadFailed', 'Failed to load submissions')}
          onRetry={() => query.refetch()}
        />
      ) : query.isLoading ? (
        <EmptyState title={t('common.loading', 'Loading...')} variant="plain" />
      ) : (query.data ?? []).length === 0 ? (
        <EmptyState
          title={t('Marketplace.MySubmissions.EmptyTitle', 'No submissions yet')}
          description={t(
            'Marketplace.MySubmissions.EmptyDescription',
            'Submit your own template to start sharing with the community.',
          )}
        />
      ) : (
        <div className="overflow-x-auto rounded-md border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-900">
          <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
            <thead className="bg-slate-50 dark:bg-slate-800">
              <tr className="text-left text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">
                <th className="px-3 py-2">{t('Marketplace.MySubmissions.Code', 'Code')}</th>
                <th className="px-3 py-2">{t('Marketplace.MySubmissions.Name', 'Name')}</th>
                <th className="px-3 py-2">{t('Marketplace.MySubmissions.Status', 'Status')}</th>
                <th className="px-3 py-2">
                  {t('Marketplace.MySubmissions.SubmittedAt', 'Submitted')}
                </th>
                <th className="px-3 py-2">
                  {t('Marketplace.MySubmissions.PublishedAt', 'Published')}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('Marketplace.MySubmissions.Downloads', 'Downloads')}
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
                    <td className="px-3 py-2">
                      <span
                        className={`inline-flex rounded-full px-2 py-0.5 text-[10px] font-medium uppercase tracking-wide ${
                          visibilityBadge[submission.visibility]
                        }`}
                      >
                        {submission.visibility}
                      </span>
                      {submission.visibility === 'Rejected' && submission.rejectionReason && (
                        <p className="mt-1 max-w-xs text-[11px] text-rose-600 dark:text-rose-400">
                          {submission.rejectionReason}
                        </p>
                      )}
                    </td>
                    <td className="px-3 py-2 text-slate-600 dark:text-slate-300">
                      {formatDate(submission.submittedAtUtc)}
                    </td>
                    <td className="px-3 py-2 text-slate-600 dark:text-slate-300">
                      {formatDate(submission.publishedAtUtc)}
                    </td>
                    <td className="px-3 py-2 text-right text-slate-800 dark:text-slate-200">
                      {submission.downloadCount}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <SubmitTemplateModal
        open={submitOpen}
        onClose={() => setSubmitOpen(false)}
        onSubmitted={() => query.refetch()}
      />
    </main>
  );
};

export default MyMarketplaceSubmissionsPage;
