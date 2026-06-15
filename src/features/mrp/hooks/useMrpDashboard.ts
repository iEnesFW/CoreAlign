import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { mrpApi } from '../api/mrpApi';

export const useMrpDashboardQuery = (topN = 20) =>
  useQuery({
    queryKey: ['mrp', 'dashboard', topN] as const,
    queryFn: () => mrpApi.dashboard(topN),
    staleTime: 60 * 1000,
  });

export const useStockProjectionQuery = (productId: string | null, daysAhead = 30) =>
  useQuery({
    queryKey: ['mrp', 'projection', productId, daysAhead] as const,
    queryFn: () => mrpApi.stockProjection(productId as string, daysAhead),
    enabled: !!productId,
    staleTime: 60 * 1000,
  });

export const useDemandForecastQuery = (productId: string | null, windowDays = 90) =>
  useQuery({
    queryKey: ['mrp', 'forecast', productId, windowDays] as const,
    queryFn: () => mrpApi.demandForecast(productId as string, windowDays),
    enabled: !!productId,
    staleTime: 60 * 1000,
  });

export const useGenerateMrpSuggestions = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (asOfDateUtc?: string | null) => mrpApi.generateSuggestions(asOfDateUtc),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['mrp'] });
      qc.invalidateQueries({ queryKey: ['purchase-requisitions'] });
    },
  });
};
