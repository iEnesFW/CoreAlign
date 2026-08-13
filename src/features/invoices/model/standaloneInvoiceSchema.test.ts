import { describe, expect, it } from 'vitest';
import { standaloneInvoiceSchema } from './standaloneInvoiceSchema';

const valid = {
  customerId: '11111111-1111-1111-1111-111111111111',
  issueDate: '2026-08-13',
  dueDays: 30,
  currency: 'TRY',
  headerDiscountPercent: '',
  shippingCost: '',
  vatExemptionCodeId: '',
  vatExemptionReason: '',
  publicNotes: '',
  internalNotes: '',
  lines: [
    {
      productSku: 'SKU-1',
      productName: 'Cam panel',
      description: '',
      quantity: 2,
      unitPrice: 100,
      lineDiscountPercent: '',
      taxRatePercent: '20',
      withholdingTaxCodeId: '',
    },
  ],
};

describe('standaloneInvoiceSchema', () => {
  it('accepts a minimal valid invoice', () => {
    expect(standaloneInvoiceSchema.safeParse(valid).success).toBe(true);
  });

  it('requires a customer', () => {
    const result = standaloneInvoiceSchema.safeParse({ ...valid, customerId: '' });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.message === 'Validation.Required')).toBe(true);
    }
  });

  it('requires at least one line', () => {
    const result = standaloneInvoiceSchema.safeParse({ ...valid, lines: [] });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.message === 'Validation.AtLeastOneLine')).toBe(true);
    }
  });

  it('requires sku, name and a positive quantity on every line', () => {
    for (const bad of [
      { productSku: '' },
      { productName: '' },
      { quantity: 0 },
      { quantity: -1 },
    ]) {
      expect(
        standaloneInvoiceSchema.safeParse({
          ...valid,
          lines: [{ ...valid.lines[0], ...bad }],
        }).success,
      ).toBe(false);
    }
  });

  it('rejects a negative unit price but accepts a free line', () => {
    expect(
      standaloneInvoiceSchema.safeParse({
        ...valid,
        lines: [{ ...valid.lines[0], unitPrice: -1 }],
      }).success,
    ).toBe(false);
    expect(
      standaloneInvoiceSchema.safeParse({
        ...valid,
        lines: [{ ...valid.lines[0], unitPrice: 0 }],
      }).success,
    ).toBe(true);
  });

  it('keeps the due days inside the payment-term window', () => {
    expect(standaloneInvoiceSchema.safeParse({ ...valid, dueDays: 0 }).success).toBe(true);
    expect(standaloneInvoiceSchema.safeParse({ ...valid, dueDays: 365 }).success).toBe(true);
    expect(standaloneInvoiceSchema.safeParse({ ...valid, dueDays: -1 }).success).toBe(false);
    expect(standaloneInvoiceSchema.safeParse({ ...valid, dueDays: 366 }).success).toBe(false);
    expect(standaloneInvoiceSchema.safeParse({ ...valid, dueDays: 1.5 }).success).toBe(false);
  });

  it('rejects a currency that is not a three-letter uppercase code', () => {
    expect(standaloneInvoiceSchema.safeParse({ ...valid, currency: 'try' }).success).toBe(false);
    expect(standaloneInvoiceSchema.safeParse({ ...valid, currency: 'TRYX' }).success).toBe(false);
  });
});
