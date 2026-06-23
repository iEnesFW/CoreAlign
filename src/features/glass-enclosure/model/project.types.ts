import type { GlassOpeningType, GlassSystemType } from './glassEnclosure.types';

export type GlassProjectStatus =
  | 'Draft'
  | 'Surveyed'
  | 'Quoted'
  | 'Confirmed'
  | 'InProduction'
  | 'Ready'
  | 'InTransit'
  | 'Installed'
  | 'Defective'
  | 'Cancelled';

export type GlassValidationSeverity = 'Info' | 'Warning' | 'Error';

export interface GlassProjectPanelDto {
  id: string;
  runId: string;
  panelIndex: number;
  widthMm: number;
  openingType: GlassOpeningType;
  glassTypeId: string;
  hasHandle: boolean;
  hasLock: boolean;
  hasBrushSeal: boolean;
  notes: string | null;
  heightMm?: number | null;
  topShape?: PanelTopShape | null;
  topRightHeightMm?: number | null;
  archRiseMm?: number | null;
  cornerRadiiMm?: CornerRadiiMm | null;
  shapeKind?: PanelShapeKind | null;
  shapePointsJson?: string | null;
}

export interface GlassProjectRunDto {
  id: string;
  projectId: string;
  orderIndex: number;
  label: string;
  lengthMm: number;
  heightMm: number;
  originX: number;
  originY: number;
  rotationDeg: number;
  profileSystemId: string;
  colorId: string | null;
  hasTopDrip: boolean;
  hasBottomThreshold: boolean;
  geomZ?: number | null;
  geomTiltDeg?: number | null;
  geomArcRadiusMm?: number | null;
  geomArcSweepDeg?: number | null;
  arcGlassBent?: boolean | null;
  notes: string | null;
  panels: GlassProjectPanelDto[];
}

export interface PolygonVertex {
  xMm: number;
  yMm: number;
}

export interface RunConnectionDto {
  id: string;
  projectId: string;
  runAId: string;
  runBId: string;
  jointAngleDeg: number;
  mitreCutDeg: number;
  usesCornerPost: boolean;
  cornerProfileId: string | null;
}

export interface GlassProjectDto {
  id: string;
  code: string;
  customerId: string;
  customerName: string | null;
  projectName: string;
  siteAddressLine1: string | null;
  siteAddressLine2: string | null;
  siteCity: string | null;
  siteDistrict: string | null;
  sitePostalCode: string | null;
  siteCountryCode: string | null;
  status: GlassProjectStatus;
  createdByUserId: string;
  assignedDesignerUserId: string | null;
  assignedSalespersonUserId: string | null;
  floorNumber: number | null;
  buildingHeightM: number | null;
  windZoneId: string | null;
  climateZoneId: string | null;
  fireSafetyClass: string | null;
  scaffoldingRequired: boolean;
  craneRequired: boolean;
  totalAreaM2: number;
  totalPanels: number;
  subtotal: number;
  discountTotal: number;
  taxTotal: number;
  grandTotal: number;
  currency: string;
  fxRateToBase: number;
  fxRateLockedAtUtc: string | null;
  windLoadPaCalculated: number | null;
  weightedUValue: number | null;
  weightedSoundDb: number | null;
  validUntilDate: string | null;
  currentSceneVersion: number;
  notes: string | null;
  isBomStale: boolean;
  bomStaleReason: string | null;
  staleSinceUtc: string | null;
  enclosureCategory: EnclosureCategory | null;
  enclosureSubtype: EnclosureSubtype | null;
  geometryMode: GeometryMode | null;
  mountingTopology: MountingTopology | null;
  roofPitchDeg: number | null;
  ridgeHeightMm: number | null;
  eaveHeightMm: number | null;
  polygonVerticesJson: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  runs: GlassProjectRunDto[];
  connections: RunConnectionDto[];
}

export interface ConvertProjectToOrderInput {
  forceConvertWithShortage?: boolean;
  forceWithStaleBom?: boolean;
}

export interface GenerateShareTokenInput {
  overrideTtlDays?: number | null;
  forceWithStaleBom?: boolean;
}

export interface GlassProjectListItem {
  id: string;
  code: string;
  projectName: string;
  customerId: string;
  customerName: string | null;
  status: GlassProjectStatus;
  grandTotal: number;
  currency: string;
  totalPanels: number;
  totalAreaM2: number;
  updatedAtUtc: string;
}

