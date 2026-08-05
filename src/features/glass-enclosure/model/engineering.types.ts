export interface BOMLineDto {
  id: string;
  kind:
    | 'ProfileCut'
    | 'GlassPiece'
    | 'HardwarePiece'
    | 'Labor'
    | 'Transport'
    | 'Installation'
    | 'Insurance'
    | 'Discount';
  refId: string | null;
  description: string;
  quantity: number;
  unit: string;
  unitCost: number;
  lineCost: number;
  currency: string;
  source: string | null;
  sortOrder: number;
  productId: string | null;
  isService: boolean;
  cutSpecJson: string | null;
  isManual: boolean;
  unitPriceOverride: number | null;
}

export interface AddManualBomLineInput {
  description: string;
  quantity: number;
  unit: string;
  unitPrice: number;
  kind?: BOMLineDto['kind'];
}

export interface PushBomLinePriceResultDto {
  lineId: string;
  catalogItemId: string;
  kind: BOMLineDto['kind'];
  pushedUnitPrice: number;
  newCatalogPrice: number;
  currency: string;
}

export interface BOMSummaryDto {
  totalAreaM2: number;
  totalPanels: number;
  totalWeightKg: number;
  profileCost: number;
  glassCost: number;
  hardwareCost: number;
  laborCost: number;
  wasteCost: number;
  transportCost: number;
  scaffoldingCost: number;
  craneCost: number;
  subtotal: number;
  marginAmount: number;
  taxAmount: number;
  grandTotal: number;
  currency: string;
  lines: BOMLineDto[];
}

export interface CuttingCut1DDto {
  label: string;
  lengthMm: number;
  offsetMm: number;
  pieceIndex?: number;
  pieceCount?: number;
}

export interface CuttingPattern1DDto {
  barIndex: number;
  stockBarLengthMm: number;
  cuts: CuttingCut1DDto[];
  wasteMm: number;
  offcutMm: number;
}

export interface CuttingResult1DDto {
  stockBarLengthMm: number;
  kerfMm: number;
  totalBars: number;
  totalCuts: number;
  totalUsedMm: number;
  totalWasteMm: number;
  utilizationPercent: number;
  patterns: CuttingPattern1DDto[];
}

export interface PanelCutShapeDto {
  topShape: string | null;
  nominalHeightMm: number;
  topRightHeightMm: number | null;
  archRiseMm: number | null;
  cornerRadiusTlMm: number | null;
  cornerRadiusTrMm: number | null;
  cornerRadiusBrMm: number | null;
  cornerRadiusBlMm: number | null;
  netAreaMm2: number;
  shapeKind?: string | null;
  shapePointsJson?: string | null;
}

export interface CuttingPlacement2DDto {
  label: string;
  x: number;
  y: number;
  widthMm: number;
  heightMm: number;
  rotated: boolean;
  shape?: PanelCutShapeDto | null;
}

export interface CuttingSheet2DDto {
  sheetIndex: number;
  widthMm: number;
  heightMm: number;
  placements: CuttingPlacement2DDto[];
  wasteMm2: number;
  groupKey?: string | null;
}

export interface CuttingGroup2DDto {
  groupKey: string | null;
  totalSheets: number;
  totalUsedMm2: number;
  totalWasteMm2: number;
  utilizationPercent: number;
}

export interface CuttingResult2DDto {
  sheetWidthMm: number;
  sheetHeightMm: number;
  kerfMm: number;
  guillotineOnly: boolean;
  totalSheets: number;
  totalUsedMm2: number;
  totalWasteMm2: number;
  utilizationPercent: number;
  sheets: CuttingSheet2DDto[];
  unplaced: string[];
  groups?: CuttingGroup2DDto[];
}

export interface CuttingReportDto {
  projectId: string;
  generatedAtUtc: string;
  profile1D: CuttingResult1DDto;
  glass2D: CuttingResult2DDto;
}

export interface WindLoadPanelDto {
  runId: string;
  panelId: string;
  appliedPressurePa: number;
  currentThicknessMm: number;
  requiredMinThicknessMm: number;
  isSufficient: boolean;
  shortSpanMm: number;
  aspectRatio: number;
  maxBendingStressMPa: number;
  designStrengthMPa: number;
  stressUtilisation: number;
  maxDeflectionMm: number;
  deflectionLimitMm: number;
  deflectionUtilisation: number;
  governingLimit: string;
}

export interface WindLoadDto {
  basePressurePa: number;
  heightFactor: number;
  appliedPressurePa: number;
  panels: WindLoadPanelDto[];
  basicWindSpeedMs: number;
  designWindSpeedMs: number;
  referenceHeightM: number;
  roughnessFactor: number;
  meanWindSpeedMs: number;
  turbulenceIntensity: number;
  peakVelocityPressurePa: number;
  externalPressureCoefficient: number;
  internalPressureCoefficient: number;
  terrainCategory: string;
  standardReference: string;
}

export interface ThermalAcousticDto {
  totalAreaM2: number;
  weightedUValue: number;
  weightedSoundDb: number;
  estimatedWinterHeatLossKwh: number;
  estimatedDbReductionVsOpen: number;
}

export interface TechnicalSummaryDto {
  projectId: string;
  windLoad: WindLoadDto | null;
  thermal: ThermalAcousticDto;
  panelCount: number;
  runCount: number;
  totalAreaM2: number;
  totalWeightKg: number;
}

export interface Glass2DPlacedPanelDto {
  panelId: string;
  label: string;
  x: number;
  y: number;
  widthMm: number;
  heightMm: number;
  rotated: boolean;
  shape?: PanelCutShapeDto | null;
}

export interface Glass2DPlacedSheetDto {
  sheetId: string;
  sheetIndex: number;
  sheetWidthMm: number;
  sheetHeightMm: number;
  panels: Glass2DPlacedPanelDto[];
  usedAreaMm2: number;
  wasteAreaMm2: number;
  utilizationPercent: number;
  glassLabel: string;
}

export interface Glass2DUnplacedPanelDto {
  panelId: string;
  label: string;
  widthMm: number;
  heightMm: number;
  reason: string;
}

export interface Glass2DNestingReportDto {
  projectId: string;
  generatedAtUtc: string;
  algorithm: string;
  heuristic: string;
  sheetsUsed: number;
  totalUsedAreaMm2: number;
  totalWasteAreaMm2: number;
  totalUtilizationPercent: number;
  sheets: Glass2DPlacedSheetDto[];
  unplacedPanels: Glass2DUnplacedPanelDto[];
}

export interface Optimize2DNestingInput {
  algorithm?: string;
  heuristic?: string;
  minimizeSheets?: boolean;
  acceptableUtilization?: number;
  guillotineOnly?: boolean;
  allowRotation?: boolean;
}
