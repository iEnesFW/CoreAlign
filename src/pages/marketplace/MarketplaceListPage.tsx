import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Store, Upload } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { EmptyState } from '@/shared/ui/EmptyState/EmptyState';
import { MarketplaceCard } from '@/features/marketplace/ui/MarketplaceCard';
import { SubmitTemplateModal } from '@/features/marketplace/ui/SubmitTemplateModal';
import { useMarketplaceListQuery } from '@/features/marketplace/hooks/useMarketplace';
import type {
  MarketplaceListParams,
  MarketplaceSortBy,
} from '@/features/marketplace/api/marketplaceApi';
import type { EnclosureCategory } from '@/features/glass-enclosure/model/project.types';

const CATEGORIES: ReadonlyArray<EnclosureCategory> = [
  'Vertical',
  'HorizontalOrPitched',
  'Functional',
  'Special',
];

const SORTS: ReadonlyArray<MarketplaceSortBy> = ['Popularity', 'Newest', 'Rating', 'Alphabetical'];

const MIN_RATING_OPTIONS: ReadonlyArray<number> = [0, 3, 4, 4.5];

export const MarketplaceListPage = () => {
  const { t } = useTranslation();
  const [category, setCategory] = useState<EnclosureCategory | ''>('');
  const [sortBy, setSortBy] = useState<MarketplaceSortBy>('Popularity');
  const [minRating, setMinRating] = useState<number>(0);
  const [submitOpen, setSubmitOpen] = useState(false);

  const params: MarketplaceListParams = useMemo(
    () => ({
      category: category || undefined,
      sortBy,
      skip: 0,
      take: 48,
    }),
    [category, sortBy],
  );

  const query = useMarketplaceListQuery(params);

  const filteredTemplates = useMemo(() => {
    if (!query.data) return [];
    if (minRating <= 0) return query.data;
    return query.data.filter((tpl) => (tpl.averageRating ?? 0) >= minRating);
  }, [query.data, minRating]);

  return (
    <main className="space-y-4 p-4">
      <PageHeader
        icon={<Store size={20} />}
        eyebrow={t('Marketplace.List.Eyebrow', 'Community')}
        title={t('Marketplace.List.Title', 'Template marketplace')}
        subtitle={t(
          'Marketplace.List.Subtitle',
          'Browse community templates and install them into your workspace.',
        )}
        actions={
          <button
            type="button"
            onClick={() => setSubmitOpen(true)}
            className="inline-flex items-center gap-2 rounded-md bg-emerald-600 px-3 py-1.5 text-sm font-semibold text-white hover:bg-emerald-700"
          >
            <Upload size={14} />
            {t('Marketplace.List.SubmitButton', 'Submit a template')}
          </button>
        }
      />

      <section className="flex flex-wrap items-end gap-3 rounded-md border border-slate-200 bg-white p-3 dark:border-slate-700 dark:bg-slate-900">
        <label className="flex flex-col text-xs">
          <span className="text-slate-600 dark:text-slate-300">
            {t('Marketplace.Filters.Category', 'Category')}
          </span>
          <select
            value={category}
            onChange={(event) => setCategory(event.target.value as EnclosureCategory | '')}
            className="mt-1 rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
          >
            <option value="">{t('Marketplace.Filters.AllCategories', 'All categories')}</option>
            {CATEGORIES.map((cat) => (
              <option key={cat} value={cat}>
                {cat}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col text-xs">
          <span className="text-slate-600 dark:text-slate-300">
            {t('Marketplace.Filters.Sort', 'Sort by')}
          </span>
          <select
            value={sortBy}
            onChange={(event) => setSortBy(event.target.value as MarketplaceSortBy)}
            className="mt-1 rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
          >
            {SORTS.map((sort) => (
              <option key={sort} value={sort}>
                {t(`Marketplace.Filters.Sort.${sort}`, sort)}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col text-xs">
          <span className="text-slate-600 dark:text-slate-300">
            {t('Marketplace.Filters.MinRating', 'Minimum rating')}
          </span>
          <select
            value={minRating}
            onChange={(event) => setMinRating(Number(event.target.value))}
            className="mt-1 rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
          >
            {MIN_RATING_OPTIONS.map((value) => (
              <option key={value} value={value}>
                {value === 0 ? t('Marketplace.Filters.AnyRating', 'Any') : `${value.toFixed(1)}+`}
              </option>
            ))}
          </select>
        </label>
      </section>

      {query.isError ? (
        <QueryError
          description={t('Marketplace.List.LoadFailed', 'Failed to load templates')}
          onRetry={() => query.refetch()}
        />
      ) : query.isLoading ? (
        <EmptyState title={t('common.loading', 'Loading...')} variant="plain" />
      ) : filteredTemplates.length === 0 ? (
        <EmptyState
          title={t('Marketplace.List.EmptyTitle', 'No templates match these filters')}
          description={t(
            'Marketplace.List.EmptyDescription',
            'Try a different category or lower the minimum rating.',
          )}
        />
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {filteredTemplates.map((tpl) => (
            <MarketplaceCard key={tpl.id} template={tpl} />
          ))}
        </div>
      )}

      <SubmitTemplateModal open={submitOpen} onClose={() => setSubmitOpen(false)} />
    </main>
  );
};

export default MarketplaceListPage;
