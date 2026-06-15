import { useCallback, useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { queueToast } from '@/shared/api/toastQueue';
import type { ConcurrencyConflictDetails } from '@/shared/api/concurrencyConflict';
import { ConflictResolutionModal } from './ConflictResolutionModal';
import {
  CONFLICT_EVENT,
  type ConflictRequestEventDetail,
  type ConflictResolutionChoice,
} from './conflictEvents';

const FORCE_OVERWRITE_EVENT = 'corealign:force-overwrite';

export const ConflictResolutionHost = () => {
  const { t } = useTranslation();
  const [conflict, setConflict] = useState<ConcurrencyConflictDetails | null>(null);
  const resolverRef = useRef<((choice: ConflictResolutionChoice) => void) | null>(null);

  const settle = useCallback((choice: ConflictResolutionChoice) => {
    const resolver = resolverRef.current;
    resolverRef.current = null;
    setConflict(null);
    resolver?.(choice);
  }, []);

  useEffect(() => {
    if (typeof window === 'undefined') return undefined;
    const handler = (event: Event) => {
      const detail = (event as CustomEvent<ConflictRequestEventDetail>).detail;
      if (!detail?.conflict) return;
      resolverRef.current?.('cancel');
      resolverRef.current = detail.resolve ?? null;
      setConflict(detail.conflict);
    };
    window.addEventListener(CONFLICT_EVENT, handler);
    return () => window.removeEventListener(CONFLICT_EVENT, handler);
  }, []);

  const handleCancel = useCallback(() => {
    settle('cancel');
  }, [settle]);

  const handleReload = useCallback(() => {
    settle('reload');
    if (typeof window !== 'undefined') {
      window.location.reload();
    }
  }, [settle]);

  const handleForceOverwrite = useCallback(() => {
    if (typeof window !== 'undefined') {
      window.dispatchEvent(new CustomEvent(FORCE_OVERWRITE_EVENT, { detail: { conflict } }));
    }
    settle('overwrite');
    queueToast({
      dedupeKey: 'conflict:force-overwrite:pending',
      description: t('Conflict.OverwritePendingPhase'),
      variant: 'info',
    });
  }, [conflict, settle, t]);

  return (
    <ConflictResolutionModal
      open={conflict !== null}
      conflictMessage={conflict?.message ?? null}
      currentVersion={conflict?.currentVersion ?? null}
      attemptedVersion={conflict?.attemptedVersion ?? null}
      conflictingFields={conflict?.conflictingFields ?? []}
      canOverwrite={false}
      onReload={handleReload}
      onForceOverwrite={handleForceOverwrite}
      onCancel={handleCancel}
    />
  );
};
