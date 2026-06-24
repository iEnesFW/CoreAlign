import { beforeEach, describe, expect, it } from 'vitest';
import { useAiHelperStore } from './aiHelperStore';

describe('aiHelperStore', () => {
  beforeEach(() => {
    useAiHelperStore.setState({ isOpen: false });
  });

  it('opens the panel', () => {
    useAiHelperStore.getState().open();
    expect(useAiHelperStore.getState().isOpen).toBe(true);
  });

  it('closes the panel', () => {
    useAiHelperStore.getState().open();
    useAiHelperStore.getState().close();
    expect(useAiHelperStore.getState().isOpen).toBe(false);
  });

  it('toggles the panel', () => {
    useAiHelperStore.getState().toggle();
    expect(useAiHelperStore.getState().isOpen).toBe(true);
    useAiHelperStore.getState().toggle();
    expect(useAiHelperStore.getState().isOpen).toBe(false);
  });
});
