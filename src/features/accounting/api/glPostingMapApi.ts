import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';
import type { ConfigureGLPostingMapRequest, GLPostingMapping } from '../model/glPostingMap.types';

const BASE = '/accounting/gl-posting-map';

export const glPostingMapApi = {
  list: () => apiClient.get<ApiResponse<GLPostingMapping[]>>(BASE).then((r) => r.data),

  configure: (request: ConfigureGLPostingMapRequest) =>
    apiClient.put<ApiResponse<GLPostingMapping>>(BASE, request).then((r) => r.data),
};
