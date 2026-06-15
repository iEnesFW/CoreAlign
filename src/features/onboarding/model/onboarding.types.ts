import type { Step } from 'react-joyride';

export type TourKey = 'dashboard' | 'designer' | 'mrp' | 'installation';

export type TourStatus = 'pending' | 'completed' | 'skipped';

export interface TourDefinition {
  key: TourKey;
  i18nNamespace: string;
  buildSteps: (translate: TourTranslate) => Step[];
}

export type TourTranslate = (key: string, defaultValue: string) => string;

export const TOUR_KEYS: readonly TourKey[] = [
  'dashboard',
  'designer',
  'mrp',
  'installation',
] as const;
