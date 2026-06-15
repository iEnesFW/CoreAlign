import { Star } from 'lucide-react';
import { cn } from '@/shared/lib/cn';

interface TemplateRatingStarsProps {
  rating: number | null;
  reviewCount?: number;
  interactive?: boolean;
  onSelect?: (value: number) => void;
  size?: number;
  className?: string;
}

const MAX_STARS = 5;

export const TemplateRatingStars = ({
  rating,
  reviewCount,
  interactive = false,
  onSelect,
  size = 16,
  className,
}: TemplateRatingStarsProps) => {
  const value = rating ?? 0;
  return (
    <div
      className={cn('inline-flex items-center gap-1', className)}
      role={interactive ? 'radiogroup' : 'img'}
      aria-label={`Rating ${value.toFixed(1)} out of ${MAX_STARS}`}
    >
      <div className="inline-flex">
        {Array.from({ length: MAX_STARS }).map((_, index) => {
          const filled = index + 1 <= Math.round(value);
          const StarIcon = (
            <Star
              size={size}
              className={cn(
                'transition-colors',
                filled
                  ? 'fill-amber-400 stroke-amber-500'
                  : 'fill-transparent stroke-slate-300 dark:stroke-slate-600',
              )}
            />
          );
          if (!interactive) {
            return (
              <span key={index} className="px-0.5">
                {StarIcon}
              </span>
            );
          }
          return (
            <button
              key={index}
              type="button"
              role="radio"
              aria-checked={filled}
              onClick={() => onSelect?.(index + 1)}
              className="px-0.5 hover:scale-110 focus:outline-none focus-visible:ring-2 focus-visible:ring-emerald-500 rounded"
            >
              {StarIcon}
            </button>
          );
        })}
      </div>
      {rating !== null && (
        <span className="text-xs text-slate-600 dark:text-slate-400">
          {value.toFixed(1)}
          {typeof reviewCount === 'number' && reviewCount > 0 && (
            <span className="ml-1 text-slate-400 dark:text-slate-500">({reviewCount})</span>
          )}
        </span>
      )}
    </div>
  );
};
