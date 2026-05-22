import { useMemo, useState } from 'react';
import { ArrowDown, ArrowUp, ArrowUpDown } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { EmptyState } from '@/shared/ui/EmptyState/EmptyState';
import { TableSkeleton } from '@/shared/ui/Skeleton/Skeleton';

export type SortDir = 'asc' | 'desc';

export interface SortState<K extends string = string> {
  key: K;
  dir: SortDir;
}

export interface DataTableColumn<T, K extends string = string> {
  key: K;
  label: React.ReactNode;
  width?: string;
  align?: 'left' | 'right' | 'center';
  sortable?: boolean;
  sortValue?: (row: T) => string | number | null | undefined;
  cell: (row: T, ctx: { rowIndex: number }) => React.ReactNode;
  cellClassName?: string;
  headerClassName?: string;
  hideOnMobile?: boolean;
  sticky?: 'left' | 'right';
}

export type Density = 'compact' | 'comfortable';

interface Props<T, K extends string = string> {
  rows: T[];
  columns: DataTableColumn<T, K>[];
  getRowId: (row: T) => string;
  isLoading?: boolean;
  density?: Density;
  selectedId?: string | null;
  onRowClick?: (row: T) => void;
  onRowDoubleClick?: (row: T) => void;
  rowActions?: (row: T) => React.ReactNode;
  rowActionsHeader?: React.ReactNode;
  rowClassName?: (row: T) => string | undefined;
  emptyTitle?: string;
  emptyDescription?: string;
  emptyIcon?: React.ReactNode;
  emptyAction?: React.ReactNode;
  externalSort?: SortState<K>;
  onSortChange?: (sort: SortState<K> | null) => void;
  className?: string;
  stickyHeader?: boolean;
  zebra?: boolean;
  /** Opt-in row selection (checkbox column). */
  selectable?: boolean;
  selectedIds?: string[];
  onSelectionChange?: (ids: string[]) => void;
}

