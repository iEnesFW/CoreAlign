import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ssoApi } from '../api/ssoApi';
import type {
  CreateSsoIdentityProviderRequest,
  UpdateSsoIdentityProviderRequest,
} from '../model/sso.types';

export const ssoQueryKeys = {
  all: ['sso-identity-providers'] as const,
  detail: (id: string) => ['sso-identity-providers', id] as const,
};

export const useSsoIdentityProviders = () =>
  useQuery({
    queryKey: ssoQueryKeys.all,
    queryFn: () => ssoApi.list(),
  });

export const useSsoIdentityProvider = (id: string | undefined) =>
  useQuery({
    queryKey: id ? ssoQueryKeys.detail(id) : ssoQueryKeys.all,
    queryFn: () => ssoApi.get(id as string),
    enabled: Boolean(id),
  });

export const useCreateSsoIdentityProvider = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateSsoIdentityProviderRequest) => ssoApi.create(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ssoQueryKeys.all }),
  });
};

export const useUpdateSsoIdentityProvider = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateSsoIdentityProviderRequest }) =>
      ssoApi.update(id, body),
    onSuccess: (_, vars) => {
      qc.invalidateQueries({ queryKey: ssoQueryKeys.all });
      qc.invalidateQueries({ queryKey: ssoQueryKeys.detail(vars.id) });
    },
  });
};

export const useDeleteSsoIdentityProvider = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => ssoApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ssoQueryKeys.all }),
  });
};

export const useTestSsoConnection = () =>
  useMutation({
    mutationFn: (id: string) => ssoApi.testConnection(id),
  });
