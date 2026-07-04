export type WarehouseType = 'Main' | 'Transit' | 'Return' | 'Damaged' | 'Quarantine';

export interface Brand {
  id: string;
  code: string;
  name: string;
  description: string | null;
  isActive: boolean;
}

export interface ProductCategory {
  id: string;
  code: string;
  name: string;
  parentCategoryId: string | null;
  description: string | null;
  isActive: boolean;
}

export interface CustomerGroup {
  id: string;
  code: string;
  name: string;
  description: string | null;
  defaultPriceListId: string | null;
  defaultDiscountPercent: number;
  isActive: boolean;
}

export interface UnitOfMeasure {
  id: string;
  code: string;
  name: string;
  symbol: string | null;
  baseUomId: string | null;
  conversionFactor: number;
  decimalPlaces: number;
  isBase: boolean;
  isActive: boolean;
}

export interface TaxRate {
  id: string;
  code: string;
  name: string;
  ratePercent: number;
  isWithholding: boolean;
  countryCode: string | null;
  description: string | null;
  isActive: boolean;
}

export type WithholdingTaxKind = 'Partial' | 'Full';

export interface WithholdingTaxCode {
  id: string;
  code: string;
  name: string;
  kind: WithholdingTaxKind;
  numerator: number;
  denominator: number;
  isActive: boolean;
}

export type VatExemptionKind = 'Full' | 'Partial' | 'NotSubject' | 'ExportRegistered';

export interface VatExemptionCode {
  id: string;
  code: string;
  name: string;
  lawReference: string | null;
  kind: VatExemptionKind;
  isActive: boolean;
}

export interface PaymentTerm {
  id: string;
  code: string;
  name: string;
  netDays: number;
  discountDays: number;
  discountPercent: number;
  endOfMonth: boolean;
  description: string | null;
  isActive: boolean;
}

export interface PriceList {
  id: string;
  code: string;
  name: string;
  currency: string;
  isTaxInclusive: boolean;
  validFromUtc: string | null;
  validUntilUtc: string | null;
  isDefault: boolean;
  description: string | null;
  isActive: boolean;
}

export interface PriceListItem {
  id: string;
  priceListId: string;
  productId: string;
  price: number;
  minQuantity?: number | null;
  maxQuantity?: number | null;
  discountPercent?: number | null;
}

export interface Warehouse {
  id: string;
  code: string;
  name: string;
  type: WarehouseType;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  state: string | null;
  postalCode: string | null;
  country: string | null;
  phone: string | null;
  isDefault: boolean;
  isActive: boolean;
}

export interface BankAccount {
  id: string;
  accountName: string;
  bankName: string;
  branchName: string | null;
  iban: string;
  swift: string | null;
  currency: string;
  openingBalance: number;
  isPrimary: boolean;
  isActive: boolean;
  notes: string | null;
}
