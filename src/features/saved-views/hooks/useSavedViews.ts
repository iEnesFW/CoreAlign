import { useMemo, useState } from 'react';
import { createUserScopedSlot } from '@/shared/storage/userScopedSlot';
import { newOperationId } from '@/shared/lib/operationId';
import {
  addView,
  parseActiveViewId,
  parseSavedViews,
  removeViewFrom,
  renameViewIn,
  type SavedView,
  type SavedViewSnapshot,
} from '../model/savedView.types';

export interface UseSavedViews {
  views: SavedView[];
  activeViewId: string | null;
  saveView: (name: string, snapshot: SavedViewSnapshot) => SavedView;
  renameView: (id: string, name: string) => void;
  deleteView: (id: string) => void;
  setActive: (id: string | null) => void;
}

export const useSavedViews = (pageKey: string): UseSavedViews => {
  const viewsSlot = useMemo(
    () => createUserScopedSlot<SavedView[]>({ feature: 'views', pageKey, schema: parseSavedViews }),
    [pageKey],
  );
  const activeSlot = useMemo(
    () =>
      createUserScopedSlot<string>({ feature: 'activeView', pageKey, schema: parseActiveViewId }),
    [pageKey],
  );

  const [views, setViews] = useState<SavedView[]>(() => viewsSlot.get() ?? []);
  const [activeViewId, setActiveViewId] = useState<string | null>(() => activeSlot.get());
  const [lastPageKey, setLastPageKey] = useState(pageKey);
  if (pageKey !== lastPageKey) {
    setLastPageKey(pageKey);
    setViews(viewsSlot.get() ?? []);
    setActiveViewId(activeSlot.get());
  }

  const persistViews = (next: SavedView[]) => {
    setViews(next);
    viewsSlot.set(next);
  };

  const persistActive = (id: string | null) => {
    setActiveViewId(id);
    if (id === null) activeSlot.remove();
    else activeSlot.set(id);
  };

  const saveView = (name: string, snapshot: SavedViewSnapshot): SavedView => {
    const view: SavedView = {
      id: newOperationId(),
      name,
      filters: snapshot.filters,
      sort: snapshot.sort,
      columnState: snapshot.columnState,
    };
    persistViews(addView(views, view));
    persistActive(view.id);
    return view;
  };

  const renameView = (id: string, name: string) => {
    persistViews(renameViewIn(views, id, name));
  };

  const deleteView = (id: string) => {
    persistViews(removeViewFrom(views, id));
    if (activeViewId === id) persistActive(null);
  };

  return { views, activeViewId, saveView, renameView, deleteView, setActive: persistActive };
};
