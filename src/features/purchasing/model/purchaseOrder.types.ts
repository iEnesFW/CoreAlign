export type PurchaseOrderStatus =
  | 'Draft'
  | 'Submitted'
  | 'Approved'
  | 'PartiallyReceived'
  | 'Received'
  | 'Closed'
  | 'Cancelled';

export interface PurchaseOrderLine {
  id: string;
  productId: string;
  productSku: string;
  productName: string;
  quantity: number;
  quantityReceived: number;
  quantityBilled: number;
  quantityRemainingToReceive: number;
  unitCost: number;
  taxRatePercent: number;
  taxAmount: number;
  lineSubtotal: number;
  lineTotal: number;
  uomId: string | null;
  uomCode: string | null;
  lineNotes: string | null;
}

export interface PurchaseOrder {
  id: string;
  poNumber: string;
  vendorId: string;
  vendorName: string;
  orderDate: string;
  expectedDate: string | null;
  currency: string;
  exchangeRate: number;
  warehouseId: string | null;
  status: PurchaseOrderStatus;
  subtotal: number;
  taxTotal: number;
  total: number;
  notes: string | null;
  lines: PurchaseOrderLine[];
  createdAtUtc: string;
}

export interface PurchaseOrderLineInput {
  productId: string;
  quantity: number;
  unitCost: number;
  taxRatePercent?: number;
  uomId?: string | null;
  uomCode?: string | null;
  lineNotes?: string | null;
}

export interface CreatePurchaseOrderInput {
  vendorId: string;
  orderDate: string;
  currency: string;
  lines: PurchaseOrderLineInput[];
  poNumber?: string | null;
  expectedDate?: string | null;
  exchangeRate?: number;
  warehouseId?: string | null;
  notes?: string | null;
}

export interface UpdatePurchaseOrderInput extends CreatePurchaseOrderInput {
  id: string;
}

export interface ReceiptLineInput {
  orderLineId: string;
  quantity: number;
}

export interface ReceivePurchaseOrderInput {
  id: string;
  lines: ReceiptLineInput[];
  warehouseId?: string | null;
  notes?: string | null;
}

export interface PurchaseOrderListParams {
  vendorId?: string;
  status?: PurchaseOrderStatus;
  page?: number;
  pageSize?: number;
}
