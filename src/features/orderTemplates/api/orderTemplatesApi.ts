import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  CreateOrderTemplateInput,
  OrderTemplate,
  OrderTemplateListParams,
  UpdateOrderTemplateInput,
} from '../model/orderTemplate.types';

const BASE = '/order-templates';

export const orderTemplatesApi = {
  list: (params: OrderTemplateListParams) =>
    apiClient.get<ApiResponse<PagedResult<OrderTemplate>>>(BASE, { params }).then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<ApiResponse<OrderTemplate>>(`${BASE}/${id}`).then((r) => r.data),

  create: (input: CreateOrderTemplateInput) =>
    apiClient.post<ApiResponse<OrderTemplate>>(BASE, input).then((r) => r.data),

  update: (input: UpdateOrderTemplateInput) =>
    apiClient.put<ApiResponse<OrderTemplate>>(`${BASE}/${input.id}`, input).then((r) => r.data),

  remove: (id: string) =>
    apiClient.delete<ApiResponse<boolean>>(`${BASE}/${id}`).then((r) => r.data),

  activate: (id: string) =>
    apiClient.post<ApiResponse<OrderTemplate>>(`${BASE}/${id}/activate`).then((r) => r.data),

  deactivate: (id: string) =>
    apiClient.post<ApiResponse<OrderTemplate>>(`${BASE}/${id}/deactivate`).then((r) => r.data),

  runNow: (id: string) =>
    apiClient.post<ApiResponse<{ orderId: string }>>(`${BASE}/${id}/run`).then((r) => r.data),
};
