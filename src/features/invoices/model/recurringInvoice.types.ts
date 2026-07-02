export type RecurrenceFrequency = 'Weekly' | 'Monthly' | 'Quarterly' | 'Yearly';

export type RecurringInvoiceStatus = 'Active' | 'Paused' | 'Completed' | 'Cancelled';

export interface RecurringInvoiceTemplateLine {
  id: string;
  lineNumber: number;
  productId: string | null;
  productSku: string;
  productName: string;
  description: string | null;
  quantity: number;
  unitPrice: number;
  taxRatePercent: number;
  taxRateId: string | null;
  lineDiscountPercent: number | null;
  lineDiscountAmount: number | null;
  withholdingRatePercent: number | null;
  isTaxInclusive: boolean;
  uomId: string | null;
  uomCode: string | null;
}

export interface RecurringInvoiceTemplate {
  id: string;
  name: string;
  customerId: string;
  currency: string;
  frequency: RecurrenceFrequency;
  intervalCount: number;
  anchorDayOfMonth: number | null;
  anchorDayOfWeek: string | null;
  startDate: string;
  endDate: string | null;
  maxOccurrences: number | null;
  nextRunDate: string;
  lastRunDate: string | null;
  occurrencesGenerated: number;
  dueDays: number;
  paymentTermsId: string | null;
  headerDiscountPercent: number | null;
  headerDiscountAmount: number | null;
  shippingCost: number | null;
  roundingAdjustment: number | null;
  status: RecurringInvoiceStatus;
  autoConfirm: boolean;
  publicNotes: string | null;
  internalNotes: string | null;
  createdByUserId: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  lines: RecurringInvoiceTemplateLine[];
}

export interface RecurringInvoiceTemplateSummary {
  id: string;
  name: string;
  customerId: string;
  customerName: string;
  currency: string;
  frequency: RecurrenceFrequency;
  intervalCount: number;
  nextRunDate: string;
  occurrencesGenerated: number;
  status: RecurringInvoiceStatus;
  lineCount: number;
  createdAtUtc: string;
}

export interface RecurringInvoiceLineInput {
  productId?: string | null;
  productName?: string | null;
  description?: string | null;
  quantity: number;
  unitPrice: number;
  taxRatePercent?: number;
}

export interface CreateRecurringInvoiceInput {
  name: string;
  customerId: string;
  currency: string;
  frequency: RecurrenceFrequency;
  intervalCount: number;
  anchorDayOfMonth?: number | null;
  startDate: string;
  endDate?: string | null;
  maxOccurrences?: number | null;
  lines: RecurringInvoiceLineInput[];
  dueDays?: number;
  publicNotes?: string | null;
  internalNotes?: string | null;
}

export interface UpdateRecurringInvoiceInput extends CreateRecurringInvoiceInput {
  id: string;
}

export interface RecurringInvoiceListParams {
  search?: string;
  customerId?: string;
  status?: RecurringInvoiceStatus;
  page?: number;
  pageSize?: number;
}
