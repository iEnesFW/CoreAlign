export type DunningType = 'InvoiceDueReminder' | 'QuoteExpiringReminder' | 'StockCriticalReminder';

export const DUNNING_TYPES: readonly DunningType[] = [
  'InvoiceDueReminder',
  'QuoteExpiringReminder',
  'StockCriticalReminder',
];

export interface DunningSetting {
  type: DunningType;
  isEnabled: boolean;
  sendInApp: boolean;
  sendEmail: boolean;
  recipientUserIds: string[];
}

export type UpsertDunningSettingInput = DunningSetting;
