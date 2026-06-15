type Listener = () => void;

let current: string | null = null;
const listeners = new Set<Listener>();

// Live numeric feedback shown while moving/rotating an object (position, delta,
// angle), so the user sees exact values during the gesture instead of after.
export const setDragReadout = (text: string | null) => {
  if (current === text) return;
  current = text;
  for (const listener of listeners) listener();
};

export const getDragReadout = (): string | null => current;

export const subscribeDragReadout = (listener: Listener): (() => void) => {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
};
