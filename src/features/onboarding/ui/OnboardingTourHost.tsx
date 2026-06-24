import { useEffect, useMemo, useRef } from 'react';
import { useLocation } from 'react-router-dom';
import { Joyride, EVENTS, STATUS, type EventData, type Step } from 'react-joyride';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '@/shared/lib/store/authStore';
import { usePersonaStore } from '@/shared/lib/persona';
import { useOnboardingStore } from '../model/onboardingStore';
import type { TourKey, TourTranslate } from '../model/onboarding.types';
import { TOUR_BUILDERS } from '../tours';
import { readTourStatus, writeTourStatus } from '../hooks/onboardingStorage';

const ROUTE_TO_TOUR: { matcher: RegExp; tourKey: TourKey }[] = [
  { matcher: /^\/dashboard\/?$/, tourKey: 'dashboard' },
  { matcher: /^\/dashboard\/glass-enclosure\/projects\/[^/]+$/, tourKey: 'designer' },
  { matcher: /^\/dashboard\/mrp\/?$/, tourKey: 'mrp' },
  { matcher: /^\/dashboard\/installation\/acceptances\/[^/]+$/, tourKey: 'installation' },
];

const resolveTourKeyForPath = (pathname: string): TourKey | null => {
  for (const entry of ROUTE_TO_TOUR) {
    if (entry.matcher.test(pathname)) return entry.tourKey;
  }
  return null;
};

export const OnboardingTourHost = () => {
  const { t } = useTranslation();
  const location = useLocation();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const userId = useAuthStore((s) => s.user?.id ?? null);
  const personaMode = usePersonaStore((s) => s.mode);
  const activeTour = useOnboardingStore((s) => s.activeTour);
  const startTour = useOnboardingStore((s) => s.startTour);
  const stopTour = useOnboardingStore((s) => s.stopTour);

  const autoStartedRef = useRef<Set<string>>(new Set());

  const tourForRoute = useMemo(() => resolveTourKeyForPath(location.pathname), [location.pathname]);

  useEffect(() => {
    if (!isAuthenticated) return;
    if (personaMode !== 'Simple') return;
    if (!tourForRoute) return;
    const sessionKey = `${userId ?? 'anon'}:${tourForRoute}`;
    if (autoStartedRef.current.has(sessionKey)) return;
    const status = readTourStatus(userId, tourForRoute);
    if (status !== 'pending') return;
    autoStartedRef.current.add(sessionKey);
    const timer = window.setTimeout(() => {
      startTour(tourForRoute);
    }, 600);
    return () => window.clearTimeout(timer);
  }, [isAuthenticated, personaMode, tourForRoute, userId, startTour]);

  const tourKey: TourKey | null = activeTour;

  const steps: Step[] = useMemo(() => {
    if (!tourKey) return [];
    const translate: TourTranslate = (key, defaultValue) =>
      t(key, { defaultValue }) as unknown as string;
    return TOUR_BUILDERS[tourKey](translate);
  }, [tourKey, t]);

  const handleEvent = (data: EventData): void => {
    const finished: string[] = [STATUS.FINISHED, STATUS.SKIPPED];
    if (data.type === EVENTS.TOUR_END && finished.includes(data.status)) {
      if (tourKey) {
        const finalStatus = data.status === STATUS.SKIPPED ? 'skipped' : 'completed';
        writeTourStatus(userId, tourKey, finalStatus);
      }
      stopTour();
    }
  };

  if (!isAuthenticated || !tourKey || steps.length === 0) {
    return null;
  }

  return (
    <Joyride
      steps={steps}
      run
      continuous
      scrollToFirstStep
      onEvent={handleEvent}
      locale={{
        back: t('Onboarding.Action.Previous', { defaultValue: 'Geri' }) as string,
        close: t('Onboarding.Action.Skip', { defaultValue: 'Atla' }) as string,
        last: t('Onboarding.Action.Finish', { defaultValue: 'Bitir' }) as string,
        next: t('Onboarding.Action.Next', { defaultValue: 'İleri' }) as string,
        skip: t('Onboarding.Action.Skip', { defaultValue: 'Atla' }) as string,
      }}
      options={{
        primaryColor: '#6366f1',
        zIndex: 10000,
        showProgress: true,
        buttons: ['back', 'primary', 'skip'],
        overlayClickAction: false,
      }}
    />
  );
};