export interface CreateGlassProjectInput {
  customerId: string;
  projectName: string;
  siteAddressLine1?: string | null;
  siteAddressLine2?: string | null;
  siteCity?: string | null;
  siteDistrict?: string | null;
  sitePostalCode?: string | null;
  siteCountryCode?: string | null;
  floorNumber?: number | null;
  buildingHeightM?: number | null;
  currency: string;
  validUntilDate?: string | null;
  notes?: string | null;
}

export interface UpdateGlassProjectHeaderInput {
  projectName: string;
  siteAddressLine1?: string | null;
  siteAddressLine2?: string | null;
  siteCity?: string | null;
  siteDistrict?: string | null;
  sitePostalCode?: string | null;
  siteCountryCode?: string | null;
  floorNumber?: number | null;
  buildingHeightM?: number | null;
  windZoneId?: string | null;
  climateZoneId?: string | null;
  fireSafetyClass?: string | null;
  scaffoldingRequired: boolean;
  craneRequired: boolean;
  validUntilDate?: string | null;
  notes?: string | null;
}

export interface AddRunInput {
  lengthMm: number;
  heightMm: number;
  profileSystemId: string;
  originX: number;
  originY: number;
  rotationDeg: number;
  label: string;
  colorId?: string | null;
  hasTopDrip: boolean;
  hasBottomThreshold: boolean;
  geomZ?: number | null;
  geomTiltDeg?: number | null;
  geomArcRadiusMm?: number | null;
  geomArcSweepDeg?: number | null;
  arcGlassBent?: boolean | null;
  notes?: string | null;
  panelCount?: number | null;
}

export type UpdateRunInput = AddRunInput;

export interface AddPanelInput {
  widthMm: number;
  openingType: GlassOpeningType;
  glassTypeId: string;
  hasHandle: boolean;
  hasLock: boolean;
  hasBrushSeal: boolean;
  notes?: string | null;
  heightMm?: number | null;
  topShape?: PanelTopShape | null;
  topRightHeightMm?: number | null;
  archRiseMm?: number | null;
  cornerRadiiMm?: CornerRadiiMm | null;
  shapeKind?: PanelShapeKind | null;
  shapePointsJson?: string | null;
}

export type UpdatePanelInput = AddPanelInput;

export interface BulkRebalancePanelsInput {
  panelCount: number;
  defaultOpeningType: GlassOpeningType;
  defaultGlassTypeId: string;
}

export interface AddRunConnectionInput {
  runAId: string;
  runBId: string;
  jointAngleDeg: number;
  mitreCutDeg: number;
  usesCornerPost: boolean;
  cornerProfileId?: string | null;
}

export interface UpdateRunConnectionInput {
  jointAngleDeg: number;
  mitreCutDeg: number;
  usesCornerPost: boolean;
  cornerProfileId?: string | null;
}

export interface SaveSceneInput {
  sceneJson: string;
  thumbnailDataUrl?: string | null;
  cameraStateJson?: string | null;
  label?: string | null;
}

export interface SceneVersionDto {
  id: string;
  version: number;
  label: string | null;
  thumbnailUrl: string | null;
  savedByUserId: string;
  savedAtUtc: string;
  isCustomerApproved: boolean;
}

export interface SceneLatestDto {
  version: number;
  sceneJson: string;
  cameraStateJson: string | null;
  thumbnailUrl: string | null;
  savedAtUtc: string;
}

export interface GlassValidationFindingDto {
  severity: GlassValidationSeverity;
  code: string;
  messageKey: string;
  messageArgs: string | null;
  affectedRunId: string | null;
  affectedPanelId: string | null;
}

export interface GlassProjectValidationResultDto {
  findings: GlassValidationFindingDto[];
}

export interface AssignProjectTeamInput {
  designerUserId: string | null;
  salespersonUserId: string | null;
}

export interface TransitionProjectStatusInput {
  targetStatus: GlassProjectStatus;
}

export interface ProjectsListParams {
  search?: string;
  status?: GlassProjectStatus;
  customerId?: string;
  assignedDesignerUserId?: string;
  assignedSalespersonUserId?: string;
  page: number;
  pageSize: number;
}

export interface SceneState {
  runs: SceneRunState[];
  connections: SceneConnectionState[];
  walls?: SceneWallState[];
  slabs?: SceneSlabState[];
  surfaces?: SceneSurfaceState[];
  camera: SceneCameraState | null;
  metadata: SceneMetadata;
}

export interface SceneSurfacePoint {
  x: number;
  y: number;
}

export interface SceneSurfaceState {
  id: string;
  kind: 'floor' | 'roof';
  points: SceneSurfacePoint[];
  elevationMm: number;
  thicknessMm: number;
  colorHex?: string | null;
  materialKey?: string | null;
  locked?: boolean;
}

