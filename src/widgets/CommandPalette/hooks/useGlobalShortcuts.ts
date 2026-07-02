import { useEffect } from 'react';
import { isEditableTarget } from '@/shared/hooks/isEditableTarget';
import { useCommandPaletteStore, useShortcutsStore } from '../model/paletteStore';

export const useGlobalShortcuts = () => {
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      const palette = useCommandPaletteStore.getState();
      const shortcuts = useShortcutsStore.getState();

      const isCmdK = (e.metaKey || e.ctrlKey) && (e.key === 'k' || e.key === 'K');
      if (isCmdK) {
        e.preventDefault();
        palette.toggle();
        return;
      }
      if (e.key === 'Escape') {
        if (palette.isOpen) palette.close();
        else if (shortcuts.isHelpOpen) shortcuts.close();
        return;
      }
      if (isEditableTarget(e.target)) return;
      if (e.key === '?') {
        e.preventDefault();
        shortcuts.toggle();
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, []);
};
