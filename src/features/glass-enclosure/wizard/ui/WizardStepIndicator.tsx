import { useTranslation } from 'react-i18next';
import { Check } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { useUxMode } from '@/shared/lib/persona';
import { useWizardStore, type WizardStep } from '../model/wizardStore';

interface StepDescriptor {
  step: WizardStep;
  labelKey: string;
  labelDefault: string;
  subtitleKey: string;
  subtitleDefault: string;
}

const STEPS: ReadonlyArray<StepDescriptor> = [
  {
    step: 1,
    labelKey: 'GlassEnclosure.NewProjectWizard.Indicator.Step1.Label',
    labelDefault: 'Kategori',
    subtitleKey: 'GlassEnclosure.NewProjectWizard.Indicator.Step1.Subtitle',
    subtitleDefault: 'Mekan tipi',
  },
  {
    step: 2,
    labelKey: 'GlassEnclosure.NewProjectWizard.Indicator.Step2.Label',
    labelDefault: 'Şablon',
    subtitleKey: 'GlassEnclosure.NewProjectWizard.Indicator.Step2.Subtitle',
    subtitleDefault: 'Hazır şablon veya boş',
  },
  {
    step: 3,
    labelKey: 'GlassEnclosure.NewProjectWizard.Indicator.Step3.Label',
    labelDefault: 'Proje Bilgisi',
    subtitleKey: 'GlassEnclosure.NewProjectWizard.Indicator.Step3.Subtitle',
    subtitleDefault: 'Müşteri ve adres',
  },
  {
    step: 4,
    labelKey: 'GlassEnclosure.NewProjectWizard.Indicator.Step4.Label',
    labelDefault: 'Ölçü',
    subtitleKey: 'GlassEnclosure.NewProjectWizard.Indicator.Step4.Subtitle',
    subtitleDefault: 'Hızlı boyut girişi',
  },
];

export const WizardStepIndicator = () => {
  const { t } = useTranslation();
  const currentStep = useWizardStore((s) => s.step);
  const setStep = useWizardStore((s) => s.setStep);
  const mode = useUxMode();

  const handleClick = (target: WizardStep) => {
    if (target < currentStep) setStep(target);
  };

  return (
    <ol className="flex items-center gap-2 sm:gap-3">
      {STEPS.map((desc, idx) => {
        const isActive = desc.step === currentStep;
        const isCompleted = desc.step < currentStep;
        const isClickable = desc.step < currentStep;
        return (
          <li key={desc.step} className="flex flex-1 items-center gap-2 sm:gap-3">
            <button
              type="button"
              onClick={() => handleClick(desc.step)}
              disabled={!isClickable}
              className={cn(
                'flex items-center gap-2 rounded-md text-left transition-colors',
                isClickable && 'hover:bg-slate-100 dark:hover:bg-slate-800',
                !isClickable && 'cursor-default',
              )}
            >
              <span
                className={cn(
                  'flex h-7 w-7 shrink-0 items-center justify-center rounded-full border text-[11px] font-semibold transition-colors',
                  isActive &&
                    'border-primary-500 bg-gradient-to-br from-primary-500 to-purple-600 text-white shadow-sm shadow-primary-500/30',
                  isCompleted && 'border-success-500 bg-success-500 text-white',
                  !isActive &&
                    !isCompleted &&
                    'border-slate-300 bg-white text-slate-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-400',
                )}
              >
                {isCompleted ? <Check size={14} /> : desc.step}
              </span>
              <span className="hidden min-w-0 flex-col sm:flex">
                <span
                  className={cn(
                    'truncate text-[11px] font-semibold uppercase tracking-wider',
                    isActive
                      ? 'text-primary-600 dark:text-primary-300'
                      : 'text-slate-500 dark:text-slate-400',
                  )}
                >
                  {t(desc.labelKey, { defaultValue: desc.labelDefault })}
                </span>
                {mode === 'Pro' && (
                  <span className="truncate text-[10px] text-slate-400 dark:text-slate-500">
                    {t(desc.subtitleKey, { defaultValue: desc.subtitleDefault })}
                  </span>
                )}
              </span>
            </button>
            {idx < STEPS.length - 1 && (
              <span
                className={cn(
                  'hidden h-px flex-1 sm:block',
                  isCompleted
                    ? 'bg-success-400 dark:bg-success-500'
                    : 'bg-slate-200 dark:bg-slate-800',
                )}
                aria-hidden
              />
            )}
          </li>
        );
      })}
    </ol>
  );
};

export default WizardStepIndicator;
