import { useCallback, useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Check, ChevronDown, Lightbulb, Settings2, SlidersHorizontal } from 'lucide-react';
import {
  usePersonaStore,
  useScreenOverride,
  useScreenUxMode,
  type UxComplexityMode,
} from '@/shared/lib/persona';
import { syncPerScreenOverridesDebounced } from '../hooks/usePersonaSync';

interface ScreenPersonaMenuProps {
  screenKey: string;
  i18nNamespace?: string;
}

type OverrideChoice = 'Default' | UxComplexityMode;

export const ScreenPersonaMenu = ({
  screenKey,
  i18nNamespace = 'GlassEnclosure.Designer.Shell.PersonaOverride',
}: ScreenPersonaMenuProps) => {
  const { t } = useTranslation();
  const effectiveMode = useScreenUxMode(screenKey);
  const override = useScreenOverride(screenKey);
  const setScreenOverride = usePersonaStore((s) => s.setScreenOverride);
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (!open) return;
    const onClick = (e: MouseEvent) => {
      if (!containerRef.current) return;
      if (!containerRef.current.contains(e.target as Node)) setOpen(false);
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOpen(false);
    };
    document.addEventListener('mousedown', onClick);
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('mousedown', onClick);
      document.removeEventListener('keydown', onKey);
    };
  }, [open]);

  const apply = useCallback(
    (choice: OverrideChoice) => {
      const next = choice === 'Default' ? null : choice;
      setScreenOverride(screenKey, next);
      const overrides = usePersonaStore.getState().perScreenOverrides;
      syncPerScreenOverridesDebounced(overrides);
      setOpen(false);
    },
    [screenKey, setScreenOverride],
  );

  const triggerLabel =
    override === null
      ? t(`${i18nNamespace}.Default`, { defaultValue: 'Default' })
      : override === 'Pro'
        ? t(`${i18nNamespace}.Pro`, { defaultValue: 'Pro' })
        : t(`${i18nNamespace}.Simple`, { defaultValue: 'Simple' });

  const TriggerIcon =
    effectiveMode === 'Pro'
      ? Settings2
      : effectiveMode === 'Simple'
        ? Lightbulb
        : SlidersHorizontal;

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-haspopup="menu"
        aria-expanded={open}
        aria-label={t(`${i18nNamespace}.TriggerAria`, { defaultValue: 'Screen persona mode' })}
        title={t(`${i18nNamespace}.TriggerTitle`, {
          defaultValue: 'Per-screen persona mode',
        })}
        className="inline-flex items-center gap-1.5 rounded-[5px] border border-slate-200 px-2 py-1 text-[11px] font-medium text-slate-700 transition-colors hover:bg-slate-50 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800"
      >
        <TriggerIcon size={14} />
        <span className="hidden sm:inline">{triggerLabel}</span>
        <ChevronDown size={12} aria-hidden />
      </button>

      {open && (
        <div
          role="menu"
          className="absolute right-0 z-30 mt-1 w-56 overflow-hidden rounded-md border border-slate-200 bg-white shadow-lg dark:border-slate-700 dark:bg-slate-900"
        >
          <div className="border-b border-slate-100 px-3 py-2 text-[11px] font-medium uppercase tracking-wide text-slate-500 dark:border-slate-800 dark:text-slate-400">
            {t(`${i18nNamespace}.MenuTitle`, { defaultValue: 'On this screen' })}
          </div>
          <PersonaMenuItem
            active={override === null}
            icon={<SlidersHorizontal size={14} />}
            label={t(`${i18nNamespace}.Default`, { defaultValue: 'Use my default' })}
            description={t(`${i18nNamespace}.DefaultHelp`, {
              defaultValue: 'Follow the global persona mode',
            })}
            onSelect={() => apply('Default')}
          />
          <PersonaMenuItem
            active={override === 'Simple'}
            icon={<Lightbulb size={14} />}
            label={t(`${i18nNamespace}.Simple`, { defaultValue: 'Simple' })}
            description={t(`${i18nNamespace}.SimpleHelp`, {
              defaultValue: 'Guided, fewer panels',
            })}
            onSelect={() => apply('Simple')}
          />
          <PersonaMenuItem
            active={override === 'Pro'}
            icon={<Settings2 size={14} />}
            label={t(`${i18nNamespace}.Pro`, { defaultValue: 'Pro' })}
            description={t(`${i18nNamespace}.ProHelp`, {
              defaultValue: 'Full controls, dense info',
            })}
            onSelect={() => apply('Pro')}
          />
        </div>
      )}
    </div>
  );
};

interface PersonaMenuItemProps {
  active: boolean;
  icon: React.ReactNode;
  label: string;
  description: string;
  onSelect: () => void;
}

const PersonaMenuItem = ({ active, icon, label, description, onSelect }: PersonaMenuItemProps) => (
  <button
    type="button"
    role="menuitemradio"
    aria-checked={active}
    onClick={onSelect}
    className="flex w-full items-start gap-2 px-3 py-2 text-left text-xs transition-colors hover:bg-slate-50 focus:bg-slate-50 focus:outline-none dark:hover:bg-slate-800 dark:focus:bg-slate-800"
  >
    <span className="mt-0.5 text-slate-500 dark:text-slate-400">{icon}</span>
    <span className="flex-1">
      <span className="block font-medium text-slate-800 dark:text-slate-100">{label}</span>
      <span className="block text-[11px] text-slate-500 dark:text-slate-400">{description}</span>
    </span>
    {active && <Check size={14} className="mt-0.5 text-primary-600 dark:text-primary-400" />}
  </button>
);
