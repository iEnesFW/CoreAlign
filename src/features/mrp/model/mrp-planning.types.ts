export type MrpBucketKind = 'Day' | 'Week';

export type LotSizingPolicy =
  | 'LotForLot'
  | 'FixedOrderQuantity'
  | 'MinMax'
  | 'EconomicOrderQuantity'
  | 'PeriodOrderQuantity';

export type MrpActionType =
  | 'Release'
  | 'RescheduleIn'
  | 'RescheduleOut'
  | 'Expedite'
  | 'CancelSupply'
  | 'BelowSafetyStock'
  | 'ProjectedStockout';

export type MrpActionSeverity = 'Info' | 'Warning' | 'Critical';

export type MrpPlanRunStatus = 'Preview' | 'Committed';

export type MrpPegSourceKind = 'SalesOrder' | 'PlannedOrder' | 'Forecast';

export type ProcurementType = 'Buy' | 'Make';

export type AbcClass = 'A' | 'B' | 'C' | 'Unclassified';

export type OrderSinkKind = 'PurchaseRequisition' | 'ProductionOrder';

export type PlannedProductionOrderStatus = 'Planned' | 'Firm' | 'Released' | 'Closed';

export const sinkKindToProcurementType = (kind: OrderSinkKind): ProcurementType =>
  kind === 'ProductionOrder' ? 'Make' : 'Buy';

export const transferSuggestionKey = (s: MrpTransferSuggestion): string =>
  `${s.productId}:${s.fromWarehouseId}:${s.toWarehouseId}`;

export interface MrpBucket {
  startUtc: string;
  grossRequirements: number;
  scheduledReceipts: number;
  projectedOnHand: number;
  netRequirements: number;
  plannedReceipts: number;
  plannedReleases: number;
}

export interface MrpPlannedOrder {
  productId: string;
  lowLevelCode: number;
  quantity: number;
  dueDateUtc: string;
  releaseDateUtc: string;
  preferredSupplierId: string | null;
  estimatedUnitCost: number;
  sourcePolicy: LotSizingPolicy;
  procurementType: ProcurementType;
  id?: string;
  planRunId?: string;
  productSku?: string;
  productName?: string;
  isFirmed?: boolean;
  isReleased?: boolean;
  convertedRequisitionId?: string | null;
  productionOrderId?: string | null;
  originalQuantity?: number | null;
  originalDueDateUtc?: string | null;
  isQuantityOverridden?: boolean;
  isDueDateOverridden?: boolean;
}

export interface MrpProductionOrderDraft {
  productId: string;
  lowLevelCode: number;
  quantity: number;
  dueDateUtc: string;
  releaseDateUtc: string;
  estimatedUnitCost: number;
  sourcePolicy: LotSizingPolicy;
  peggingParentProductId: string | null;
  peggingSourceOrderLineId: string | null;
  id?: string | null;
  status?: PlannedProductionOrderStatus | null;
}

export interface PlannedProductionOrder {
  id: string;
  planRunId: string;
  productId: string;
  productSku: string;
  productName: string;
  quantity: number;
  dueDateUtc: string;
  releaseDateUtc: string;
  status: PlannedProductionOrderStatus;
  sourcePlanRunId: string;
  peggingParentProductId: string | null;
}

export interface ChangeImpactSupplyOrder {
  productId: string;
  lowLevelCode: number;
  sinkKind: OrderSinkKind;
  quantity: number;
  dueDateUtc: string;
  releaseDateUtc: string;
  directParentProductId: string | null;
}

export interface ChangeImpactResult {
  planRunId: string;
  sourceOrderLineId: string;
  downstreamSupply: ChangeImpactSupplyOrder[];
}

export interface MrpActionMessage {
  id: string;
  planRunId: string;
  productId: string;
  productSku: string;
  productName: string;
  actionType: MrpActionType;
  severity: MrpActionSeverity;
  quantity: number;
  currentDateUtc: string | null;
  suggestedDateUtc: string | null;
  relatedPurchaseOrderId: string | null;
  relatedPlannedOrderId: string | null;
  daysUntilStockOut: number;
  isDismissed: boolean;
  dismissedAtUtc: string | null;
  message: string;
}

export interface MrpPegging {
  componentProductId: string;
  requirementQuantity: number;
  dueDateUtc: string;
  sourceKind: MrpPegSourceKind;
  sourceParentProductId: string | null;
  sourceParentProductName: string | null;
  sourceOrderLineId: string | null;
  sourceOrderNumber: string | null;
}

export interface MrpItemPlan {
  productId: string;
  sku: string;
  name: string;
  lowLevelCode: number;
  onHand: number;
  reserved: number;
  safetyStock: number;
  reorderPoint: number;
  policy: LotSizingPolicy;
  procurementType: ProcurementType;
  abcClass: AbcClass;
  preferredSupplierId: string | null;
  leadTimeDays: number;
  buckets: MrpBucket[];
  plannedOrders: MrpPlannedOrder[];
  productionOrders: MrpProductionOrderDraft[];
  actions: MrpActionMessage[];
  pegs: MrpPegging[];
}

