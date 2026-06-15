import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  CONSENT_VERSION,
  readConsentDecision,
  writeConsentDecision,
  type ConsentDecision,
} from './consentStorage';
import { useCaptureConsentMutation } from './hooks';

type Mode = 'closed' | 'summary' | 'details';

const FOCUSABLE_SELECTOR =
  'button:not([disabled]), [href], input:not([disabled]):not([type="hidden"]), [tabindex]:not([tabindex="-1"])';

export const CookieBanner = () => {
  const { t } = useTranslation();
  const initialDecision = useMemo(() => readConsentDecision(), []);
  const [mode, setMode] = useState<Mode>(initialDecision ? 'closed' : 'summary');
  const [analytics, setAnalytics] = useState(initialDecision?.analytics ?? false);
  const [marketing, setMarketing] = useState(initialDecision?.marketing ?? false);
  const dialogRef = useRef<HTMLDivElement | null>(null);
  const lastFocusedRef = useRef<HTMLElement | null>(null);
  const captureMutation = useCaptureConsentMutation();

  const persist = useCallback(
    (next: ConsentDecision) => {
      ['essential', 'analytics', 'marketing'].forEach((purpose) => {
        const given = purpose === 'essential' || next[purpose as 'analytics' | 'marketing'];
        captureMutation.mutate({
          purpose: purpose as 'essential' | 'analytics' | 'marketing',
          version: CONSENT_VERSION,
          given,
        });
      });
    },
    [captureMutation],
  );

  const acceptAll = useCallback(() => {
    const next = writeConsentDecision(true, true);
    setAnalytics(true);
    setMarketing(true);
    persist(next);
    setMode('closed');
  }, [persist]);

  const essentialOnly = useCallback(() => {
    const next = writeConsentDecision(false, false);
    setAnalytics(false);
    setMarketing(false);
    persist(next);
    setMode('closed');
  }, [persist]);

  const savePreferences = useCallback(() => {
    const next = writeConsentDecision(analytics, marketing);
    persist(next);
    setMode('closed');
  }, [analytics, marketing, persist]);

  useEffect(() => {
    if (mode === 'closed') return;
    lastFocusedRef.current = document.activeElement as HTMLElement | null;
    const dialog = dialogRef.current;
    if (!dialog) return;

    const focusable = Array.from(dialog.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR));
    focusable[0]?.focus();

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault();
        essentialOnly();
        return;
      }
      if (event.key !== 'Tab') return;
      const live = Array.from(dialog.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR));
      if (live.length === 0) return;
      const first = live[0];
      const last = live[live.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    };

    document.addEventListener('keydown', onKeyDown);
    return () => {
      document.removeEventListener('keydown', onKeyDown);
      lastFocusedRef.current?.focus();
    };
  }, [mode, essentialOnly]);

  const categories = useMemo(
    () => [
      {
        key: 'essential' as const,
        title: t('consent.banner.categories.essential.title'),
        description: t('consent.banner.categories.essential.description'),
        checked: true,
        disabled: true,
        onChange: (): void => undefined,
      },
      {
        key: 'analytics' as const,
        title: t('consent.banner.categories.analytics.title'),
        description: t('consent.banner.categories.analytics.description'),
        checked: analytics,
        disabled: false,
        onChange: () => setAnalytics((current) => !current),
      },
      {
        key: 'marketing' as const,
        title: t('consent.banner.categories.marketing.title'),
        description: t('consent.banner.categories.marketing.description'),
        checked: marketing,
        disabled: false,
        onChange: () => setMarketing((current) => !current),
      },
    ],
    [analytics, marketing, t],
  );

  if (mode === 'closed') return null;

  return (
    <div
      ref={dialogRef}
      role="dialog"
      aria-modal="true"
      aria-label={t('consent.banner.title')}
      className="fixed inset-x-0 bottom-0 z-50 flex justify-center px-4 pb-4 sm:px-6"
    >
      <div className="w-full max-w-3xl rounded-2xl border border-slate-200 bg-white p-5 shadow-2xl dark:border-slate-700 dark:bg-slate-900">
        <h2 className="text-base font-semibold text-slate-900 dark:text-white">
          {t('consent.banner.title')}
        </h2>
        <p className="mt-2 text-sm text-slate-600 dark:text-slate-300">
          {t('consent.banner.intro')}
        </p>

        {mode === 'details' && (
          <ul className="mt-4 space-y-3" aria-label={t('consent.banner.categoriesAria')}>
            {categories.map((cat) => (
              <li
                key={cat.key}
                className="flex items-start justify-between gap-3 rounded-lg border border-slate-200 px-3 py-2 dark:border-slate-700"
              >
                <div>
                  <p className="text-sm font-medium text-slate-900 dark:text-white">{cat.title}</p>
                  <p className="text-xs text-slate-500 dark:text-slate-400">{cat.description}</p>
                </div>
                <label className="inline-flex cursor-pointer items-center gap-2 text-xs text-slate-600 dark:text-slate-300">
                  <input
                    type="checkbox"
                    checked={cat.checked}
                    onChange={cat.onChange}
                    disabled={cat.disabled}
                    aria-label={cat.title}
                    className="h-4 w-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500 disabled:cursor-not-allowed disabled:opacity-50"
                  />
                  {cat.disabled ? t('consent.banner.alwaysOn') : null}
                </label>
              </li>
            ))}
          </ul>
        )}

        <div className="mt-4 flex flex-col gap-2 sm:flex-row sm:flex-wrap sm:justify-end">
          {mode === 'summary' && (
            <button
              type="button"
              onClick={() => setMode('details')}
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              {t('consent.banner.managePreferences')}
            </button>
          )}
          {mode === 'details' && (
            <button
              type="button"
              onClick={savePreferences}
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              {t('consent.banner.savePreferences')}
            </button>
          )}
          <button
            type="button"
            onClick={essentialOnly}
            className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-800"
          >
            {t('consent.banner.essentialOnly')}
          </button>
          <button
            type="button"
            onClick={acceptAll}
            className="rounded-lg bg-indigo-600 px-3 py-2 text-sm font-semibold text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 dark:focus:ring-offset-slate-900"
          >
            {t('consent.banner.acceptAll')}
          </button>
        </div>
      </div>
    </div>
  );
};
