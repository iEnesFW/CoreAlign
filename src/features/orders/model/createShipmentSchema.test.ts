import { describe, expect, it } from 'vitest';
import { createShipmentSchema } from './createShipmentSchema';

const base = {
  warehouseId: 'wh-1',
  notes: '',
  lines: [
    { orderLineId: 'ol-1', selected: true, quantity: 4, available: 6 },
    { orderLineId: 'ol-2', selected: false, quantity: 0, available: 3 },
  ],
};

const messages = (result: ReturnType<typeof createShipmentSchema.safeParse>) =>
  result.success ? [] : result.error.issues.map((i) => i.message);

describe('createShipmentSchema', () => {
  it('accepts a selected line within its available quantity', () => {
    expect(createShipmentSchema.safeParse(base).success).toBe(true);
  });

  it('requires a warehouse', () => {
    const result = createShipmentSchema.safeParse({ ...base, warehouseId: '' });
    expect(messages(result)).toContain('Validation.Required');
  });

  it('rejects a shipment with no selected line', () => {
    const result = createShipmentSchema.safeParse({
      ...base,
      lines: base.lines.map((l) => ({ ...l, selected: false })),
    });
    expect(messages(result)).toContain('orders.shipments.selectAtLeastOne');
  });

  it('rejects a selected line with a zero quantity', () => {
    const result = createShipmentSchema.safeParse({
      ...base,
      lines: [{ orderLineId: 'ol-1', selected: true, quantity: 0, available: 6 }],
    });
    expect(messages(result)).toContain('Validation.Positive');
  });

  it('rejects a quantity above what open shipments left available', () => {
    const result = createShipmentSchema.safeParse({
      ...base,
      lines: [{ orderLineId: 'ol-1', selected: true, quantity: 7, available: 6 }],
    });
    expect(messages(result)).toContain('orders.shipments.exceedsAvailable');
  });

  it('ignores the quantity of an unselected line', () => {
    const result = createShipmentSchema.safeParse({
      ...base,
      lines: [
        { orderLineId: 'ol-1', selected: true, quantity: 1, available: 6 },
        { orderLineId: 'ol-2', selected: false, quantity: 99, available: 3 },
      ],
    });
    expect(result.success).toBe(true);
  });
});
