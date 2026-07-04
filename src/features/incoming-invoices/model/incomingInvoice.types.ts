export type IncomingInvoiceStatus = 'New' | 'Reviewed' | 'Processed' | 'Ignored';

export interface IncomingInvoiceDto {
  id: string;
  ettn: string;
  senderVkn: string;
  senderName: string | null;
  invoiceNumber: string;
  issueDate: string;
  providerName: string;
  providerStatus: string | null;
  status: IncomingInvoiceStatus;
  linkedVendorBillId: string | null;
  processedAtUtc: string | null;
  notes: string | null;
  createdAtUtc: string;
}

export interface IncomingInvoiceListParams {
  status?: IncomingInvoiceStatus;
  page?: number;
  pageSize?: number;
}

export interface ProcessIncomingInvoiceInput {
  subtotal: number;
  taxAmount: number;
  vendorName?: string | null;
  currency?: string | null;
}

export interface IgnoreIncomingInvoiceInput {
  reason?: string | null;
}

export interface ProcessIncomingInvoiceResult {
  incomingInvoiceId: string;
  vendorBillId: string;
  vendorId: string;
  vendorCreated: boolean;
}
