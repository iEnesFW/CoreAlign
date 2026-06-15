export type StockCountStatus = 'Plan' | 'Counting' | 'Reconciliation' | 'Posted' | 'Cancelled';

export interface StockCountLine {
  id: string;
  productId: string;
  productSku: string;
  productName: string;
  lotId: string | null;
  lotNumber: string | null;
  binLocation: string | null;
  expectedQuantity: number;
  countedQuantity: number | null;
  varianceQuantity: number;
  snapshotUnitCost: number;
  varianceCost: number;
  countedAtUtc: string | null;
  countedByUserId: string | null;
  lineNotes: string | null;
}

export interface StockCount {
  id: string;
  countNumber: string;
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  status: StockCountStatus;
  plannedAtUtc: string;
  countingStartedAtUtc: string | null;
  reconciledAtUtc: string | null;
  postedAtUtc: string | null;
  plannedByUserId: string | null;
  postedByUserId: string | null;
  notes: string | null;
  totalVarianceQuantity: number;
  totalVarianceCost: number;
  lines: StockCountLine[];
  createdAtUtc: string;
}

export interface PlanStockCountInput {
  warehouseId: string;
  countNumber?: string | null;
  notes?: string | null;
}

export interface RecordCountLineInput {
  lineId: string;
  countedQuantity: number;
  lineNotes?: string | null;
}

export interface RecordCountInput {
  id: string;
  lines: RecordCountLineInput[];
}

export interface ReconcileStockCountInput {
  id: string;
  notes?: string | null;
}

export interface StockCountListParams {
  warehouseId?: string;
  status?: StockCountStatus;
  page?: number;
  pageSize?: number;
}
