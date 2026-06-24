import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';
import { toast } from 'sonner';
import { ArrowLeft, Download } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { EmptyState } from '@/shared/ui/EmptyState/EmptyState';
import {
  useMarketplaceDetailQuery,
  useMarketplaceReviewsQuery,
  useInstallTemplateMutation,
} from '@/features/marketplace/hooks/useMarketplace';
import { TemplateRatingStars } from '@/features/marketplace/ui/TemplateRatingStars';
import { TemplateReviewList } from '@/features/marketplace/ui/TemplateReviewList';
import { ReviewForm } from '@/features/marketplace/ui/ReviewForm';

const formatDate = (iso: string | null): string => {
  if (!iso) return '-';
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleDateString();
};

export const MarketplaceDetailPage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const detailQuery = useMarketplaceDetailQuery(id);
  const reviewsQuery = useMarketplaceReviewsQuery(id);
  const installMutation = useInstallTemplateMutation();

  if (!id) {
    return (
      <main className="p-4">
        <EmptyState
          title={t('Marketplace.Detail.MissingId', 'Template id missing')}
          variant="plain"
        />
      </main>
    );
  }

  if (detailQuery.isError) {
    return (
      <main className="p-4">
        <QueryError
          description={t('Marketplace.Detail.LoadFailed', 'Failed to load template')}
          onRetry={() => detailQuery.refetch()}
        />
      </main>
    );
  }

  if (detailQuery.isLoading || !detailQuery.data) {
    return (
      <main className="p-4">
        <EmptyState title={t('common.loading', 'Loading...')} variant="plain" />
      </main>
    );
  }

  const template = detailQuery.data;
  const description = template.descriptionKey
    ? (t(template.descriptionKey, { defaultValue: '' }) as string)
    : '';
  const name = t(template.displayNameKey, { defaultValue: template.code });

  const handleInstall = async () => {
    try {
      const result = await installMutation.mutateAsync(template.id);
      toast.success(t('Marketplace.Detail.Installed', 'Template installed'));
      if (result?.installedTemplateId) {
        navigate(
          `/dashboard/glass-enclosure/projects/new?templateId=${encodeURIComponent(
            result.installedTemplateId,
          )}`,
        );
      }
    } catch {
      toast.error(t('Marketplace.Detail.InstallFailed', 'Failed to install template'));
    }
  };

  return (
    <main className="space-y-4 p-4">
      <button
        type="button"
        onClick={() => navigate('/dashboard/marketplace')}
        className="inline-flex items-center gap-1 text-sm text-success-700 hover:text-success-900 dark:text-success-400"
      >
        <ArrowLeft size={14} />
        {t('Marketplace.Detail.Back', 'Back to marketplace')}
      </button>

      <PageHeader
        eyebrow={template.category}
        title={name}
        subtitle={description || t('Marketplace.Detail.NoDescription', 'No description provided.')}
        actions={
          <button
            type="button"
            onClick={handleInstall}
            disabled={installMutation.isPending}
            className="inline-flex items-center gap-2 rounded-md bg-success-600 px-4 py-2 text-sm font-semibold text-white hover:bg-success-700 disabled:opacity-50"
          >
            <Download size={14} />
            {installMutation.isPending
              ? t('Marketplace.Detail.Installing', 'Installing...')
              : t('Marketplace.Detail.Install', 'Install template')}
          </button>
        }
      />

      <section className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-900 lg:col-span-2">
          <div className="aspect-[16/9] bg-slate-100 dark:bg-slate-800">
            {template.thumbnailUrl ? (
              <img src={template.thumbnailUrl} alt={name} className="h-full w-full object-cover" />
            ) : (
              <div className="flex h-full w-full items-center justify-center text-sm text-slate-400">
                {t('Marketplace.Detail.NoPreview', 'No preview available')}
              </div>
            )}
          </div>
        </div>

        <aside className="space-y-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-900">
          <h2 className="text-sm font-semibold text-slate-800 dark:text-slate-100">
            {t('Marketplace.Detail.Specs', 'Specifications')}
          </h2>
          <dl className="grid grid-cols-2 gap-x-3 gap-y-2 text-xs">
            <dt className="text-slate-500 dark:text-slate-400">
              {t('Marketplace.Detail.Subtype', 'Subtype')}
            </dt>
            <dd className="text-slate-800 dark:text-slate-200">{template.subtype}</dd>
            <dt className="text-slate-500 dark:text-slate-400">
              {t('Marketplace.Detail.GeometryMode', 'Geometry')}
            </dt>
            <dd className="text-slate-800 dark:text-slate-200">{template.geometryMode}</dd>
            <dt className="text-slate-500 dark:text-slate-400">
              {t('Marketplace.Detail.Mounting', 'Mounting')}
            </dt>
            <dd className="text-slate-800 dark:text-slate-200">{template.mountingTopology}</dd>
            <dt className="text-slate-500 dark:text-slate-400">
              {t('Marketplace.Detail.Connector', 'Connector')}
            </dt>
            <dd className="text-slate-800 dark:text-slate-200">{template.defaultConnectorKind}</dd>
            <dt className="text-slate-500 dark:text-slate-400">
              {t('Marketplace.Detail.RunPresets', 'Run presets')}
            </dt>
            <dd className="text-slate-800 dark:text-slate-200">{template.runPresetCount}</dd>
            <dt className="text-slate-500 dark:text-slate-400">
              {t('Marketplace.Detail.Downloads', 'Downloads')}
            </dt>
            <dd className="text-slate-800 dark:text-slate-200">{template.downloadCount}</dd>
            <dt className="text-slate-500 dark:text-slate-400">
              {t('Marketplace.Detail.PublishedAt', 'Published')}
            </dt>
            <dd className="text-slate-800 dark:text-slate-200">
              {formatDate(template.publishedAtUtc)}
            </dd>
          </dl>
          <div className="border-t border-slate-200 pt-3 dark:border-slate-700">
            <TemplateRatingStars
              rating={template.averageRating}
              reviewCount={template.reviewCount}
            />
          </div>
        </aside>
      </section>

      <section className="space-y-3">
        <h2 className="text-sm font-semibold text-slate-800 dark:text-slate-100">
          {t('Marketplace.Reviews.Title', 'Reviews')}
        </h2>
        <ReviewForm templateId={template.id} />
        <TemplateReviewList reviews={reviewsQuery.data ?? []} isLoading={reviewsQuery.isLoading} />
      </section>
    </main>
  );
};

export default MarketplaceDetailPage;
