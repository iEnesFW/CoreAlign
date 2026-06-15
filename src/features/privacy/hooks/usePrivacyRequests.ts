import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { privacyApi } from '../api/privacyApi';
import type {
  DataSubjectRequestStatus,
  ProcessDataSubjectRequestBody,
  SubmitDataSubjectRequestBody,
  UpsertRetentionPolicyBody,
} from '../model/privacy.types';

const STALE_TIME_30S = 30 * 1000;
const STALE_TIME_5MIN = 5 * 60 * 1000;

export const privacyKeys = {
  all: ['privacy'] as const,
  requests: () => [...privacyKeys.all, 'requests'] as const,
  request: (id: string) => [...privacyKeys.all, 'request', id] as const,
  adminRequests: (status: DataSubjectRequestStatus | undefined, page: number, pageSize: number) =>
    [...privacyKeys.all, 'admin', 'requests', { status, page, pageSize }] as const,
  retentionPolicies: () => [...privacyKeys.all, 'retention-policies'] as const,
};

const invalidatePrivacy = (qc: ReturnType<typeof useQueryClient>) => {
  qc.invalidateQueries({ queryKey: privacyKeys.all });
};

export const useAdminRequestsQuery = (
  status: DataSubjectRequestStatus | undefined,
  page = 1,
  pageSize = 25,
) =>
  useQuery({
    queryKey: privacyKeys.adminRequests(status, page, pageSize),
    queryFn: () => privacyApi.listAdminRequests(status, page, pageSize),
    staleTime: STALE_TIME_30S,
  });

export const usePrivacyRequestQuery = (id: string | null | undefined) =>
  useQuery({
    queryKey: privacyKeys.request(id ?? ''),
    queryFn: () => privacyApi.getRequest(id as string),
    enabled: !!id,
    staleTime: STALE_TIME_30S,
  });

export const useRetentionPoliciesQuery = () =>
  useQuery({
    queryKey: privacyKeys.retentionPolicies(),
    queryFn: () => privacyApi.listRetentionPolicies(),
    staleTime: STALE_TIME_5MIN,
  });

export const useSubmitPrivacyRequest = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: SubmitDataSubjectRequestBody) => privacyApi.submitRequest(body),
    onSuccess: () => invalidatePrivacy(qc),
  });
};

export const useProcessPrivacyRequest = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: ProcessDataSubjectRequestBody }) =>
      privacyApi.processRequest(id, body),
    onSuccess: () => invalidatePrivacy(qc),
  });
};

export const useCreateRetentionPolicy = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: UpsertRetentionPolicyBody) => privacyApi.createRetentionPolicy(body),
    onSuccess: () => invalidatePrivacy(qc),
  });
};

export const useUpdateRetentionPolicy = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpsertRetentionPolicyBody }) =>
      privacyApi.updateRetentionPolicy(id, body),
    onSuccess: () => invalidatePrivacy(qc),
  });
};
