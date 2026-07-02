import { describe, expect, it } from 'vitest';
import { resolveNextAction } from './nextActionMap';

describe('resolveNextAction', () => {
  it('maps order statuses to the expected primary action', () => {
    expect(resolveNextAction('order', 'Draft')?.action).toBe('submit');
    expect(resolveNextAction('order', 'Submitted')?.action).toBe('approve');
    expect(resolveNextAction('order', 'Approved')?.action).toBe('allocate');
    expect(resolveNextAction('order', 'Allocated')?.action).toBe('createShipment');
    expect(resolveNextAction('order', 'Shipped')?.action).toBe('generateInvoice');
    expect(resolveNextAction('order', 'Delivered')?.action).toBe('generateInvoice');
  });

  it('maps quote statuses to the expected primary action', () => {
    expect(resolveNextAction('quote', 'Draft')?.action).toBe('send');
    expect(resolveNextAction('quote', 'Sent')?.action).toBe('accept');
    expect(resolveNextAction('quote', 'Accepted')?.action).toBe('convertToOrder');
  });

  it('maps invoice statuses to collectPayment when collectable', () => {
    expect(resolveNextAction('invoice', 'Issued')?.action).toBe('collectPayment');
    expect(resolveNextAction('invoice', 'PartiallyPaid')?.action).toBe('collectPayment');
    expect(resolveNextAction('invoice', 'Overdue')?.action).toBe('collectPayment');
  });

  it('builds an entity-scoped i18n label key', () => {
    expect(resolveNextAction('order', 'Draft')?.labelKey).toBe('NextBestAction.order.submit');
    expect(resolveNextAction('quote', 'Accepted')?.labelKey).toBe(
      'NextBestAction.quote.convertToOrder',
    );
  });

  it('returns null for terminal / non-actionable statuses', () => {
    expect(resolveNextAction('order', 'Closed')).toBeNull();
    expect(resolveNextAction('order', 'Cancelled')).toBeNull();
    expect(resolveNextAction('quote', 'Rejected')).toBeNull();
    expect(resolveNextAction('invoice', 'Paid')).toBeNull();
    expect(resolveNextAction('invoice', 'Void')).toBeNull();
    expect(resolveNextAction('invoice', 'Draft')).toBeNull();
  });

  it('returns null for an unknown status', () => {
    expect(resolveNextAction('order', 'Bogus')).toBeNull();
    expect(resolveNextAction('invoice', '')).toBeNull();
  });
});
