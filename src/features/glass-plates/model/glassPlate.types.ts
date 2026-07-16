export type PlateKind = 'Fresh' | 'Remnant';
export type GlassPlateStatus = 'Available' | 'Reserved' | 'InUse' | 'Consumed' | 'Scrapped';
export type PlateCondition = 'Good' | 'Chipped' | 'Cracked' | 'Scratched';
export type StorageLocationKind = 'Rack' | 'Shelf' | 'Pallet' | 'Floor' | 'Zone';
export type GlassScrapMode = 'Area' | 'Count';

export interface StorageLocation {
  id: string;
  warehouseId: string;
  parentLocationId: string | null;
  code: string;
  name: string;
  kind: StorageLocationKind;
  isActive: boolean;
  notes: string | null;
}

export interface GlassPlate {
  id: string;
  productId: string;
  warehouseId: string;
  warehouseName: string;
  storageLocationId: string | null;
  storageLocationCode: string | null;
  storageLocationName: string | null;
  lotId: string | null;
  plateNumber: string;
  kind: PlateKind;
  status: GlassPlateStatus;
  widthMm: number;
  heightMm: number;
  thicknessMm: number;
  originalAreaMm2: number;
  remainingAreaMm2: number;
  utilizationPercent: number;
  parentPlateId: string | null;
  condition: PlateCondition;
  receivedAtUtc: string;
  consumedAtUtc: string | null;
}

export interface GlassPlateConsumption {
  id: string;
  glassPlateId: string;
  productId: string;
  warehouseId: string;
  orderLineId: string | null;
  jobId: string | null;
  cutAreaMm2: number;
  pieces: number;
  scrappedAreaMm2: number;
  resultingRemnantPlateId: string | null;
  occurredAtUtc: string;
}

export interface LowStockPlate {
  productId: string;
  sku: string;
  productName: string;
  warehouseId: string;
  warehouseName: string;
  availableCount: number;
  minPlateCount: number;
}

export interface GlassPlateListParams {
  productId?: string;
  warehouseId?: string;
  storageLocationId?: string;
  status?: GlassPlateStatus;
  kind?: PlateKind;
  take?: number;
}

export interface ReceiveGlassPlateLine {
  plateNumber: string;
  widthMm: number;
  heightMm: number;
  thicknessMm: number;
  condition?: PlateCondition;
}

export interface ReceiveGlassPlatesInput {
  productId: string;
  warehouseId: string;
  storageLocationId?: string | null;
  lotId?: string | null;
  unitCostPerM2: number;
  plates: ReceiveGlassPlateLine[];
  notes?: string | null;
}

export interface ReceiveGlassPlatesResult {
  movementId: string;
  plateCount: number;
  totalAreaM2: number;
}

export interface ScrapGlassPlateInput {
  plateId?: string | null;
  productId?: string | null;
  warehouseId?: string | null;
  mode: GlassScrapMode;
  areaMm2?: number | null;
  reasonCodeId: string;
  notes?: string | null;
  workCenterId?: string | null;
  operatorId?: string | null;
}

export interface GlassScrapResult {
  movementId: string;
  scrappedAreaMm2: number;
  platesScrapped: number;
}

export interface ConsumeGlassPlateInput {
  plateId: string;
  cutAreaMm2: number;
  pieces: number;
  cutWidthMm?: number | null;
  cutHeightMm?: number | null;
  remnantWidthMm?: number | null;
  remnantHeightMm?: number | null;
  remnantPlateNumber?: string | null;
  orderLineId?: string | null;
  jobId?: string | null;
  workCenterId?: string | null;
  operatorId?: string | null;
}

export interface ConsumeGlassPlateResult {
  movementId: string;
  consumedAreaMm2: number;
  remnantPlateId: string | null;
  remnantAreaMm2: number;
  scrappedAreaMm2: number;
}

export interface MoveGlassPlateInput {
  warehouseId: string;
  storageLocationId?: string | null;
}

export interface SetGlassPlateTrackingInput {
  productId: string;
  isPlateTracked: boolean;
  minRemnantAreaMm2?: number | null;
  minRemnantWidthMm?: number | null;
  minRemnantHeightMm?: number | null;
  minPlateCount?: number | null;
  standardWidthMm?: number | null;
  standardHeightMm?: number | null;
}

export interface CreateStorageLocationInput {
  warehouseId: string;
  code: string;
  name: string;
  kind: StorageLocationKind;
  parentLocationId?: string | null;
  notes?: string | null;
}

export interface UpdateStorageLocationInput {
  id: string;
  code: string;
  name: string;
  kind: StorageLocationKind;
  parentLocationId?: string | null;
  isActive: boolean;
  notes?: string | null;
}

export interface UsablePlatesParams {
  productId: string;
  widthMm: number;
  heightMm: number;
  warehouseId?: string;
  take?: number;
}

export interface AssignUserWarehousesInput {
  userId: string;
  warehouseIds: string[];
}
