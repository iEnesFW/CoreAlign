import { useQuery } from '@tanstack/react-query';
import { getTypedClient } from './typedClient';

export const useTypedDashboard = (enabled = false) =>
  useQuery({
    queryKey: ['typed', 'customer-portal', 'dashboard'] as const,
    queryFn: () => getTypedClient().dashboard(),
    enabled,
    staleTime: 60_000,
  });

export const useTypedProfile = (enabled = false) =>
  useQuery({
    queryKey: ['typed', 'customer-portal', 'profile'] as const,
    queryFn: () => getTypedClient().profileGET(),
    enabled,
    staleTime: 60_000,
  });

export const useTypedDealers = (enabled = false) =>
  useQuery({
    queryKey: ['typed', 'customer-portal', 'dealers'] as const,
    queryFn: () => getTypedClient().dealers(),
    enabled,
    staleTime: 60_000,
  });
