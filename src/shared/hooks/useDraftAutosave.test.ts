import { describe, it, expect, beforeEach } from 'vitest';
import { act, renderHook } from '@testing-library/react';
import { useDraftAutosave } from './useDraftAutosave';

const KEY = 'test:draft-autosave';

describe('useDraftAutosave', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('peekDraft returns null when nothing is stored', () => {
    const { result } = renderHook(() => useDraftAutosave(KEY, { a: 1 }, { enabled: false }));
    expect(result.current.peekDraft()).toBeNull();
  });

  it('saveNow persists the value, peekDraft reads it back, clearDraft removes it', () => {
    const { result } = renderHook(() =>
      useDraftAutosave(KEY, { a: 1, b: 'x' }, { enabled: false }),
    );

    act(() => result.current.saveNow());
    expect(result.current.peekDraft()).toEqual({ a: 1, b: 'x' });
    expect(result.current.lastSavedAt).not.toBeNull();

    act(() => result.current.clearDraft());
    expect(result.current.peekDraft()).toBeNull();
    expect(result.current.lastSavedAt).toBeNull();
  });

  it('peekDraft returns null when the stored value is malformed JSON', () => {
    localStorage.setItem(KEY, '{not-valid-json');
    const { result } = renderHook(() => useDraftAutosave(KEY, { a: 1 }, { enabled: false }));
    expect(result.current.peekDraft()).toBeNull();
  });
});
