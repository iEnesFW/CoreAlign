import { describe, expect, it } from 'vitest';
import type { Product } from './product.types';
import { buildProductUpdateInput } from './productUpdateMerge';
import { patchProductInPaged, type PagedProductsResponse } from './productCachePatch';

const product = (id: string, name: string): Product => ({
  id,
  sku: `SKU-${id}`,
  barcode: null,
  mpn: null,
  name,
  shortDescription: null,
  description: null,
  slug: null,
  brandId: null,
  categoryId: null,
  parentProductId: null,
  variantAttributesJson: null,
  color: null,
  thicknessMm: null,
  tagsJson: null,
  unit: 'adet',
  baseUomId: null,
  purchaseUomId: null,
  salesUomId: null,
  price: 100,
  listPrice: 120,
  minSellingPrice: 80,
  standardCost: 60,
  lastPurchaseCost: 62,
  averageCost: 61,
  currency: 'TRY',
  taxRateId: null,
  isPriceTaxInclusive: false,
  stockQuantity: 42,
  isStockTracked: true,
  isLotTracked: false,
  isSerialTracked: false,
  minStock: 1,
  maxStock: 99,
  reorderPoint: 5,
  safetyStock: 2,
  leadTimeDays: 3,
  procurementType: 'Buy',
  costingMethod: 'WeightedAverage',
  weightKg: null,
  widthCm: null,
  heightCm: null,
  depthCm: null,
  volumeM3: null,
  status: 'Active',
  launchDate: null,
  endOfLifeDate: null,
  isActive: true,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: '2026-06-01T00:00:00Z',
});

const page = (...products: Product[]): PagedProductsResponse => ({
  isSuccess: true,
  data: { items: products, total: products.length, page: 1, pageSize: 10 },
  errors: [],
  statusCode: 200,
});

describe('patchProductInPaged', () => {
  it('patches only the matching row and preserves Product-only fields', () => {
    const old = page(product('a', 'Eski'), product('b', 'Diğer'));
    const input = buildProductUpdateInput(old.data!.items[0], { name: 'Yeni' });

    const patched = patchProductInPaged(old, input);

    expect(patched).not.toBe(old);
    expect(patched!.data!.items[0].name).toBe('Yeni');
    expect(patched!.data!.items[0].stockQuantity).toBe(42);
    expect(patched!.data!.items[0].averageCost).toBe(61);
    expect(patched!.data!.items[1]).toBe(old.data!.items[1]);
  });

  it('returns the same reference when the page does not contain the product', () => {
    const old = page(product('x', 'Başka'));
    const input = buildProductUpdateInput(product('zzz', 'Yok'), { name: 'Yeni' });

    expect(patchProductInPaged(old, input)).toBe(old);
  });

  it('returns undefined/null-data caches untouched', () => {
    const input = buildProductUpdateInput(product('a', 'Ad'), { name: 'Yeni' });
    expect(patchProductInPaged(undefined, input)).toBeUndefined();

    const nullData: PagedProductsResponse = {
      isSuccess: false,
      data: null,
      errors: ['x'],
      statusCode: 500,
    };
    expect(patchProductInPaged(nullData, input)).toBe(nullData);
  });

  it('skips undefined input fields instead of clobbering row values', () => {
    const old = page(product('a', 'Eski'));
    const input = { ...buildProductUpdateInput(old.data!.items[0]), description: undefined };

    const patched = patchProductInPaged(old, input);

    expect(patched!.data!.items[0].description).toBeNull();
  });
});
