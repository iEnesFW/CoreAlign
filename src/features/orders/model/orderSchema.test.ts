import { describe, expect, it } from 'vitest';
import { orderSchema } from './orderSchema';

const valid = {
  orderNumber: 'ORD-2026-0001',
  customerId: '11111111-1111-1111-1111-111111111111',
  orderDate: '2026-05-12',
  status: 'Draft' as const,
  currency: 'USD',
  notes: '',
  lines: [{ productId: '22222222-2222-2222-2222-222222222222', quantity: 1, unitPrice: 10 }],
};

describe('orderSchema', () => {
  it('accepts valid order with single line', () => {
    expect(orderSchema.safeParse(valid).success).toBe(true);
  });

  it('rejects order without lines', () => {
    const result = orderSchema.safeParse({ ...valid, lines: [] });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.message === 'Validation.AtLeastOneLine')).toBe(true);
    }
  });

  it('rejects zero or negative quantity', () => {
    expect(
      orderSchema.safeParse({
        ...valid,
        lines: [{ productId: valid.lines[0].productId, quantity: 0, unitPrice: 10 }],
      }).success,
    ).toBe(false);
  });

  it('rejects invalid status', () => {
    expect(
      orderSchema.safeParse({
        ...valid,
        status: 'Unknown' as never,
      }).success,
    ).toBe(false);
  });

  it('rejects non-uppercase currency', () => {
    expect(orderSchema.safeParse({ ...valid, currency: 'usd' }).success).toBe(false);
  });
});
