import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useTranslation } from 'react-i18next';

export interface PaginationProps {
  page: number;
  pageSize: number;
  total: number;
  onPageChange: (page: number) => void;
  pageSizeOptions?: number[];
  onPageSizeChange?: (pageSize: number) => void;
  itemLabel?: string;
}

export const Pagination = ({
  page,
  pageSize,
  total,
  onPageChange,
  pageSizeOptions,
  onPageSizeChange,
  itemLabel,
}: PaginationProps) => {
  const { t } = useTranslation();
  const resolvedItemLabel = itemLabel ?? t('Common.ItemLabel', { defaultValue: 'kayıt' });
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  if (totalPages <= 1 && !pageSizeOptions) return null;

  return (
    <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-slate-500 dark:text-slate-400">
      <span>
        {t('Common.PaginationSummary', {
          defaultValue: '{{total}} {{itemLabel}} — sayfa {{page}} / {{totalPages}}',
          total,
          itemLabel: resolvedItemLabel,
          page,
          totalPages,
        })}
      </span>
      <div className="flex items-center gap-2">
        {pageSizeOptions && onPageSizeChange && (
          <select
            value={pageSize}
            onChange={(e) => onPageSizeChange(Number(e.target.value))}
            className="rounded border border-slate-200 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900"
            aria-label={t('Common.PageSize', { defaultValue: 'Sayfa boyutu' })}
          >
            {pageSizeOptions.map((size) => (
              <option key={size} value={size}>
                {t('Common.PageSizeOption', { defaultValue: '{{size}} / sayfa', size })}
              </option>
            ))}
          </select>
        )}
        <div className="flex gap-1">
          <button
            type="button"
            onClick={() => onPageChange(Math.max(1, page - 1))}
            disabled={page <= 1}
            aria-label={t('Common.Previous', { defaultValue: 'Önceki sayfa' })}
            className="inline-flex h-7 w-7 items-center justify-center rounded border border-slate-200 bg-white hover:bg-slate-50 disabled:opacity-30 dark:border-slate-700 dark:bg-slate-900"
          >
            <ChevronLeft size={14} />
          </button>
          <button
            type="button"
            onClick={() => onPageChange(Math.min(totalPages, page + 1))}
            disabled={page >= totalPages}
            aria-label={t('Common.Next', { defaultValue: 'Sonraki sayfa' })}
            className="inline-flex h-7 w-7 items-center justify-center rounded border border-slate-200 bg-white hover:bg-slate-50 disabled:opacity-30 dark:border-slate-700 dark:bg-slate-900"
          >
            <ChevronRight size={14} />
          </button>
        </div>
      </div>
    </div>
  );
};
