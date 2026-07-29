import { useCallback, useEffect, useState, type RefObject } from 'react';

/**
 * Drive the Fullscreen API for one element.
 *
 * WHY the state is read from the DOM rather than tracked locally: the user can leave fullscreen
 * with Esc or the browser's own control, and neither of those calls back through our toggle. The
 * `fullscreenchange` event is the only signal that covers every exit.
 */
export const useFullscreen = (ref: RefObject<HTMLElement | null>) => {
  const [isFullscreen, setIsFullscreen] = useState(false);

  useEffect(() => {
    const sync = () => setIsFullscreen(document.fullscreenElement === ref.current);
    sync();
    document.addEventListener('fullscreenchange', sync);
    return () => document.removeEventListener('fullscreenchange', sync);
  }, [ref]);

  const toggle = useCallback(() => {
    const el = ref.current;
    if (!el) return;
    // Both calls reject when the gesture is not user-initiated or the browser blocks it; swallow
    // that rather than surfacing an unhandled rejection — the button simply does nothing.
    if (document.fullscreenElement === el) void document.exitFullscreen().catch(() => {});
    else void el.requestFullscreen().catch(() => {});
  }, [ref]);

  const supported = typeof document !== 'undefined' && document.fullscreenEnabled;

  return { isFullscreen, toggle, supported };
};
