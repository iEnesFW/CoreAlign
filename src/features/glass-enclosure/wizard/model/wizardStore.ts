import { create } from 'zustand';
import type { WizardEnclosureCategory } from './enclosure.types';

export type WizardStep = 1 | 2 | 3 | 4;

export interface ProjectMeta {
  name: string;
  customerId: string | null;
  addressText: string;
  notes: string;
  targetTotalWidthMm?: number | null;
  targetTotalHeightMm?: number | null;
}

export interface QuickRunDimensions {
  widthMm: number;
  heightMm: number;
  panelCount: number;
  turnDeg?: number;
}

export interface QuickDimensionsInput {
  runs: QuickRunDimensions[];
  skipped: boolean;
}

interface WizardState {
  step: WizardStep;
  category: WizardEnclosureCategory | null;
  templateId: string | null;
  meta: ProjectMeta;
  quickDims: QuickDimensionsInput;
  setStep: (step: WizardStep) => void;
  setCategory: (category: WizardEnclosureCategory) => void;
  setTemplate: (templateId: string | null) => void;
  patchMeta: (patch: Partial<ProjectMeta>) => void;
  setQuickDims: (input: QuickDimensionsInput) => void;
  reset: () => void;
}

const initialMeta: ProjectMeta = {
  name: '',
  customerId: null,
  addressText: '',
  notes: '',
  targetTotalWidthMm: null,
  targetTotalHeightMm: null,
};

const initialQuickDims: QuickDimensionsInput = {
  runs: [],
  skipped: false,
};

const initialState = {
  step: 1 as WizardStep,
  category: null,
  templateId: null,
  meta: initialMeta,
  quickDims: initialQuickDims,
};

export const useWizardStore = create<WizardState>((set) => ({
  ...initialState,
  setStep: (step) => set({ step }),
  setCategory: (category) => set({ category, templateId: null }),
  setTemplate: (templateId) => set({ templateId }),
  patchMeta: (patch) => set((s) => ({ meta: { ...s.meta, ...patch } })),
  setQuickDims: (quickDims) => set({ quickDims }),
  reset: () => set(initialState),
}));
