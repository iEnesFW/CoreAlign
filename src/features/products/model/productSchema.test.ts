import { describe, expect, it } from 'vitest';
import { productSchema } from './productSchema';

const valid = {
  sku: 'PRD-001',
  name: 'Widget',
  description: '',
  unit: 'pcs',
  price: 9.99,
  currency: 'USD',
  stockQuantity: 100,
  isActive: true,
};

describe('productSchema', () => {
  it('accepts valid product', () => {
    expect(productSchema.safeParse(valid).success).toBe(true);
  });

  it('rejects negative price', () => {
    expect(productSchema.safeParse({ ...valid, price: -1 }).success).toBe(false);
  });

  it('rejects negative stock', () => {
    expect(productSchema.safeParse({ ...valid, stockQuantity: -5 }).success).toBe(false);
  });

  it('rejects lowercase currency', () => {
    expect(productSchema.safeParse({ ...valid, currency: 'usd' }).success).toBe(false);
  });

  it('rejects currency with wrong length', () => {
    expect(productSchema.safeParse({ ...valid, currency: 'USDT' }).success).toBe(false);
  });

  it('rejects empty SKU', () => {
    expect(productSchema.safeParse({ ...valid, sku: '' }).success).toBe(false);
  });
});
