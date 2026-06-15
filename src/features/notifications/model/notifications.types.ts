export type NotificationChannel = 'InApp' | 'Email' | 'Sms' | 'Push' | 'WhatsApp';
export type NotificationStatus =
  | 'Pending'
  | 'Queued'
  | 'Sending'
  | 'Sent'
  | 'Delivered'
  | 'Failed'
  | 'Bounced'
  | 'Read';

export interface NotificationMessageView {
  id: string;
  channel: NotificationChannel;
  status: NotificationStatus;
  templateKey: string;
  categoryKey: string;
  subject: string | null;
  bodyMarkdown: string;
  createdAtUtc: string;
  readAtUtc: string | null;
}

export interface NotificationPreferenceView {
  categoryKey: string;
  channel: NotificationChannel;
  isEnabled: boolean;
}

export interface UpsertNotificationPreferenceInput {
  categoryKey: string;
  channel: NotificationChannel;
  isEnabled: boolean;
}
