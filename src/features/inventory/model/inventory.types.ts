export type StockMovementType =
  | 'OpeningBalance'
  | 'Receipt'
  | 'Issue'
  | 'TransferIn'
  | 'TransferOut'
  | 'AdjustmentPositive'
  | 'AdjustmentNegative'
  | 'CountVariancePositive'
  | 'CountVarianceNegative'
  | 'Reservation'
  | 'UnReservation';

export type StockSourceDocumentType =
  | 'None'
  | 'Order'
  | 'Invoice'
  | 'Shipment'
  | 'Return'
  | 'Transfer'
  | 'Adjustment'
  | 'OpeningBalance'
  | 'CycleCount'
  | 'Production';

export type StockReasonCategory =
  | 'Receipt'
  | 'Issue'
  | 'Adjustment'
  | 'Transfer'
  | 'Loss'
  | 'Found'
  | 'CycleCount'
  | 'Return'
  | 'DamageWriteOff'
  | 'Expired';

export type AllocationStatus = 'Active' | 'PartiallyConsumed' | 'Consumed' | 'Released';

export interface StockItem {
  id: string;
  productId: string;
  productSku: string;
  productName: string;
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  lotId: string | null;
  lotNumber: string | null;
  lotExpiryDate: string | null;
  binLocation: string | null;
  onHand: number;
  reserved: number;
  availableToPromise: number;
  avgCost: number;
  reorderPoint: number | null;
  minStock: number | null;
  currency: string;
  lastMovementAtUtc: string | null;
}

export interface StockSummary {
  productId: string;
  productSku: string;
  productName: string;
  totalOnHand: number;
  totalReserved: number;
  totalAvailable: number;
  averageCost: number;
  currency: string;
  warehouseCount: number;
  isBelowReorder: boolean;
}

export interface StockMovement {
  id: string;
  productId: string;
  productSku: string;
  productName: string;
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  lotId: string | null;
  lotNumber: string | null;
  serialNumber: string | null;
  type: StockMovementType;
  quantity: number;
  unitCost: number;
  totalCost: number;
  onHandAfter: number;
  avgCostAfter: number;
  occurredAtUtc: string;
  sourceDocumentType: StockSourceDocumentType;
  sourceDocumentId: string | null;
  sourceReference: string | null;
  reasonCodeId: string | null;
  reasonCodeName: string | null;
  postedByUserId: string | null;
  notes: string | null;
}

export interface StockAllocation {
  id: string;
  orderId: string;
  orderLineId: string;
  productId: string;
  productSku: string;
  productName: string;
  warehouseId: string;
  warehouseName: string;
  lotId: string | null;
  lotNumber: string | null;
  quantity: number;
  quantityConsumed: number;
  remaining: number;
  status: AllocationStatus;
  allocatedAtUtc: string;
  releasedAtUtc: string | null;
}

export interface Lot {
  id: string;
  productId: string;
  lotNumber: string;
  manufactureDate: string | null;
  expiryDate: string | null;
  supplierLotRef: string | null;
  countryOfOrigin: string | null;
  notes: string | null;
  isBlocked: boolean;
  blockReason: string | null;
  isExpired: boolean;
  daysUntilExpiry: number | null;
}

export interface StockReasonCode {
  id: string;
  code: string;
  name: string;
  category: StockReasonCategory;
  affectsCost: boolean;
  description: string | null;
  isActive: boolean;
}

export interface AdjustStockInput {
  productId: string;
  warehouseId: string;
  delta: number;
  unitCost?: number | null;
  reasonCodeId?: string | null;
  lotId?: string | null;
  notes?: string | null;
}

export interface ReceiveStockInput {
  productId: string;
  warehouseId: string;
  quantity: number;
  unitCost: number;
  lotId?: string | null;
  serialNumber?: string | null;
  reasonCodeId?: string | null;
  reference?: string | null;
  notes?: string | null;
}

export interface IssueStockInput {
  productId: string;
  warehouseId: string;
  quantity: number;
  lotId?: string | null;
  serialNumber?: string | null;
  reasonCodeId?: string | null;
  reference?: string | null;
  notes?: string | null;
}

export interface CreateLotInput {
  productId: string;
  lotNumber: string;
  manufactureDate?: string | null;
  expiryDate?: string | null;
  supplierLotRef?: string | null;
  countryOfOrigin?: string | null;
  notes?: string | null;
}

export interface UpdateLotInput {
  id: string;
  lotNumber: string;
  manufactureDate?: string | null;
  expiryDate?: string | null;
  supplierLotRef?: string | null;
  countryOfOrigin?: string | null;
  notes?: string | null;
  isBlocked: boolean;
  blockReason?: string | null;
}

export interface ProduceInput {
  productId: string;
  warehouseId: string;
  quantity: number;
  notes?: string | null;
}

export interface TransferStockInput {
  productId: string;
  fromWarehouseId: string;
  toWarehouseId: string;
  quantity: number;
  operationId?: string | null;
  reference?: string | null;
}

export interface StockTransferResult {
  productId: string;
  fromWarehouseId: string;
  toWarehouseId: string;
  quantity: number;
  unitCost: number;
  fromOnHandAfter: number;
  toOnHandAfter: number;
  sourceDocumentId: string;
  movementsCreated: number;
  transferOut: StockMovement;
  transferIn: StockMovement;
}
