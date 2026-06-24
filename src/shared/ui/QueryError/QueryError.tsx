import { useTranslation } from 'react-i18next';
import { AlertTriangle, RefreshCw } from 'lucide-react';
import { cn } from '@/shared/lib/cn';

interface Props {
  onRetry?: () => void;
  isRetrying?: boolean;
  title?: string;
  description?: string;
  className?: string;
  variant?: 'card' | 'plain';
}

export const QueryError = ({
  onRetry,
  isRetrying,
  title,
  description,
  className,
  variant = 'card',
}: Props) => {
  const { t } = useTranslation();
  return (
    <div
      className={cn(
        variant === 'card'
          ? 'rounded-xl border border-dashed border-danger-300 bg-danger-50/50 px-6 py-10 dark:border-danger-500/30 dark:bg-danger-500/5'
          : 'px-6 py-10',
        'flex flex-col items-center justify-center text-center',
        className,
      )}
      role="alert"
    >
      <div className="mb-3 flex h-12 w-12 items-center justify-center rounded-2xl bg-danger-500/10 text-danger-500 ring-1 ring-danger-200/60 dark:text-danger-300 dark:ring-danger-500/30">
        <AlertTriangle size={22} />
      </div>
      <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
        {title ?? t('errorState.title', { defaultValue: 'Something went wrong' })}
      </h3>
      <p className="mt-1 max-w-sm text-xs text-slate-500 dark:text-slate-400">
        {description ??
          t('errorState.description', {
            defaultValue: 'The data could not be loaded. Please try again.',
          })}
      </p>
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          disabled={isRetrying}
          className="mt-4 inline-flex items-center gap-1.5 rounded-lg bg-danger-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-danger-700 disabled:opacity-50"
        >
          <RefreshCw size={13} className={isRetrying ? 'animate-spin' : undefined} />
          {t('errorState.retry', { defaultValue: 'Retry' })}
        </button>
      )}
    </div>
  );
};
