import { Search, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';

interface Props {
  search?: {
    value: string;
    onChange: (v: string) => void;
    placeholder?: string;
    autoFocus?: boolean;
  };
  filters?: React.ReactNode;
  leading?: React.ReactNode;
  trailing?: React.ReactNode;
  sticky?: boolean;
  resultCount?: { count: number; label: string };
  onClearFilters?: () => void;
  hasActiveFilters?: boolean;
  className?: string;
  density?: React.ReactNode;
  viewMode?: React.ReactNode;
}

export const DataToolbar = ({
  search,
  filters,
  leading,
  trailing,
  sticky = false,
  resultCount,
  onClearFilters,
  hasActiveFilters,
  className,
  density,
  viewMode,
}: Props) => {
  const { t } = useTranslation();
  return (
    <div
      className={cn(
        'rounded-xl border border-slate-200/70 bg-white/85 p-3 shadow-sm backdrop-blur-sm dark:border-slate-800/70 dark:bg-slate-900/70',
        sticky && 'sticky top-0 z-30',
        'animate-fade-in',
        className,
      )}
    >
      <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center sm:justify-between">
        <div className="flex flex-1 flex-wrap items-center gap-2">
          {leading}
          {search && (
            <div className="relative min-w-[200px] flex-1 sm:max-w-xs">
              <Search
                size={14}
                className="pointer-events-none absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-400"
              />
              <input
                type="search"
                value={search.value}
                onChange={(e) => search.onChange(e.target.value)}
                placeholder={search.placeholder}
                autoFocus={search.autoFocus}
                className="w-full rounded-lg border border-slate-200 bg-white py-1.5 pl-8 pr-8 text-xs text-slate-900 transition-colors focus:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-500/20 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
              />
              {search.value && (
                <button
                  type="button"
                  onClick={() => search.onChange('')}
                  className="absolute right-2 top-1/2 -translate-y-1/2 rounded-md p-0.5 text-slate-400 hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-800"
                  aria-label={t('Common.Clear', { defaultValue: 'Clear' })}
                >
                  <X size={12} />
                </button>
              )}
            </div>
          )}
          {viewMode}
          {density}
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {resultCount && (
            <span className="text-[10px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
              <span className="font-bold text-slate-900 tabular-nums dark:text-slate-100">
                {resultCount.count}
              </span>{' '}
              {resultCount.label}
            </span>
          )}
          {trailing}
        </div>
      </div>

      {filters && (
        <div className="mt-2 flex flex-wrap items-center gap-1.5 border-t border-slate-100 pt-2 dark:border-slate-800">
          <span className="text-[10px] font-semibold uppercase tracking-[0.14em] text-slate-400">
            {t('Common.Filters', { defaultValue: 'Filters' })}
          </span>
          {filters}
          {hasActiveFilters && onClearFilters && (
            <button
              type="button"
              onClick={onClearFilters}
              className="ml-1 inline-flex items-center gap-0.5 rounded-full border border-transparent px-2 py-0.5 text-[10px] font-medium text-danger-600 hover:bg-danger-50 dark:text-danger-400 dark:hover:bg-danger-500/10"
            >
              <X size={10} />
              {t('Common.Clear', { defaultValue: 'Clear' })}
            </button>
          )}
        </div>
      )}
    </div>
  );
};
