import { useCallback, useEffect, useRef, useState } from 'react';

export interface RafState<T> {
  value: T;
  /** Coalesced write: at most one commit per animation frame. Use from pointermove. */
  schedule: (next: T) => void;
  /** Immediate write that cancels any pending frame. Use from pointerup / cancel. */
  set: (next: T) => void;
}

/**
 * State whose writes are collapsed to one per animation frame.
 *
 * WHY: `pointermove` fires at the pointing device's polling rate — 125 Hz on a plain mouse and up
 * to 1000 Hz on a gaming mouse — not at the display rate. A preview that rebuilds geometry from
 * state therefore runs its rebuild several times per displayed frame, and every rebuild past the
 * first is thrown away unseen. On a free-drawn polygon (hundreds of vertices, so earcut plus an
 * extrude plus a creased-normal pass) that surplus work is what freezes the designer mid-drag.
 * Scheduling through rAF caps the rebuild at exactly one per frame the user can actually see.
 */
export const useRafState = <T>(initial: T): RafState<T> => {
  const [value, setValue] = useState<T>(initial);
  const frameRef = useRef<number | null>(null);
  const pendingRef = useRef<T>(initial);

  const cancelPending = useCallback(() => {
    if (frameRef.current === null) return;
    cancelAnimationFrame(frameRef.current);
    frameRef.current = null;
  }, []);

  useEffect(() => cancelPending, [cancelPending]);

  const schedule = useCallback((next: T) => {
    pendingRef.current = next;
    if (frameRef.current !== null) return;
    frameRef.current = requestAnimationFrame(() => {
      frameRef.current = null;
      setValue(pendingRef.current);
    });
  }, []);

  const set = useCallback(
    (next: T) => {
      // WHY cancel first: a frame queued by the last pointermove would otherwise land AFTER this
      // write and resurrect the stale preview on top of the committed value.
      cancelPending();
      pendingRef.current = next;
      setValue(next);
    },
    [cancelPending],
  );

  return { value, schedule, set };
};
