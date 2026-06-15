import { useMemo } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { providersAdminApi } from '../api/providersAdminApi';
import { providerKeys } from './providerKeys';
import type { SafeResult } from '@/shared/lib/safeRequest';
import type {
  ProviderInfo,
  UpsertProviderConfigInput,
  WebhookHistoryFilters,
} from '../api/providersAdminApi';
import type { ProviderCategory } from '../providers.types';

const unwrapSafe = async <T>(promise: Promise<SafeResult<T>>): Promise<T> => {
  const [data, error] = await promise;
  if (error) {
    throw error;
  }
  return data as T;
};

export const useProvidersListQuery = () =>
  useQuery({
    queryKey: providerKeys.lists(),
    queryFn: () => unwrapSafe<ProviderInfo[]>(providersAdminApi.list()),
  });

export const useProviderFromList = (
  category: ProviderCategory | null,
  name: string | null,
): ProviderInfo | null => {
  const { data } = useProvidersListQuery();
  return useMemo(() => {
    if (!data || !category || !name) return null;
    return data.find((p) => p.category === category && p.name === name) ?? null;
  }, [data, category, name]);
};

export const useUpsertProviderConfigMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: UpsertProviderConfigInput) =>
      unwrapSafe(providersAdminApi.upsertConfig(body)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: providerKeys.lists() });
    },
  });
};

export const useDeleteProviderConfigMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => unwrapSafe(providersAdminApi.delete(id)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: providerKeys.lists() });
    },
  });
};

export const useCheckProviderHealthMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ category, name }: { category: ProviderCategory; name: string }) =>
      unwrapSafe(providersAdminApi.checkHealth(category, name)),
    onSuccess: (_data, vars) => {
      queryClient.invalidateQueries({ queryKey: providerKeys.lists() });
      queryClient.invalidateQueries({
        queryKey: providerKeys.health(vars.category, vars.name),
      });
    },
  });
};

export const useSetDefaultProviderMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (provider: ProviderInfo) =>
      unwrapSafe(
        providersAdminApi.upsertConfig({
          category: provider.category,
          providerName: provider.name,
          displayName: provider.displayName,
          isDefault: true,
          isEnabled: provider.isEnabled,
          enabledCapabilities: 0,
        }),
      ),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: providerKeys.lists() });
    },
  });
};

export const useSetProviderEnabledMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ provider, enabled }: { provider: ProviderInfo; enabled: boolean }) =>
      unwrapSafe(
        providersAdminApi.upsertConfig({
          category: provider.category,
          providerName: provider.name,
          displayName: provider.displayName,
          isDefault: provider.isDefault,
          isEnabled: enabled,
          enabledCapabilities: 0,
        }),
      ),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: providerKeys.lists() });
    },
  });
};

export const useRunProviderTestSuiteMutation = () =>
  useMutation({
    mutationFn: ({ category, name }: { category: ProviderCategory; name: string }) =>
      unwrapSafe(providersAdminApi.runTestSuite(category, name)),
  });

export const useWebhookHistoryQuery = (filters: WebhookHistoryFilters, enabled = true) =>
  useQuery({
    queryKey: providerKeys.webhookHistory(filters),
    queryFn: () => unwrapSafe(providersAdminApi.listWebhookHistory(filters)),
    enabled,
    placeholderData: (previous) => previous,
  });

export const useReplayWebhookMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => unwrapSafe(providersAdminApi.replayWebhook(id)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [...providerKeys.all, 'webhook-history'] });
    },
  });
};
