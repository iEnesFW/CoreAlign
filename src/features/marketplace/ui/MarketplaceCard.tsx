import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Download } from 'lucide-react';
import type { MarketplaceTemplateSummaryDto } from '../api/marketplaceApi';
import { TemplateRatingStars } from './TemplateRatingStars';

interface MarketplaceCardProps {
  template: MarketplaceTemplateSummaryDto;
  to?: string;
}

const formatNumber = (value: number): string =>
  new Intl.NumberFormat(undefined, { notation: 'compact' }).format(value);

export const MarketplaceCard = ({ template, to }: MarketplaceCardProps) => {
  const { t } = useTranslation();
  const href = to ?? `/dashboard/marketplace/${template.id}`;
  const name = t(template.displayNameKey, { defaultValue: template.code });
  const description = template.descriptionKey
    ? (t(template.descriptionKey, { defaultValue: '' }) as string)
    : '';

  return (
    <Link
      to={href}
      className="group flex h-full flex-col overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm transition hover:-translate-y-0.5 hover:shadow-md dark:border-slate-700 dark:bg-slate-900"
    >
      <div className="aspect-[16/10] w-full overflow-hidden bg-slate-100 dark:bg-slate-800">
        {template.thumbnailUrl ? (
          <img
            src={template.thumbnailUrl}
            alt={name}
            loading="lazy"
            className="h-full w-full object-cover transition group-hover:scale-105"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-xs text-slate-400">
            {t('Marketplace.Card.NoPreview', 'No preview')}
          </div>
        )}
      </div>
      <div className="flex flex-1 flex-col gap-2 p-4">
        <div className="flex items-start justify-between gap-2">
          <h3 className="line-clamp-1 text-sm font-semibold text-slate-800 dark:text-slate-100">
            {name}
          </h3>
          <span className="rounded-full bg-success-50 px-2 py-0.5 text-[10px] font-medium uppercase tracking-wide text-success-700 dark:bg-success-900/30 dark:text-success-300">
            {template.category}
          </span>
        </div>
        {description && (
          <p className="line-clamp-2 text-xs text-slate-500 dark:text-slate-400">{description}</p>
        )}
        <div className="mt-auto flex items-center justify-between pt-2 text-xs text-slate-500 dark:text-slate-400">
          <TemplateRatingStars rating={template.averageRating} reviewCount={template.reviewCount} />
          <span className="inline-flex items-center gap-1">
            <Download size={12} />
            {formatNumber(template.downloadCount)}
          </span>
        </div>
      </div>
    </Link>
  );
};
