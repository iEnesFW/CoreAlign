import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { journalEntryApi } from '../api/journalEntryApi';
import type {
  CreateJournalEntryRequest,
  JournalEntryListParams,
  JournalLineInput,
} from '../model/journalEntry.types';

const listKey = (params: JournalEntryListParams) =>
  ['accounting', 'journal-entries', 'list', params] as const;
const detailKey = (id: string) => ['accounting', 'journal-entries', 'detail', id] as const;
const trialBalanceKey = (params: { fromDate?: string; toDate?: string }) =>
  ['accounting', 'trial-balance', params] as const;

const invalidate = (qc: ReturnType<typeof useQueryClient>) => {
  qc.invalidateQueries({ queryKey: ['accounting', 'journal-entries'] });
  qc.invalidateQueries({ queryKey: ['accounting', 'trial-balance'] });
};

export const useJournalEntriesQuery = (params: JournalEntryListParams) =>
  useQuery({
    queryKey: listKey(params),
    queryFn: () => journalEntryApi.search(params),
    staleTime: 30 * 1000,
  });

export const useJournalEntryQuery = (id: string | undefined) =>
  useQuery({
    queryKey: detailKey(id ?? ''),
    queryFn: () => journalEntryApi.getById(id as string),
    enabled: !!id,
    staleTime: 30 * 1000,
  });

export const useCreateJournalEntry = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateJournalEntryRequest) => journalEntryApi.create(request),
    onSuccess: () => invalidate(qc),
  });
};

export const useReplaceJournalEntryLines = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, lines }: { id: string; lines: JournalLineInput[] }) =>
      journalEntryApi.replaceLines(id, lines),
    onSuccess: () => invalidate(qc),
  });
};

export const usePostJournalEntry = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => journalEntryApi.post(id),
    onSuccess: () => invalidate(qc),
  });
};

export const useReverseJournalEntry = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reversalPostingDate }: { id: string; reversalPostingDate?: string }) =>
      journalEntryApi.reverse(id, reversalPostingDate),
    onSuccess: () => invalidate(qc),
  });
};

export const useDeleteJournalEntry = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => journalEntryApi.remove(id),
    onSuccess: () => invalidate(qc),
  });
};

export const useTrialBalanceQuery = (params: { fromDate?: string; toDate?: string }) =>
  useQuery({
    queryKey: trialBalanceKey(params),
    queryFn: () => journalEntryApi.trialBalance(params),
    staleTime: 60 * 1000,
  });

export const useJournalEntriesBySource = (sourceDocumentId: string | null) =>
  useQuery({
    queryKey: ['accounting', 'journal-entries', 'by-source', sourceDocumentId] as const,
    queryFn: () => journalEntryApi.bySource(sourceDocumentId as string),
    enabled: !!sourceDocumentId,
    staleTime: 30 * 1000,
  });
