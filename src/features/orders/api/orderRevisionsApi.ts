import { apiClient } from '@/shared/api/apiClient';
import { invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';

const BASE = '/orders';
const INVALIDATION = [/\/orders/i] as const;

export type RevisionStatus = 'Proposed' | 'Approved' | 'Rejected' | 'Cancelled' | 'Superseded';

export interface RevisionLineInput {
  productId: string;
  quantity: number;
  unitPrice: number;
  lineNumber?: number;
  lineDiscountPercent?: number;
  lineDiscountAmount?: number;
  taxRatePercent?: number;
  isTaxInclusive?: boolean;
  withholdingRatePercent?: number;
  lineNotes?: string | null;
}

export interface OrderRevisionLine {
  productId: string;
  productSku: string;
  productName: string;
  lineNumber: number;
  quantity: number;
  unitPrice: number;
  lineDiscountPercent: number;
  lineDiscountAmount: number;
  taxRatePercent: number;
  isTaxInclusive: boolean;
  withholdingRatePercent: number;
  lineNotes?: string | null;
}

export interface OrderRevision {
  id: string;
  orderId: string;
  revisionNumber: number;
  requestedByUserId: string;
  requestedByPersona: string;
  requestedAtUtc: string;
  status: RevisionStatus;
  counterpartyDecisionByUserId?: string | null;
  decidedAtUtc?: string | null;
  rejectionReason?: string | null;
  requestNotes?: string | null;
  proposedLines: OrderRevisionLine[];
}

export interface OrderRevisionTimeline {
  orderId: string;
  currentRevisionId?: string | null;
  appliedRevisionCount: number;
  revisions: OrderRevision[];
}

export const orderRevisionsApi = {
  list: (orderId: string) =>
    apiClient
      .get<ApiResponse<OrderRevisionTimeline>>(`${BASE}/${orderId}/revisions`)
      .then((r) => r.data),

  request: (orderId: string, proposedLines: RevisionLineInput[], requestNotes?: string | null) =>
    apiClient
      .post<
        ApiResponse<OrderRevision>
      >(`${BASE}/${orderId}/revisions`, { proposedLines, requestNotes: requestNotes ?? null })
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  approve: (orderId: string, revisionId: string) =>
    apiClient
      .post<ApiResponse<OrderRevision>>(`${BASE}/${orderId}/revisions/${revisionId}/approve`)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  reject: (orderId: string, revisionId: string, reason: string) =>
    apiClient
      .post<
        ApiResponse<OrderRevision>
      >(`${BASE}/${orderId}/revisions/${revisionId}/reject`, { reason })
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  cancel: (orderId: string, revisionId: string) =>
    apiClient
      .post<ApiResponse<OrderRevision>>(`${BASE}/${orderId}/revisions/${revisionId}/cancel`)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),
};
