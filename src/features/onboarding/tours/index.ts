import type { Step } from 'react-joyride';
import type { TourKey, TourTranslate } from '../model/onboarding.types';
import { buildDashboardTour } from './dashboardTour';
import { buildDesignerTour } from './designerTour';
import { buildMrpDashboardTour } from './mrpDashboardTour';
import { buildInstallationTour } from './installationTour';

export const TOUR_BUILDERS: Record<TourKey, (t: TourTranslate) => Step[]> = {
  dashboard: buildDashboardTour,
  designer: buildDesignerTour,
  mrp: buildMrpDashboardTour,
  installation: buildInstallationTour,
};

export { buildDashboardTour, buildDesignerTour, buildMrpDashboardTour, buildInstallationTour };
