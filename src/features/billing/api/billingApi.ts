import { apiClient } from '@/shared/api/apiClient';
import { invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  CreateSubscriptionOrderInput,
  MockApproveInput,
  ModuleDto,
  PaymentGatewayDescriptor,
  SubscriptionOrderCreationResult,
  SubscriptionOrderDto,
  SubscriptionOrderStatus,
  TenantModuleDto,
} from '../model/billing.types';

const MODULES_BASE = '/modules';
const BILLING_BASE = '/billing';

const BILLING_INVALIDATION = [/\/billing/i, /\/modules/i] as const;

export interface ListOrdersParams {
  status?: SubscriptionOrderStatus;
  page?: number;
  pageSize?: number;
}

export const billingApi = {
  listCatalog: () => apiClient.get<ApiResponse<ModuleDto[]>>(MODULES_BASE).then((r) => r.data),

  listGateways: () =>
    apiClient
      .get<ApiResponse<PaymentGatewayDescriptor[]>>(`${BILLING_BASE}/gateways`)
      .then((r) => r.data),

  listActiveModules: () =>
    apiClient.get<ApiResponse<TenantModuleDto[]>>(`${MODULES_BASE}/active`).then((r) => r.data),

  listOrders: (params: ListOrdersParams = {}) =>
    apiClient
      .get<ApiResponse<PagedResult<SubscriptionOrderDto>>>(`${BILLING_BASE}/orders`, {
        params: {
          status: params.status,
          page: params.page ?? 1,
          pageSize: params.pageSize ?? 25,
        },
      })
      .then((r) => r.data),

  getOrder: (id: string) =>
    apiClient
      .get<ApiResponse<SubscriptionOrderDto>>(`${BILLING_BASE}/orders/${id}`)
      .then((r) => r.data),

  createOrder: (input: CreateSubscriptionOrderInput) =>
    apiClient
      .post<ApiResponse<SubscriptionOrderCreationResult>>(`${BILLING_BASE}/orders`, input)
      .then((r) => {
        invalidateHttpCache(BILLING_INVALIDATION);
        return r.data;
      }),

  cancelOrder: (id: string, reason?: string) =>
    apiClient
      .post<ApiResponse<SubscriptionOrderDto>>(`${BILLING_BASE}/orders/${id}/cancel`, {
        reason: reason ?? null,
      })
      .then((r) => {
        invalidateHttpCache(BILLING_INVALIDATION);
        return r.data;
      }),

  mockApprove: (input: MockApproveInput) =>
    apiClient
      .post<ApiResponse<SubscriptionOrderDto>>(
        `${BILLING_BASE}/orders/${input.orderId}/mock-approve`,
        {
          action: input.action,
          reference: input.reference,
          reason: input.reason,
        },
      )
      .then((r) => {
        invalidateHttpCache(BILLING_INVALIDATION);
        return r.data;
      }),
};
