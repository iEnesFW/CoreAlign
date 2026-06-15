import { apiClient } from '@/shared/api/apiClient';
import { safeRequest, type SafeResult } from '@/shared/lib/safeRequest';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type { ProviderCategory, ProviderHealthStatus } from '../providers.types';

export interface ProviderInfo {
  name: string;
  displayName: string;
  category: ProviderCategory;
  isConfigured: boolean;
  isEnabled: boolean;
  isDefault: boolean;
  isSandbox: boolean;
  lastHealthStatus: ProviderHealthStatus;
  lastHealthMessage: string | null;
  lastHealthCheckedUtc: string | null;
  lastUsedAtUtc: string | null;
  capabilities: string[];
}

export interface UpsertProviderConfigInput {
  category: ProviderCategory;
  providerName: string;
  displayName?: string | null;
  isDefault: boolean;
  isEnabled: boolean;
  plaintextCredentialsJson?: string | null;
  enabledCapabilities: number;
}

export interface UpsertProviderConfigResult {
  id: string;
  category: string;
  providerName: string;
  displayName: string | null;
  isDefault: boolean;
  isEnabled: boolean;
  enabledCapabilities: number;
  lastHealthCheckUtc: string | null;
  lastHealthStatus: ProviderHealthStatus;
  lastHealthMessage: string | null;
}

export interface ProviderHealthSummary {
  providerName: string;
  category: ProviderCategory;
  isHealthy: boolean;
  message: string | null;
  responseTimeMs: number;
  checkedAtUtc: string;
  endpointProbed: string | null;
  httpStatusCode: number | null;
}

export interface TestSuiteStepResult {
  stepName: string;
  passed: boolean;
  detail: string | null;
  durationMs: number;
}

export interface TestSuiteResult {
  providerName: string;
  category: ProviderCategory;
  sandbox: boolean;
  allPassed: boolean;
  startedAtUtc: string;
  completedAtUtc: string;
  steps: TestSuiteStepResult[];
}

export type WebhookInboxStatus =
  | 'Received'
  | 'Processed'
  | 'Failed'
  | 'Retrying'
  | 'Discarded'
  | 'Pending';

export interface WebhookInboxItem {
  id: string;
  providerName: string;
  category: ProviderCategory;
  eventType: string | null;
  status: WebhookInboxStatus;
  processingError: string | null;
  retryCount: number;
  receivedAtUtc: string;
  processedAtUtc: string | null;
}

export interface WebhookHistoryFilters {
  providerName?: string;
  category?: ProviderCategory;
  status?: WebhookInboxStatus;
  fromUtc?: string;
  toUtc?: string;
  page?: number;
  pageSize?: number;
}

const PROVIDERS_BASE = '/admin/providers';
const WEBHOOKS_BASE = '/admin/webhooks';

const unwrap = async <T>(promise: Promise<{ data: ApiResponse<T> }>): Promise<T> => {
  const { data } = await promise;
  if (!data.isSuccess || data.data === null || data.data === undefined) {
    throw new Error(data.errors?.[0] ?? 'Request failed.');
  }
  return data.data as T;
};

export const providersAdminApi = {
  list: (): Promise<SafeResult<ProviderInfo[]>> =>
    safeRequest(unwrap<ProviderInfo[]>(apiClient.get(`${PROVIDERS_BASE}/catalog`))),

  upsertConfig: (
    body: UpsertProviderConfigInput,
  ): Promise<SafeResult<UpsertProviderConfigResult>> =>
    safeRequest(unwrap<UpsertProviderConfigResult>(apiClient.put(PROVIDERS_BASE, body))),

  delete: (id: string): Promise<SafeResult<void>> =>
    safeRequest(apiClient.delete(`${PROVIDERS_BASE}/${id}`).then(() => undefined)),

  checkHealth: (
    category: ProviderCategory,
    name: string,
  ): Promise<SafeResult<ProviderHealthSummary>> =>
    safeRequest(
      unwrap<ProviderHealthSummary>(apiClient.get(`${PROVIDERS_BASE}/${category}/${name}/health`)),
    ),

  runTestSuite: (category: ProviderCategory, name: string): Promise<SafeResult<TestSuiteResult>> =>
    safeRequest(
      unwrap<TestSuiteResult>(apiClient.post(`${PROVIDERS_BASE}/${category}/${name}/test-suite`)),
    ),

  listWebhookHistory: (
    filters: WebhookHistoryFilters,
  ): Promise<SafeResult<PagedResult<WebhookInboxItem>>> =>
    safeRequest(
      unwrap<PagedResult<WebhookInboxItem>>(
        apiClient.get(`${WEBHOOKS_BASE}/inbox`, { params: filters }),
      ),
    ),

  replayWebhook: (id: string): Promise<SafeResult<unknown>> =>
    safeRequest(apiClient.post(`${WEBHOOKS_BASE}/inbox/${id}/replay`).then((r) => r.data)),
};
