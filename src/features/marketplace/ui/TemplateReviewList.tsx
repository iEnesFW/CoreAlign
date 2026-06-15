import { useTranslation } from 'react-i18next';
import type { MarketplaceReviewDto } from '../api/marketplaceApi';
import { TemplateRatingStars } from './TemplateRatingStars';

interface TemplateReviewListProps {
  reviews: MarketplaceReviewDto[];
  isLoading?: boolean;
}

const formatDate = (iso: string): string => {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleDateString();
};

const shortenUser = (id: string): string => id.slice(0, 8);

export const TemplateReviewList = ({ reviews, isLoading = false }: TemplateReviewListProps) => {
  const { t } = useTranslation();

  if (isLoading) {
    return (
      <p className="text-sm text-slate-500 dark:text-slate-400">
        {t('Marketplace.Reviews.Loading', 'Loading reviews...')}
      </p>
    );
  }

  if (reviews.length === 0) {
    return (
      <p className="text-sm text-slate-500 dark:text-slate-400">
        {t('Marketplace.Reviews.Empty', 'No reviews yet.')}
      </p>
    );
  }

  return (
    <ul className="space-y-3">
      {reviews.map((review) => (
        <li
          key={review.id}
          className="rounded-md border border-slate-200 bg-white p-3 dark:border-slate-700 dark:bg-slate-900"
        >
          <header className="flex items-center justify-between gap-2">
            <div className="flex items-center gap-2">
              <TemplateRatingStars rating={review.ratingStars} size={12} />
              <span className="text-xs font-medium text-slate-600 dark:text-slate-300">
                {t('Marketplace.Reviews.User', 'User')} #{shortenUser(review.reviewerUserId)}
              </span>
            </div>
            <span className="text-[11px] text-slate-400 dark:text-slate-500">
              {formatDate(review.reviewedAtUtc)}
            </span>
          </header>
          {review.commentMd && (
            <p className="mt-2 whitespace-pre-wrap text-sm text-slate-700 dark:text-slate-200">
              {review.commentMd}
            </p>
          )}
        </li>
      ))}
    </ul>
  );
};
