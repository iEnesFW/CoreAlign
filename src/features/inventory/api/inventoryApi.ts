import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  AdjustStockInput,
  CreateLotInput,
  IssueStockInput,
  Lot,
  ReceiveStockInput,
  StockAllocation,
  StockItem,
  StockMovement,
  StockMovementType,
  StockReasonCategory,
  StockReasonCode,
  StockSummary,
  UpdateLotInput,
} from '../model/inventory.types';

const BASE = '/stock';
const INVALIDATION_PATTERNS = [/\/stock\//i, /\/products\//i] as const;

export interface StockMovementsParams {
  productId?: string;
  warehouseId?: string;
  type?: StockMovementType;
  fromUtc?: string;
  toUtc?: string;
  page?: number;
  pageSize?: number;
}

export interface StockItemsParams {
  productId?: string;
  warehouseId?: string;
  onlyBelowReorder?: boolean;
  page?: number;
  pageSize?: number;
}

export const inventoryApi = {
  stockItems: (params: StockItemsParams) =>
    cachedGet<ApiResponse<PagedResult<StockItem>>>(apiClient, `${BASE}/items`, { params }),

  stockByProduct: (productId: string) =>
    cachedGet<ApiResponse<StockItem[]>>(apiClient, `${BASE}/items/by-product/${productId}`),

  stockSummary: (productId: string) =>
    cachedGet<ApiResponse<StockSummary>>(apiClient, `${BASE}/summary/${productId}`),

  movements: (params: StockMovementsParams) =>
    cachedGet<ApiResponse<PagedResult<StockMovement>>>(apiClient, `${BASE}/movements`, { params }),

  allocationsByOrder: (orderId: string) =>
    cachedGet<ApiResponse<StockAllocation[]>>(apiClient, `${BASE}/allocations/by-order/${orderId}`),

  lotsByProduct: (productId: string) =>
    cachedGet<ApiResponse<Lot[]>>(apiClient, `${BASE}/lots/by-product/${productId}`),

  reasonCodes: (category?: StockReasonCategory, isActive?: boolean) =>
    cachedGet<ApiResponse<StockReasonCode[]>>(apiClient, `${BASE}/reason-codes`, {
      params: { category, isActive },
    }),

  adjust: (input: AdjustStockInput) =>
    apiClient.post<ApiResponse<StockMovement>>(`${BASE}/adjust`, input).then((r) => {
      invalidateHttpCache(INVALIDATION_PATTERNS);
      return r.data;
    }),

  receive: (input: ReceiveStockInput) =>
    apiClient.post<ApiResponse<StockMovement>>(`${BASE}/receive`, input).then((r) => {
      invalidateHttpCache(INVALIDATION_PATTERNS);
      return r.data;
    }),

  issue: (input: IssueStockInput) =>
    apiClient.post<ApiResponse<StockMovement>>(`${BASE}/issue`, input).then((r) => {
      invalidateHttpCache(INVALIDATION_PATTERNS);
      return r.data;
    }),

  createLot: (input: CreateLotInput) =>
    apiClient.post<ApiResponse<Lot>>(`${BASE}/lots`, input).then((r) => {
      invalidateHttpCache(INVALIDATION_PATTERNS);
      return r.data;
    }),

  updateLot: (input: UpdateLotInput) =>
    apiClient.put<ApiResponse<Lot>>(`${BASE}/lots/${input.id}`, input).then((r) => {
      invalidateHttpCache(INVALIDATION_PATTERNS);
      return r.data;
    }),
};
