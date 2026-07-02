import { useTranslation } from 'react-i18next';
import { Keyboard } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { useShortcutsStore } from '../model/paletteStore';

const SHORTCUTS: { keys: string; labelKey: string }[] = [
  { keys: '⌘ / Ctrl + K', labelKey: 'Shortcuts.openPalette' },
  { keys: '?', labelKey: 'Shortcuts.openHelp' },
  { keys: 'Esc', labelKey: 'Shortcuts.closeOverlay' },
  { keys: '↑ ↓', labelKey: 'Shortcuts.navigate' },
  { keys: 'Enter', labelKey: 'Shortcuts.select' },
];

export const ShortcutsHelp = () => {
  const { t } = useTranslation();
  const isHelpOpen = useShortcutsStore((s) => s.isHelpOpen);
  const close = useShortcutsStore((s) => s.close);

  return (
    <Modal
      open={isHelpOpen}
      onClose={close}
      title={t('Shortcuts.title', { defaultValue: 'Klavye kısayolları' })}
      icon={<Keyboard size={18} />}
      size="sm"
    >
      <ul className="space-y-2">
        {SHORTCUTS.map((s) => (
          <li key={s.keys} className="flex items-center justify-between gap-3 text-sm">
            <span className="text-slate-700 dark:text-slate-200">
              {t(s.labelKey, { defaultValue: s.labelKey })}
            </span>
            <kbd className="rounded border border-slate-300 bg-slate-50 px-2 py-0.5 font-mono text-xs text-slate-600 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-300">
              {s.keys}
            </kbd>
          </li>
        ))}
      </ul>
    </Modal>
  );
};
