import { create } from 'zustand';
import type { TourKey } from './onboarding.types';

interface OnboardingState {
  activeTour: TourKey | null;
  startTour: (tourKey: TourKey) => void;
  stopTour: () => void;
}

export const useOnboardingStore = create<OnboardingState>((set) => ({
  activeTour: null,
  startTour: (tourKey) => set({ activeTour: tourKey }),
  stopTour: () => set({ activeTour: null }),
}));
