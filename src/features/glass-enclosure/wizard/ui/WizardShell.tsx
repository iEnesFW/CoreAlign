import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, ArrowRight, X } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { useUxMode } from '@/shared/lib/persona';
import { useWizardStore, type WizardStep } from '../model/wizardStore';
import { WizardStepIndicator } from './WizardStepIndicator';

interface WizardShellProps {
  children: ReactNode;
  onClose: () => void;
  onBack?: () => void;
  onNext?: () => void;
  nextLabel?: string;
  nextDisabled?: boolean;
  nextLoading?: boolean;
  hideNext?: boolean;
  hideBack?: boolean;
}

const STEP_COUNT = 4;

const computeProgressPercent = (step: WizardStep): number => Math.round((step / STEP_COUNT) * 100);

export const WizardShell = ({
  children,
  onClose,
  onBack,
  onNext,
  nextLabel,
  nextDisabled,
  nextLoading,
  hideNext,
  hideBack,
}: WizardShellProps) => {
  const { t } = useTranslation();
  const step = useWizardStore((s) => s.step);
  const setStep = useWizardStore((s) => s.setStep);
  const mode = useUxMode();

  const handleBack = () => {
    if (onBack) {
      onBack();
      return;
    }
    if (step > 1) setStep((step - 1) as WizardStep);
  };

  const progress = computeProgressPercent(step);

  const headerLabel =
    mode === 'Simple'
      ? t('GlassEnclosure.NewProjectWizard.Header.Simple', {
          step,
          total: STEP_COUNT,
          defaultValue: 'Adım {{step}} / {{total}}',
        })
      : t('GlassEnclosure.NewProjectWizard.Header.Pro', {
          step,
          total: STEP_COUNT,
          defaultValue: 'Step {{step}}/{{total}}',
        });

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center sm:items-center sm:p-4">
      <button
        type="button"
        className="absolute inset-0 bg-slate-900/40 backdrop-blur-sm"
        aria-label={t('GlassEnclosure.NewProjectWizard.CloseAria', { defaultValue: 'Kapat' })}
        onClick={onClose}
      />
      <div
        role="dialog"
        aria-modal="true"
        className={cn(
          'relative flex max-h-[95vh] w-full flex-col overflow-hidden rounded-t-2xl border border-slate-200 bg-white shadow-2xl ring-1 ring-black/5',
          'sm:max-w-3xl sm:rounded-2xl',
          'dark:border-slate-800 dark:bg-slate-950 dark:ring-white/5',
        )}
      >
        <header className="flex items-start gap-3 border-b border-slate-200/80 bg-slate-50/40 px-4 py-3 dark:border-slate-800/80 dark:bg-slate-900/40 sm:px-6">
          <div className="min-w-0 flex-1">
            <p className="text-[11px] font-semibold uppercase tracking-wider text-primary-600 dark:text-primary-300">
              {headerLabel}
            </p>
            <h2 className="truncate text-sm font-semibold text-slate-900 dark:text-slate-100 sm:text-base">
              {t('GlassEnclosure.NewProjectWizard.Title', { defaultValue: 'Yeni Proje' })}
            </h2>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-md p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800 dark:hover:text-slate-200"
            aria-label={t('GlassEnclosure.NewProjectWizard.CloseAria', { defaultValue: 'Kapat' })}
          >
            <X size={16} />
          </button>
        </header>

        <div className="border-b border-slate-200/80 bg-slate-50/40 px-4 py-3 dark:border-slate-800/80 dark:bg-slate-900/40 sm:px-6">
          <WizardStepIndicator />
          <div className="mt-3 h-1.5 w-full overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
            <div
              className="h-full rounded-full bg-gradient-to-r from-primary-500 to-purple-600 transition-all"
              style={{ width: `${progress}%` }}
            />
          </div>
        </div>

        <div className="flex-1 overflow-y-auto px-4 py-4 sm:px-6 sm:py-6">{children}</div>

        <footer
          className={cn(
            'sticky bottom-0 flex items-center justify-between gap-2 border-t border-slate-200/80 bg-white/95 px-4 py-3 backdrop-blur-sm',
            'dark:border-slate-800/80 dark:bg-slate-950/95 sm:px-6',
          )}
        >
          {hideBack ? (
            <span />
          ) : (
            <button
              type="button"
              onClick={handleBack}
              disabled={step === 1}
              className={cn(
                'inline-flex items-center gap-1.5 rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50',
                'disabled:cursor-not-allowed disabled:opacity-50',
                'dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800',
              )}
            >
              <ArrowLeft size={14} />
              {t('GlassEnclosure.NewProjectWizard.Back', { defaultValue: 'Geri' })}
            </button>
          )}
          {!hideNext && onNext && (
            <button
              type="button"
              onClick={onNext}
              disabled={nextDisabled || nextLoading}
              className={cn(
                'inline-flex items-center gap-1.5 rounded-md bg-gradient-to-r from-primary-600 to-purple-600 px-4 py-1.5 text-sm font-semibold text-white shadow-md shadow-primary-500/20',
                'transition-opacity hover:opacity-95',
                'disabled:cursor-not-allowed disabled:opacity-50',
              )}
            >
              {nextLoading && (
                <span
                  aria-hidden
                  className="inline-block h-3.5 w-3.5 animate-spin rounded-full border-2 border-white/40 border-t-white"
                />
              )}
              {nextLabel ?? t('GlassEnclosure.NewProjectWizard.Next', { defaultValue: 'İleri' })}
              <ArrowRight size={14} />
            </button>
          )}
        </footer>
      </div>
    </div>
  );
};

export default WizardShell;
