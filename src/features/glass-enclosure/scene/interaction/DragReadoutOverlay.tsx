import { useEffect, useSyncExternalStore } from 'react';
import { getDragReadout, setDragReadout, subscribeDragReadout } from '@/shared/three-engine';

// Fixed chip showing the live position/delta/angle of the active move or rotate
// gesture, so the user reads exact values during the drag.
export function DragReadoutOverlay() {
  const text = useSyncExternalStore(subscribeDragReadout, getDragReadout, getDragReadout);
  // Clear the module-level readout if the canvas unmounts mid-gesture (view
  // switch / error-boundary retry) so a stale chip cannot persist.
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
