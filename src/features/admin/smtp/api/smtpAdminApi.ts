import { apiClient } from '@/shared/api/apiClient';
import { safeRequest, type SafeResult } from '@/shared/lib/safeRequest';
import type { ApiResponse } from '@/shared/types/api';
import type {
  SmtpSettings,
  SmtpHealthResult,
  SmtpTestResult,
  UpsertSmtpInput,
} from '../smtp.types';

const SMTP_BASE = '/admin/notifications/smtp';

const unwrap = async <T>(promise: Promise<{ data: ApiResponse<T> }>): Promise<T> => {
  const { data } = await promise;
  if (!data.isSuccess || data.data === null || data.data === undefined) {
    throw new Error(data.errors?.[0] ?? 'Request failed.');
  }
  return data.data as T;
};

export const smtpAdminApi = {
  get: (): Promise<SafeResult<SmtpSettings>> =>
    safeRequest(unwrap<SmtpSettings>(apiClient.get(SMTP_BASE))),

  upsert: (body: UpsertSmtpInput): Promise<SafeResult<SmtpSettings>> =>
    safeRequest(unwrap<SmtpSettings>(apiClient.put(SMTP_BASE, body))),

  test: (toAddress: string): Promise<SafeResult<SmtpTestResult>> =>
    safeRequest(unwrap<SmtpTestResult>(apiClient.post(`${SMTP_BASE}/test`, { toAddress }))),

  health: (): Promise<SafeResult<SmtpHealthResult>> =>
    safeRequest(unwrap<SmtpHealthResult>(apiClient.get(`${SMTP_BASE}/health`))),
};
