import { useTranslation } from 'react-i18next';
import { Sparkles } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { useUxMode } from '@/shared/lib/persona';
import { useWizardStore } from '../model/wizardStore';
import { useSystemTemplatesQuery } from '../hooks/useSystemTemplatesQuery';
import { TemplateCard } from './TemplateCard';

const SKELETON_KEYS = ['s1', 's2', 's3', 's4'] as const;

export const Step2Template = () => {
  const { t } = useTranslation();
  const category = useWizardStore((s) => s.category);
  const templateId = useWizardStore((s) => s.templateId);
  const setTemplate = useWizardStore((s) => s.setTemplate);
  const setStep = useWizardStore((s) => s.setStep);
  const mode = useUxMode();

  const templatesQuery = useSystemTemplatesQuery(category);
  const templates = templatesQuery.data ?? [];

  const handlePick = (id: string | null) => {
    setTemplate(id);
    setStep(3);
  };

  return (
    <section className="space-y-4">
      <header className="space-y-1">
        <h3 className="text-base font-semibold text-slate-900 dark:text-slate-100">
          {t('GlassEnclosure.NewProjectWizard.Step2.Title', {
            defaultValue: 'Hazır bir şablonla başla',
          })}
        </h3>
        <p className="text-xs text-slate-500 dark:text-slate-400">
          {mode === 'Simple'
            ? t('GlassEnclosure.NewProjectWizard.Step2.HintSimple', {
                defaultValue: 'En çok kullanılan düzenler. Boş başlamak da serbest.',
              })
            : t('GlassEnclosure.NewProjectWizard.Step2.HintPro', {
                defaultValue: 'Sistem şablonları run/panel preset ile birlikte gelir.',
              })}
        </p>
      </header>

      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
        <button
          type="button"
          onClick={() => handlePick(null)}
          className={cn(
            'group flex h-full flex-col items-center justify-center gap-2 rounded-xl border border-dashed p-3 text-center transition-all',
            'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500',
            templateId === null && templatesQuery.isSuccess
              ? 'border-primary-500 bg-primary-50/60 dark:bg-primary-500/10'
              : 'border-slate-300 bg-white hover:border-primary-300 dark:border-slate-700 dark:bg-slate-900 dark:hover:border-primary-700',
          )}
        >
          <span className="flex h-12 w-12 items-center justify-center rounded-xl bg-gradient-to-br from-success-500 to-teal-600 text-white shadow-md shadow-success-500/20">
            <Sparkles size={22} />
          </span>
          <span className="text-sm font-semibold text-slate-900 dark:text-slate-100">
            {t('GlassEnclosure.NewProjectWizard.Step2.StartBlank', {
              defaultValue: 'Boş başla',
            })}
          </span>
          <span className="text-[11px] text-slate-500 dark:text-slate-400">
            {t('GlassEnclosure.NewProjectWizard.Step2.StartBlankHint', {
              defaultValue: 'Şablonsuz, sıfırdan tasarım.',
            })}
          </span>
        </button>

        {templatesQuery.isLoading &&
          SKELETON_KEYS.map((key) => (
            <div
              key={key}
              className="h-44 animate-pulse rounded-xl border border-slate-200 bg-slate-100 dark:border-slate-800 dark:bg-slate-800/50"
            />
          ))}

        {templatesQuery.isSuccess &&
          templates.map((template) => (
            <TemplateCard
              key={template.id}
              template={template}
              selected={templateId === template.id}
              onSelect={(id) => handlePick(id)}
            />
          ))}
      </div>

      {templatesQuery.isSuccess && templates.length === 0 && (
        <div className="rounded-lg border border-dashed border-slate-300 bg-slate-50 px-4 py-6 text-center text-xs text-slate-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-400">
          {t('GlassEnclosure.NewProjectWizard.Step2.EmptyHint', {
            defaultValue: 'Bu kategori için hazır şablon yok. "Boş başla" ile devam et.',
          })}
        </div>
      )}
    </section>
  );
};

export default Step2Template;