export function DataTable<T, K extends string = string>({
  rows,
  columns,
  getRowId,
  isLoading,
  density = 'compact',
  selectedId,
  onRowClick,
  onRowDoubleClick,
  rowActions,
  rowActionsHeader,
  rowClassName,
  emptyTitle,
  emptyDescription,
  emptyIcon,
  emptyAction,
  externalSort,
  onSortChange,
  className,
  stickyHeader = true,
  zebra = false,
  selectable = false,
  selectedIds,
  onSelectionChange,
}: Props<T, K>) {
  const [internalSort, setInternalSort] = useState<SortState<K> | null>(null);
  const sort = externalSort ?? internalSort;

  const selectedSet = useMemo(() => new Set(selectedIds ?? []), [selectedIds]);
  const toggleOne = (id: string) => {
    if (!onSelectionChange) return;
    const next = new Set(selectedSet);
    if (next.has(id)) next.delete(id);
    else next.add(id);
    onSelectionChange([...next]);
  };

  const setSort = (next: SortState<K> | null) => {
    if (onSortChange) onSortChange(next);
    else setInternalSort(next);
  };

  const toggleSort = (key: K) => {
    if (!sort || sort.key !== key) {
      setSort({ key, dir: 'asc' });
      return;
    }
    if (sort.dir === 'asc') {
      setSort({ key, dir: 'desc' });
    } else {
      setSort(null);
    }
  };

  // Resolve the sort accessor outside the memo so the memo only re-runs when the
  // *active sort* changes (or rows do) — not on every parent re-render which
  // would normally produce a fresh `columns` array and waste a sort pass.
  const sortValueFn = sort ? (columns.find((c) => c.key === sort.key)?.sortValue ?? null) : null;

  const sortedRows = useMemo(() => {
    if (!sort || !sortValueFn) return rows;
    const factor = sort.dir === 'asc' ? 1 : -1;
    return [...rows].sort((a, b) => {
      const av = sortValueFn(a) ?? null;
      const bv = sortValueFn(b) ?? null;
      if (av === null && bv === null) return 0;
      if (av === null) return 1;
      if (bv === null) return -1;
      if (typeof av === 'number' && typeof bv === 'number') {
        return (av - bv) * factor;
      }
      return String(av).localeCompare(String(bv)) * factor;
    });
  }, [rows, sort, sortValueFn]);

  if (isLoading && rows.length === 0) {
    return (
      <TableSkeleton
        rows={8}
        columns={columns.length + (rowActions ? 1 : 0)}
        className={className}
      />
    );
  }

  if (rows.length === 0) {
    return (
      <EmptyState
        icon={emptyIcon}
        title={emptyTitle ?? 'No data'}
        description={emptyDescription}
        action={emptyAction}
        className={className}
      />
    );
  }

  const padY = density === 'compact' ? 'py-2' : 'py-3';
  const padX = 'px-3';

  const pageIds = sortedRows.map(getRowId);
  const allSelected =
    selectable && pageIds.length > 0 && pageIds.every((id) => selectedSet.has(id));
  const someSelected = selectable && pageIds.some((id) => selectedSet.has(id)) && !allSelected;
  const toggleAll = () => {
    if (!onSelectionChange) return;
    if (allSelected) {
      onSelectionChange((selectedIds ?? []).filter((id) => !pageIds.includes(id)));
    } else {
      onSelectionChange([...new Set([...(selectedIds ?? []), ...pageIds])]);
    }
  };

  return (
    <div
      className={cn(
        'overflow-hidden rounded-xl border border-slate-200/70 bg-white shadow-sm dark:border-slate-800/70 dark:bg-slate-900 animate-fade-in',
        className,
      )}
    >
      <div className="relative overflow-x-auto">
        <table className="w-full text-left text-xs sm:text-sm">
          <thead
            className={cn(
              'bg-slate-50/80 text-[10px] uppercase tracking-[0.12em] text-slate-500 dark:bg-slate-900/40 dark:text-slate-400',
              stickyHeader && 'sticky top-0 z-10 backdrop-blur',
            )}
          >
            <tr className="border-b border-slate-200/70 dark:border-slate-800/70">
              {selectable && (
                <th scope="col" className={cn('w-8', padX, padY)}>
                  <input
                    type="checkbox"
                    aria-label="Select all"
                    className="h-3.5 w-3.5 rounded border-slate-300 text-indigo-600"
                    checked={allSelected}
                    ref={(el) => {
                      if (el) el.indeterminate = someSelected;
                    }}
                    onChange={toggleAll}
                  />
                </th>
              )}
              {columns.map((col) => {
                const isSorted = sort?.key === col.key;
                const align = col.align ?? 'left';
                const stickyClass =
                  col.sticky === 'left'
                    ? 'sticky left-0 z-10 bg-slate-50/95 dark:bg-slate-900/95'
                    : col.sticky === 'right'
                      ? 'sticky right-0 z-10 bg-slate-50/95 dark:bg-slate-900/95'
                      : '';
                // a11y: announce sort state to assistive tech via aria-sort.
                const ariaSort: 'ascending' | 'descending' | 'none' | undefined =
                  col.sortable && col.sortValue
                    ? isSorted
                      ? sort!.dir === 'asc'
                        ? 'ascending'
                        : 'descending'
                      : 'none'
                    : undefined;
                return (
                  <th
                    key={col.key}
                    scope="col"
                    aria-sort={ariaSort}
                    style={col.width ? { width: col.width } : undefined}
                    className={cn(
                      'whitespace-nowrap font-semibold',
                      padX,
                      padY,
                      align === 'right' && 'text-right',
                      align === 'center' && 'text-center',
                      col.hideOnMobile && 'hidden sm:table-cell',
                      stickyClass,
                      col.headerClassName,
                    )}
                  >
                    {col.sortable && col.sortValue ? (
                      <button
                        type="button"
                        onClick={() => toggleSort(col.key)}
                        className={cn(
                          'inline-flex items-center gap-1 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 rounded',
                          align === 'right' && 'flex-row-reverse',
                          isSorted
                            ? 'text-indigo-600 dark:text-indigo-300'
                            : 'hover:text-slate-700 dark:hover:text-slate-200',
                        )}
                      >
                        <span>{col.label}</span>
                        {isSorted ? (
                          sort!.dir === 'asc' ? (
                            <ArrowUp size={10} strokeWidth={3} aria-hidden />
                          ) : (
                            <ArrowDown size={10} strokeWidth={3} aria-hidden />
                          )
                        ) : (
                          <ArrowUpDown size={10} className="opacity-40" aria-hidden />
                        )}
                      </button>
                    ) : (
                      col.label
                    )}
                  </th>
                );
              })}
              {rowActions && (
                <th
                  className={cn(
                    'sticky right-0 z-10 bg-slate-50/95 text-right font-semibold dark:bg-slate-900/95',
                    padX,
                    padY,
                  )}
                >
                  {rowActionsHeader ?? <span className="sr-only">Actions</span>}
                </th>
              )}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200/60 dark:divide-slate-800/60">
            {sortedRows.map((row, rowIndex) => {
              const id = getRowId(row);
              const isSelected = selectedId === id;
              const extra = rowClassName?.(row);
              const clickable = !!onRowClick;
              return (
                <tr
                  key={id}
                  onClick={() => onRowClick?.(row)}
                  onDoubleClick={() => onRowDoubleClick?.(row)}
                  // a11y: clickable rows act like buttons — make them keyboard
                  // reachable and operable with Enter/Space.
                  tabIndex={clickable ? 0 : undefined}
                  role={clickable ? 'button' : undefined}
                  aria-selected={selectedId !== undefined ? isSelected : undefined}
                  onKeyDown={
                    clickable
                      ? (e) => {
                          if (e.key === 'Enter' || e.key === ' ') {
                            e.preventDefault();
                            onRowClick?.(row);
                          }
                        }
                      : undefined
                  }
                  className={cn(
                    'group transition-colors',
                    clickable &&
                      'cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-indigo-500',
                    isSelected
                      ? 'bg-indigo-50/70 dark:bg-indigo-500/10'
                      : zebra
                        ? rowIndex % 2 === 0
                          ? 'bg-white dark:bg-slate-900'
                          : 'bg-slate-50/40 dark:bg-slate-900/40'
                        : 'bg-white dark:bg-slate-900',
                    'hover:bg-indigo-50/40 dark:hover:bg-indigo-500/[0.06]',
                    extra,
                  )}
                >
                  {selectable && (
                    <td
                      className={cn('w-8 align-middle', padX, padY)}
                      onClick={(e) => e.stopPropagation()}
                    >
                      <input
                        type="checkbox"
                        aria-label="Select row"
                        className="h-3.5 w-3.5 rounded border-slate-300 text-indigo-600"
                        checked={selectedSet.has(id)}
                        onChange={() => toggleOne(id)}
                      />
                    </td>
                  )}
                  {columns.map((col) => {
                    const align = col.align ?? 'left';
                    const stickyClass =
                      col.sticky === 'left'
                        ? 'sticky left-0 z-[1] bg-inherit'
                        : col.sticky === 'right'
                          ? 'sticky right-0 z-[1] bg-inherit'
                          : '';
                    return (
                      <td
                        key={col.key}
                        className={cn(
                          'align-middle text-slate-700 dark:text-slate-200',
                          padX,
                          padY,
                          align === 'right' && 'text-right',
                          align === 'center' && 'text-center',
                          col.hideOnMobile && 'hidden sm:table-cell',
                          stickyClass,
                          col.cellClassName,
                        )}
                      >
                        {col.cell(row, { rowIndex })}
                      </td>
                    );
                  })}
                  {rowActions && (
                    <td
                      className={cn('sticky right-0 z-[1] bg-inherit text-right', padX, padY)}
                      onClick={(e) => e.stopPropagation()}
                    >
                      <div className="inline-flex items-center gap-0.5 opacity-60 transition-opacity group-hover:opacity-100">
                        {rowActions(row)}
                      </div>
                    </td>
                  )}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

interface RowActionButtonProps {
  icon: React.ReactNode;
  label: string;
  onClick?: () => void;
  to?: string;
  tone?: 'default' | 'danger';
  as?: 'button' | 'link';
}

export const RowActionButton = ({
  icon,
  label,
  onClick,
  tone = 'default',
}: RowActionButtonProps) => {
  const toneClass =
    tone === 'danger'
      ? 'text-slate-500 hover:bg-rose-50 hover:text-rose-600 dark:text-slate-400 dark:hover:bg-rose-500/10 dark:hover:text-rose-300'
      : 'text-slate-500 hover:bg-indigo-50 hover:text-indigo-600 dark:text-slate-400 dark:hover:bg-indigo-500/10 dark:hover:text-indigo-300';
  return (
    <button
      type="button"
      onClick={onClick}
      title={label}
      aria-label={label}
      className={cn(
        'rounded-md p-1.5 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500',
        toneClass,
      )}
    >
      {icon}
    </button>
  );
};
