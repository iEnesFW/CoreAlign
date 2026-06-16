export type VendorBillStatus =
  | 'Draft'
  | 'Posted'
  | 'PartiallyPaid'
  | 'Paid'
  | 'Cancelled'
  | 'PendingApproval';

export interface VendorBillLine {
  productId: string;
  productSku: string;
  productName: string;
  purchaseOrderLineId?: string | null;
  quantity: number;
  unitPrice: number;
  poUnitCost: number;
  priceVariance: number;
  taxAmount: number;
  lineSubtotal: number;
  lineTotal: number;
}

export interface VendorBill {
  id: string;
  vendorId: string;
  vendorName: string;
  billNumber: string;
  billDate: string;
  dueDate: string | null;
  currency: string;
  subtotal: number;
  taxAmount: number;
  total: number;
  amountPaid: number;
  amountDue: number;
  status: VendorBillStatus;
  purchaseOrderId: string | null;
  notes: string | null;
  createdAtUtc: string;
  lines?: VendorBillLine[];
  requiresApproval?: boolean;
  heldAtUtc?: string | null;
  holdReason?: string | null;
  approvedAtUtc?: string | null;
}

export interface VendorPayment {
  id: string;
  vendorId: string;
  vendorName: string;
  paymentNumber: string;
  paymentDate: string;
  amount: number;
  appliedAmount: number;
  unappliedAmount: number;
  isVoided: boolean;
  voidedAtUtc: string | null;
  voidReason: string | null;
  currency: string;
  method: string | null;
  vendorBillId: string | null;
  notes: string | null;
  createdAtUtc: string;
}

export interface VendorPaymentApplication {
  id: string;
  vendorPaymentId: string;
  paymentNumber: string;
  vendorBillId: string;
  billNumber: string;
  appliedAmount: number;
  appliedAtUtc: string;
  appliedByUserId: string | null;
  notes: string | null;
}

export interface UpdateVendorBillInput {
  id: string;
  billNumber: string;
  billDate: string;
  currency: string;
  subtotal: number;
  taxAmount: number;
  dueDate?: string | null;
  exchangeRate?: number;
  purchaseOrderId?: string | null;
  notes?: string | null;
  lines?: VendorBillLineInput[];
}

export interface UpdateVendorPaymentInput {
  id: string;
  paymentDate: string;
  amount: number;
  currency: string;
  exchangeRate?: number;
  method?: string | null;
  notes?: string | null;
}

export interface ApplyVendorPaymentInput {
  vendorPaymentId: string;
  vendorBillId: string;
  amount: number;
  notes?: string | null;
}

export interface ThreeWayMatchRow {
  purchaseOrderId: string;
  poNumber: string;
  vendorId: string;
  vendorName: string;
  currency: string;
  productId: string;
  productSku: string;
  productName: string;
  expectedQty: number;
  receivedQty: number;
  billedQty: number;
  expectedAmount: number;
  billedAmount: number;
  discrepancies: string[];
}

export interface VendorBillLineInput {
  productId: string;
  quantity: number;
  unitPrice: number;
  taxRatePercent?: number;
  purchaseOrderLineId?: string | null;
  poUnitCost?: number;
  uomId?: string | null;
  uomCode?: string | null;
  lineNotes?: string | null;
}

export interface CreateVendorBillInput {
  vendorId: string;
  billNumber: string;
  billDate: string;
  currency: string;
  subtotal: number;
  taxAmount: number;
  dueDate?: string | null;
  exchangeRate?: number;
  purchaseOrderId?: string | null;
  notes?: string | null;
  lines?: VendorBillLineInput[];
}

export interface CreateVendorPaymentInput {
  vendorId: string;
  amount: number;
  paymentDate: string;
  currency: string;
  method?: string | null;
  vendorBillId?: string | null;
  exchangeRate?: number;
  notes?: string | null;
}

export interface VendorBillListParams {
  vendorId?: string;
  status?: VendorBillStatus;
  page?: number;
  pageSize?: number;
}

export interface VendorAgingRow {
  vendorId: string;
  vendorName: string;
  currency: string;
  current: number;
  days1To30: number;
  days31To60: number;
  days61To90: number;
  daysOver90: number;
  total: number;
}
