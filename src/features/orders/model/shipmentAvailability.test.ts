import { describe, expect, it } from 'vitest';
import { availableToShip, claimedQuantityByOrderLine } from './shipmentAvailability';
import type { Shipment, ShipmentLine, ShipmentStatus } from './order.types';

const line = (orderLineId: string, quantity: number): ShipmentLine => ({
  id: `sl-${orderLineId}-${quantity}`,
  orderLineId,
  productId: 'p1',
  productSku: 'SKU',
  productName: 'Product',
  lotId: null,
  lotNumber: null,
  serialNumber: null,
  quantity,
  unitCostSnapshot: 0,
  notes: null,
});

const shipment = (status: ShipmentStatus, lines: ShipmentLine[]): Shipment =>
  ({ id: `s-${status}-${lines.length}`, status, lines }) as unknown as Shipment;

describe('claimedQuantityByOrderLine', () => {
  it('counts draft, picked and packed shipments', () => {
    const claimed = claimedQuantityByOrderLine([
      shipment('Draft', [line('ol-1', 2)]),
      shipment('Picked', [line('ol-1', 3)]),
      shipment('Packed', [line('ol-2', 4)]),
    ]);

    expect(claimed.get('ol-1')).toBe(5);
    expect(claimed.get('ol-2')).toBe(4);
  });

  it('ignores cancelled and returned shipments because they release their claim', () => {
    const claimed = claimedQuantityByOrderLine([
      shipment('Cancelled', [line('ol-1', 7)]),
      shipment('Returned', [line('ol-1', 7)]),
    ]);

    expect(claimed.get('ol-1')).toBeUndefined();
  });

  it('ignores dispatched and delivered shipments because they already moved quantityShipped', () => {
    const claimed = claimedQuantityByOrderLine([
      shipment('Dispatched', [line('ol-1', 6)]),
      shipment('Delivered', [line('ol-1', 6)]),
    ]);

    expect(claimed.get('ol-1')).toBeUndefined();
  });
});

describe('availableToShip', () => {
  it('subtracts the claimed quantity', () => {
    expect(availableToShip(10, 4)).toBe(6);
  });

  it('never goes below zero', () => {
    expect(availableToShip(3, 8)).toBe(0);
  });

  it('keeps four decimal places without floating point drift', () => {
    expect(availableToShip(1.3, 0.1)).toBe(1.2);
  });
});
