import { useCallback, useEffect, useRef, useState } from 'react';
import type { ConcurrencyConflictDetails } from '@/shared/api/concurrencyConflict';
import {
  CONFLICT_EVENT,
  type ConflictRequestEventDetail,
  type ConflictResolutionChoice,
  requestConflictResolution,
} from './conflictEvents';

export type { ConflictResolutionChoice };
export { requestConflictResolution };

export interface ConflictModalState {
  open: boolean;
  conflict: ConcurrencyConflictDetails | null;
}

export interface UseConflictModalResult extends ConflictModalState {
  show: (conflict: ConcurrencyConflictDetails) => Promise<ConflictResolutionChoice>;
  hide: () => void;
  onReload: () => void;
  onForceOverwrite: () => void;
  onCancel: () => void;
}

type Resolver = (choice: ConflictResolutionChoice) => void;

export const useConflictModal = (): UseConflictModalResult => {
  const [state, setState] = useState<ConflictModalState>({ open: false, conflict: null });
  const resolverRef = useRef<Resolver | null>(null);

  const settle = useCallback((choice: ConflictResolutionChoice) => {
    const resolver = resolverRef.current;
    resolverRef.current = null;
    setState({ open: false, conflict: null });
    resolver?.(choice);
  }, []);

  const show = useCallback(
    (conflict: ConcurrencyConflictDetails) =>
      new Promise<ConflictResolutionChoice>((resolve) => {
        resolverRef.current?.('cancel');
        resolverRef.current = resolve;
        setState({ open: true, conflict });
      }),
    [],
  );

  const hide = useCallback(() => settle('cancel'), [settle]);
  const onReload = useCallback(() => settle('reload'), [settle]);
  const onForceOverwrite = useCallback(() => settle('overwrite'), [settle]);
  const onCancel = useCallback(() => settle('cancel'), [settle]);

  useEffect(() => {
    if (typeof window === 'undefined') return undefined;
    const handler = (event: Event) => {
      const detail = (event as CustomEvent<ConflictRequestEventDetail>).detail;
      if (!detail) return;
      resolverRef.current?.('cancel');
      resolverRef.current = detail.resolve ?? null;
      setState({ open: true, conflict: detail.conflict });
    };
    window.addEventListener(CONFLICT_EVENT, handler);
    return () => window.removeEventListener(CONFLICT_EVENT, handler);
  }, []);

  return {
    open: state.open,
    conflict: state.conflict,
    show,
    hide,
    onReload,
    onForceOverwrite,
    onCancel,
  };
};