export interface CornerRadiiMm {
  tl?: number;
  tr?: number;
  br?: number;
  bl?: number;
}

export interface SceneWallState {
  id: string;
  originX: number;
  originY: number;
  lengthMm: number;
  rotationDeg: number;
  heightMm: number;
  heightEndMm?: number | null;
  thicknessMm: number;
  geomZ?: number | null; // base elevation (mm) so a wall can rest on top of others
  geomArcRadiusMm?: number | null;
  geomArcSweepDeg?: number | null;
  colorHex?: string | null;
  materialKey?: string | null;
  groupId?: string | null;
  cornerRadiiMm?: CornerRadiiMm;
  openings?: SceneWallOpening[];
  features?: SceneWallFeature[];
  locked?: boolean;
}

export type WallFeatureShape = 'rect' | 'circle' | 'ellipse' | 'triangle' | 'polygon' | 'free';

export type WallFeatureMode = 'recess' | 'protrude' | 'hole';

export interface SceneWallFeaturePoint {
  x: number;
  z: number;
}

export interface SceneWallFeature {
  id: string;
  shape: WallFeatureShape;
  mode: WallFeatureMode;
  side: 1 | -1;
  offsetMm: number;
  centerZMm: number;
  widthMm: number;
  heightMm: number;
  depthMm: number;
  sides?: number;
  points?: SceneWallFeaturePoint[];
  colorHex?: string | null;
}

export interface SceneWallOpening {
  id: string;
  kind: 'window' | 'door';
  offsetMm: number;
  sillMm: number;
  widthMm: number;
  heightMm: number;
}

export type SceneHardwareKind =
  | 'Handle'
  | 'PullHandle'
  | 'Lock'
  | 'Hinge'
  | 'Roller'
  | 'Stopper'
  | 'CornerJoint'
  | 'GasketStrip'
  | 'DripProfile'
  | 'Vent'
  | 'Louver'
  | 'Bracket'
  | 'Accessory';

export interface SceneHardwareItem {
  id: string;
  kind: SceneHardwareKind;
  colorHex: string;
  offsetXmm: number;
  offsetYmm: number;
  offsetZmm: number;
  widthMm: number;
  heightMm: number;
  depthMm: number;
}

export interface SceneRunState {
  id: string;
  orderIndex: number;
  label: string;
  lengthMm: number;
  heightMm: number;
  originX: number;
  originY: number;
  rotationDeg: number;
  profileSystemId: string;
  colorId: string | null;
  hasTopDrip: boolean;
  hasBottomThreshold: boolean;
  geomZ?: number | null;
  geomArcRadiusMm?: number | null;
  geomArcSweepDeg?: number | null;
  arcGlassBent?: boolean;
  frameEdges?: RunFrameEdges | null;
  hasMullions?: boolean | null;
  locked?: boolean;
  panels: ScenePanelState[];
}

// Per-edge frame visibility for frameless / structural-glazing designs (e.g. only a
// bottom rail, glass top + sides; or no mullions, glasses joined with silicone).
// Persisted in the sceneJson blob (reconciled on load like arcGlassBent) — no migration.
export interface RunFrameEdges {
  top: boolean;
  bottom: boolean;
  left: boolean;
  right: boolean;
}

export interface SceneSlabState {
  id: string;
  kind: 'floor' | 'roof';
  originX: number;
  originY: number;
  rotationDeg: number;
  lengthMm: number;
  depthMm: number;
  thicknessMm: number;
  elevationMm: number;
  arcRiseMm?: number | null;
  colorHex?: string | null;
  materialKey?: string | null;
  cornerRadiiMm?: CornerRadiiMm;
  features?: SceneWallFeature[];
  locked?: boolean;
}

export type PanelTopShape = 'flat' | 'raked' | 'arched';

export type PanelShapeKind = 'ellipse' | 'polygon';

export interface ScenePanelState {
  id: string;
  panelIndex: number;
  widthMm: number;
  openingType: GlassOpeningType;
  glassTypeId: string;
  hasHandle: boolean;
  hasLock: boolean;
  hasBrushSeal: boolean;
  hardware: SceneHardwareItem[];
  heightMm?: number | null;
  topShape?: PanelTopShape | null;
  topRightHeightMm?: number | null;
  archRiseMm?: number | null;
  cornerRadiiMm?: CornerRadiiMm;
  shapeKind?: PanelShapeKind | null;
  shapePointsJson?: string | null;
}

export interface SceneConnectionState {
  id: string;
  runAId: string;
  runBId: string;
  jointAngleDeg: number;
  mitreCutDeg: number;
  usesCornerPost: boolean;
  cornerProfileId: string | null;
}

