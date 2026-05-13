export type PriceSource =
  | 'ProductListPrice'
  | 'PriceList'
  | 'CustomerProductPrice'
  | 'Promotion'
  | 'ManualOverride';

export interface ResolvedPrice {
  unitPrice: number;
  currency: string;
  discountPercent: number;
  source: PriceSource;
  sourceLabel: string;
  referenceListPrice: number | null;
  taxRatePercent: number;
  isTaxInclusive: boolean;
  taxRateId: string | null;
  appliedRecordId: string | null;
}

export interface CustomerProductPrice {
  id: string;
  customerId: string;
  customerName: string;
  productId: string;
  productSku: string;
  productName: string;
  currency: string;
  price: number;
  discountPercent: number | null;
  minQuantity: number | null;
  maxQuantity: number | null;
  validFromUtc: string | null;
  validUntilUtc: string | null;
  notes: string | null;
  isActive: boolean;
}

export interface CreateCustomerProductPriceInput {
  customerId: string;
  productId: string;
  price: number;
  currency?: string;
  discountPercent?: number | null;
  minQuantity?: number | null;
  maxQuantity?: number | null;
  validFromUtc?: string | null;
  validUntilUtc?: string | null;
  notes?: string | null;
}

export type AccountingPeriodStatus = 'Open' | 'Closing' | 'Closed' | 'Locked';

export interface AccountingPeriod {
  id: string;
  year: number;
  month: number;
  code: string;
  startDate: string;
  endDate: string;
  status: AccountingPeriodStatus;
  closedAtUtc: string | null;
  closedByUserId: string | null;
  reopenedAtUtc: string | null;
  notes: string | null;
}
