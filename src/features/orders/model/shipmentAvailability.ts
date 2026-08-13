import type { Shipment, ShipmentStatus } from './order.types';

const OPEN_STATUSES: readonly ShipmentStatus[] = ['Draft', 'Picked', 'Packed'];

// WHY only these three count: QuantityShipped moves on dispatch, so an open shipment's claim is
// still invisible to the order line, while Cancelled/Returned release theirs and
// Dispatched/Delivered are already inside quantityRemainingToShip.
export const claimedQuantityByOrderLine = (shipments: readonly Shipment[]): Map<string, number> => {
  const claimed = new Map<string, number>();
  for (const shipment of shipments) {
    if (!OPEN_STATUSES.includes(shipment.status)) continue;
    for (const line of shipment.lines) {
      claimed.set(line.orderLineId, (claimed.get(line.orderLineId) ?? 0) + line.quantity);
    }
  }
  return claimed;
};

export const availableToShip = (remainingToShip: number, claimed: number): number =>
  Math.max(0, Math.round((remainingToShip - claimed) * 10000) / 10000);
