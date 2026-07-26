import type { DunningSetting } from './dunning.types';

export const dunningSettingSignature = (setting: DunningSetting): string =>
  JSON.stringify([
    setting.type,
    setting.isEnabled,
    setting.sendInApp,
    setting.sendEmail,
    [...new Set(setting.recipientUserIds)].sort(),
  ]);

export const dunningSettingsEqual = (a: DunningSetting, b: DunningSetting): boolean =>
  dunningSettingSignature(a) === dunningSettingSignature(b);
