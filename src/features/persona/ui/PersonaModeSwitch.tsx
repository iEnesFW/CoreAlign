import { useTranslation } from 'react-i18next';
import { Lightbulb, Settings2 } from 'lucide-react';
import { usePersonaStore, type UxComplexityMode } from '@/shared/lib/persona';
import { personaApi } from '../api/personaApi';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';

export const PersonaModeSwitch = () => {
  const { t } = useTranslation();
  const mode = usePersonaStore((s) => s.mode);
  const setMode = usePersonaStore((s) => s.setMode);

  const toggle = async (): Promise<void> => {
    const next: UxComplexityMode = mode === 'Pro' ? 'Simple' : 'Pro';
    setMode(next);
    await safeRequestWithNotify(personaApi.update({ mode: next }), {
      successMessage: t('Persona.Mode.Updated', {
        mode: t(`Persona.Mode.${next}`),
        defaultValue: 'Mode updated.',
      }),
      showSuccessNotification: true,
    });
  };

  const Icon = mode === 'Pro' ? Settings2 : Lightbulb;
  const label =
    mode === 'Pro'
      ? t('Persona.Mode.Pro', { defaultValue: 'Pro' })
      : t('Persona.Mode.Simple', { defaultValue: 'Simple' });

  return (
    <button
      type="button"
      onClick={toggle}
      aria-label={label}
      title={label}
      data-tour="persona-switch"
      className="inline-flex items-center gap-1.5 rounded-[5px] border border-slate-200 px-2 py-1 text-[11px] font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800 transition-colors focus:outline-none focus:ring-1 focus:ring-primary-500"
    >
      <Icon size={14} />
      <span className="hidden sm:inline">{label}</span>
    </button>
  );
};
