import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type {
  BankAccount,
  Brand,
  CustomerGroup,
  PaymentTerm,
  PriceList,
  PriceListItem,
  ProductCategory,
  TaxRate,
  UnitOfMeasure,
  VatExemptionCode,
  Warehouse,
  WarehouseType,
  WithholdingTaxCode,
} from '../model/masterData.types';

const BASE = '/master-data';

const buildCrud = <TItem, TCreate, TUpdate extends { id: string }>(resource: string) => {
  const invalidationPattern = [new RegExp(`${BASE}/${resource}`, 'i')] as const;
  return {
    list: (isActive?: boolean) =>
      cachedGet<ApiResponse<TItem[]>>(apiClient, `${BASE}/${resource}`, {
        params: isActive === undefined ? {} : { isActive },
      }),
    getById: (id: string) => cachedGet<ApiResponse<TItem>>(apiClient, `${BASE}/${resource}/${id}`),
    create: (input: TCreate) =>
      apiClient.post<ApiResponse<TItem>>(`${BASE}/${resource}`, input).then((r) => {
        invalidateHttpCache(invalidationPattern);
        return r.data;
      }),
    update: (input: TUpdate) =>
      apiClient.put<ApiResponse<TItem>>(`${BASE}/${resource}/${input.id}`, input).then((r) => {
        invalidateHttpCache(invalidationPattern);
        return r.data;
      }),
    remove: (id: string) =>
      apiClient.delete<ApiResponse<boolean>>(`${BASE}/${resource}/${id}`).then((r) => {
        invalidateHttpCache(invalidationPattern);
        return r.data;
      }),
  };
};

export interface BrandInput {
  code: string;
  name: string;
  description?: string | null;
}
export interface BrandUpdateInput extends BrandInput {
  id: string;
  isActive: boolean;
}

export interface ProductCategoryInput {
  code: string;
  name: string;
  parentCategoryId?: string | null;
  description?: string | null;
}
export interface ProductCategoryUpdateInput extends ProductCategoryInput {
  id: string;
  isActive: boolean;
}

export interface CustomerGroupInput {
  code: string;
  name: string;
  description?: string | null;
  defaultPriceListId?: string | null;
  defaultDiscountPercent?: number;
}
export interface CustomerGroupUpdateInput extends CustomerGroupInput {
  id: string;
  isActive: boolean;
}

export interface UnitOfMeasureInput {
  code: string;
  name: string;
  symbol?: string | null;
  baseUomId?: string | null;
  conversionFactor?: number;
  decimalPlaces?: number;
}
export interface UnitOfMeasureUpdateInput extends UnitOfMeasureInput {
  id: string;
  isActive: boolean;
}

export interface TaxRateInput {
  code: string;
  name: string;
  ratePercent: number;
  isWithholding?: boolean;
  countryCode?: string | null;
  description?: string | null;
}
export interface TaxRateUpdateInput extends TaxRateInput {
  id: string;
  isActive: boolean;
}

export interface PaymentTermInput {
  code: string;
  name: string;
  netDays: number;
  discountDays?: number;
  discountPercent?: number;
  endOfMonth?: boolean;
  description?: string | null;
}
export interface PaymentTermUpdateInput extends PaymentTermInput {
  id: string;
  isActive: boolean;
}

export interface PriceListInput {
  code: string;
  name: string;
  currency: string;
  isTaxInclusive?: boolean;
  validFromUtc?: string | null;
  validUntilUtc?: string | null;
  isDefault?: boolean;
  description?: string | null;
}
export interface PriceListUpdateInput extends PriceListInput {
  id: string;
  isActive: boolean;
}

export interface WarehouseInput {
  code: string;
  name: string;
  type?: WarehouseType;
  isDefault?: boolean;
}
export interface WarehouseUpdateInput {
  id: string;
  code: string;
  name: string;
  type: WarehouseType;
  addressLine1?: string | null;
  addressLine2?: string | null;
  city?: string | null;
  state?: string | null;
  postalCode?: string | null;
  country?: string | null;
  phone?: string | null;
  managerUserId?: string | null;
  isDefault: boolean;
  isActive: boolean;
}

export interface BankAccountInput {
  accountName: string;
  bankName: string;
  iban: string;
  currency: string;
  openingBalance?: number;
  branchName?: string | null;
  swift?: string | null;
  isPrimary?: boolean;
  notes?: string | null;
}
export interface BankAccountUpdateInput {
  id: string;
  accountName: string;
  bankName: string;
  iban: string;
  currency: string;
  openingBalance: number;
  branchName?: string | null;
  swift?: string | null;
  isPrimary: boolean;
  isActive: boolean;
  notes?: string | null;
}

export const masterDataApi = {
  brands: buildCrud<Brand, BrandInput, BrandUpdateInput>('brands'),
  bankAccounts: buildCrud<BankAccount, BankAccountInput, BankAccountUpdateInput>('bank-accounts'),
  categories: buildCrud<ProductCategory, ProductCategoryInput, ProductCategoryUpdateInput>(
    'categories',
  ),
  customerGroups: buildCrud<CustomerGroup, CustomerGroupInput, CustomerGroupUpdateInput>(
    'customer-groups',
  ),
  uoms: {
    ...buildCrud<UnitOfMeasure, UnitOfMeasureInput, UnitOfMeasureUpdateInput>('units-of-measure'),
    seedStandard: () =>
      apiClient.post<ApiResponse<number>>(`${BASE}/units-of-measure/seed-standard`).then((r) => {
        invalidateHttpCache([new RegExp(`${BASE}/units-of-measure`, 'i')]);
        return r.data;
      }),
  },
  taxRates: buildCrud<TaxRate, TaxRateInput, TaxRateUpdateInput>('tax-rates'),
  withholdingTaxCodes: {
    list: (isActive?: boolean) =>
      cachedGet<ApiResponse<WithholdingTaxCode[]>>(apiClient, `${BASE}/withholding-tax-codes`, {
        params: isActive === undefined ? {} : { isActive },
      }),
  },
  vatExemptionCodes: {
    list: (isActive?: boolean) =>
      cachedGet<ApiResponse<VatExemptionCode[]>>(apiClient, `${BASE}/vat-exemption-codes`, {
        params: isActive === undefined ? {} : { isActive },
      }),
  },
  paymentTerms: buildCrud<PaymentTerm, PaymentTermInput, PaymentTermUpdateInput>('payment-terms'),
  priceLists: buildCrud<PriceList, PriceListInput, PriceListUpdateInput>('price-lists'),
  priceListItems: (listId: string) =>
    cachedGet<ApiResponse<PriceListItem[]>>(apiClient, `/price-lists/${listId}/items`),
  warehouses: buildCrud<Warehouse, WarehouseInput, WarehouseUpdateInput>('warehouses'),
};
