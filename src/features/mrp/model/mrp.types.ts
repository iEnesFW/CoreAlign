export type PurchaseRequisitionStatus =
  | 'Draft'
  | 'Submitted'
  | 'Approved'
  | 'Rejected'
  | 'Converted'
  | 'Cancelled';

export type PurchaseRequisitionReason = 'MRPSuggestion' | 'Manual' | 'EmergencyOrder' | 'StockOut';

export interface StockProjectionPoint {
  date: string;
  projectedQuantity: number;
  demand: number;
  onOrder: number;
  committed: number;
}

export interface StockProjection {
  productId: string;
  productSku: string;
  productName: string;
  currentOnHand: number;
  currentReserved: number;
  totalOnOrder: number;
  totalCommitted: number;
  reorderPoint: number;
  daysAhead: number;
  points: StockProjectionPoint[];
  shouldReorder: boolean;
  suggestedOrderQuantity: number;
}

export interface DemandForecast {
  productId: string;
  productSku: string;
  productName: string;
  windowDays: number;
  totalDemand: number;
  averageDailyDemand: number;
  peakDailyDemand: number | null;
  asOfUtc: string;
}

export interface MrpReorderCandidate {
  productId: string;
  productSku: string;
  productName: string;
  onHand: number;
  reserved: number;
  onOrder: number;
  committed: number;
  projectedAvailable: number;
  reorderPoint: number;
  suggestedOrderQuantity: number;
  preferredSupplierId: string | null;
  leadTimeDays: number;
  daysUntilStockOut: number;
}

export interface MrpDashboard {
  totalProductsTracked: number;
  reorderCandidateCount: number;
  pendingRequisitionCount: number;
  openPurchaseOrderCount: number;
  topCandidates: MrpReorderCandidate[];
  generatedAtUtc: string;
}

export interface PurchaseRequisitionLine {
  id: string;
  lineNumber: number;
  productId: string;
  productSku: string;
  productName: string;
  quantityRequested: number;
  estimatedUnitCost: number;
  estimatedLineTotal: number;
  preferredSupplierId: string | null;
  expectedDeliveryDate: string | null;
  notes: string | null;
}

export interface PurchaseRequisition {
  id: string;
  number: string;
  status: PurchaseRequisitionStatus;
  reason: PurchaseRequisitionReason;
  requestedAtUtc: string;
  requestedByUserId: string;
  approvedByUserId: string | null;
  approvedAtUtc: string | null;
  submittedAtUtc: string | null;
  rejectedAtUtc: string | null;
  rejectReason: string | null;
  cancelledAtUtc: string | null;
  cancelReason: string | null;
  convertedAtUtc: string | null;
  convertedPurchaseOrderId: string | null;
  notes: string | null;
  lines: PurchaseRequisitionLine[];
  estimatedTotal: number;
  createdAtUtc: string;
  concurrencyToken: number;
}

export interface PurchaseRequisitionLineInput {
  productId: string;
  quantityRequested: number;
  estimatedUnitCost: number;
  preferredSupplierId?: string | null;
  expectedDeliveryDate?: string | null;
  notes?: string | null;
}

export interface CreatePurchaseRequisitionInput {
  reason: PurchaseRequisitionReason;
  lines: PurchaseRequisitionLineInput[];
  notes?: string | null;
}

export interface ConvertRequisitionInput {
  id: string;
  vendorId: string;
  currency: string;
  expectedDate?: string | null;
}

export interface RequisitionListParams {
  status?: PurchaseRequisitionStatus;
  productId?: string;
  fromUtc?: string;
  toUtc?: string;
  page?: number;
  pageSize?: number;
}

export interface MrpSuggestionResult {
  candidatesEvaluated: number;
  requisitionsCreated: number;
  linesCreated: number;
  requisitionIds: string[];
  asOfDateUtc: string;
}
