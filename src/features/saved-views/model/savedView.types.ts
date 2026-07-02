import type { SortState } from '@/shared/ui/DataTable/DataTable';
import { parseColumnState, type ColumnState } from '@/shared/ui/DataTable/columnState';

export interface SavedView {
  id: string;
  name: string;
  filters: Record<string, unknown>;
  sort: SortState | null;
  columnState: ColumnState | null;
}

export interface SavedViewSnapshot {
  filters: Record<string, unknown>;
  sort: SortState | null;
  columnState: ColumnState | null;
}

const parseSort = (raw: unknown): SortState | null => {
  if (!raw || typeof raw !== 'object') return null;
  const record = raw as Record<string, unknown>;
  if (typeof record.key !== 'string') return null;
  if (record.dir !== 'asc' && record.dir !== 'desc') return null;
  return { key: record.key, dir: record.dir };
};

const parseView = (raw: unknown): SavedView | null => {
  if (!raw || typeof raw !== 'object') return null;
  const record = raw as Record<string, unknown>;
  if (typeof record.id !== 'string' || record.id.length === 0) return null;
  if (typeof record.name !== 'string' || record.name.length === 0) return null;
  const filters =
    record.filters && typeof record.filters === 'object' && !Array.isArray(record.filters)
      ? (record.filters as Record<string, unknown>)
      : {};
  return {
    id: record.id,
    name: record.name,
    filters,
    sort: parseSort(record.sort),
    columnState: parseColumnState(record.columnState),
  };
};

export const parseSavedViews = (raw: unknown): SavedView[] | null => {
  if (!Array.isArray(raw)) return null;
  return raw.map(parseView).filter((view): view is SavedView => view !== null);
};

export const parseActiveViewId = (raw: unknown): string | null =>
  typeof raw === 'string' && raw.length > 0 ? raw : null;

export const addView = (views: SavedView[], view: SavedView): SavedView[] => [...views, view];

export const renameViewIn = (views: SavedView[], id: string, name: string): SavedView[] =>
  views.map((view) => (view.id === id ? { ...view, name } : view));

export const removeViewFrom = (views: SavedView[], id: string): SavedView[] =>
  views.filter((view) => view.id !== id);
