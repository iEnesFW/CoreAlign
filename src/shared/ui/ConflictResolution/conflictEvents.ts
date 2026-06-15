import type { ConcurrencyConflictDetails } from '@/shared/api/concurrencyConflict';

export type ConflictResolutionChoice = 'reload' | 'overwrite' | 'cancel';

export const CONFLICT_EVENT = 'corealign:concurrency-conflict';

export interface ConflictRequestEventDetail {
  conflict: ConcurrencyConflictDetails;
  resolve?: (choice: ConflictResolutionChoice) => void;
}

export const dispatchConflict = (detail: ConflictRequestEventDetail): void => {
  if (typeof window === 'undefined') {
    detail.resolve?.('cancel');
    return;
  }
  window.dispatchEvent(new CustomEvent<ConflictRequestEventDetail>(CONFLICT_EVENT, { detail }));
};

export const requestConflictResolution = (
  conflict: ConcurrencyConflictDetails,
): Promise<ConflictResolutionChoice> =>
  new Promise<ConflictResolutionChoice>((resolve) => {
    dispatchConflict({ conflict, resolve });
  });