export interface SceneCameraState {
  position: [number, number, number];
  target: [number, number, number];
  zoom: number;
}

export interface SceneMetadata {
  schemaVersion: number;
  savedAt: string;
}

export type SystemTypeLabel = Record<GlassSystemType, string>;

export type EnclosureCategory = 'Vertical' | 'HorizontalOrPitched' | 'Functional' | 'Special';

export type EnclosureSubtype =
  | 'Balcony'
  | 'OfficePartition'
  | 'Window'
  | 'CurtainWall'
  | 'Greenhouse'
  | 'WinterGardenRoof'
  | 'Skylight'
  | 'Canopy'
  | 'ShowerCabin'
  | 'Balustrade'
  | 'FramelessDoor'
  | 'GlassFloor'
  | 'SpiderFacade'
  | 'CableTensioned'
  | 'ChannelRailing'
  | 'FreeForm';

export type GeometryMode = 'Planar' | 'Pitched' | 'Curved' | 'MultiLevel' | 'FreeForm';

export type MountingTopology =
  | 'ProfileFramed'
  | 'ChannelBase'
  | 'TopRail'
  | 'WallAnchored'
  | 'FloorAnchored'
  | 'RoofAnchored'
  | 'SelfSupporting'
  | 'CableTensioned'
  | 'SpiderArm'
  | 'PatchFitting';

export type ConnectorKind =
  | 'Profile'
  | 'CornerProfile'
  | 'GlassToGlassPolish'
  | 'GlassClamp'
  | 'SpiderFitting'
  | 'PatchFitting'
  | 'StructuralSilicone'
  | 'ButtJointSilicone'
  | 'HShapeProfile'
  | 'UChannel'
  | 'Adhesive';

export interface EnclosurePresetDto {
  subtype: string;
  category: string;
  defaultGeometryMode: string;
  defaultMountingTopology: string;
  defaultConnectorKind: string;
  defaultPanelWidthMm: number | null;
  defaultPanelHeightMm: number | null;
  defaultPanelCount: number | null;
  defaultRoofPitchDeg: number | null;
  notes: string | null;
}

export interface ConfigureEnclosureInput {
  category: EnclosureCategory;
  subtype: EnclosureSubtype;
  geometryMode?: GeometryMode | null;
  mountingTopology?: MountingTopology | null;
  roofPitchDeg?: number | null;
  ridgeHeightMm?: number | null;
  eaveHeightMm?: number | null;
  curtainWallCassetteSpecJson?: string | null;
  polygonVerticesJson?: string | null;
  metadataJson?: string | null;
}

export type GlassOpeningTypeName = 'Fixed' | 'Folding' | 'SlidingLeft' | 'SlidingRight';

export interface ProjectTemplateRunPresetDto {
  id: string;
  orderIndex: number;
  labelKey: string;
  lengthMm: number;
  heightMm: number;
  originX: number;
  originY: number;
  rotationDeg: number;
  defaultPanelCount: number;
  defaultPanelWidthMm: number;
  defaultOpeningType: GlassOpeningTypeName;
  hasTopDrip: boolean;
  hasBottomThreshold: boolean;
  connectsToPreviousAsCorner: boolean;
  cornerJointAngleDeg: number | null;
  cornerUsesPost: boolean;
}

export interface ProjectTemplateSummaryDto {
  id: string;
  code: string;
  displayNameKey: string;
  category: EnclosureCategory;
  subtype: EnclosureSubtype;
  geometryMode: GeometryMode;
  mountingTopology: MountingTopology;
  defaultConnectorKind: ConnectorKind;
  roofPitchDeg: number | null;
  thumbnailUrl: string | null;
  descriptionKey: string | null;
  runPresetCount: number;
  isSystemTemplate: boolean;
  isActive: boolean;
  sortOrder: number;
}

export interface ProjectTemplateDetailDto {
  id: string;
  code: string;
  displayNameKey: string;
  category: EnclosureCategory;
  subtype: EnclosureSubtype;
  geometryMode: GeometryMode;
  mountingTopology: MountingTopology;
  defaultConnectorKind: ConnectorKind;
  roofPitchDeg: number | null;
  ridgeHeightMm: number | null;
  eaveHeightMm: number | null;
  thumbnailUrl: string | null;
  descriptionKey: string | null;
  metadataJson: string | null;
  isSystemTemplate: boolean;
  isActive: boolean;
  sortOrder: number;
  runPresets: ProjectTemplateRunPresetDto[];
}

export interface CreateProjectFromTemplateInput {
  templateId: string;
  customerId: string;
  projectName: string;
  currency?: string | null;
}
