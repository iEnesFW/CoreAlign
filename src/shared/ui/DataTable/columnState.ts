import type { DataTableColumn } from './DataTable';

export interface ColumnState {
  order: string[];
  hidden: string[];
}

export interface ColumnMeta {
  key: string;
  label: string;
}

export const parseColumnState = (raw: unknown): ColumnState | null => {
  if (!raw || typeof raw !== 'object') return null;
  const record = raw as Record<string, unknown>;
  if (!Array.isArray(record.order) || !Array.isArray(record.hidden)) return null;
  return {
    order: record.order.filter((value): value is string => typeof value === 'string'),
    hidden: record.hidden.filter((value): value is string => typeof value === 'string'),
  };
};

export const normalizeColumnState = (
  state: ColumnState | null,
  allKeys: readonly string[],
): ColumnState => {
  const known = new Set(allKeys);
  const order = (state?.order ?? []).filter((key) => known.has(key));
  for (const key of allKeys) {
    if (!order.includes(key)) order.push(key);
  }
  const hidden = (state?.hidden ?? []).filter((key) => known.has(key));
  return { order, hidden };
};

export const applyColumnState = <T, K extends string>(
  columns: DataTableColumn<T, K>[],
  state: ColumnState | null | undefined,
): DataTableColumn<T, K>[] => {
  if (!state) return columns;
  const byKey = new Map(columns.map((column) => [column.key as string, column]));
  const hidden = new Set(state.hidden);
  const ordered: DataTableColumn<T, K>[] = [];
  for (const key of state.order) {
    const column = byKey.get(key);
    if (column && !hidden.has(key)) {
      ordered.push(column);
      byKey.delete(key);
    }
  }
  for (const column of columns) {
    const key = column.key as string;
    if (byKey.has(key) && !hidden.has(key)) ordered.push(column);
  }
  return ordered.length > 0 ? ordered : columns;
};
