import type { AddressSnapshot, CustomerSnapshot } from '@/features/orders/model/order.types';

export type QuoteStatus = 'Draft' | 'Sent' | 'Accepted' | 'Rejected' | 'Expired';

export const QUOTE_STATUSES: QuoteStatus[] = ['Draft', 'Sent', 'Accepted', 'Rejected', 'Expired'];

export interface QuoteLine {
  id: string;
  lineNumber: number;
  productId: string;
  productSku: string;
  productName: string;
  productDescription: string | null;
  uomId: string | null;
  uomCode: string | null;
  uomConversionFactor: number;
  quantity: number;
  listPriceSnapshot: number;
  unitPrice: number;
  lineDiscountPercent: number;
  lineDiscountAmount: number;
  isManualPriceOverride: boolean;
  taxRateId: string | null;
  taxRatePercent: number;
  taxAmount: number;
  isTaxInclusive: boolean;
  withholdingRatePercent: number;
  withholdingAmount: number;
  lineSubtotal: number;
  lineNetAmount: number;
  lineTotal: number;
  lineNotes: string | null;
}

export interface Quote {
  id: string;
  quoteNumber: string;
  status: QuoteStatus;
  customerId: string;
  customerName: string;
  billingAddressId: string | null;
  shippingAddressId: string | null;
  customerSnapshot: CustomerSnapshot | null;
  billingAddressSnapshot: AddressSnapshot | null;
  shippingAddressSnapshot: AddressSnapshot | null;
  quoteDate: string;
  validUntilUtc: string;
  sentAtUtc: string | null;
  acceptedAtUtc: string | null;
  rejectedAtUtc: string | null;
  expiredAtUtc: string | null;
  convertedAtUtc: string | null;
  currency: string;
  exchangeRate: number;
  paymentTermsId: string | null;
  paymentTermsNetDaysSnapshot: number | null;
  priceListId: string | null;
  salesRepUserId: string | null;
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
  convertedOrderId: string | null;
  rejectionReason: string | null;
  internalNotes: string | null;
  customerNotes: string | null;
  publicNotes: string | null;
  termsAndConditions: string | null;
  notes: string | null;
  lines: QuoteLine[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface QuoteSummary {
  id: string;
  quoteNumber: string;
  customerId: string;
  customerName: string;
  quoteDate: string;
  validUntilUtc: string;
  status: QuoteStatus;
  currency: string;
  total: number;
  convertedOrderId: string | null;
}

export interface QuoteLineInput {
  productId: string;
  quantity: number;
  unitPrice: number;
  lineDiscountPercent?: number;
  lineDiscountAmount?: number;
  taxRatePercent?: number;
  isTaxInclusive?: boolean;
  withholdingRatePercent?: number;
  taxRateId?: string | null;
  uomId?: string | null;
  uomCode?: string | null;
  uomConversionFactor?: number;
  lineNotes?: string | null;
  isManualPriceOverride?: boolean;
}

export interface CreateQuotePayload {
  quoteNumber?: string | null;
  customerId: string;
  quoteDate: string;
  validUntilUtc: string;
  currency: string;
  notes?: string | null;
  lines: QuoteLineInput[];
  billingAddressId?: string | null;
  shippingAddressId?: string | null;
  paymentTermsId?: string | null;
  priceListId?: string | null;
  exchangeRate?: number;
  shippingCost?: number;
  headerDiscountPercent?: number;
  headerDiscountAmount?: number;
  salesRepUserId?: string | null;
  internalNotes?: string | null;
  customerNotes?: string | null;
  publicNotes?: string | null;
  termsAndConditions?: string | null;
  roundingAdjustment?: number;
}

export interface QuoteListParams {
  page: number;
  pageSize: number;
  search?: string;
  customerId?: string;
  status?: QuoteStatus;
}
