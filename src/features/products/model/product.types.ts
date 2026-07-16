import type {
  Product,
  ProductStatus,
  ProcurementType,
  CostingMethod,
} from '@/shared/model/product.types';

export type { Product, ProductStatus, ProcurementType, CostingMethod };

export interface CreateProductInput {
  sku: string;
  name: string;
  description?: string | null;
  shortDescription?: string | null;
  barcode?: string | null;
  mpn?: string | null;
  slug?: string | null;
  brandId?: string | null;
  categoryId?: string | null;
  parentProductId?: string | null;
  variantAttributesJson?: string | null;
  color?: string | null;
  thicknessMm?: number | null;
  tagsJson?: string | null;
  unit: string;
  baseUomId?: string | null;
  purchaseUomId?: string | null;
  salesUomId?: string | null;
  price: number;
  listPrice?: number;
  minSellingPrice?: number;
  standardCost?: number;
  currency: string;
  taxRateId?: string | null;
  isPriceTaxInclusive?: boolean;
  stockQuantity: number;
  isStockTracked?: boolean;
  isLotTracked?: boolean;
  isSerialTracked?: boolean;
  minStock?: number;
  maxStock?: number;
  reorderPoint?: number;
  safetyStock?: number;
  leadTimeDays?: number;
  procurementType?: ProcurementType;
  costingMethod?: CostingMethod;
  weightKg?: number | null;
  widthCm?: number | null;
  heightCm?: number | null;
  depthCm?: number | null;
  volumeM3?: number | null;
  status?: ProductStatus;
  launchDate?: string | null;
  endOfLifeDate?: string | null;
}

export interface UpdateProductInput {
  id: string;
  sku: string;
  name: string;
  description?: string | null;
  shortDescription?: string | null;
  barcode?: string | null;
  mpn?: string | null;
  slug?: string | null;
  brandId?: string | null;
  categoryId?: string | null;
  parentProductId?: string | null;
  variantAttributesJson?: string | null;
  color?: string | null;
  thicknessMm?: number | null;
  tagsJson?: string | null;
  unit: string;
  baseUomId?: string | null;
  purchaseUomId?: string | null;
  salesUomId?: string | null;
  price: number;
  listPrice: number;
  minSellingPrice: number;
  standardCost: number;
  currency: string;
  taxRateId?: string | null;
  isPriceTaxInclusive: boolean;
  isStockTracked: boolean;
  isLotTracked: boolean;
  isSerialTracked: boolean;
  minStock: number;
  maxStock: number;
  reorderPoint: number;
  safetyStock: number;
  leadTimeDays: number;
  procurementType: ProcurementType;
  costingMethod: CostingMethod;
  weightKg?: number | null;
  widthCm?: number | null;
  heightCm?: number | null;
  depthCm?: number | null;
  volumeM3?: number | null;
  status: ProductStatus;
  launchDate?: string | null;
  endOfLifeDate?: string | null;
  expectedConcurrencyToken?: number | null;
}

export interface ProductListParams {
  page: number;
  pageSize: number;
  search?: string;
  isActive?: boolean;
}

export type StockTransactionType = 'Initial' | 'Sale' | 'SaleCancelled' | 'Restock' | 'Adjustment';

export interface StockTransaction {
  id: string;
  productId: string;
  occurredAtUtc: string;
  type: StockTransactionType;
  quantity: number;
  balanceAfter: number;
  orderId: string | null;
  reference: string | null;
  notes: string | null;
}

export interface ProductComponent {
  id: string;
  parentProductId: string;
  componentProductId: string;
  componentSku: string;
  componentName: string;
  componentUnit: string;
  quantity: number;
  notes: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface AddProductComponentInput {
  parentProductId: string;
  componentProductId: string;
  quantity: number;
  notes?: string | null;
}

export interface UpdateProductComponentInput {
  parentProductId: string;
  id: string;
  quantity: number;
  notes?: string | null;
}
