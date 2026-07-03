import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  CreateOrderInput,
  CreateShipmentInput,
  DeliverShipmentInput,
  DispatchShipmentInput,
  Order,
  OrderListParams,
  OrderSummary,
  RecordOrderScrapInput,
  Shipment,
  UpdateOrderInput,
} from '../model/order.types';

const BASE = '/orders';
const SHIPMENT_BASE = '/shipments';
const INVALIDATION = [/\/orders/i, /\/shipments/i, /\/stock\//i, /\/customers\//i] as const;

export type BulkOrderActionKind = 'Submit' | 'Approve' | 'Allocate' | 'Cancel';

export interface BulkOrderActionItemResult {
  orderId: string;
  success: boolean;
  error: string | null;
}

export interface BulkOrderActionResult {
  succeededCount: number;
  failedCount: number;
  items: BulkOrderActionItemResult[];
}

export const ordersApi = {
  list: (params: OrderListParams) =>
    apiClient.get<ApiResponse<PagedResult<OrderSummary>>>(BASE, { params }).then((r) => r.data),

  getById: (id: string) => apiClient.get<ApiResponse<Order>>(`${BASE}/${id}`).then((r) => r.data),

  create: (input: CreateOrderInput) =>
    apiClient.post<ApiResponse<Order>>(BASE, input).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  update: (input: UpdateOrderInput) =>
    apiClient.put<ApiResponse<Order>>(`${BASE}/${input.id}`, input).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  remove: (id: string) =>
    apiClient.delete<ApiResponse<boolean>>(`${BASE}/${id}`).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  submit: (id: string) =>
    apiClient.post<ApiResponse<Order>>(`${BASE}/${id}/submit`).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  revertToDraft: (id: string) =>
    apiClient.post<ApiResponse<Order>>(`${BASE}/${id}/revert-to-draft`).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  reorder: (id: string) =>
    apiClient.post<ApiResponse<Order>>(`${BASE}/${id}/reorder`).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  bulkAction: (orderIds: string[], action: BulkOrderActionKind, reason?: string | null) =>
    apiClient
      .post<ApiResponse<BulkOrderActionResult>>(`${BASE}/bulk-action`, {
        orderIds,
        action,
        reason: reason ?? null,
      })
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  approve: (id: string, approvedByUserId?: string | null) =>
    apiClient
      .post<
        ApiResponse<Order>
      >(`${BASE}/${id}/approve`, { id, approvedByUserId: approvedByUserId ?? null })
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  allocate: (id: string, preferredWarehouseId?: string | null) =>
    apiClient
      .post<ApiResponse<Order>>(`${BASE}/${id}/allocate`, {
        id,
        preferredWarehouseId: preferredWarehouseId ?? null,
      })
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  cancel: (id: string, reason?: string | null) =>
    apiClient
      .post<ApiResponse<Order>>(`${BASE}/${id}/cancel`, { id, reason: reason ?? null })
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  deliver: (id: string, deliveredAtUtc?: string | null) =>
    apiClient
      .post<
        ApiResponse<Order>
      >(`${BASE}/${id}/deliver`, { id, deliveredAtUtc: deliveredAtUtc ?? null })
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  close: (id: string) =>
    apiClient.post<ApiResponse<Order>>(`${BASE}/${id}/close`).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  getShipmentsByOrder: (orderId: string) =>
    cachedGet<ApiResponse<Shipment[]>>(apiClient, `${SHIPMENT_BASE}/by-order/${orderId}`),

  createShipment: (input: CreateShipmentInput) =>
    apiClient.post<ApiResponse<Shipment>>(SHIPMENT_BASE, input).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  pickShipment: (id: string) =>
    apiClient.post<ApiResponse<Shipment>>(`${SHIPMENT_BASE}/${id}/pick`, { id }).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  packShipment: (id: string) =>
    apiClient.post<ApiResponse<Shipment>>(`${SHIPMENT_BASE}/${id}/pack`).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  dispatchShipment: (input: DispatchShipmentInput) =>
    apiClient
      .post<ApiResponse<Shipment>>(`${SHIPMENT_BASE}/${input.id}/dispatch`, input)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  deliverShipment: (input: DeliverShipmentInput) =>
    apiClient
      .post<ApiResponse<Shipment>>(`${SHIPMENT_BASE}/${input.id}/deliver`, input)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  cancelShipment: (id: string, reason?: string | null) =>
    apiClient
      .post<ApiResponse<Shipment>>(`${SHIPMENT_BASE}/${id}/cancel`, { id, reason: reason ?? null })
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  recordScrap: (input: RecordOrderScrapInput) =>
    apiClient.post<ApiResponse<Order>>(`${BASE}/${input.id}/scrap`, input).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),
};
