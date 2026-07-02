import { describe, expect, it } from 'vitest';
import { isEditableTarget } from './isEditableTarget';

const el = (tagName: string, contentEditable = false): EventTarget =>
  ({ tagName, isContentEditable: contentEditable }) as unknown as EventTarget;

describe('isEditableTarget', () => {
  it('returns true for text-entry elements', () => {
    expect(isEditableTarget(el('INPUT'))).toBe(true);
    expect(isEditableTarget(el('TEXTAREA'))).toBe(true);
    expect(isEditableTarget(el('SELECT'))).toBe(true);
  });

  it('returns true for contenteditable elements', () => {
    expect(isEditableTarget(el('DIV', true))).toBe(true);
  });

  it('returns false for non-editable elements', () => {
    expect(isEditableTarget(el('DIV'))).toBe(false);
    expect(isEditableTarget(el('BUTTON'))).toBe(false);
    expect(isEditableTarget(el('A'))).toBe(false);
  });

  it('returns false for null / non-element targets', () => {
    expect(isEditableTarget(null)).toBe(false);
    expect(isEditableTarget({} as EventTarget)).toBe(false);
  });
});
