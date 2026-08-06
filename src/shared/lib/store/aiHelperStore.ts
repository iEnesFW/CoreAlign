import { create } from 'zustand';

/**
 * The AI helper's open state plus whether the server has the helper switched on.
 *
 * WHY it lives in shared: the PANEL is mounted at the app root while its TRIGGER lives in the
 * dashboard Footer, so two different widgets read the same state — a widget→widget import would
 * be a same-layer FSD violation (the authStore/persona precedent).
 */
interface AiHelperState {
  isOpen: boolean;
  isAvailable: boolean;
  open: () => void;
  close: () => void;
  toggle: () => void;
  setAvailable: (isAvailable: boolean) => void;
}

export const useAiHelperStore = create<AiHelperState>((set) => ({
  isOpen: false,
  isAvailable: false,
  open: () => set({ isOpen: true }),
  close: () => set({ isOpen: false }),
  toggle: () => set((state) => ({ isOpen: !state.isOpen })),
  setAvailable: (isAvailable) => set({ isAvailable }),
}));
