import { describe, expect, it } from 'vitest';
import { stockVoucherSchema } from './stockVoucherSchema';

const base = {
  warehouseId: 'w1',
  toWarehouseId: '',
  reference: '',
  notes: '',
  lines: [{ productId: 'p1', quantity: 5, unitCost: 0 }],
};

describe('stockVoucherSchema', () => {
  it('requires a warehouse and at least one line with a product', () => {
    expect(stockVoucherSchema('receive').safeParse({ ...base, warehouseId: '' }).success).toBe(
      false,
    );
    expect(stockVoucherSchema('receive').safeParse({ ...base, lines: [] }).success).toBe(false);
    expect(
      stockVoucherSchema('receive').safeParse({ ...base, lines: [{ productId: '', quantity: 1 }] })
        .success,
    ).toBe(false);
  });

  it('lets a count post a zero while receive, issue and transfer demand a positive quantity', () => {
    const zeroLine = { ...base, lines: [{ productId: 'p1', quantity: 0 }] };

    expect(stockVoucherSchema('count').safeParse(zeroLine).success).toBe(true);
    expect(stockVoucherSchema('receive').safeParse(zeroLine).success).toBe(false);
    expect(stockVoucherSchema('issue').safeParse(zeroLine).success).toBe(false);
    expect(
      stockVoucherSchema('transfer').safeParse({ ...zeroLine, toWarehouseId: 'w2' }).success,
    ).toBe(false);
  });

  it('never accepts a negative quantity, not even on a count', () => {
    expect(
      stockVoucherSchema('count').safeParse({ ...base, lines: [{ productId: 'p1', quantity: -1 }] })
        .success,
    ).toBe(false);
  });

  it('demands a different target warehouse for a transfer only', () => {
    expect(stockVoucherSchema('transfer').safeParse(base).success).toBe(false);
    expect(stockVoucherSchema('transfer').safeParse({ ...base, toWarehouseId: 'w1' }).success).toBe(
      false,
    );
    expect(stockVoucherSchema('transfer').safeParse({ ...base, toWarehouseId: 'w2' }).success).toBe(
      true,
    );
    expect(stockVoucherSchema('receive').safeParse({ ...base, toWarehouseId: '' }).success).toBe(
      true,
    );
  });
});
