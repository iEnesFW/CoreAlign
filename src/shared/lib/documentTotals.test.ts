import { describe, expect, it } from 'vitest';
import { computeDocumentTotals, type DocumentTotalsLine } from './documentTotals';

const line = (over: Partial<DocumentTotalsLine> = {}): DocumentTotalsLine => ({
  productId: 'p1',
  quantity: 1,
  unitPrice: 100,
  ...over,
});

describe('computeDocumentTotals', () => {
  it('returns zeroes for an empty document', () => {
    const t = computeDocumentTotals({ lines: undefined });

    expect(t.subtotal).toBe(0);
    expect(t.grandTotal).toBe(0);
    expect(t.taxPct).toBeNull();
    expect(t.headerDiscountPct).toBe(0);
  });

  it('sums gross before discounts and nets tax after the line discount', () => {
    const t = computeDocumentTotals({
      lines: [line({ quantity: 2, unitPrice: 100, lineDiscountPercent: 10, taxRatePercent: 20 })],
    });

    expect(t.subtotal).toBe(200);
    expect(t.lineDiscount).toBe(20);
    expect(t.tax).toBe(36);
    expect(t.grandTotal).toBe(216);
  });

  it('applies the header discount to the amount left after line discounts', () => {
    const t = computeDocumentTotals({
      lines: [line({ quantity: 1, unitPrice: 200, lineDiscountPercent: 50 })],
      headerDiscountPercent: '10',
    });

    expect(t.lineDiscount).toBe(100);
    expect(t.headerDiscount).toBe(10);
    expect(t.taxableTotal).toBe(90);
    expect(t.headerDiscountPct).toBe(10);
  });

  it('adds shipping and subtracts withholding from the grand total', () => {
    const t = computeDocumentTotals({
      lines: [line({ unitPrice: 1000, taxRatePercent: 20, withholdingRatePercent: 5 })],
      shippingCost: '150',
    });

    expect(t.tax).toBe(200);
    expect(t.withholding).toBe(50);
    expect(t.shipping).toBe(150);
    expect(t.grandTotal).toBe(1300);
  });

  it('prefers the withholding code fraction of the tax over the free percent', () => {
    const t = computeDocumentTotals({
      lines: [
        line({
          unitPrice: 1000,
          taxRatePercent: 20,
          withholdingRatePercent: 5,
          withholdingTaxCodeId: 'w1',
        }),
      ],
      withholdingCodeById: new Map([['w1', { numerator: 7, denominator: 10 }]]),
    });

    expect(t.withholding).toBeCloseTo(140, 10);
  });

  it('falls back to the free percent when the code has a zero denominator', () => {
    const t = computeDocumentTotals({
      lines: [
        line({
          unitPrice: 1000,
          taxRatePercent: 20,
          withholdingRatePercent: 5,
          withholdingTaxCodeId: 'w1',
        }),
      ],
      withholdingCodeById: new Map([['w1', { numerator: 7, denominator: 0 }]]),
    });

    expect(t.withholding).toBe(50);
  });

  it('reports a uniform percent only when every active line agrees and it is positive', () => {
    const same = computeDocumentTotals({
      lines: [line({ taxRatePercent: 20 }), line({ productId: 'p2', taxRatePercent: 20 })],
    });
    const mixed = computeDocumentTotals({
      lines: [line({ taxRatePercent: 20 }), line({ productId: 'p2', taxRatePercent: 10 })],
    });
    const zero = computeDocumentTotals({ lines: [line({ taxRatePercent: 0 })] });

    expect(same.taxPct).toBe(20);
    expect(mixed.taxPct).toBeNull();
    expect(zero.taxPct).toBeNull();
  });

  it('ignores lines without a product when deciding whether a percent is uniform', () => {
    const t = computeDocumentTotals({
      lines: [line({ taxRatePercent: 20 }), { productId: '', taxRatePercent: 1 }],
    });

    expect(t.taxPct).toBe(20);
  });

  it('treats blank and unparsable numbers as zero', () => {
    const t = computeDocumentTotals({
      lines: [line({ quantity: '', unitPrice: 'abc', taxRatePercent: undefined })],
      headerDiscountPercent: '',
      shippingCost: undefined,
    });

    expect(t.subtotal).toBe(0);
    expect(t.tax).toBe(0);
    expect(t.grandTotal).toBe(0);
  });

  it('accepts numeric strings for every money input', () => {
    const t = computeDocumentTotals({
      lines: [
        line({
          quantity: '3',
          unitPrice: '50',
          lineDiscountPercent: '10',
          taxRatePercent: '20',
        }),
      ],
      headerDiscountPercent: '5',
      shippingCost: '25',
    });

    expect(t.subtotal).toBe(150);
    expect(t.lineDiscount).toBe(15);
    expect(t.headerDiscount).toBeCloseTo(6.75, 10);
    expect(t.taxableTotal).toBeCloseTo(128.25, 10);
    expect(t.tax).toBe(27);
    expect(t.grandTotal).toBeCloseTo(180.25, 10);
  });
});
