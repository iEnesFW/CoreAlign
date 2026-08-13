import { describe, expect, it } from 'vitest';
import { issueCreditNoteSchema } from './issueCreditNoteSchema';

const base = {
  reason: '',
  lines: [
    { invoiceLineId: 'il-1', selected: true, quantity: 2, remaining: 5 },
    { invoiceLineId: 'il-2', selected: false, quantity: 0, remaining: 0 },
  ],
};

const messages = (result: ReturnType<typeof issueCreditNoteSchema.safeParse>) =>
  result.success ? [] : result.error.issues.map((i) => i.message);

describe('issueCreditNoteSchema', () => {
  it('accepts a selected line within its remaining quantity', () => {
    expect(issueCreditNoteSchema.safeParse(base).success).toBe(true);
  });

  it('rejects a credit note with no selected line', () => {
    const result = issueCreditNoteSchema.safeParse({
      ...base,
      lines: base.lines.map((l) => ({ ...l, selected: false })),
    });
    expect(messages(result)).toContain('invoices.creditNote.selectAtLeastOne');
  });

  it('rejects a selected line with a zero quantity', () => {
    const result = issueCreditNoteSchema.safeParse({
      ...base,
      lines: [{ invoiceLineId: 'il-1', selected: true, quantity: 0, remaining: 5 }],
    });
    expect(messages(result)).toContain('Validation.Positive');
  });

  it('rejects a quantity above what earlier credit notes left', () => {
    const result = issueCreditNoteSchema.safeParse({
      ...base,
      lines: [{ invoiceLineId: 'il-1', selected: true, quantity: 6, remaining: 5 }],
    });
    expect(messages(result)).toContain('invoices.creditNote.exceedsRemaining');
  });

  it('rejects a fully credited line even when it is selected', () => {
    const result = issueCreditNoteSchema.safeParse({
      ...base,
      lines: [{ invoiceLineId: 'il-1', selected: true, quantity: 1, remaining: 0 }],
    });
    expect(messages(result)).toContain('invoices.creditNote.exceedsRemaining');
  });

  it('rejects a reason longer than the server accepts', () => {
    const result = issueCreditNoteSchema.safeParse({ ...base, reason: 'x'.repeat(501) });
    expect(messages(result)).toContain('Validation.TooLong');
  });
});
