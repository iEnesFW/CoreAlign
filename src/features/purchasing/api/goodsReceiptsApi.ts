import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type { GoodsReceipt, GoodsReceiptListParams } from '../model/goodsReceipt.types';

const BASE = '/goods-receipts';
const INVALIDATION = [
  /\/goods-receipts/i,
  /\/purchase-orders/i,
  /\/stock/i,
  /\/products/i,
] as const;

const mutate = <T>(p: Promise<{ data: ApiResponse<T> }>) =>
  p.then((r) => {
    invalidateHttpCache(INVALIDATION);
    return r.data;
  });

export const goodsReceiptsApi = {
  search: (params: GoodsReceiptListParams) =>
    cachedGet<ApiResponse<PagedResult<GoodsReceipt>>>(apiClient, BASE, { params }),

  getById: (id: string) => cachedGet<ApiResponse<GoodsReceipt>>(apiClient, `${BASE}/${id}`),

  reverse: (id: string, reason?: string | null) =>
    mutate(
      apiClient.post<ApiResponse<GoodsReceipt>>(`${BASE}/${id}/reverse`, {
        id,
        reason: reason ?? null,
      }),
    ),
};