export interface MrpPlanResult {
  planRunId: string | null;
  status: MrpPlanRunStatus;
  asOfUtc: string;
  bucketKind: MrpBucketKind;
  horizonDays: number;
  productsEvaluated: number;
  plannedOrderCount: number;
  actionMessageCount: number;
  makeOrderCount: number;
  buyOrderCount: number;
  stockoutRiskCount: number;
  projectedStockoutCount: number;
  excessSupplyCount: number;
  onOrderCount: number;
  items: MrpItemPlan[];
}

export interface MrpTransferSuggestion {
  productId: string;
  productSku: string;
  productName: string;
  fromWarehouseId: string;
  fromWarehouseCode: string;
  fromWarehouseName: string;
  toWarehouseId: string;
  toWarehouseCode: string;
  toWarehouseName: string;
  quantity: number;
}

export interface MrpWarehouseNetPosition {
  productId: string;
  productSku: string;
  productName: string;
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  available: number;
  demand: number;
  net: number;
}

export interface MrpExternalReplenishment {
  productId: string;
  productSku: string;
  productName: string;
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  quantity: number;
}

export interface MrpTransferSuggestionsResult {
  productsEvaluated: number;
  transferCount: number;
  externalReplenishmentCount: number;
  transfers: MrpTransferSuggestion[];
  netPositions: MrpWarehouseNetPosition[];
  externalReplenishment: MrpExternalReplenishment[];
}

export interface MrpCapacityBucket {
  startUtc: string;
  loadMinutes: number;
  capacityMinutes: number;
  isOverloaded: boolean;
}

export interface MrpWorkCenterLoad {
  workCenterId: string;
  code: string;
  name: string;
  dailyCapacityMinutes: number;
  buckets: MrpCapacityBucket[];
}

export interface MrpCapacityLoadResult {
  asOfUtc: string;
  bucketKind: MrpBucketKind;
  horizonDays: number;
  bucketStarts: string[];
  workCenters: MrpWorkCenterLoad[];
  unroutedProductionOrderCount: number;
}

export interface MrpCapacityLoadParams {
  bucketKind?: MrpBucketKind;
  horizonDays?: number;
}

export interface ClassifyAbcResult {
  totalEvaluated: number;
  classA: number;
  classB: number;
  classC: number;
  unclassified: number;
  policyDefaultsApplied: number;
  asOfUtc: string;
}

export interface MrpPlanRun {
  id: string;
  number: string;
  status: MrpPlanRunStatus;
  asOfDateUtc: string;
  bucketKind: MrpBucketKind;
  horizonDays: number;
  productsEvaluated: number;
  plannedOrderCount: number;
  actionMessageCount: number;
  createdByUserId: string;
  createdAtUtc: string;
}

export interface ReleaseResult {
  planRunId: string;
  requisitionIds: string[];
  plannedOrdersReleased: number;
}

export interface MrpPreviewParams {
  asOfDateUtc?: string | null;
  bucketKind?: MrpBucketKind;
  horizonDays?: number;
}

export type ProcurementFilter = ProcurementType | 'All';

export interface ChangeImpactParams {
  planRunId: string;
  sourceOrderLineId: string;
}

export interface MrpItemPlanParams extends MrpPreviewParams {
  productId: string;
}

export interface MrpActionMessageParams {
  planRunId?: string | null;
  type?: MrpActionType | null;
  severity?: MrpActionSeverity | null;
  supplierId?: string | null;
  includeDismissed?: boolean;
  page?: number;
  pageSize?: number;
}

export interface CommitMrpPlanInput {
  asOfDateUtc?: string | null;
  bucketKind?: MrpBucketKind;
  horizonDays?: number;
  operationId: string;
}

export interface ReleasePlannedOrdersInput {
  planRunId: string;
  plannedOrderIds: string[];
  operationId: string;
}

export interface FirmPlannedOrderInput {
  plannedOrderId: string;
  overrideQuantity?: number | null;
  overrideDueDateUtc?: string | null;
  operationId: string;
}

export interface FirmProductionOrderInput {
  productionOrderId: string;
  operationId: string;
}

export interface ReleaseProductionOrderInput {
  productionOrderId: string;
  operationId: string;
}

export interface CompleteProductionOrderInput {
  productionOrderId: string;
  warehouseId?: string | null;
  operationId: string;
}

export interface CompletePlannedProductionOrderResult {
  plannedProductionOrderId: string;
  productId: string;
  warehouseId: string;
  producedQuantity: number;
  componentsIssued: number;
  unitCost: number;
  totalCost: number;
  status: PlannedProductionOrderStatus;
  alreadyCompleted: boolean;
}
