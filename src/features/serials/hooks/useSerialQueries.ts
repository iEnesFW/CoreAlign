import { useQuery } from '@tanstack/react-query';
import { serialsApi } from '../api/serialsApi';

export const useSerialWhereUsedQuery = (serialNumber: string, enabled: boolean) =>
  useQuery({
    queryKey: ['serials', 'where-used', serialNumber] as const,
    queryFn: () => serialsApi.whereUsed(serialNumber),
    enabled: enabled && serialNumber.trim().length > 0,
  });
