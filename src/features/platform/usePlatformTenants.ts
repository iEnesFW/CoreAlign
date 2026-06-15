import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { platformTenantApi, type UpdatePlatformTenantRequest } from './platformTenantApi';

const LIST_KEY = (
  search: string | undefined,
  page: number,
  pageSize: number,
  includeArchived: boolean,
) => ['platform', 'tenants', search ?? '', page, pageSize, includeArchived] as const;
const DETAIL_KEY = (id: string) => ['platform', 'tenants', id] as const;

export const usePlatformTenantsQuery = (
  search: string | undefined,
  page: number,
  pageSize: number,
  includeArchived: boolean,
) =>
  useQuery({
    queryKey: LIST_KEY(search, page, pageSize, includeArchived),
    queryFn: () => platformTenantApi.list(search, page, pageSize, includeArchived),
    staleTime: 30 * 1000,
  });

export const usePlatformTenantQuery = (id: string | undefined) =>
  useQuery({
    queryKey: DETAIL_KEY(id ?? ''),
    queryFn: () => platformTenantApi.getById(id as string),
    enabled: !!id,
    staleTime: 30 * 1000,
  });

export const useUpdatePlatformTenant = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (request: UpdatePlatformTenantRequest) => platformTenantApi.update(request),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['platform', 'tenants'] }),
  });
};

export const useArchivePlatformTenant = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => platformTenantApi.archive(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['platform', 'tenants'] }),
  });
};

export const useRestorePlatformTenant = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => platformTenantApi.restore(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['platform', 'tenants'] }),
  });
};
