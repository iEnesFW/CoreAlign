import { describe, beforeEach, expect, it } from 'vitest';
import { useWizardStore } from '../model/wizardStore';

describe('wizardStore', () => {
  beforeEach(() => {
    useWizardStore.getState().reset();
  });

  it('initialises with step 1 and empty selections', () => {
    const state = useWizardStore.getState();
    expect(state.step).toBe(1);
    expect(state.category).toBeNull();
    expect(state.templateId).toBeNull();
    expect(state.meta.name).toBe('');
    expect(state.meta.customerId).toBeNull();
    expect(state.quickDims.runs).toEqual([]);
    expect(state.quickDims.skipped).toBe(false);
  });

  it('navigates between steps via setStep', () => {
    useWizardStore.getState().setStep(2);
    expect(useWizardStore.getState().step).toBe(2);
    useWizardStore.getState().setStep(4);
    expect(useWizardStore.getState().step).toBe(4);
  });

  it('selecting a category clears the previously selected template', () => {
    useWizardStore.getState().setCategory('Balcony');
    useWizardStore.getState().setTemplate('template-1');
    expect(useWizardStore.getState().templateId).toBe('template-1');

    useWizardStore.getState().setCategory('Greenhouse');
    expect(useWizardStore.getState().category).toBe('Greenhouse');
    expect(useWizardStore.getState().templateId).toBeNull();
  });

  it('patchMeta merges into existing meta without replacing untouched fields', () => {
    useWizardStore.getState().patchMeta({ name: 'Yildiz Apt' });
    useWizardStore.getState().patchMeta({ customerId: 'cust-1' });
    const meta = useWizardStore.getState().meta;
    expect(meta.name).toBe('Yildiz Apt');
    expect(meta.customerId).toBe('cust-1');
    expect(meta.addressText).toBe('');
  });

  it('setQuickDims replaces the entire quickDims input', () => {
    useWizardStore.getState().setQuickDims({
      runs: [{ widthMm: 3000, heightMm: 2400, panelCount: 4 }],
      skipped: false,
    });
    const dims = useWizardStore.getState().quickDims;
    expect(dims.runs).toHaveLength(1);
    expect(dims.runs[0]?.widthMm).toBe(3000);
    expect(dims.skipped).toBe(false);
  });

  it('reset returns the store to its initial state', () => {
    useWizardStore.getState().setStep(3);
    useWizardStore.getState().setCategory('ShowerCabin');
    useWizardStore.getState().setTemplate('template-2');
    useWizardStore.getState().patchMeta({ name: 'Temp', notes: 'pending' });
    useWizardStore.getState().setQuickDims({
      runs: [{ widthMm: 1000, heightMm: 2000, panelCount: 2 }],
      skipped: true,
    });

    useWizardStore.getState().reset();

    const state = useWizardStore.getState();
    expect(state.step).toBe(1);
    expect(state.category).toBeNull();
    expect(state.templateId).toBeNull();
    expect(state.meta.name).toBe('');
    expect(state.meta.notes).toBe('');
    expect(state.quickDims.runs).toEqual([]);
    expect(state.quickDims.skipped).toBe(false);
  });
});
