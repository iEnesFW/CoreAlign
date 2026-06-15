import { useMutation, useQuery, useQueryClient, type UseQueryOptions } from '@tanstack/react-query';
import { dealerApi, type CreateDealerAccountRequest, type InviteDealerUserRequest } from './api';
import { portalKeys } from '@/features/portal/hooks';
import type { DealerUser } from '@/features/portal/types';

export const dealerKeys = {
  users: (dealerAccountId: string) => ['dealer-users', dealerAccountId] as const,
  links: (customerId?: string) => ['dealer-customer-links', customerId ?? null] as const,
};

export const useDealerUsers = (
  dealerAccountId: string | undefined,
  options?: Omit<UseQueryOptions<DealerUser[]>, 'queryKey' | 'queryFn' | 'enabled'>,
) =>
  useQuery({
    queryKey: dealerKeys.users(dealerAccountId ?? ''),
    queryFn: () => dealerApi.listDealerUsers(dealerAccountId!),
    enabled: !!dealerAccountId,
    staleTime: 30_000,
    ...options,
  });

export const useDealerLinks = (customerId?: string) =>
  useQuery({
    queryKey: dealerKeys.links(customerId),
    queryFn: () => dealerApi.listLinks(undefined, customerId),
    staleTime: 30_000,
  });

const invalidateDealerLists = (queryClient: ReturnType<typeof useQueryClient>) => {
  void queryClient.invalidateQueries({ queryKey: portalKeys.dealers });
  void queryClient.invalidateQueries({ queryKey: portalKeys.dashboard });
};

export const useCreateDealerAccount = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateDealerAccountRequest) => dealerApi.createDealer(payload),
    onSuccess: () => invalidateDealerLists(queryClient),
  });
};

export const useInviteDealerUser = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: InviteDealerUserRequest) => dealerApi.inviteDealerUser(payload),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: dealerKeys.users(variables.dealerAccountId) });
    },
  });
};

export const useUpdateDealerUserStatus = (dealerAccountId: string) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      status,
      reason,
    }: {
      id: string;
      status: 'Active' | 'Suspended' | 'Archived';
      reason?: string;
    }) => dealerApi.updateDealerUserStatus(id, status, reason),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: dealerKeys.users(dealerAccountId) });
      invalidateDealerLists(queryClient);
    },
  });
};

export const useUnlinkDealer = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ linkId, reason }: { linkId: string; reason?: string }) =>
      dealerApi.unlinkDealer(linkId, reason),
    onSuccess: () => invalidateDealerLists(queryClient),
  });
};
