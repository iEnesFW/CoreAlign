import { useGlobalShortcuts } from '../hooks/useGlobalShortcuts';
import { useCommandPaletteStore, useShortcutsStore } from '../model/paletteStore';
import { CommandPalette } from './CommandPalette';
import { ShortcutsHelp } from './ShortcutsHelp';

export const CommandPaletteWidget = () => {
  useGlobalShortcuts();
  const isOpen = useCommandPaletteStore((s) => s.isOpen);
  const isHelpOpen = useShortcutsStore((s) => s.isHelpOpen);

  return (
    <>
      {isOpen && <CommandPalette />}
      {isHelpOpen && <ShortcutsHelp />}
    </>
  );
};
