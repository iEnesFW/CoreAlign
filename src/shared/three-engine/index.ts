export { SceneViewport, type ViewportAppearance } from './SceneViewport';
export {
  viewportCamera,
  isViewportCameraReady,
  DESIGNER_ROOT_NAME,
  ZOOM_STEP,
  type ViewportCameraApi,
} from './viewportCamera';
export { ensureBoundsTree } from './acceleratedRaycast';
export { Bar } from './primitives/Bar';
export { useSurfaceMaterial, type SurfaceMaterialParams } from './materials/useSurfaceMaterial';
export {
  usePhysicalGlassMaterial,
  type PhysicalGlassParams,
} from './materials/usePhysicalGlassMaterial';
export {
  QUALITY_SETTINGS,
  type QualityPreset,
  type QualitySettings,
} from './quality/qualityPreset';
export {
  getProceduralTexture,
  isProceduralMaterialKey,
  PROCEDURAL_MATERIAL_KEYS,
  type ProceduralMaterialKey,
} from './materials/proceduralTextures';
export { useTiledProceduralTexture } from './materials/useTiledProceduralTexture';
export {
  useDrag3D,
  type DragConstraint,
  type DragDeltaMm,
  type Drag3D,
  type Drag3DHandlers,
  type UseDrag3DOptions,
} from './interaction/useDrag3D';
export {
  applyPlanMoveSnap,
  lineProbePoints,
  snapDimensionMm,
  stickyDimensionMm,
  filterSnapTargets,
  EMPTY_SNAP_TARGETS,
  type PlanPoint,
  type PlanSnapPoint,
  type PlanSnapSegment,
  type PlanSnapTargets,
  type PlanMoveDelta,
  type PlanSnapGuide,
  type PlanSnapGuideKind,
  type PlanSnapResult,
} from './interaction/planSnap';
export {
  buildPlanFootprint,
  buildPolygonFootprint,
  restElevationMm,
  supportTopBelowMm,
  liftToClearMm,
  SUPPORT_TOLERANCE_MM,
  WALKABLE_STEP_UP_MM,
  restsOnSupportAtMm,
  isFloating,
  normalizePlanAngleDeg,
  penetratesAny,
  firstPenetratingOwner,
  footprintsPenetrate,
  footprintsOverlapXY,
  clampPlanMove,
  clampPlanMoveNoDeepen,
  slidePlanMove,
  clampPlanStretch,
  clampPlanRotation,
  type PlanFootprint,
  type PlanFootprintSet,
} from './interaction/planCollision';
export { rotatePlanPointDeg, type PlanPosition } from './interaction/planTransform';
export { registerSceneRef, getSceneRef } from './interaction/sceneRefs';
export { setBodyPreview } from './interaction/bodyPreview';
export {
  useObjectGestures,
  type PlanGestureAdapter,
  type PlanRotationCommit,
  type ObjectGestures,
  type ObjectGestureMode,
  type UseObjectGesturesOptions,
} from './interaction/useObjectGestures';
export { StretchFaces, type StretchFaceDef } from './interaction/StretchFaces';
export { setSnapGuides, clearSnapGuides, subscribeSnapGuides } from './interaction/snapGuides';
export { setDragReadout, getDragReadout, subscribeDragReadout } from './interaction/dragReadout';
export {
  trackModifierKeys,
  isCtrlPressed,
  isShiftPressed,
  isAltPressed,
} from './interaction/modifierKeys';
export { SnapGuideOverlay } from './interaction/SnapGuideOverlay';
export { snapAngleDeg } from './interaction/angleSnap';
