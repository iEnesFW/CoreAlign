import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  CreateJournalEntryRequest,
  JournalEntry,
  JournalEntryListParams,
  JournalEntrySummary,
  JournalLineInput,
  TrialBalanceReport,
} from '../model/journalEntry.types';

const BASE = '/accounting/journal-entries';

export const journalEntryApi = {
  search: (params: JournalEntryListParams) =>
    apiClient
      .get<ApiResponse<PagedResult<JournalEntrySummary>>>(BASE, { params })
      .then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<ApiResponse<JournalEntry>>(`${BASE}/${id}`).then((r) => r.data),

  create: (request: CreateJournalEntryRequest) =>
    apiClient.post<ApiResponse<JournalEntry>>(BASE, request).then((r) => r.data),

  updateHeader: (
    id: string,
    request: {
      entryDate: string;
      postingDate: string;
      type: JournalEntry['type'];
      description?: string | null;
      reference?: string | null;
    },
  ) =>
    apiClient
      .put<ApiResponse<JournalEntry>>(`${BASE}/${id}/header`, { id, ...request })
      .then((r) => r.data),

  replaceLines: (id: string, lines: JournalLineInput[]) =>
    apiClient
      .put<ApiResponse<JournalEntry>>(`${BASE}/${id}/lines`, { id, lines })
      .then((r) => r.data),

  post: (id: string, postedByUserId?: string) =>
    apiClient
      .post<ApiResponse<JournalEntry>>(`${BASE}/${id}/post`, { id, postedByUserId })
      .then((r) => r.data),

  reverse: (id: string, reversalPostingDate?: string) =>
    apiClient
      .post<ApiResponse<JournalEntry>>(`${BASE}/${id}/reverse`, { id, reversalPostingDate })
      .then((r) => r.data),

  remove: (id: string) =>
    apiClient.delete<ApiResponse<boolean>>(`${BASE}/${id}`).then((r) => r.data),

  trialBalance: (params: { fromDate?: string; toDate?: string }) =>
    apiClient
      .get<ApiResponse<TrialBalanceReport>>('/accounting/trial-balance', { params })
      .then((r) => r.data),
};
