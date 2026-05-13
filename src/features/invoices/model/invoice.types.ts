import type { AddressSnapshot, CustomerSnapshot } from '@/features/orders/model/order.types';

export type InvoiceStatus =
  | 'Draft'
  | 'Issued'
  | 'Sent'
  | 'PartiallyPaid'
  | 'Paid'
  | 'Overdue'
  | 'Void'
  | 'Cancelled';

export const INVOICE_STATUSES: InvoiceStatus[] = [
  'Draft',
  'Issued',
  'Sent',
  'PartiallyPaid',
  'Paid',
  'Overdue',
  'Void',
  'Cancelled',
];

export type InvoiceType = 'SalesInvoice' | 'ProForma' | 'CreditNote' | 'DebitNote' | 'Advance';

export interface TaxBreakdownItem {
  rate: number;
  base: number;
  amount: number;
}

export interface InvoiceLine {
  id: string;
  lineNumber: number;
  productId: string | null;
  productSku: string;
  productName: string;
  description: string | null;
  uomId: string | null;
  uomCode: string | null;
  quantity: number;
  unitPrice: number;
  lineDiscountPercent: number;
  lineDiscountAmount: number;
  taxRateId: string | null;
  taxRatePercent: number;
  taxAmount: number;
  isTaxInclusive: boolean;
  withholdingRatePercent: number;
  withholdingAmount: number;
  lineSubtotal: number;
  lineNetAmount: number;
  lineTotal: number;
  revenueAccountCode: string | null;
  originOrderLineId: string | null;
}

export interface Invoice {
  id: string;
  invoiceNumber: string;
  type: InvoiceType;
  status: InvoiceStatus;
  orderId: string | null;
  originInvoiceId: string | null;
  creditNoteId: string | null;
  customerId: string;
  customerName: string;
  customerSnapshot: CustomerSnapshot | null;
  billingAddressSnapshot: AddressSnapshot | null;
  shippingAddressSnapshot: AddressSnapshot | null;
  issueDate: string;
  dueDate: string;
  postingDate: string;
  issuedAtUtc: string | null;
  sentAtUtc: string | null;
  paidAtUtc: string | null;
  cancelledAtUtc: string | null;
  voidedAtUtc: string | null;
  currency: string;
  exchangeRate: number;
  paymentTermsId: string | null;
  paymentTermsNetDaysSnapshot: number | null;
  subtotal: number;
  lineDiscountTotal: number;
  headerDiscountAmount: number;
  headerDiscountPercent: number;
  taxableTotal: number;
  taxTotal: number;
  withholdingTotal: number;
  shippingCost: number;
  roundingAdjustment: number;
  total: number;
  amountPaid: number;
  amountDue: number;
  taxBreakdown: TaxBreakdownItem[];
  cancelReason: string | null;
  voidReason: string | null;
  internalNotes: string | null;
  publicNotes: string | null;
  termsAndConditions: string | null;
  notes: string | null;
  eInvoiceUuid: string | null;
  eInvoiceStatus: string | null;
  isPostedToLedger: boolean;
  isOverdue: boolean;
  lines: InvoiceLine[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface InvoiceSummary {
  id: string;
  invoiceNumber: string;
  type: InvoiceType;
  orderId: string | null;
  customerName: string;
  issueDate: string;
  dueDate: string;
  status: InvoiceStatus;
  currency: string;
  total: number;
  amountPaid: number;
  amountDue: number;
  isOverdue: boolean;
}

export interface GenerateInvoiceRequest {
  dueDays?: number;
  notes?: string | null;
}

export interface InvoiceListParams {
  page: number;
  pageSize: number;
  search?: string;
  customerId?: string;
}
