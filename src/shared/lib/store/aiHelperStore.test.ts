import { beforeEach, describe, expect, it } from 'vitest';
import { useAiHelperStore } from './aiHelperStore';

describe('aiHelperStore', () => {
  beforeEach(() => {
    useAiHelperStore.setState({ isOpen: false, isAvailable: false });
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

  // The trigger lives in the Footer while the status probe lives in the panel widget, so
  // availability has to travel through the store — a hidden trigger means the server said off.
  it('starts unavailable so the Footer trigger stays hidden until the server answers', () => {
    expect(useAiHelperStore.getState().isAvailable).toBe(false);
  });

  it('publishes availability for the trigger', () => {
    useAiHelperStore.getState().setAvailable(true);
    expect(useAiHelperStore.getState().isAvailable).toBe(true);
    useAiHelperStore.getState().setAvailable(false);
    expect(useAiHelperStore.getState().isAvailable).toBe(false);
  });

  it('keeps open state and availability independent', () => {
    useAiHelperStore.getState().open();
    useAiHelperStore.getState().setAvailable(true);
    expect(useAiHelperStore.getState().isOpen).toBe(true);
    useAiHelperStore.getState().setAvailable(false);
    expect(useAiHelperStore.getState().isOpen).toBe(true);
  });
});
