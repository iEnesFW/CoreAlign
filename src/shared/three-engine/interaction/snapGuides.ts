import type { PlanSnapGuide } from './planSnap';

type SnapGuideListener = (guides: PlanSnapGuide[]) => void;

let current: PlanSnapGuide[] = [];
const listeners = new Set<SnapGuideListener>();

export const setSnapGuides = (guides: PlanSnapGuide[]) => {
  current = guides;
  for (const listener of listeners) listener(current);
};

export const clearSnapGuides = () => {
  if (current.length > 0) setSnapGuides([]);
};

export const subscribeSnapGuides = (listener: SnapGuideListener) => {
  listeners.add(listener);
  listener(current);
  return () => {
    listeners.delete(listener);
  };
};
