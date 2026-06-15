import type { ProviderCategory } from '../providers.types';
import type { WebhookHistoryFilters } from '../api/providersAdminApi';

export const providerKeys = {
  all: ['admin', 'providers'] as const,
  lists: () => [...providerKeys.all, 'list'] as const,
  health: (category: ProviderCategory, name: string) =>
    [...providerKeys.all, 'health', category, name] as const,
  webhookHistory: (filters: WebhookHistoryFilters) =>
    [...providerKeys.all, 'webhook-history', filters] as const,
};
