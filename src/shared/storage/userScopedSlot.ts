import { useAuthStore } from '@/shared/lib/store/authStore';
import { createStorageSlot, type StorageSlot } from './storage';

const currentUserId = (): string => useAuthStore.getState().user?.id ?? 'anon';

export interface UserScopedSlot<T> {
  get(): T | null;
  set(value: T): boolean;
  remove(): void;
  subscribe(listener: (value: T | null) => void): () => void;
}

interface Options<T> {
  feature: string;
  pageKey: string;
  schema?: (raw: unknown) => T | null;
}

export const createUserScopedSlot = <T>({
  feature,
  pageKey,
  schema,
}: Options<T>): UserScopedSlot<T> => {
  const cache = new Map<string, StorageSlot<T>>();
  const slotFor = (userId: string): StorageSlot<T> => {
    const existing = cache.get(userId);
    if (existing) return existing;
    const slot = createStorageSlot<T>({ key: `ui.${userId}.${pageKey}.${feature}`, schema });
    cache.set(userId, slot);
    return slot;
  };

  return {
    get: () => slotFor(currentUserId()).get(),
    set: (value) => slotFor(currentUserId()).set(value),
    remove: () => slotFor(currentUserId()).remove(),
    subscribe: (listener) => slotFor(currentUserId()).subscribe(listener),
  };
};
