import { apiClient } from '@/shared/api/apiClient';

export interface PortalProfile {
  userId: string;
  email: string;
  username: string;
  firstName: string | null;
  lastName: string | null;
  phoneNumber: string | null;
  avatarUrl: string | null;
  preferredLocale: string | null;
  isTwoFactorEnabled: boolean;
  tenantId: string;
  tenantName: string;
}

export interface UpdatePortalProfileInput {
  firstName?: string | null;
  lastName?: string | null;
  phoneNumber?: string | null;
  preferredLocale?: string | null;
}

export interface ChangePasswordInput {
  currentPassword: string;
  newPassword: string;
}

export interface PortalSession {
  id: string;
  deviceInfo: string | null;
  ipAddress: string | null;
  createdAtUtc: string;
  lastActivityAtUtc: string;
  expiresAtUtc: string;
  isCurrent: boolean;
}

export interface PortalNotificationPreference {
  notificationKind: string;
  emailEnabled: boolean;
  inAppEnabled: boolean;
}

export interface TwoFactorEnrollment {
  qrCodeUri: string;
  manualKey: string;
}

export interface TwoFactorBackupCodes {
  backupCodes: string[];
}

export interface InitiateInvoicePaymentResult {
  paymentSessionId: string;
  gatewayName: string;
  intentId: string;
  redirectUrl: string | null;
  amount: number;
  currency: string;
  invoiceNumber: string;
}

export const portalProfileApi = {
  getProfile: async (): Promise<PortalProfile> => {
    const { data } = await apiClient.get<PortalProfile>('/customer-portal/profile');
    return data;
  },
  updateProfile: async (input: UpdatePortalProfileInput): Promise<PortalProfile> => {
    const { data } = await apiClient.put<PortalProfile>('/customer-portal/profile', input);
    return data;
  },
  changePassword: async (input: ChangePasswordInput): Promise<boolean> => {
    const { data } = await apiClient.post<boolean>('/auth/change-password', input);
    return data;
  },
  listSessions: async (): Promise<PortalSession[]> => {
    const { data } = await apiClient.get<PortalSession[]>('/customer-portal/profile/sessions');
    return data;
  },
  revokeAllSessions: async (): Promise<number> => {
    const { data } = await apiClient.post<number>('/customer-portal/profile/sessions/revoke-all');
    return data;
  },
  listNotificationPreferences: async (): Promise<PortalNotificationPreference[]> => {
    const { data } = await apiClient.get<PortalNotificationPreference[]>(
      '/customer-portal/notification-preferences',
    );
    return data;
  },
  updateNotificationPreference: async (
    input: PortalNotificationPreference,
  ): Promise<PortalNotificationPreference> => {
    const { data } = await apiClient.put<PortalNotificationPreference>(
      '/customer-portal/notification-preferences',
      input,
    );
    return data;
  },
  enrollTwoFactor: async (): Promise<TwoFactorEnrollment> => {
    const { data } = await apiClient.post<TwoFactorEnrollment>('/auth/2fa/enroll');
    return data;
  },
  verifyTwoFactor: async (code: string): Promise<TwoFactorBackupCodes> => {
    const { data } = await apiClient.post<TwoFactorBackupCodes>('/auth/2fa/verify', { code });
    return data;
  },
  disableTwoFactor: async (password: string): Promise<boolean> => {
    const { data } = await apiClient.post<boolean>('/auth/2fa/disable', { password });
    return data;
  },
  regenerateBackupCodes: async (password: string): Promise<TwoFactorBackupCodes> => {
    const { data } = await apiClient.post<TwoFactorBackupCodes>(
      '/auth/2fa/backup-codes/regenerate',
      {
        password,
      },
    );
    return data;
  },
  payInvoice: async (invoiceId: string): Promise<InitiateInvoicePaymentResult> => {
    const { data } = await apiClient.post<InitiateInvoicePaymentResult>(
      `/customer-portal/invoices/${invoiceId}/pay`,
      {},
    );
    return data;
  },
};
