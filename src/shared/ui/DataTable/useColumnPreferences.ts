import { useMemo, useState } from 'react';
import { createUserScopedSlot } from '@/shared/storage/userScopedSlot';
import { normalizeColumnState, parseColumnState, type ColumnState } from './columnState';

export interface ColumnPreferences {
  columnState: ColumnState;
  toggleHidden: (key: string) => void;
  move: (key: string, direction: -1 | 1) => void;
  replace: (next: ColumnState) => void;
  reset: () => void;
}

export const useColumnPreferences = (
  pageKey: string,
  allKeys: readonly string[],
): ColumnPreferences => {
  const slot = useMemo(
    () =>
      createUserScopedSlot<ColumnState>({ feature: 'columns', pageKey, schema: parseColumnState }),
    [pageKey],
  );

  const keySignature = allKeys.join('|');
  const [state, setState] = useState<ColumnState>(() => normalizeColumnState(slot.get(), allKeys));
  const [lastSignature, setLastSignature] = useState(keySignature);
  if (keySignature !== lastSignature) {
    setLastSignature(keySignature);
    setState((prev) => normalizeColumnState(prev, allKeys));
  }

  const persist = (next: ColumnState) => {
    setState(next);
    slot.set(next);
  };

  const toggleHidden = (key: string) => {
    const hidden = state.hidden.includes(key)
      ? state.hidden.filter((entry) => entry !== key)
      : [...state.hidden, key];
    persist(normalizeColumnState({ order: state.order, hidden }, allKeys));
  };

  const move = (key: string, direction: -1 | 1) => {
    const order = [...state.order];
    const index = order.indexOf(key);
    const target = index + direction;
    if (index < 0 || target < 0 || target >= order.length) return;
    [order[index], order[target]] = [order[target], order[index]];
    persist(normalizeColumnState({ order, hidden: state.hidden }, allKeys));
  };

  const replace = (next: ColumnState) => {
    persist(normalizeColumnState(next, allKeys));
  };

  const reset = () => {
    slot.remove();
    setState(normalizeColumnState(null, allKeys));
  };

  return { columnState: state, toggleHidden, move, replace, reset };
};
