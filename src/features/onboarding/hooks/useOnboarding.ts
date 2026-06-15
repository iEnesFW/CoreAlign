import { useCallback, useEffect, useState } from 'react';
import { useAuthStore } from '@/features/auth/model/authStore';
import { usePersonaStore } from '@/features/persona/model/personaStore';
import type { TourKey, TourStatus } from '../model/onboarding.types';
import { TOUR_KEYS } from '../model/onboarding.types';
import {
  clearAllTours,
  clearTourStatus,
  readTourStatus,
  writeTourStatus,
} from './onboardingStorage';

interface UseOnboardingStateResult {
  status: TourStatus;
  isCompleted: boolean;
  isSkipped: boolean;
  shouldAutoStart: boolean;
  markCompleted: () => void;
  markSkipped: () => void;
  restart: () => void;
}

export const useOnboardingState = (tourKey: TourKey): UseOnboardingStateResult => {
  const userId = useAuthStore((s) => s.user?.id ?? null);
  const personaMode = usePersonaStore((s) => s.mode);
  const [status, setStatus] = useState<TourStatus>(() => readTourStatus(userId, tourKey));

  useEffect(() => {
    setStatus(readTourStatus(userId, tourKey));
  }, [userId, tourKey]);

  const markCompleted = useCallback((): void => {
    writeTourStatus(userId, tourKey, 'completed');
    setStatus('completed');
  }, [userId, tourKey]);

  const markSkipped = useCallback((): void => {
    writeTourStatus(userId, tourKey, 'skipped');
    setStatus('skipped');
  }, [userId, tourKey]);

  const restart = useCallback((): void => {
    clearTourStatus(userId, tourKey);
    setStatus('pending');
  }, [userId, tourKey]);

  const shouldAutoStart = personaMode === 'Simple' && status === 'pending';

  return {
    status,
    isCompleted: status === 'completed',
    isSkipped: status === 'skipped',
    shouldAutoStart,
    markCompleted,
    markSkipped,
    restart,
  };
};

interface UseOnboardingControllerResult {
  statuses: Record<TourKey, TourStatus>;
  resetAll: () => void;
  resetOne: (tourKey: TourKey) => void;
}

export const useOnboardingController = (): UseOnboardingControllerResult => {
  const userId = useAuthStore((s) => s.user?.id ?? null);
  const [statuses, setStatuses] = useState<Record<TourKey, TourStatus>>(() => {
    const out = {} as Record<TourKey, TourStatus>;
    TOUR_KEYS.forEach((k) => {
      out[k] = readTourStatus(userId, k);
    });
    return out;
  });

  useEffect(() => {
    const out = {} as Record<TourKey, TourStatus>;
    TOUR_KEYS.forEach((k) => {
      out[k] = readTourStatus(userId, k);
    });
    setStatuses(out);
  }, [userId]);

  const resetAll = useCallback((): void => {
    clearAllTours(userId);
    const reset = {} as Record<TourKey, TourStatus>;
    TOUR_KEYS.forEach((k) => {
      reset[k] = 'pending';
    });
    setStatuses(reset);
  }, [userId]);

  const resetOne = useCallback(
    (tourKey: TourKey): void => {
      clearTourStatus(userId, tourKey);
      setStatuses((prev) => ({ ...prev, [tourKey]: 'pending' }));
    },
    [userId],
  );

  return { statuses, resetAll, resetOne };
};
