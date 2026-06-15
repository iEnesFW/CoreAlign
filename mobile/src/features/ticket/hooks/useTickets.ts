import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  ticketApi,
  type AssignTicketRequest,
  type CreateTicketRequest,
  type ResolveTicketRequest,
  type ServiceTicketDetail,
  type ServiceTicketListItem,
} from '../api/ticketApi';
import { ticketCache, ticketQueue } from '@/shared/db/offlineQueue';

const KEYS = {
  list: ['tickets', 'list'] as const,
  assigned: ['tickets', 'assigned'] as const,
  detail: (id: string) => ['tickets', 'detail', id] as const,
};

const isNetworkError = (err: unknown): boolean => {
  if (typeof err !== 'object' || err === null) return false;
  const candidate = err as { code?: string; response?: unknown };
  if (candidate.code === 'ERR_NETWORK' || candidate.code === 'ECONNABORTED') return true;
  return candidate.response === undefined;
};

export const useTicketList = () =>
  useQuery<ServiceTicketListItem[]>({
    queryKey: KEYS.list,
    queryFn: () => ticketApi.list(),
    staleTime: 60_000,
  });

export const useAssignedTickets = () =>
  useQuery<ServiceTicketListItem[]>({
    queryKey: KEYS.assigned,
    queryFn: () => ticketApi.listAssignedToMe(),
    staleTime: 60_000,
  });

export const useTicketDetail = (id: string | null) =>
  useQuery<ServiceTicketDetail | null>({
    queryKey: id ? KEYS.detail(id) : ['tickets', 'detail', 'none'],
    enabled: Boolean(id),
    queryFn: async () => {
      if (!id) return null;
      try {
        const detail = await ticketApi.getById(id);
        await ticketCache.upsert(id, detail);
        return detail;
      } catch (err) {
        if (isNetworkError(err)) {
          const cached = await ticketCache.get<ServiceTicketDetail>(id);
          if (cached) return cached;
        }
        throw err;
      }
    },
    staleTime: 30_000,
  });

export interface CreateTicketOutcome {
  queued: boolean;
  detail?: ServiceTicketDetail;
  queueId?: string;
}

export const useCreateTicket = () => {
  const qc = useQueryClient();
  return useMutation<CreateTicketOutcome, Error, CreateTicketRequest>({
    mutationFn: async (body) => {
      try {
        const detail = await ticketApi.create(body);
        return { queued: false, detail };
      } catch (err) {
        if (isNetworkError(err)) {
          const queueId = await ticketQueue.enqueue('new', { kind: 'create', body });
          return { queued: true, queueId };
        }
        throw err;
      }
    },
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: KEYS.list });
      void qc.invalidateQueries({ queryKey: KEYS.assigned });
    },
  });
};

export const useAssignTicket = (id: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: AssignTicketRequest) => ticketApi.assign(id, body),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: KEYS.detail(id) });
      void qc.invalidateQueries({ queryKey: KEYS.list });
    },
  });
};

export const useResolveTicket = (id: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: ResolveTicketRequest) => ticketApi.resolve(id, body),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: KEYS.detail(id) });
      void qc.invalidateQueries({ queryKey: KEYS.list });
      void qc.invalidateQueries({ queryKey: KEYS.assigned });
    },
  });
};

export const useAddTicketComment = (id: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: string) => ticketApi.addComment(id, body),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: KEYS.detail(id) });
    },
  });
};

export const ticketQueryKeys = KEYS;
