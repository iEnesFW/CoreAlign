import { describe, expect, it } from 'vitest';
import type { Product } from './product.types';
import { buildProductUpdateInput } from './productUpdateMerge';

const product: Product = {
  id: 'p1',
  sku: 'SKU-1',
  barcode: '869000000001',
  mpn: 'MPN-1',
  name: 'Eski Ad',
  shortDescription: 'kısa',
  description: 'uzun',
  slug: 'eski-ad',
  brandId: 'b1',
  categoryId: 'c1',
  parentProductId: null,
  variantAttributesJson: null,
  tagsJson: '["a"]',
  unit: 'adet',
  baseUomId: null,
  purchaseUomId: null,
  salesUomId: null,
  price: 150.5,
  listPrice: 200,
  minSellingPrice: 120,
  standardCost: 90,
  lastPurchaseCost: 95,
  averageCost: 92.5,
  currency: 'TRY',
  taxRateId: 't1',
  isPriceTaxInclusive: true,
  stockQuantity: 42,
  isStockTracked: true,
  isLotTracked: false,
  isSerialTracked: false,
  minStock: 5,
  maxStock: 100,
  reorderPoint: 10,
  safetyStock: 3,
  leadTimeDays: 7,
  procurementType: 'Buy',
  weightKg: 1.2,
  widthCm: 10,
  heightCm: 20,
  depthCm: 5,
  volumeM3: 0.001,
  status: 'Active',
  launchDate: '2026-01-01',
  endOfLifeDate: null,
  isActive: true,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: '2026-06-01T00:00:00Z',
};

describe('buildProductUpdateInput', () => {
  it('overrides only the name and preserves every money/stock/status field', () => {
    const input = buildProductUpdateInput(product, { name: 'Yeni Ad' });
    expect(input.name).toBe('Yeni Ad');
    expect(input.id).toBe('p1');
    expect(input.price).toBe(150.5);
    expect(input.listPrice).toBe(200);
    expect(input.minSellingPrice).toBe(120);
    expect(input.standardCost).toBe(90);
    expect(input.status).toBe('Active');
    expect(input.minStock).toBe(5);
    expect(input.maxStock).toBe(100);
    expect(input.reorderPoint).toBe(10);
    expect(input.safetyStock).toBe(3);
    expect(input.isStockTracked).toBe(true);
    expect(input.procurementType).toBe('Buy');
    expect(input.currency).toBe('TRY');
    expect(input.unit).toBe('adet');
    expect(input.sku).toBe('SKU-1');
  });

  it('does not leak Product-only fields into the update payload', () => {
    const input = buildProductUpdateInput(product, { name: 'X' });
    const keys = Object.keys(input);
    expect(keys).not.toContain('stockQuantity');
    expect(keys).not.toContain('averageCost');
    expect(keys).not.toContain('lastPurchaseCost');
    expect(keys).not.toContain('isActive');
    expect(keys).not.toContain('createdAtUtc');
    expect(keys).not.toContain('updatedAtUtc');
  });

  it('returns an unchanged snapshot with empty overrides', () => {
    const input = buildProductUpdateInput(product);
    expect(input.name).toBe('Eski Ad');
    expect(input.description).toBe('uzun');
    expect(input.barcode).toBe('869000000001');
    expect(input.launchDate).toBe('2026-01-01');
    expect(input.endOfLifeDate).toBeNull();
  });
});
