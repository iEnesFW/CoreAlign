export type SerialStatus = 'InStock' | 'Shipped' | 'Returned' | 'Scrapped';

export interface SerialComponent {
  id: string;
  productId: string;
  serialNumber: string;
  status: SerialStatus;
}

export interface SerialWhereUsed {
  id: string;
  productId: string;
  serialNumber: string;
  status: SerialStatus;
  warehouseId: string | null;
  lotId: string | null;
  unitCost: number;
  receivedAtUtc: string;
  orderId: string | null;
  shipmentId: string | null;
  currentOwnerCustomerId: string | null;
  parentSerialUnitId: string | null;
  components: SerialComponent[];
}

export interface RegisterSerialsInput {
  productId: string;
  serialNumbers: string[];
  warehouseId?: string | null;
  lotId?: string | null;
  unitCost?: number;
  sourceReceiptMovementId?: string | null;
}

export interface ShipSerialsInput {
  productId: string;
  serialNumbers: string[];
  orderId: string;
  shipmentId?: string | null;
  customerId?: string | null;
}
