import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { dealerApi, type SetDealerProductVisibilityRequest } from './api';

export const visibilityKeys = {
  visibility: (linkId: string) => ['dealer-visibility', linkId] as const,
  catalog: (search: string) => ['customer-catalog-products', search] as const,
};

export const useDealerVisibility = (linkId: string | undefined) =>
  useQuery({
    queryKey: visibilityKeys.visibility(linkId ?? ''),
    queryFn: () => dealerApi.getDealerVisibility(linkId!),
    enabled: !!linkId,
    staleTime: 30_000,
  });

export const useSetDealerVisibility = (linkId: string) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: SetDealerProductVisibilityRequest) =>
      dealerApi.setDealerVisibility(linkId, payload),
    onSuccess: (data) => {
      queryClient.setQueryData(visibilityKeys.visibility(linkId), data);
    },
  });
};

export const useCustomerCatalogProducts = (search: string, enabled: boolean = true) =>
  useQuery({
    queryKey: visibilityKeys.catalog(search),
    queryFn: () =>
      dealerApi.listCatalogProducts({ search: search || undefined, page: 1, pageSize: 50 }),
    enabled,
    staleTime: 30_000,
  });
