import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';
import type {
  ImportCommitResult,
  ImportEntityKind,
  ImportPreviewResult,
} from '../model/import.types';

const BASE = '/imports';

export const importsApi = {
  preview: <TRow>(kind: ImportEntityKind, file: File) => {
    const form = new FormData();
    form.append('file', file);
    return apiClient
      .post<ApiResponse<ImportPreviewResult<TRow>>>(`${BASE}/${kind}/preview`, form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      .then((r) => r.data);
  },

  commit: (kind: ImportEntityKind, sessionId: string, skipInvalidRows: boolean) =>
    apiClient
      .post<ApiResponse<ImportCommitResult>>(`${BASE}/${kind}/commit`, {
        sessionId,
        skipInvalidRows,
      })
      .then((r) => r.data),
};
