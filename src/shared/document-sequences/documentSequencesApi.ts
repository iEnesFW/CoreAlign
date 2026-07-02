import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';
import type { ConfigureDocumentSequenceRequest, DocumentSequenceConfig } from './types';

const BASE = '/settings/document-sequences';

export const documentSequencesApi = {
  list: () => apiClient.get<ApiResponse<DocumentSequenceConfig[]>>(BASE).then((r) => r.data),

  configure: (request: ConfigureDocumentSequenceRequest) =>
    apiClient.post<ApiResponse<DocumentSequenceConfig>>(BASE, request).then((r) => r.data),
};
