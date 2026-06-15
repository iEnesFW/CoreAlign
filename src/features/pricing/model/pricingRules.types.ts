export type DiscountRuleScope = 'Global' | 'CustomerGroup' | 'ProductCategory' | 'Product';
export type DiscountValueType = 'Percent' | 'FixedAmount';
export type TaxRuleScope =
  | 'Global'
  | 'Region'
  | 'ProductClass'
  | 'RegionAndProductClass'
  | 'Product';

export interface PriceListItem {
  id: string;
  priceListId: string;
  productId: string;
  price: number;
  minQuantity: number | null;
  maxQuantity: number | null;
  discountPercent: number | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface PriceListItemInput {
  priceListId: string;
  productId: string;
  price: number;
  minQuantity?: number | null;
  maxQuantity?: number | null;
  discountPercent?: number | null;
}

export interface PriceListItemUpdateInput {
  priceListId: string;
  id: string;
  price: number;
  minQuantity: number | null;
  maxQuantity: number | null;
  discountPercent: number | null;
}

export interface DiscountRule {
  id: string;
  code: string;
  name: string;
  scope: DiscountRuleScope;
  customerGroupId: string | null;
  productCategoryId: string | null;
  productId: string | null;
  validFromUtc: string | null;
  validUntilUtc: string | null;
  minQuantity: number | null;
  valueType: DiscountValueType;
  value: number;
  priority: number;
  isActive: boolean;
  description: string | null;
}

export interface DiscountRuleInput {
  code: string;
  name: string;
  scope: DiscountRuleScope;
  valueType: DiscountValueType;
  value: number;
  customerGroupId?: string | null;
  productCategoryId?: string | null;
  productId?: string | null;
  validFromUtc?: string | null;
  validUntilUtc?: string | null;
  minQuantity?: number | null;
  priority?: number;
  isActive?: boolean;
  description?: string | null;
}

export interface DiscountRuleUpdateInput {
  id: string;
  name: string;
  scope: DiscountRuleScope;
  valueType: DiscountValueType;
  value: number;
  customerGroupId: string | null;
  productCategoryId: string | null;
  productId: string | null;
  validFromUtc: string | null;
  validUntilUtc: string | null;
  minQuantity: number | null;
  priority: number;
  isActive: boolean;
  description: string | null;
}

export interface TaxRule {
  id: string;
  code: string;
  name: string;
  scope: TaxRuleScope;
  regionCode: string | null;
  productClass: string | null;
  productCategoryId: string | null;
  productId: string | null;
  ratePercent: number;
  fallbackTaxRateId: string | null;
  validFromUtc: string | null;
  validUntilUtc: string | null;
  priority: number;
  isActive: boolean;
  description: string | null;
}

export interface TaxRuleInput {
  code: string;
  name: string;
  scope: TaxRuleScope;
  ratePercent: number;
  regionCode?: string | null;
  productClass?: string | null;
  productCategoryId?: string | null;
  productId?: string | null;
  fallbackTaxRateId?: string | null;
  validFromUtc?: string | null;
  validUntilUtc?: string | null;
  priority?: number;
  isActive?: boolean;
  description?: string | null;
}

export interface TaxRuleUpdateInput {
  id: string;
  name: string;
  scope: TaxRuleScope;
  ratePercent: number;
  regionCode: string | null;
  productClass: string | null;
  productCategoryId: string | null;
  productId: string | null;
  fallbackTaxRateId: string | null;
  validFromUtc: string | null;
  validUntilUtc: string | null;
  priority: number;
  isActive: boolean;
  description: string | null;
}
