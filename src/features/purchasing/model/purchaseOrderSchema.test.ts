import { describe, expect, it } from 'vitest';
import { purchaseOrderSchema } from './purchaseOrderSchema';

const valid = {
  vendorId: '11111111-1111-1111-1111-111111111111',
  orderDate: '2026-08-13',
  expectedDate: '',
  currency: 'TRY',
  exchangeRate: '1',
  warehouseId: '',
  notes: '',
  lines: [
    {
      productId: '22222222-2222-2222-2222-222222222222',
      quantity: 3,
      unitCost: 50,
      taxRatePercent: '20',
      lineNotes: '',
    },
  ],
};

describe('purchaseOrderSchema', () => {
  it('accepts a minimal valid purchase order', () => {
    expect(purchaseOrderSchema.safeParse(valid).success).toBe(true);
  });

  it('requires a vendor and an order date', () => {
    expect(purchaseOrderSchema.safeParse({ ...valid, vendorId: '' }).success).toBe(false);
    expect(purchaseOrderSchema.safeParse({ ...valid, orderDate: '' }).success).toBe(false);
  });

  it('requires at least one line', () => {
    const result = purchaseOrderSchema.safeParse({ ...valid, lines: [] });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.message === 'Validation.AtLeastOneLine')).toBe(true);
    }
  });

  it('refuses a line with no product or a non-positive quantity', () => {
    for (const bad of [{ productId: '' }, { quantity: 0 }, { quantity: -2 }]) {
      expect(
        purchaseOrderSchema.safeParse({ ...valid, lines: [{ ...valid.lines[0], ...bad }] }).success,
      ).toBe(false);
    }
  });

  it('allows a zero unit cost but not a negative one', () => {
    expect(
      purchaseOrderSchema.safeParse({ ...valid, lines: [{ ...valid.lines[0], unitCost: 0 }] })
        .success,
    ).toBe(true);
    expect(
      purchaseOrderSchema.safeParse({ ...valid, lines: [{ ...valid.lines[0], unitCost: -1 }] })
        .success,
    ).toBe(false);
  });

  it('rejects a currency that is not a three-letter uppercase code', () => {
    expect(purchaseOrderSchema.safeParse({ ...valid, currency: 'usd' }).success).toBe(false);
    expect(purchaseOrderSchema.safeParse({ ...valid, currency: 'US' }).success).toBe(false);
  });

  it('treats the optional fields as optional', () => {
    expect(
      purchaseOrderSchema.safeParse({
        ...valid,
        expectedDate: '',
        exchangeRate: '',
        warehouseId: '',
        notes: '',
        lines: [{ ...valid.lines[0], taxRatePercent: '', lineNotes: '' }],
      }).success,
    ).toBe(true);
  });
});
