import type { TourKey, TourStatus } from '../model/onboarding.types';
import { TOUR_KEYS } from '../model/onboarding.types';

const STORAGE_PREFIX = 'corealign.onboarding';

const resolveUserId = (explicitUserId: string | null | undefined): string => {
  if (explicitUserId) return explicitUserId;
  if (typeof window === 'undefined') return 'anon';
  try {
    const raw = window.localStorage.getItem('user');
    if (!raw) return 'anon';
    const parsed = JSON.parse(raw) as { id?: string } | null;
    return parsed?.id ?? 'anon';
  } catch {
    return 'anon';
  }
};

const buildStorageKey = (userId: string, tourKey: TourKey): string =>
  `${STORAGE_PREFIX}.${userId}.${tourKey}`;

export const readTourStatus = (userId: string | null, tourKey: TourKey): TourStatus => {
  if (typeof window === 'undefined') return 'pending';
  const resolvedUserId = resolveUserId(userId);
  try {
    const value = window.localStorage.getItem(buildStorageKey(resolvedUserId, tourKey));
    if (value === 'completed' || value === 'skipped') return value;
    return 'pending';
  } catch {
    return 'pending';
  }
};

export const writeTourStatus = (
  userId: string | null,
  tourKey: TourKey,
  status: Exclude<TourStatus, 'pending'>,
): void => {
  if (typeof window === 'undefined') return;
  const resolvedUserId = resolveUserId(userId);
  try {
    window.localStorage.setItem(buildStorageKey(resolvedUserId, tourKey), status);
  } catch {
    // Storage may be full or disabled; tour gating is non-critical.
  }
};

export const clearTourStatus = (userId: string | null, tourKey: TourKey): void => {
  if (typeof window === 'undefined') return;
  const resolvedUserId = resolveUserId(userId);
  try {
    window.localStorage.removeItem(buildStorageKey(resolvedUserId, tourKey));
  } catch {
    // ignore
  }
};

export const clearAllTours = (userId: string | null): void => {
  TOUR_KEYS.forEach((key) => clearTourStatus(userId, key));
};
