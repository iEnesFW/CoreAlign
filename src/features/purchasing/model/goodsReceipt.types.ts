export type GoodsReceiptStatus = 'Posted' | 'Reversed';

export type GoodsReceiptQcStatus = 'NotRequired' | 'PendingInspection' | 'Approved' | 'Rejected';

export interface GoodsReceiptLine {
  id: string;
  lineNumber: number;
  purchaseOrderLineId: string;
  productId: string;
  productSku: string;
  productName: string;
  quantityReceived: number;
  unitCost: number;
  lineCost: number;
  stockMovementId: string | null;
}

export interface GoodsReceipt {
  id: string;
  grnNumber: string;
  vendorId: string;
  vendorName: string;
  purchaseOrderId: string;
  poNumber: string;
  receiptDateUtc: string;
  warehouseId: string;
  status: GoodsReceiptStatus;
  currency: string;
  notes: string | null;
  totalCost: number;
  reversedAtUtc: string | null;
  reversalReason: string | null;
  qcStatus: GoodsReceiptQcStatus;
  qcDecisionAtUtc: string | null;
  qcDecidedByUserId: string | null;
  qcRejectionReason: string | null;
  lines: GoodsReceiptLine[];
}

export interface GoodsReceiptListParams {
  purchaseOrderId?: string;
  vendorId?: string;
  status?: GoodsReceiptStatus;
  qcStatus?: GoodsReceiptQcStatus;
  page?: number;
  pageSize?: number;
}

export interface ReverseGoodsReceiptInput {
  id: string;
  reason?: string | null;
}
