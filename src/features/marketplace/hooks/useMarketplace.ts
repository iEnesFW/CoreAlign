import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { SafeResult } from '@/shared/lib/safeRequest';
import {
  marketplaceApi,
  type MarketplaceListParams,
  type MarketplaceReviewDto,
  type MarketplaceSubmissionDto,
  type MarketplaceTemplateDetailDto,
  type MarketplaceTemplateSummaryDto,
  type PublishMarketplacePayload,
  type RateMarketplacePayload,
  type RejectMarketplacePayload,
  type SubmitMarketplacePayload,
} from '../api/marketplaceApi';

const FIVE_MINUTES = 5 * 60 * 1000;

export const marketplaceKeys = {
  all: ['marketplace'] as const,
  list: (params: MarketplaceListParams) => ['marketplace', 'list', params] as const,
  detail: (id: string) => ['marketplace', 'detail', id] as const,
  reviews: (id: string) => ['marketplace', 'reviews', id] as const,
  mySubmissions: () => ['marketplace', 'my-submissions'] as const,
  pending: () => ['marketplace', 'admin', 'pending'] as const,
};

const unwrapSafe = async <T>(promise: Promise<SafeResult<T>>): Promise<T> => {
  const [data, error] = await promise;
  if (error) {
    throw error;
  }
  return data as T;
};

export const useMarketplaceListQuery = (params: MarketplaceListParams = {}) =>
  useQuery({
    queryKey: marketplaceKeys.list(params),
    queryFn: () => unwrapSafe<MarketplaceTemplateSummaryDto[]>(marketplaceApi.list(params)),
    staleTime: FIVE_MINUTES,
    placeholderData: (previous) => previous,
  });

export const useMarketplaceDetailQuery = (id: string | undefined) =>
  useQuery({
    queryKey: marketplaceKeys.detail(id ?? ''),
    queryFn: () => unwrapSafe<MarketplaceTemplateDetailDto>(marketplaceApi.detail(id as string)),
    enabled: Boolean(id),
    staleTime: FIVE_MINUTES,
  });

export const useMarketplaceReviewsQuery = (id: string | undefined) =>
  useQuery({
    queryKey: marketplaceKeys.reviews(id ?? ''),
    queryFn: () => unwrapSafe<MarketplaceReviewDto[]>(marketplaceApi.reviews(id as string)),
    enabled: Boolean(id),
    staleTime: FIVE_MINUTES,
  });

export const useMySubmissionsQuery = () =>
  useQuery({
    queryKey: marketplaceKeys.mySubmissions(),
    queryFn: () => unwrapSafe<MarketplaceSubmissionDto[]>(marketplaceApi.listMySubmissions()),
  });

export const usePendingSubmissionsQuery = (enabled = true) =>
  useQuery({
    queryKey: marketplaceKeys.pending(),
    queryFn: () => unwrapSafe<MarketplaceSubmissionDto[]>(marketplaceApi.listPending()),
    enabled,
  });

export const useInstallTemplateMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => unwrapSafe(marketplaceApi.install(id)),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: marketplaceKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: marketplaceKeys.all });
    },
  });
};

export const useSubmitTemplateMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: SubmitMarketplacePayload) => unwrapSafe(marketplaceApi.submit(payload)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: marketplaceKeys.mySubmissions() });
    },
  });
};

export const useRateTemplateMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: RateMarketplacePayload) => unwrapSafe(marketplaceApi.rate(payload)),
    onSuccess: (_data, vars) => {
      queryClient.invalidateQueries({ queryKey: marketplaceKeys.reviews(vars.templateId) });
      queryClient.invalidateQueries({ queryKey: marketplaceKeys.detail(vars.templateId) });
    },
  });
};

export const usePublishTemplateMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: PublishMarketplacePayload) => unwrapSafe(marketplaceApi.publish(payload)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: marketplaceKeys.pending() });
      queryClient.invalidateQueries({ queryKey: marketplaceKeys.all });
    },
  });
};

export const useRejectTemplateMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: RejectMarketplacePayload) => unwrapSafe(marketplaceApi.reject(payload)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: marketplaceKeys.pending() });
    },
  });
};
