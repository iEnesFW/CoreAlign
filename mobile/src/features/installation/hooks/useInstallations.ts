import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  installationApi,
  type AcceptInstallationRequest,
  type CreatePunchItemRequest,
  type InstallationDetail,
  type InstallationListItem,
  type RejectInstallationRequest,
  type SubmitSignatureRequest,
  type UpdateChecklistItemRequest,
} from '../api/installationApi';
import { acceptanceQueue, installationCache, newIdempotencyKey } from '@/shared/db/offlineQueue';

const KEYS = {
  list: ['installations', 'pending'] as const,
  detail: (id: string) => ['installations', 'detail', id] as const,
};

const isNetworkError = (err: unknown): boolean => {
  if (typeof err !== 'object' || err === null) return false;
  const candidate = err as { message?: string; code?: string; response?: unknown };
  if (candidate.code === 'ERR_NETWORK' || candidate.code === 'ECONNABORTED') return true;
  if (candidate.response === undefined) return true;
  return false;
};

export const useInstallationList = () => {
  return useQuery<InstallationListItem[]>({
    queryKey: KEYS.list,
    queryFn: () => installationApi.listPending(),
    staleTime: 30_000,
  });
};

export const useInstallationDetail = (id: string | null) => {
  return useQuery<InstallationDetail | null>({
    queryKey: id ? KEYS.detail(id) : ['installations', 'detail', 'none'],
    enabled: Boolean(id),
    queryFn: async () => {
      if (!id) return null;
      try {
        const detail = await installationApi.getById(id);
        await installationCache.upsert(id, detail);
        return detail;
      } catch (err) {
        if (isNetworkError(err)) {
          const cached = await installationCache.get<InstallationDetail>(id);
          if (cached) return cached;
        }
        throw err;
      }
    },
    staleTime: 15_000,
  });
};

export const useStartInstallation = (id: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => installationApi.start(id),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: KEYS.detail(id) });
      void qc.invalidateQueries({ queryKey: KEYS.list });
    },
  });
};

export const useUpdateChecklistItem = (id: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: UpdateChecklistItemRequest) => installationApi.updateChecklistItem(id, body),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: KEYS.detail(id) });
    },
  });
};

export const useSubmitSignature = (id: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: SubmitSignatureRequest) => installationApi.submitSignature(id, body),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: KEYS.detail(id) });
    },
  });
};

export interface AcceptOutcome {
  queued: boolean;
  detail?: InstallationDetail;
  queueId?: string;
}

export const useAcceptInstallation = (id: string) => {
  const qc = useQueryClient();
  return useMutation<AcceptOutcome, Error, Omit<AcceptInstallationRequest, 'idempotencyKey'>>({
    mutationFn: async (payload) => {
      const body: AcceptInstallationRequest = {
        ...payload,
        idempotencyKey: newIdempotencyKey(),
      };
      try {
        const detail = await installationApi.accept(id, body);
        return { queued: false, detail };
      } catch (err) {
        if (isNetworkError(err)) {
          const queueId = await acceptanceQueue.enqueue(id, body);
          return { queued: true, queueId };
        }
        throw err;
      }
    },
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: KEYS.list });
      void qc.invalidateQueries({ queryKey: KEYS.detail(id) });
    },
  });
};

export const useRejectInstallation = (id: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: Omit<RejectInstallationRequest, 'idempotencyKey'>) =>
      installationApi.reject(id, { ...payload, idempotencyKey: newIdempotencyKey() }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: KEYS.list });
      void qc.invalidateQueries({ queryKey: KEYS.detail(id) });
    },
  });
};

export const useAddPunchItem = (id: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreatePunchItemRequest) => installationApi.addPunchItem(id, body),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: KEYS.detail(id) });
    },
  });
};

export const useResolvePunchItem = (id: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (punchItemId: string) => installationApi.resolvePunchItem(id, punchItemId),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: KEYS.detail(id) });
    },
  });
};

export const installationQueryKeys = KEYS;
