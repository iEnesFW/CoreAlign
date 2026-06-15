import { useTranslation } from 'react-i18next';
import { RotateCcw } from 'lucide-react';
import { useOnboardingController } from '../hooks/useOnboarding';
import { useOnboardingStore } from '../model/onboardingStore';
import type { TourKey, TourStatus } from '../model/onboarding.types';
import { TOUR_KEYS } from '../model/onboarding.types';

const TOUR_NAME_KEY: Record<TourKey, string> = {
  dashboard: 'Onboarding.TourName.Dashboard',
  designer: 'Onboarding.TourName.Designer',
  mrp: 'Onboarding.TourName.Mrp',
  installation: 'Onboarding.TourName.Installation',
};

const STATUS_KEY: Record<TourStatus, string> = {
  pending: 'Onboarding.Settings.StatusPending',
  completed: 'Onboarding.Settings.StatusCompleted',
  skipped: 'Onboarding.Settings.StatusSkipped',
};

const STATUS_TONE: Record<TourStatus, string> = {
  pending: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
  completed: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
  skipped: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
};

export const OnboardingSettingsSection = () => {
  const { t } = useTranslation();
  const { statuses, resetAll, resetOne } = useOnboardingController();
  const startTour = useOnboardingStore((s) => s.startTour);

  const handleResetAll = (): void => {
    resetAll();
  };

  const handleResetOne = (tourKey: TourKey): void => {
    resetOne(tourKey);
    startTour(tourKey);
  };

  return (
    <section className="space-y-4">
      <header className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
            {t('Onboarding.Settings.Title')}
          </h2>
          <p className="mt-0.5 text-xs text-slate-500 dark:text-slate-400">
            {t('Onboarding.Settings.Description')}
          </p>
        </div>
        <button
          type="button"
          onClick={handleResetAll}
          className="inline-flex items-center gap-1.5 rounded-md border border-slate-300 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
        >
          <RotateCcw size={13} />
          {t('Onboarding.Settings.ResetAll')}
        </button>
      </header>

      <ul className="space-y-2">
        {TOUR_KEYS.map((tourKey) => {
          const status = statuses[tourKey];
          return (
            <li
              key={tourKey}
              className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-slate-200 bg-white px-3 py-2 dark:border-slate-700 dark:bg-slate-900"
            >
              <div className="flex items-center gap-3">
                <span className="text-sm font-medium text-slate-800 dark:text-slate-200">
                  {t(TOUR_NAME_KEY[tourKey])}
                </span>
                <span
                  className={`rounded-full px-2 py-0.5 text-[10px] font-medium ${STATUS_TONE[status]}`}
                >
                  {t(STATUS_KEY[status])}
                </span>
              </div>
              <button
                type="button"
                onClick={() => handleResetOne(tourKey)}
                className="inline-flex items-center gap-1.5 rounded-md border border-indigo-300 bg-indigo-50 px-2.5 py-1 text-[11px] font-medium text-indigo-700 hover:bg-indigo-100 dark:border-indigo-700 dark:bg-indigo-500/10 dark:text-indigo-300"
              >
                {t('Onboarding.Settings.ResetOne')}
              </button>
            </li>
          );
        })}
      </ul>
    </section>
  );
};
