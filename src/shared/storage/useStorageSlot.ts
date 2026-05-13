import { useCallback, useSyncExternalStore } from 'react';
import type { StorageSlot } from './storage';

export function useStorageSlot<T>(
  slot: StorageSlot<T>,
): [T | null, (value: T) => void, () => void] {
  const value = useSyncExternalStore(
    (notify) => slot.subscribe(() => notify()),
    () => slot.get(),
    () => null,
  );

  const write = useCallback(
    (next: T) => {
      slot.set(next);
    },
    [slot],
  );

  const clear = useCallback(() => {
    slot.remove();
  }, [slot]);

  return [value, write, clear];
}
