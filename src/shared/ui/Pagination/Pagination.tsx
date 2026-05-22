import { ChevronLeft, ChevronRight } from 'lucide-react';

export interface PaginationProps {
  page: number;
  pageSize: number;
  total: number;
  onPageChange: (page: number) => void;
  /** Optional page-size selector. Omit to hide. */
  pageSizeOptions?: number[];
  onPageSizeChange?: (pageSize: number) => void;
  /** Label for the item noun, e.g. "kayıt", "record". */
  itemLabel?: string;
}

/**
 * Shared pager — replaces the bespoke prev/next blocks each list page rebuilt.
 * Renders nothing when there is a single page and no page-size selector.
 */
export const Pagination = ({
  page,
  pageSize,
  total,
  onPageChange,
  pageSizeOptions,
  onPageSizeChange,
  itemLabel = 'kayıt',
}: PaginationProps) => {
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  if (totalPages <= 1 && !pageSizeOptions) return null;

  return (
    <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-slate-500 dark:text-slate-400">
      <span>
        {total} {itemLabel} — sayfa {page} / {totalPages}
      </span>
      <div className="flex items-center gap-2">
        {pageSizeOptions && onPageSizeChange && (
          <select
            value={pageSize}
            onChange={(e) => onPageSizeChange(Number(e.target.value))}
            className="rounded border border-slate-200 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900"
            aria-label="Sayfa boyutu"
          >
            {pageSizeOptions.map((size) => (
              <option key={size} value={size}>
                {size} / sayfa
              </option>
            ))}
          </select>
        )}
        <div className="flex gap-1">
          <button
            type="button"
            onClick={() => onPageChange(Math.max(1, page - 1))}
            disabled={page <= 1}
            aria-label="Önceki sayfa"
            className="inline-flex h-7 w-7 items-center justify-center rounded border border-slate-200 bg-white hover:bg-slate-50 disabled:opacity-30 dark:border-slate-700 dark:bg-slate-900"
          >
            <ChevronLeft size={14} />
          </button>
          <button
            type="button"
            onClick={() => onPageChange(Math.min(totalPages, page + 1))}
            disabled={page >= totalPages}
            aria-label="Sonraki sayfa"
            className="inline-flex h-7 w-7 items-center justify-center rounded border border-slate-200 bg-white hover:bg-slate-50 disabled:opacity-30 dark:border-slate-700 dark:bg-slate-900"
          >
            <ChevronRight size={14} />
          </button>
        </div>
      </div>
    </div>
  );
};
