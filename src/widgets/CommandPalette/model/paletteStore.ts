import { create } from 'zustand';

interface CommandPaletteState {
  isOpen: boolean;
  open: () => void;
  close: () => void;
  toggle: () => void;
}

export const useCommandPaletteStore = create<CommandPaletteState>((set) => ({
  isOpen: false,
  open: () => set({ isOpen: true }),
  close: () => set({ isOpen: false }),
  toggle: () => set((state) => ({ isOpen: !state.isOpen })),
}));

interface ShortcutsState {
  isHelpOpen: boolean;
  open: () => void;
  close: () => void;
  toggle: () => void;
}

export const useShortcutsStore = create<ShortcutsState>((set) => ({
  isHelpOpen: false,
  open: () => set({ isHelpOpen: true }),
  close: () => set({ isHelpOpen: false }),
  toggle: () => set((state) => ({ isHelpOpen: !state.isHelpOpen })),
}));
