import { describe, it, expect } from 'vitest';
import { dunningSettingSignature, dunningSettingsEqual } from './dunningSettingSignature';
import type { DunningSetting } from './dunning.types';

const base: DunningSetting = {
  type: 'InvoiceDueReminder',
  isEnabled: true,
  sendInApp: true,
  sendEmail: false,
  recipientUserIds: ['a', 'b'],
};

describe('dunningSettingSignature', () => {
  it('is stable when recipient order changes', () => {
    expect(dunningSettingSignature({ ...base, recipientUserIds: ['b', 'a'] })).toBe(
      dunningSettingSignature(base),
    );
  });

  it('ignores duplicate recipients', () => {
    expect(dunningSettingSignature({ ...base, recipientUserIds: ['b', 'a', 'a'] })).toBe(
      dunningSettingSignature(base),
    );
  });

  it('changes when a channel flag changes', () => {
    expect(dunningSettingSignature({ ...base, sendEmail: true })).not.toBe(
      dunningSettingSignature(base),
    );
  });

  it('changes when a recipient is added', () => {
    expect(dunningSettingSignature({ ...base, recipientUserIds: ['a', 'b', 'c'] })).not.toBe(
      dunningSettingSignature(base),
    );
  });

  it('changes when the enabled flag changes', () => {
    expect(dunningSettingSignature({ ...base, isEnabled: false })).not.toBe(
      dunningSettingSignature(base),
    );
  });
});

describe('dunningSettingsEqual', () => {
  it('treats reordered recipients as equal', () => {
    expect(dunningSettingsEqual(base, { ...base, recipientUserIds: ['b', 'a'] })).toBe(true);
  });

  it('treats a different recipient set as not equal', () => {
    expect(dunningSettingsEqual(base, { ...base, recipientUserIds: ['a'] })).toBe(false);
  });
});
