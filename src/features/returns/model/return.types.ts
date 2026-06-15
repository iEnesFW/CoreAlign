export type ReturnRequestStatus =
  | 'Requested'
  | 'Approved'
  | 'Rejected'
  | 'Received'
  | 'CreditNoted'
  | 'Refunded'
  | 'Cancelled';

export const RETURN_REQUEST_STATUSES: ReturnRequestStatus[] = [
  'Requested',
  'Approved',
  'Rejected',
  'Received',
  'CreditNoted',
  'Refunded',
  'Cancelled',
];

export type ReturnReasonCode =
  | 'Other'
  | 'Defective'
  | 'WrongItem'
  | 'DamagedInTransit'
  | 'CustomerChangedMind'
  | 'NotAsDescribed'
  | 'LateDelivery'
  | 'DuplicateOrder';

export const RETURN_REASON_CODES: ReturnReasonCode[] = [
  'Other',
  'Defective',
  'WrongItem',
  'DamagedInTransit',
  'CustomerChangedMind',
  'NotAsDescribed',
  'LateDelivery',
  'DuplicateOrder',
];

export interface ReturnRequestLine {
  id: string;
  lineNumber: number;
  orderLineId: string;
  productId: string;
  productSku: string;
  productName: string;
  uomId: string | null;
  uomCode: string | null;
  quantityReturned: number;
  unitPrice: number;
  taxRatePercent: number;
  taxRateId: string | null;
  isTaxInclusive: boolean;
  lineSubtotal: number;
  taxAmount: number;
  lineTotal: number;
  restockable: boolean;
  lineNotes: string | null;
}

export interface ReturnRequest {
  id: string;
  returnNumber: string;
  status: ReturnRequestStatus;
  reason: ReturnReasonCode;
  reasonText: string | null;
  orderId: string;
  orderNumber: string;
  customerId: string;
  customerName: string;
  currency: string;
  sourceInvoiceId: string | null;
  sourceInvoiceNumber: string | null;
  creditNoteId: string | null;
  creditNoteNumber: string | null;
  refundPaymentId: string | null;
  requestedAtUtc: string;
  requestedByUserId: string | null;
  approvedAtUtc: string | null;
  approvedByUserId: string | null;
  rejectedAtUtc: string | null;
  rejectedByUserId: string | null;
  rejectionReason: string | null;
  receivedAtUtc: string | null;
  receivedByUserId: string | null;
  receivedAtWarehouseId: string | null;
  creditNoteIssuedAtUtc: string | null;
  refundedAtUtc: string | null;
  cancelledAtUtc: string | null;
  internalNotes: string | null;
  customerNotes: string | null;
  lineSubtotal: number;
  taxTotal: number;
  total: number;
  lines: ReturnRequestLine[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface ReturnRequestSummary {
  id: string;
  returnNumber: string;
  status: ReturnRequestStatus;
  reason: ReturnReasonCode;
  orderId: string;
  orderNumber: string;
  customerId: string;
  customerName: string;
  currency: string;
  total: number;
  requestedAtUtc: string;
  receivedAtUtc: string | null;
  creditNoteId: string | null;
}

export interface ReturnRequestListParams {
  page: number;
  pageSize: number;
  search?: string;
  customerId?: string;
  orderId?: string;
  status?: ReturnRequestStatus;
}

export interface CreateReturnRequestLineInput {
  orderLineId: string;
  quantityReturned: number;
  restockable?: boolean;
  lineNotes?: string | null;
}

export interface CreateReturnRequestPayload {
  orderId: string;
  reason: ReturnReasonCode;
  reasonText?: string | null;
  lines: CreateReturnRequestLineInput[];
  sourceInvoiceId?: string | null;
  customerNotes?: string | null;
  internalNotes?: string | null;
}

export interface ReceiveReturnPayload {
  warehouseId: string;
  autoIssueCreditNote?: boolean;
}
