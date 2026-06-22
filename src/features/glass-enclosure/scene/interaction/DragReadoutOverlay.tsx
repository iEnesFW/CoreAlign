import { useEffect, useSyncExternalStore } from 'react';
import { getDragReadout, setDragReadout, subscribeDragReadout } from '@/shared/three-engine';

export function DragReadoutOverlay() {
  const text = useSyncExternalStore(subscribeDragReadout, getDragReadout, getDragReadout);
  useEffect(() => () => setDragReadout(null), []);
  if (!text) return null;
  return (
    <div className="pointer-events-none absolute bottom-3 left-1/2 z-30 -translate-x-1/2">
      <div className="rounded-md bg-slate-900/90 px-3 py-1.5 text-xs font-semibold tabular-nums text-white shadow-lg">
        {text}
      </div>
    </div>
  );
}
