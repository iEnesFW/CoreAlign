import { useQuery } from '@tanstack/react-query';
import { getTypedClient } from './typedClient';

export const useTypedDealerDashboard = (enabled = false) =>
  useQuery({
    queryKey: ['typed', 'dealer-portal', 'dashboard'] as const,
    queryFn: () => getTypedClient().dashboard(),
    enabled,
    staleTime: 60_000,
  });

export const useTypedDealerCustomers = (enabled = false) =>
  useQuery({
    queryKey: ['typed', 'dealer-portal', 'customers'] as const,
    queryFn: () => getTypedClient().customers(),
    enabled,
    staleTime: 60_000,
  });

export const useTypedDealerAccounts = (customerId?: string, enabled = false) =>
  useQuery({
    queryKey: ['typed', 'dealer-portal', 'dealer-accounts', customerId ?? null] as const,
    queryFn: () => getTypedClient().dealerAccountsGET(customerId),
    enabled,
    staleTime: 60_000,
  });
