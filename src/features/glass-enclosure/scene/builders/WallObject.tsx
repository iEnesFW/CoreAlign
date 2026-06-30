import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { Edges, Line } from '@react-three/drei';
import { useThree } from '@react-three/fiber';
import { useTranslation } from 'react-i18next';
import {
  DoubleSide,
  ExtrudeGeometry,
  Matrix4,
  Path,
  Quaternion,
  ShapeGeometry,
  Vector3,
} from 'three';
import { filletedShapeMm, outlineToPath, outlineToShape } from './surfaceFeatureShapes';
import { hasEdgeNotch, hasWallNotch, wallProfileOutlineMm } from '../../model/wallOutline';
import { buildCurvedBandGeometry, curvedWallPickUv } from './curvedExtrude';
import { buildBentWallGeometry } from './bentWallGeometry';
import type { ThreeEvent } from '@react-three/fiber';
import type { BufferGeometry, Group, Mesh, Texture } from 'three';
import {
  isShiftPressed,
  setDragReadout,
  stickyDimensionMm,
  useDrag3D,
  useTiledProceduralTexture,
} from '@/shared/three-engine';
import { queueToast } from '@/shared/api/toastQueue';
import { useObjectGestures } from '../interaction/useObjectGestures';
import { StretchFaces } from '../interaction/StretchFaces';
import { FootprintCornerHandles } from '../interaction/FootprintCornerHandles';
import { CurveBowHandle } from '../interaction/CurveBowHandle';
import { BendHandle } from '../interaction/BendHandle';
import { WallOpeningFrames } from './WallOpeningFrames';
import { setBodyPreview } from '../interaction/bodyPreview';
import { registerSceneRef } from '../interaction/sceneRefs';
import { captureMultiSnapshots, multiSelectionHas } from '../interaction/multiMove';
import { collectHeightLevels, snapToLevels } from '../interaction/levelSnap';
import { arcMetricsFromBulge, chordBulgeMm, tessellateArc } from '../interaction/penArc';
import {
  captureRunSnapshots,
  previewSnapshotsMove,
  previewSnapshotsRotation,
} from '../interaction/attachedRunPreview';
import { EMPTY_SNAP_TARGETS, filterSnapTargets, lineProbePoints } from '../interaction/planSnap';
import {
  buildRunFootprint,
  buildWallFootprint,
  clampPlanStretch,
  penetratesAny,
  restElevationAtPointMm,
  restElevationMm,
} from '../interaction/planCollision';
import { useDesignerStore } from '../../model/designerStore';
import { featureSideSignZ } from '../../model/project.types';
import {
  applyCurvedWallFeatures,
  applyWallFaceFeatures,
  buildFaceFeatureGeometry,
  normalizeWallSide,
  sideFromLocalNormal,
  wallFaceFrame,
  type WallBoxDims,
  type WallFeatureSide,
} from './wallFaces';
import {
  arcEndLocal,
  arcFromChordKeepingSweep,
  bowFromArc,
  bowToArcKeepingLength,
  resolveArc,
} from '../../model/arcGeometry';
import { findAttachedRunIds } from '../../model/wallAttachment';
import {
  FEATURE_EDGE_MARGIN_MM,
  FREE_SAMPLE_STEP_MM,
  FREE_SIMPLIFY_TOLERANCE_MM,
  MIN_FEATURE_SIZE_MM,
  composeSurfaceFeatures,
  featureFitsWall,
  featureOutlineMm,
  formatDraftDimensionMm,
  outlineBoundsMm,
  shrinkOutlineMm,
  simplifyFreePoints,
  wallHeightAtMm,
} from '../../model/wallFeatureGeometry';
import type { AttachedRunSnapshot } from '../interaction/attachedRunPreview';
import type { PlanGestureAdapter, PlanRotationCommit } from '../interaction/useObjectGestures';
import type { StretchFaceDef } from '../interaction/StretchFaces';
import type { PlanMoveDelta, PlanPoint, PlanSnapTargets } from '../interaction/planSnap';
import type { PlanFootprint } from '../interaction/planCollision';
import type { ComposedFeature, FeatureOutlineSpec } from '../../model/wallFeatureGeometry';
import type {
  SceneWallFeature,
  SceneWallFeaturePoint,
  SceneWallOpening,
  SceneWallState,
} from '../../model/project.types';

interface WallObjectProps {
  wall: SceneWallState;
  isSelected: boolean;
  onSelect: (wallId: string) => void;
  snapTargets?: PlanSnapTargets;
  obstacles?: PlanFootprint[];
  supports?: PlanFootprint[];
  interactive?: boolean;
  onCommitMove?: (
    wallId: string,
    delta: PlanMoveDelta,
    attachedRunIds: string[],
    groupWallIds: string[],
  ) => void;
  onStackWall?: (wallId: string, delta: PlanMoveDelta, geomZMm: number) => void;
  onCommitRotate?: (
    wallId: string,
    commit: PlanRotationCommit,
    attachedRunIds: string[],
    groupWallIds: string[],
  ) => void;
  penActive?: boolean;
  onPenFaceClick?: (
    hostKind: 'wall' | 'slab',
    hostId: string,
    side: WallFeatureSide,
    pt: { x: number; z: number },
  ) => void;
  onPenFaceArc?: (
    hostKind: 'wall' | 'slab',
    hostId: string,
    side: WallFeatureSide,
    pts: { x: number; z: number }[],
  ) => void;
  onPenFaceFinish?: () => void;
}

const WALL_COLOR = '#94a3b8';
const WALL_SELECTED = '#1d4ed8';
const WALL_EDGE = '#cbd5e1';
const FEATURE_SELECTED = '#1d4ed8';
const REGION_COLOR = '#2563eb';
const MIN_LENGTH_MM = 100;
const MIN_HEIGHT_MM = 100;
const MIN_THICKNESS_MM = 50;
const SIDE_MARGIN_M = 0.005;
const BOTTOM_MARGIN_M = 0.001;
const TOP_MARGIN_M = 0.01;
const MIN_HOLE_M = 0.02;
const DEG2RAD = Math.PI / 180;
const HALF_PI = Math.PI / 2;
const FACE_LIFT_M = 0.002;
const FEATURE_FACE_LIFT_M = 0.004;
const FACE_HIT_SIZE_M = 0.12;
const FEATURE_GAP_MM = 50;
const PLUG_INSET_MM = 1;
const MIN_PLUG_DEPTH_M = 0.003;
const HOLE_THRESHOLD_MM = 5;
const SPLIT_PREVIEW_WIDTH_M = 0.006;
const SPLIT_COLOR = '#dc2626';
const DRAW_PREVIEW_OPACITY = 0.35;
const EMPTY_OBSTACLES: PlanFootprint[] = [];
const TMP_VEC = new Vector3();

interface DraftFeature extends FeatureOutlineSpec {
  side: WallFeatureSide;
}

interface WallFeatureItem extends ComposedFeature {
  // Front/back plugs + protrusions are ExtrudeGeometry; side-face protrusions are the
  // face-oriented BufferGeometry from buildFaceFeatureGeometry — both render as a plain mesh.
  geometry: BufferGeometry | null;
}

export interface OpeningFrameRect {
  x0: number;
  x1: number;
  y0: number;
  y1: number;
  hasSill: boolean;
}

const clampedOpeningRectM = (
  opening: SceneWallOpening,
  lengthM: number,
  heightStartM: number,
  heightEndM: number,
): OpeningFrameRect | null => {
  const halfW = opening.widthMm / 2000;
  const centerX = opening.offsetMm / 1000;
  const x0 = Math.max(SIDE_MARGIN_M, centerX - halfW);
  const x1 = Math.min(lengthM - SIDE_MARGIN_M, centerX + halfW);
  if (x1 - x0 < MIN_HOLE_M) return null;
  const slope = lengthM > 0 ? (heightEndM - heightStartM) / lengthM : 0;
  const topLimit = Math.min(heightStartM + slope * x0, heightStartM + slope * x1) - TOP_MARGIN_M;
  const y0 = Math.max(BOTTOM_MARGIN_M, opening.sillMm / 1000);
  const y1 = Math.min(topLimit, (opening.sillMm + opening.heightMm) / 1000);
  if (y1 - y0 < MIN_HOLE_M) return null;
  return { x0, x1, y0, y1, hasSill: opening.sillMm > 0 };
};

const buildOpeningPath = (rect: OpeningFrameRect): Path => {
  const path = new Path();
  path.moveTo(rect.x0, rect.y0);
  path.lineTo(rect.x1, rect.y0);
  path.lineTo(rect.x1, rect.y1);
  path.lineTo(rect.x0, rect.y1);
  path.closePath();
  return path;
};

const buildWallGeometries = (
  wall: SceneWallState,
  cutFeatures = true,
): {
  body: BufferGeometry;
  featureItems: WallFeatureItem[];
  openingFrames: OpeningFrameRect[];
} => {
  const thicknessMm = wall.thicknessMm;
  const thicknessM = thicknessMm / 1000;

  // L-shaped (bent) wall: a single mitred solid. Like the curved wall it is a non-flat body, so
  // openings/features are deferred (#6c); it reuses the straight render path (no mesh rotation).
  if (wall.bendAngleDeg && Math.abs(wall.bendAngleDeg) >= 1) {
    const body = buildBentWallGeometry({
      lengthMm: wall.lengthMm,
      bendAtMm: wall.bendAtMm ?? wall.lengthMm / 2,
      bendAngleDeg: wall.bendAngleDeg,
      thicknessMm,
      heightMm: wall.heightMm,
    });
    return { body, featureItems: [], openingFrames: [] };
  }

  // Curved (arc-in-plan) wall: a single annular band. Front/back holes & recesses are now carved
  // into the band via a curved CSG cutter (applyCurvedWallFeatures); protrusions and side-face
  // features on a curved wall are a follow-up.
  if (wall.geomArcRadiusMm && wall.geomArcRadiusMm > 0) {
    // ARC-LENGTH-INVARIANT: wall.lengthMm is the developed glass length (fixed); the sweep is
    // derived (= arcLength/radius) and the band spans whatever chord that arc gives.
    const resolved = resolveArc(
      wall.lengthMm,
      wall.geomArcRadiusMm ?? 0,
      wall.geomArcSweepDeg ?? 1,
    );
    const band = buildCurvedBandGeometry(
      resolved.radiusM,
      resolved.direction,
      0,
      resolved.sweepRad,
      thicknessM,
      wall.heightMm / 1000,
    );
    const body = cutFeatures
      ? applyCurvedWallFeatures(band, wall.features ?? [], {
          // Features map along the DEVELOPED arc length, not the chord.
          lengthMm: resolved.arcLengthMm,
          radiusM: resolved.radiusM,
          direction: resolved.direction,
          sweep: resolved.sweepRad,
          thicknessM,
        })
      : band;
    if (body !== band) band.dispose();
    return { body, featureItems: [], openingFrames: [] };
  }

  const lengthM = wall.lengthMm / 1000;
  const heightStartM = wall.heightMm / 1000;
  const heightEndM = (wall.heightEndMm ?? wall.heightMm) / 1000;
  const radii = wall.cornerRadiiMm ?? {};
  // Per-corner and per-edge rectangular indentations modify the face outline directly — a real
  // boundary cut (visible from the front/back face and the edge face), not a hole. Notches and
  // fillet radii don't combine, so a notched wall uses sharp corners; plain walls keep the
  // filleted-rectangle path.
  const shape =
    hasWallNotch(wall.cornerNotchMm) || hasEdgeNotch(wall.edgeNotchMm)
      ? outlineToShape(
          wallProfileOutlineMm(
            wall.lengthMm,
            wall.heightMm,
            wall.heightEndMm ?? wall.heightMm,
            wall.cornerNotchMm,
            wall.edgeNotchMm,
          ),
        )
      : filletedShapeMm(
          [
            { x: 0, z: 0 },
            { x: wall.lengthMm, z: 0 },
            { x: wall.lengthMm, z: wall.heightEndMm ?? wall.heightMm },
            { x: 0, z: wall.heightMm },
          ],
          [radii.bl ?? 0, radii.br ?? 0, radii.tr ?? 0, radii.tl ?? 0],
        );
  const openingBounds = [];
  const openingFrames: OpeningFrameRect[] = [];
  const sorted = [...(wall.openings ?? [])].sort((a, b) => a.offsetMm - b.offsetMm);
  let lastRightMm = Number.NEGATIVE_INFINITY;
  for (const opening of sorted) {
    const leftMm = opening.offsetMm - opening.widthMm / 2;
    if (leftMm < lastRightMm + FEATURE_GAP_MM) continue;
    const rect = clampedOpeningRectM(opening, lengthM, heightStartM, heightEndM);
    if (!rect) continue;
    shape.holes.push(buildOpeningPath(rect));
    lastRightMm = opening.offsetMm + opening.widthMm / 2;
    openingBounds.push({
      minX: leftMm,
      maxX: lastRightMm,
      minZ: opening.sillMm,
      maxZ: opening.sillMm + opening.heightMm,
    });
    openingFrames.push(rect);
  }
  // Front/back features keep the extrude-with-holes + plug path; the four side faces are cut
  // into the body via CSG below (a single extrude can't carve a localised hole on a side face).
  const allFeatures = wall.features ?? [];
  const frontBackFeatures = allFeatures.filter((f) => f.side === 1 || f.side === -1);
  const sideFaceFeatures = allFeatures.filter(
    (f) => f.side === 'top' || f.side === 'bottom' || f.side === 'left' || f.side === 'right',
  );
  const composed = composeSurfaceFeatures(
    frontBackFeatures,
    (outline) => featureFitsWall(wall, outline),
    openingBounds,
    thicknessMm,
  );
  const featureItems: WallFeatureItem[] = [];
  for (const baseItem of composed) {
    const item =
      cutFeatures || baseItem.kind === 'protrude'
        ? baseItem
        : { ...baseItem, kind: 'outline' as const, cut: false };
    if (item.cut) shape.holes.push(outlineToPath(item.outline));
    let geometry: ExtrudeGeometry | null = null;
    if (item.kind === 'plug') {
      const plugDepthM = Math.max(MIN_PLUG_DEPTH_M, (thicknessMm - item.feature.depthMm) / 1000);
      geometry = new ExtrudeGeometry(outlineToShape(shrinkOutlineMm(item.outline, PLUG_INSET_MM)), {
        depth: plugDepthM,
        bevelEnabled: false,
      });
      const z0 =
        item.feature.side === 1 ? -thicknessM / 2 : -thicknessM / 2 + item.feature.depthMm / 1000;
      geometry.translate(0, 0, z0);
    } else if (item.kind === 'protrude') {
      const depthM = Math.max(0.002, item.feature.depthMm / 1000);
      geometry = new ExtrudeGeometry(outlineToShape(item.outline), {
        depth: depthM,
        bevelEnabled: false,
      });
      const z0 = item.feature.side === 1 ? thicknessM / 2 : -thicknessM / 2 - depthM;
      geometry.translate(0, 0, z0);
    }
    featureItems.push({ ...item, geometry });
  }
  // Side-face features (top/bottom/left/right): holes/recesses are carved into the body via CSG
  // below; a PROTRUSION is rendered as an additive extruded solid here (NOT a CSG union — three-csg-ts
  // degenerates a side-face union into a sliver and collapses the whole wall, #5), the same approach
  // front/back protrusions use. All are surfaced as feature items so they stay selectable (#5).
  const sideDims: WallBoxDims = {
    lengthM,
    heightM: Math.max(heightStartM, heightEndM),
    thicknessM,
  };
  for (const f of sideFaceFeatures) {
    const sideOutline = featureOutlineMm(f);
    if (sideOutline.length < 3) continue;
    const sideGeometry =
      f.mode === 'protrude'
        ? buildFaceFeatureGeometry(
            sideOutline,
            wallFaceFrame(normalizeWallSide(f.side), sideDims),
            Math.max(0.002, f.depthMm / 1000),
            true,
          )
        : null;
    featureItems.push({
      feature: f,
      outline: sideOutline,
      bounds: outlineBoundsMm(sideOutline),
      kind: f.mode === 'protrude' ? 'protrude' : 'outline',
      cut: false,
      geometry: sideGeometry,
    });
  }
  const body = new ExtrudeGeometry(shape, { depth: thicknessM, bevelEnabled: false });
  body.translate(0, 0, -thicknessM / 2);
  // Only holes/recesses are CSG-carved; protrusions are additive meshes (above).
  const carvedSideFeatures = sideFaceFeatures.filter((f) => f.mode !== 'protrude');
  if (cutFeatures && carvedSideFeatures.length > 0) {
    const finalBody = applyWallFaceFeatures(
      body,
      carvedSideFeatures.map((f) => ({
        outlineMm: featureOutlineMm(f),
        side: normalizeWallSide(f.side),
        mode: f.mode === 'hole' ? 'hole' : 'recess',
        depthMm: f.depthMm,
      })),
      sideDims,
    );
    if (finalBody !== body) body.dispose();
    return { body: finalBody, featureItems, openingFrames };
  }
  return { body, featureItems, openingFrames };
};

const clampValue = (value: number, min: number, max: number) => Math.min(max, Math.max(min, value));

export function WallObject({
  wall,
  isSelected,
  onSelect,
  snapTargets,
  obstacles,
  supports,
  interactive = true,
  onCommitMove,
  onStackWall,
  onCommitRotate,
  penActive = false,
  onPenFaceClick,
  onPenFaceArc,
  onPenFaceFinish,
}: WallObjectProps) {
  const { t } = useTranslation();
  const activeTool = useDesignerStore((s) => s.activeTool);
  const transformActive = useDesignerStore((s) => s.transformHandlesActive);
  const quality = useDesignerStore((s) => s.quality);
  const penFace = useDesignerStore((s) => s.penFace);
  const setPenFaceCursor = useDesignerStore((s) => s.setPenFaceCursor);
  const drawShape = useDesignerStore((s) => s.drawShape);
  const presentation = useDesignerStore((s) => s.presentationMode);
  const fullScene = useDesignerStore((s) => s.scene);
  const sceneRuns = useDesignerStore((s) => s.scene.runs);
  const sceneWalls = useDesignerStore((s) => s.scene.walls ?? []);
  const updateWall = useDesignerStore((s) => s.updateWall);
  const paintColor = useDesignerStore((s) => s.paintColor);
  const paintMaterial = useDesignerStore((s) => s.paintMaterial);
  const addWallFeature = useDesignerStore((s) => s.addWallFeature);
  const updateWallFeature = useDesignerStore((s) => s.updateWallFeature);
  const splitWall = useDesignerStore((s) => s.splitWall);
  const setSelection = useDesignerStore((s) => s.setSelection);

  const isArcWall = Boolean(wall.geomArcRadiusMm && wall.geomArcRadiusMm > 0);
  // An L-shaped (bent) wall is a single mitred solid; its footprint resize / curve handles don't
  // apply and it can't carry surface features yet (#6c).
  const isBentWall = Boolean(wall.bendAngleDeg && Math.abs(wall.bendAngleDeg) >= 1);
  // The 's' tool's resize faces scale the body imperatively — that lies on a curved band — so the
  // stretch tool stays off for arc/bent walls. But the Q CORNER points commit an absolute footprint,
  // so an ARC wall now keeps them: dragging resizes the CHORD (lengthMm) and the arc overlay
  // re-derives its radius. Only a bent wall hides the corner points.
  const stretchActive =
    activeTool === 'stretch' && interactive && !wall.locked && !isArcWall && !isBentWall;
  const vertexEditActive =
    transformActive && isSelected && interactive && !wall.locked && !isBentWall;
  // The curve (bow) handle stays available on an ALREADY-curved wall too, so the bow can be
  // re-adjusted. A bent wall has no bow (bend and arc are mutually exclusive profiles).
  const curveEditActive =
    transformActive && isSelected && interactive && !wall.locked && !isBentWall;
  // The bend handle initiates/re-adjusts an L on a straight or already-bent wall (never an arc wall).
  const bendEditActive = transformActive && isSelected && interactive && !wall.locked && !isArcWall;
  const wallCurveChord = (() => {
    const r = wall.rotationDeg * DEG2RAD;
    if (isArcWall) {
      const resolved = resolveArc(
        wall.lengthMm,
        wall.geomArcRadiusMm ?? 0,
        wall.geomArcSweepDeg ?? 1,
      );
      const ae = arcEndLocal(resolved.arcLengthMm, resolved.radiusMm, wall.geomArcSweepDeg ?? 1);
      const ex = wall.originX + ae.xMm * Math.cos(r) - ae.yMm * Math.sin(r);
      const ey = wall.originY + ae.xMm * Math.sin(r) + ae.yMm * Math.cos(r);
      const chordMm = Math.hypot(ex - wall.originX, ey - wall.originY);
      return {
        endX: ex,
        endY: ey,
        sagittaMm: bowFromArc(chordMm, resolved.radiusMm, wall.geomArcSweepDeg ?? 1),
      };
    }
    return {
      endX: wall.originX + wall.lengthMm * Math.cos(r),
      endY: wall.originY + wall.lengthMm * Math.sin(r),
      sagittaMm: 0,
    };
  })();
  // WHY: always cut the features (don't suppress while stretching) — the depth handle that
  // creates a recess/hole/protrusion lives in the Stretch tool, and suppressing the cut there
  // hid the result on every face until the user happened to leave the tool. Depth commits on
  // release, so the CSG/extrude rebuilds once per edit, not per frame.
  const {
    body: geometry,
    featureItems,
    openingFrames,
  } = useMemo(() => buildWallGeometries(wall, true), [wall]);
  useEffect(
    () => () => {
      geometry.dispose();
      for (const item of featureItems) item.geometry?.dispose();
    },
    [geometry, featureItems],
  );

  const lengthM = wall.lengthMm / 1000;
  const heightStartM = wall.heightMm / 1000;
  const heightEndM = (wall.heightEndMm ?? wall.heightMm) / 1000;
  const thicknessM = wall.thicknessMm / 1000;

  const groupRef = useRef<Group>(null);
  const bodyRef = useRef<Group>(null);
  const drawAnchorRef = useRef<Group>(null);
  const splitHoverRef = useRef<Mesh>(null);
  const attachedRef = useRef<AttachedRunSnapshot[]>([]);
  const drawSessionRef = useRef<{
    x0: number;
    z0: number;
    side: WallFeatureSide;
    points: SceneWallFeaturePoint[];
  } | null>(null);
  const drawFaceRef = useRef<WallFeatureSide>('front');
  const draftRef = useRef<DraftFeature | null>(null);
  const [draft, setDraftState] = useState<DraftFeature | null>(null);
  const planObstacles = obstacles ?? EMPTY_OBSTACLES;

  const setDraft = (value: DraftFeature | null) => {
    draftRef.current = value;
    setDraftState(value);
  };

  const filteredTargets = useMemo<PlanSnapTargets>(
    () => (snapTargets ? filterSnapTargets(snapTargets, wall.id) : EMPTY_SNAP_TARGETS),
    [snapTargets, wall.id],
  );

  const rad = wall.rotationDeg * DEG2RAD;
  const dirX = Math.cos(rad);
  const dirY = Math.sin(rad);
  const normalX = -dirY;
  const normalY = dirX;
  const centerXMm = wall.originX + (wall.lengthMm / 2) * dirX;
  const centerYMm = wall.originY + (wall.lengthMm / 2) * dirY;

  const moveProbes: PlanPoint[] = lineProbePoints(
    wall.originX,
    wall.originY,
    wall.lengthMm,
    wall.rotationDeg,
    wall.thicknessMm / 2,
  );

  const coMove = useMemo(() => {
    const groupWalls = wall.groupId
      ? sceneWalls.filter((w) => w.groupId === wall.groupId && w.id !== wall.id)
      : [];
    const runIds = new Set<string>(findAttachedRunIds(wall, sceneRuns));
    for (const member of groupWalls) {
      for (const id of findAttachedRunIds(member, sceneRuns)) runIds.add(id);
    }
    const runs = sceneRuns.filter((r) => runIds.has(r.id));
    return { groupWalls, runs };
  }, [wall, sceneWalls, sceneRuns]);

  const gestureObstacles = useMemo(() => {
    const movingIds = new Set<string>([
      wall.id,
      ...coMove.groupWalls.map((w) => w.id),
      ...coMove.runs.map((r) => r.id),
    ]);
    return planObstacles.filter((o) => !movingIds.has(o.ownerId));
  }, [planObstacles, wall.id, coMove]);

  // Default drag is lateral: the wall (plus any grouped walls / attached runs it
  // carries) collides side-to-side so it can butt flush against a neighbour. Holding
  // Alt instead rests a bare wall on top of whatever it overlaps. Only a wall that
  // carries no group/runs is Alt-stackable; ground (0) fallback prevents self-ratchet.
  const baseWallElevMm = wall.geomZ ?? 0;
  const stackSupports = useMemo(
    () => (supports ?? EMPTY_OBSTACLES).filter((o) => o.ownerId !== wall.id),
    [supports, wall.id],
  );
  const canStack =
    Boolean(onStackWall) && coMove.groupWalls.length === 0 && coMove.runs.length === 0;
  const restElevAt = (dxMm: number, dyMm: number) =>
    restElevationMm(
      buildWallFootprint(wall, dxMm, dyMm, wall.rotationDeg),
      stackSupports,
      baseWallElevMm,
    );
  // Fallback 0 (ground): a support under the centre lifts it; nothing under means gravity → floor.
  const centerRestAt = (dxMm: number, dyMm: number) =>
    restElevationAtPointMm(centerXMm + dxMm, centerYMm + dyMm, stackSupports, 0);
  const restingAtStart = Math.abs(centerRestAt(0, 0) - baseWallElevMm) < 5;

  const adapter: PlanGestureAdapter = {
    originXMm: wall.originX,
    originYMm: wall.originY,
    rotationDeg: wall.rotationDeg,
    baseYM: baseWallElevMm / 1000,
    centerXMm,
    centerYMm,
    moveProbes,
    footprintAt: (dxMm, dyMm, rotationDeg) => {
      const own = buildWallFootprint(wall, dxMm, dyMm, rotationDeg);
      if (rotationDeg !== wall.rotationDeg) return own;
      return [
        own,
        ...coMove.groupWalls.map((w) => buildWallFootprint(w, dxMm, dyMm, w.rotationDeg)),
        ...coMove.runs.map((r) => buildRunFootprint(r, dxMm, dyMm, r.rotationDeg)),
      ];
    },
    altLiftYMAt: canStack ? (dxMm, dyMm) => restElevAt(dxMm, dyMm) / 1000 : undefined,
    centerLiftYMAt: canStack ? (dxMm, dyMm) => centerRestAt(dxMm, dyMm) / 1000 : undefined,
    restingAtStart,
  };

  const gestures = useObjectGestures({
    adapter,
    groupRef,
    enabled: interactive && !wall.locked && Boolean(onCommitMove && onCommitRotate),
    selectedForDrag: isSelected && !wall.locked,
    snapTargets: filteredTargets,
    obstacles: gestureObstacles,
    onPick: () => onSelect(wall.id),
    onGestureStart: () => {
      const multi = useDesignerStore.getState().multiSelection;
      const multiSiblings = multiSelectionHas(multi, 'wall', wall.id)
        ? captureMultiSnapshots(useDesignerStore.getState().scene, multi, {
            kind: 'wall',
            id: wall.id,
          })
        : [];
      attachedRef.current = [
        ...captureRunSnapshots(
          sceneRuns,
          coMove.runs.map((r) => r.id),
        ),
        ...coMove.groupWalls.map((w) => ({
          runId: w.id,
          originXMm: w.originX,
          originYMm: w.originY,
          rotationDeg: w.rotationDeg,
          baseYM: 0,
        })),
        ...multiSiblings,
      ];
    },
    onMovePreview: (delta) => previewSnapshotsMove(attachedRef.current, delta.dxMm, delta.dyMm),
    onRotatePreview: (sweepDeg) =>
      previewSnapshotsRotation(attachedRef.current, centerXMm, centerYMm, sweepDeg),
    onMoveCommit: (delta, meta) => {
      // A standalone bare wall (canStack ⇒ no group / attached runs) that is stacked (explicit or
      // precise centre-over) rests at stackElevMm; a plain lateral drag keeps its elevation. A wall
      // carrying a group / runs always moves laterally via onCommitMove.
      if (canStack && onStackWall && meta.stackElevMm !== null) {
        onStackWall(wall.id, delta, meta.stackElevMm);
        return;
      }
      onCommitMove?.(
        wall.id,
        delta,
        coMove.runs.map((r) => r.id),
        coMove.groupWalls.map((w) => w.id),
      );
    },
    onRotateCommit: (commit) =>
      onCommitRotate?.(
        wall.id,
        commit,
        coMove.runs.map((r) => r.id),
        coMove.groupWalls.map((w) => w.id),
      ),
  });

  const drawActive = interactive && activeTool === 'draw';
  const drawIsSplit = drawShape === 'split';

  const localPointMm = (point: Vector3): { x: number; z: number; side: 1 | -1 } | null => {
    const group = groupRef.current;
    if (!group) return null;
    TMP_VEC.copy(point);
    group.worldToLocal(TMP_VEC);
    return { x: TMP_VEC.x * 1000, z: TMP_VEC.y * 1000, side: TMP_VEC.z >= 0 ? 1 : -1 };
  };

  const drawDims = (): WallBoxDims => ({
    lengthM: wall.lengthMm / 1000,
    heightM: Math.max(wallHeightAtMm(wall, 0), wallHeightAtMm(wall, wall.lengthMm)) / 1000,
    thicknessM: wall.thicknessMm / 1000,
  });

  // Project a world hit onto a face's in-plane (u,v) in mm (u = face width, v = face height).
  const faceUvMm = (point: Vector3, side: WallFeatureSide): { u: number; v: number } | null => {
    const group = groupRef.current;
    if (!group) return null;
    TMP_VEC.copy(point);
    group.worldToLocal(TMP_VEC);
    // A curved wall's front/back surface is cylindrical — invert it so the pick maps to (offset
    // along the developed wall, height), instead of the flat-box projection which mislocates it.
    if (isArcWall && (side === 'front' || side === 'back')) {
      const resolved = resolveArc(
        wall.lengthMm,
        wall.geomArcRadiusMm ?? 0,
        wall.geomArcSweepDeg ?? 1,
      );
      return curvedWallPickUv(
        TMP_VEC.x,
        TMP_VEC.y,
        TMP_VEC.z,
        resolved.radiusM,
        resolved.direction,
        resolved.sweepRad,
        // Feature U maps along the DEVELOPED arc length, not the chord.
        resolved.arcLengthMm,
      );
    }
    const frame = wallFaceFrame(side, drawDims());
    const rx = TMP_VEC.x - frame.origin.x;
    const ry = TMP_VEC.y - frame.origin.y;
    const rz = TMP_VEC.z - frame.origin.z;
    const u = (rx * frame.uAxis.x + ry * frame.uAxis.y + rz * frame.uAxis.z) * 1000;
    const v = (rx * frame.vAxis.x + ry * frame.vAxis.y + rz * frame.vAxis.z) * 1000;
    return { u, v };
  };

  const clampToFace = (uMm: number, vMm: number, side: WallFeatureSide): SceneWallFeaturePoint => {
    const frame = wallFaceFrame(side, drawDims());
    const uMax = frame.uMaxM * 1000;
    const vMax = frame.vMaxM * 1000;
    const m = FEATURE_EDGE_MARGIN_MM;
    return {
      x: clampValue(uMm, m, Math.max(m, uMax - m)),
      z: clampValue(vMm, m / 2, Math.max(m / 2, vMax - m / 2)),
    };
  };

  // Orient the invisible draw anchor so the drag plane + its (u,v) axes match the clicked face.
  // The third basis axis is u×v (always right-handed) so the plane normal sign never flips the
  // drag axes; front/back resolve to identity (unchanged behaviour).
  const orientDrawAnchor = (side: WallFeatureSide) => {
    const anchor = drawAnchorRef.current;
    if (!anchor) return;
    const frame = wallFaceFrame(side, drawDims());
    const w = new Vector3().crossVectors(frame.uAxis, frame.vAxis);
    anchor.quaternion.setFromRotationMatrix(new Matrix4().makeBasis(frame.uAxis, frame.vAxis, w));
    anchor.updateWorldMatrix(true, false);
  };

  const featureFitsFace = (side: WallFeatureSide, outline: { x: number; z: number }[]): boolean => {
    if (side === 'front' || side === 'back') return featureFitsWall(wall, outline);
    const frame = wallFaceFrame(side, drawDims());
    const b = outlineBoundsMm(outline);
    const m = FEATURE_EDGE_MARGIN_MM / 2;
    return (
      b.minX >= m &&
      b.maxX <= frame.uMaxM * 1000 - m &&
      b.minZ >= m &&
      b.maxZ <= frame.vMaxM * 1000 - m
    );
  };

  const commitDraft = (spec: DraftFeature) => {
    if (isBentWall) {
      queueToast({
        dedupeKey: 'glass-bent-no-feature',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.Pen.ArcNoFeature', {
          defaultValue: 'Şekilli (kavisli/eğimli) yüzeye henüz açıklık/şekil çizilemiyor.',
        }),
      });
      return;
    }
    if (spec.widthMm < MIN_FEATURE_SIZE_MM || spec.heightMm < MIN_FEATURE_SIZE_MM) {
      queueToast({
        dedupeKey: 'glass-wall-feature-fit',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.WallFeature.TooSmall', {
          defaultValue: 'Çizilen şekil çok küçük.',
        }),
      });
      return;
    }
    let points: SceneWallFeaturePoint[] | undefined;
    let offsetMm = spec.offsetMm;
    let centerZMm = spec.centerZMm;
    let widthMm = spec.widthMm;
    let heightMm = spec.heightMm;
    if (spec.shape === 'free') {
      const absolute = (spec.points ?? []).map((p) => ({
        x: spec.offsetMm + p.x,
        z: spec.centerZMm + p.z,
      }));
      const simplified = simplifyFreePoints(absolute, FREE_SIMPLIFY_TOLERANCE_MM);
      if (simplified.length < 3) return;
      const bounds = outlineBoundsMm(simplified.map((p) => ({ x: p.x, z: p.z })));
      offsetMm = (bounds.minX + bounds.maxX) / 2;
      centerZMm = (bounds.minZ + bounds.maxZ) / 2;
      widthMm = bounds.maxX - bounds.minX;
      heightMm = bounds.maxZ - bounds.minZ;
      if (widthMm < MIN_FEATURE_SIZE_MM || heightMm < MIN_FEATURE_SIZE_MM) return;
      points = simplified.map((p) => ({
        x: Math.round(p.x - offsetMm),
        z: Math.round(p.z - centerZMm),
      }));
    }
    const feature: SceneWallFeature = {
      id: crypto.randomUUID(),
      shape: spec.shape,
      // Flat walls default a drawn shape to a NON-CUTTING outline (a flush recess) so the user
      // picks hole/recess/protrusion in the inspector. A CURVED wall has no on-surface selection
      // yet, so a drawn shape there defaults to a through HOLE — the cut is applied + visible at
      // once (the common case: a window/opening in a curved wall).
      mode: isArcWall ? 'hole' : 'recess',
      // Stored side uses 1/-1 for front/back, the string for the four side faces.
      side: spec.side === 'front' ? 1 : spec.side === 'back' ? -1 : spec.side,
      offsetMm: Math.round(offsetMm),
      centerZMm: Math.round(centerZMm),
      widthMm: Math.round(widthMm),
      heightMm: Math.round(heightMm),
      depthMm: 0,
      sides: spec.shape === 'polygon' ? (spec.sides ?? 6) : undefined,
      points,
      colorHex: null,
    };
    const outline = featureOutlineMm(feature);
    if (!featureFitsFace(spec.side, outline)) {
      queueToast({
        dedupeKey: 'glass-wall-feature-fit',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.WallFeature.DoesNotFit', {
          defaultValue: 'Alan duvar sınırlarına sığmıyor.',
        }),
      });
      return;
    }
    addWallFeature(wall.id, feature);
    setSelection({
      kind: 'wallFeature',
      runId: null,
      panelId: null,
      connectionId: null,
      hardwareId: null,
      wallId: wall.id,
      slabId: null,
      featureId: feature.id,
    });
  };

  const drawDrag = useDrag3D({
    constraint: { mode: 'panelPlane', targetRef: drawAnchorRef },
    enabled: drawActive && !drawIsSplit,
    onMove: (delta) => {
      const session = drawSessionRef.current;
      if (!session) return;
      if (delta.x === 0 && delta.y === 0) {
        setDraft(null);
        return;
      }
      const cur = clampToFace(session.x0 + delta.x, session.z0 + delta.y, session.side);
      if (drawShape === 'free') {
        const last = session.points[session.points.length - 1];
        if (!last || Math.hypot(cur.x - last.x, cur.z - last.z) >= FREE_SAMPLE_STEP_MM) {
          session.points.push(cur);
        }
        const bounds = outlineBoundsMm(session.points.map((p) => ({ x: p.x, z: p.z })));
        const offsetMm = (bounds.minX + bounds.maxX) / 2;
        const centerZMm = (bounds.minZ + bounds.maxZ) / 2;
        setDraft({
          shape: 'free',
          offsetMm,
          centerZMm,
          widthMm: bounds.maxX - bounds.minX,
          heightMm: bounds.maxZ - bounds.minZ,
          points: session.points.map((p) => ({ x: p.x - offsetMm, z: p.z - centerZMm })),
          side: session.side,
        });
        return;
      }
      let widthMm = Math.abs(cur.x - session.x0);
      let heightMm = Math.abs(cur.z - session.z0);
      if (drawShape === 'circle') {
        const size = Math.max(widthMm, heightMm);
        widthMm = size;
        heightMm = size;
      }
      const signX = cur.x >= session.x0 ? 1 : -1;
      const signZ = cur.z >= session.z0 ? 1 : -1;
      setDraft({
        shape: drawShape === 'split' ? 'rect' : drawShape,
        offsetMm: session.x0 + (signX * widthMm) / 2,
        centerZMm: session.z0 + (signZ * heightMm) / 2,
        widthMm,
        heightMm,
        sides: drawShape === 'polygon' ? 6 : undefined,
        side: session.side,
      });
    },
    onCommit: () => {
      const spec = draftRef.current;
      drawSessionRef.current = null;
      setDraft(null);
      if (spec) commitDraft(spec);
    },
  });

  const handleDrawPointerDown = (e: ThreeEvent<PointerEvent>) => {
    if (e.nativeEvent.button === 0) {
      // The clicked face decides which of the six faces the feature lands on; the drag plane +
      // (u,v) axes are oriented to it. Arc walls keep front/back (their body is rotated).
      const normal = e.face?.normal;
      const side: WallFeatureSide = normal && !isArcWall ? sideFromLocalNormal(normal) : 'front';
      drawFaceRef.current = side;
      orientDrawAnchor(side);
      const uv = faceUvMm(e.point, side);
      if (uv) {
        const start = clampToFace(uv.u, uv.v, side);
        drawSessionRef.current = { x0: start.x, z0: start.z, side, points: [start] };
      }
    }
    drawDrag.handlers.onPointerDown(e);
  };

  const updateSplitHover = (e: ThreeEvent<PointerEvent>) => {
    const marker = splitHoverRef.current;
    const local = localPointMm(e.point);
    if (!marker || !local) return;
    const x = clampValue(local.x, MIN_LENGTH_MM, wall.lengthMm - MIN_LENGTH_MM);
    const heightM = wallHeightAtMm(wall, x) / 1000;
    marker.visible = true;
    marker.position.set(x / 1000, heightM / 2, 0);
    marker.scale.set(1, heightM, 1);
  };

  const hideSplitHover = () => {
    const marker = splitHoverRef.current;
    if (marker) marker.visible = false;
  };

  // The pen draws on whichever face the FIRST click hit (from its surface normal); every later
  // point on the same wall projects onto that same face's (u,v). Mirrors the draw tool's 6-face
  // mapping so a hole/recess can be penned on a side/top/bottom face, not just front/back.
  const penFacePoint = (
    point: Vector3,
    faceNormal?: { x: number; y: number; z: number },
  ): { x: number; z: number; side: WallFeatureSide } | null => {
    const session = useDesignerStore.getState().penFace;
    const established =
      session && session.hostId === wall.id ? normalizeWallSide(session.side) : null;
    const side: WallFeatureSide =
      established ?? (faceNormal && !isArcWall ? sideFromLocalNormal(faceNormal) : 'front');
    const uv = faceUvMm(point, side);
    if (!uv) return null;
    const clamped = clampToFace(uv.u, uv.v, side);
    return { x: clamped.x, z: clamped.z, side };
  };

  const penArcRef = useRef<{ active: boolean; end: { x: number; z: number } } | null>(null);
  const penSuppressClickRef = useRef(false);
  const [penArcPreview, setPenArcPreview] = useState<{ x: number; z: number }[] | null>(null);
  const getThree = useThree((s) => s.get);
  const setOrbitEnabled = (value: boolean) => {
    const controls = getThree().controls as unknown as { enabled: boolean } | null;
    if (controls) controls.enabled = value;
  };

  const handlePenDown = (e: ThreeEvent<PointerEvent>) => {
    if (e.nativeEvent.button !== 0) return;
    const session = useDesignerStore.getState().penFace;
    if (!isShiftPressed() || !session || session.hostId !== wall.id || session.points.length < 1)
      return;
    const local = penFacePoint(e.point, e.face?.normal);
    if (local) {
      penArcRef.current = { active: true, end: { x: local.x, z: local.z } };
      setOrbitEnabled(false);
      (e.target as Element | null)?.setPointerCapture?.(e.pointerId);
    }
  };

  const cancelPenArc = () => {
    penArcRef.current = null;
    setPenArcPreview(null);
    setOrbitEnabled(true);
  };

  const handlePenUp = (e: ThreeEvent<PointerEvent>) => {
    const arc = penArcRef.current;
    penArcRef.current = null;
    setPenArcPreview(null);
    setOrbitEnabled(true);
    if (!arc?.active) return;
    (e.target as Element | null)?.releasePointerCapture?.(e.pointerId);
    const session = useDesignerStore.getState().penFace;
    const local = penFacePoint(e.point, e.face?.normal);
    if (!session || !local) return;
    penSuppressClickRef.current = true;
    const anchor = session.points[session.points.length - 1];
    const m = { x: anchor.x, y: anchor.z };
    const end = { x: arc.end.x, y: arc.end.z };
    const cur = { x: local.x, y: local.z };
    const bulge = chordBulgeMm(m, end, cur);
    const pts = tessellateArc(m, end, bulge).map((p) => ({
      x: Math.round(p.x),
      z: Math.round(p.y),
    }));
    onPenFaceArc?.('wall', wall.id, session.side, pts);
  };

  const handlePenMove = (e: ThreeEvent<PointerEvent>) => {
    const arc = penArcRef.current;
    const local = penFacePoint(e.point, e.face?.normal);
    if (!local) return;
    if (arc?.active) {
      const session = useDesignerStore.getState().penFace;
      const anchor = session?.points[session.points.length - 1];
      if (anchor) {
        const m = { x: anchor.x, y: anchor.z };
        const end = { x: arc.end.x, y: arc.end.z };
        const bulge = chordBulgeMm(m, end, { x: local.x, y: local.z });
        const preview = tessellateArc(m, end, bulge);
        setPenArcPreview([anchor, ...preview.map((p) => ({ x: p.x, z: p.y }))]);
        const { radiusMm, angleDeg } = arcMetricsFromBulge(m, end, bulge);
        setDragReadout(
          radiusMm > 0
            ? `R ${Math.round(radiusMm)} mm · ${Math.round(angleDeg)}°`
            : `${Math.round(Math.hypot(end.x - m.x, end.y - m.y))} mm`,
        );
      }
      return;
    }
    if (e.nativeEvent.detail > 0) return;
    const session = useDesignerStore.getState().penFace;
    const anchor = session?.points[session.points.length - 1];
    if (anchor && session?.hostId === wall.id) {
      setDragReadout(`${Math.round(Math.hypot(local.x - anchor.x, local.z - anchor.z))} mm`);
    }
    setPenFaceCursor(wall.id, { x: local.x, z: local.z });
  };

  const handlePenClick = (e: ThreeEvent<MouseEvent>) => {
    e.stopPropagation();
    if (penSuppressClickRef.current) {
      penSuppressClickRef.current = false;
      return;
    }
    if (e.nativeEvent.detail > 1) return;
    const local = penFacePoint(e.point, e.face?.normal);
    if (local) onPenFaceClick?.('wall', wall.id, local.side, { x: local.x, z: local.z });
  };

  const handleClick = (event: ThreeEvent<MouseEvent>) => {
    event.stopPropagation();
    if (penActive) {
      handlePenClick(event);
      return;
    }
    if (gestures.consumeClick() || drawDrag.consumeClick()) return;
    if (drawActive) {
      if (drawIsSplit) {
        const local = localPointMm(event.point);
        if (local) splitWall(wall.id, local.x);
      }
      return;
    }
    if (activeTool === 'paint' && interactive) {
      if (paintMaterial) updateWall(wall.id, { materialKey: paintMaterial, colorHex: null });
      else if (paintColor) updateWall(wall.id, { colorHex: paintColor.hex, materialKey: null });
      return;
    }
    onSelect(wall.id);
  };

  const wallHandlers = penActive
    ? {
        onPointerDown: handlePenDown,
        onPointerMove: handlePenMove,
        onPointerUp: handlePenUp,
        onPointerCancel: cancelPenArc,
        onDoubleClick: () => onPenFaceFinish?.(),
      }
    : drawActive
      ? drawIsSplit
        ? { onPointerMove: updateSplitHover }
        : {
            onPointerDown: handleDrawPointerDown,
            onPointerMove: drawDrag.handlers.onPointerMove,
            onPointerUp: drawDrag.handlers.onPointerUp,
            onPointerCancel: drawDrag.handlers.onPointerCancel,
          }
      : gestures.handlers;

  const penLine = useMemo<[number, number, number][] | null>(() => {
    if (!penActive || !penFace || penFace.hostId !== wall.id) return null;
    const pts = penArcPreview ? [...penArcPreview] : [...penFace.points];
    if (!penArcPreview && penFace.cursor) pts.push(penFace.cursor);
    if (pts.length < 1) return null;
    // Map the face (u,v) points onto whichever face is being penned (front/back fall back to the
    // flat XY plane unchanged), lifted slightly off the surface along its normal.
    const frame = wallFaceFrame(normalizeWallSide(penFace.side), {
      lengthM: wall.lengthMm / 1000,
      heightM: Math.max(wall.heightMm, wall.heightEndMm ?? wall.heightMm) / 1000,
      thicknessM: wall.thicknessMm / 1000,
    });
    return pts.map((p): [number, number, number] => {
      const u = p.x / 1000;
      const v = p.z / 1000;
      return [
        frame.origin.x +
          u * frame.uAxis.x +
          v * frame.vAxis.x +
          frame.normal.x * FEATURE_FACE_LIFT_M,
        frame.origin.y +
          u * frame.uAxis.y +
          v * frame.vAxis.y +
          frame.normal.y * FEATURE_FACE_LIFT_M,
        frame.origin.z +
          u * frame.uAxis.z +
          v * frame.vAxis.z +
          frame.normal.z * FEATURE_FACE_LIFT_M,
      ];
    });
  }, [
    penActive,
    penFace,
    penArcPreview,
    wall.id,
    wall.lengthMm,
    wall.heightMm,
    wall.heightEndMm,
    wall.thicknessMm,
  ]);

  const stickyDelta = (base: number, deltaMm: number) => stickyDimensionMm(base + deltaMm) - base;
  const heightLevels = collectHeightLevels(fullScene, wall.id);
  const levelDelta = (base: number, deltaMm: number) =>
    snapToLevels(base + deltaMm, heightLevels) - base;

  const resetBody = () => {
    bodyRef.current?.scale.set(1, 1, 1);
    bodyRef.current?.position.set(0, 0, 0);
  };
  useLayoutEffect(
    () => resetBody(),
    [wall.lengthMm, wall.heightMm, wall.heightEndMm, wall.thicknessMm],
  );

  const previewLength = (deltaMm: number, fromStart: boolean) => {
    const scale = Math.max(0.05, (wall.lengthMm + deltaMm) / wall.lengthMm);
    setBodyPreview(bodyRef, [scale, 1, 1], [fromStart ? -deltaMm / 1000 : 0, 0, 0]);
  };

  const previewTop = (deltaMm: number) => {
    const scale = Math.max(0.05, (wall.heightMm + deltaMm) / wall.heightMm);
    setBodyPreview(bodyRef, [1, scale, 1], [0, 0, 0]);
  };

  const previewSide = (deltaMm: number, sign: 1 | -1) => {
    const scale = Math.max(0.05, (wall.thicknessMm + deltaMm) / wall.thicknessMm);
    setBodyPreview(bodyRef, [1, 1, scale], [0, 0, (sign * deltaMm) / 2000]);
  };

  const commitLength = (deltaMm: number, fromStart: boolean) => {
    const target = stickyDelta(wall.lengthMm, deltaMm);
    const clamped = clampPlanStretch(
      (d) =>
        buildWallFootprint(
          { ...wall, lengthMm: wall.lengthMm + d },
          fromStart ? -d * dirX : 0,
          fromStart ? -d * dirY : 0,
          wall.rotationDeg,
        ),
      planObstacles,
      target,
    );
    const next = Math.max(MIN_LENGTH_MM, Math.round(wall.lengthMm + clamped));
    if (next === wall.lengthMm) {
      resetBody();
      return;
    }
    if (!fromStart) {
      updateWall(wall.id, { lengthMm: next });
      return;
    }
    const shift = next - wall.lengthMm;
    updateWall(wall.id, {
      lengthMm: next,
      originX: Math.round(wall.originX - shift * dirX),
      originY: Math.round(wall.originY - shift * dirY),
    });
  };

  const commitTop = (deltaMm: number) => {
    const heightEnd = wall.heightEndMm ?? null;
    const target = levelDelta(wall.heightMm, deltaMm);
    const clamped = clampPlanStretch(
      (d) =>
        buildWallFootprint(
          {
            ...wall,
            heightMm: wall.heightMm + d,
            heightEndMm: heightEnd === null ? null : heightEnd + d,
          },
          0,
          0,
          wall.rotationDeg,
        ),
      planObstacles,
      target,
    );
    const next = Math.max(MIN_HEIGHT_MM, Math.round(wall.heightMm + clamped));
    const applied = next - wall.heightMm;
    if (applied === 0) {
      resetBody();
      return;
    }
    if (heightEnd === null) {
      updateWall(wall.id, { heightMm: next });
      return;
    }
    updateWall(wall.id, {
      heightMm: next,
      heightEndMm: Math.max(MIN_HEIGHT_MM, heightEnd + applied),
    });
  };

  const commitSide = (deltaMm: number, sign: 1 | -1) => {
    const target = stickyDelta(wall.thicknessMm, deltaMm);
    const clamped = clampPlanStretch(
      (d) =>
        buildWallFootprint(
          { ...wall, thicknessMm: wall.thicknessMm + d },
          sign * (d / 2) * normalX,
          sign * (d / 2) * normalY,
          wall.rotationDeg,
        ),
      planObstacles,
      target,
    );
    const next = Math.max(MIN_THICKNESS_MM, Math.round(wall.thicknessMm + clamped));
    const shift = next - wall.thicknessMm;
    if (shift === 0) {
      resetBody();
      return;
    }
    updateWall(wall.id, {
      thicknessMm: next,
      originX: Math.round(wall.originX + sign * (shift / 2) * normalX),
      originY: Math.round(wall.originY + sign * (shift / 2) * normalY),
    });
  };

  const featureSignedDepthMm = (feature: SceneWallFeature) => {
    if (feature.mode === 'protrude') return feature.depthMm;
    if (feature.mode === 'hole') return -wall.thicknessMm;
    return -feature.depthMm;
  };

  const commitFeatureDepth = (feature: SceneWallFeature, deltaMm: number) => {
    const thicknessMm = wall.thicknessMm;
    const signed = stickyDimensionMm(featureSignedDepthMm(feature) + deltaMm);
    if (signed <= -(thicknessMm - HOLE_THRESHOLD_MM)) {
      updateWallFeature(wall.id, feature.id, { mode: 'hole', depthMm: thicknessMm });
    } else if (signed < 0) {
      updateWallFeature(wall.id, feature.id, { mode: 'recess', depthMm: -signed });
    } else if (signed > 0) {
      updateWallFeature(wall.id, feature.id, { mode: 'protrude', depthMm: signed });
    } else {
      updateWallFeature(wall.id, feature.id, { mode: 'recess', depthMm: 0 });
    }
  };

  const slopeRad = Math.atan2(heightEndM - heightStartM, lengthM);
  const sideHeightM = Math.min(heightStartM, heightEndM);
  const labelMm = (value: number) => `${Math.round(value)} mm`;
  const lengthLabel = (d: number) =>
    labelMm(Math.max(MIN_LENGTH_MM, wall.lengthMm + stickyDelta(wall.lengthMm, d)));
  const heightLabel = (d: number) =>
    labelMm(Math.max(MIN_HEIGHT_MM, wall.heightMm + levelDelta(wall.heightMm, d)));
  const thicknessLabel = (d: number) =>
    labelMm(Math.max(MIN_THICKNESS_MM, wall.thicknessMm + stickyDelta(wall.thicknessMm, d)));
  const featureDepthLabel = (feature: SceneWallFeature) => (d: number) => {
    const signed = stickyDimensionMm(featureSignedDepthMm(feature) + d);
    if (signed <= -(wall.thicknessMm - HOLE_THRESHOLD_MM)) {
      return t('GlassEnclosure.Designer.Tool.ModeHole', { defaultValue: 'Boşluk (delik)' });
    }
    return labelMm(Math.abs(signed));
  };
  const stretchFaces: StretchFaceDef[] = stretchActive
    ? [
        {
          id: 'start',
          centerM: [-FACE_LIFT_M, heightStartM / 2, 0],
          rotation: [0, -HALF_PI, 0],
          widthM: thicknessM,
          heightM: heightStartM,
          hitWidthM: Math.max(thicknessM, FACE_HIT_SIZE_M),
          axis: [-1, 0, 0],
          label: lengthLabel,
          onPreview: (d) => previewLength(stickyDelta(wall.lengthMm, d), true),
          onCommit: (d) => commitLength(d, true),
        },
        {
          id: 'end',
          centerM: [lengthM + FACE_LIFT_M, heightEndM / 2, 0],
          rotation: [0, HALF_PI, 0],
          widthM: thicknessM,
          heightM: heightEndM,
          hitWidthM: Math.max(thicknessM, FACE_HIT_SIZE_M),
          axis: [1, 0, 0],
          label: lengthLabel,
          onPreview: (d) => previewLength(stickyDelta(wall.lengthMm, d), false),
          onCommit: (d) => commitLength(d, false),
        },
        {
          id: 'top',
          centerM: [lengthM / 2, (heightStartM + heightEndM) / 2 + FACE_LIFT_M, 0],
          rotation: [-HALF_PI, -slopeRad, 0],
          widthM: Math.hypot(lengthM, heightEndM - heightStartM),
          heightM: thicknessM,
          hitHeightM: Math.max(thicknessM, FACE_HIT_SIZE_M),
          axis: [0, 1, 0],
          label: heightLabel,
          onPreview: (d) => previewTop(levelDelta(wall.heightMm, d)),
          onCommit: commitTop,
        },
        {
          id: 'side-a',
          centerM: [lengthM / 2, sideHeightM / 2, thicknessM / 2 + FACE_LIFT_M],
          rotation: [0, 0, 0],
          widthM: lengthM,
          heightM: sideHeightM,
          axis: [0, 0, 1],
          label: thicknessLabel,
          onPreview: (d) => previewSide(stickyDelta(wall.thicknessMm, d), 1),
          onCommit: (d) => commitSide(d, 1),
        },
        {
          id: 'side-b',
          centerM: [lengthM / 2, sideHeightM / 2, -thicknessM / 2 - FACE_LIFT_M],
          rotation: [0, Math.PI, 0],
          widthM: lengthM,
          heightM: sideHeightM,
          axis: [0, 0, -1],
          label: thicknessLabel,
          onPreview: (d) => previewSide(stickyDelta(wall.thicknessMm, d), -1),
          onCommit: (d) => commitSide(d, -1),
        },
        ...featureItems
          .filter(({ feature }) => feature.side === 1 || feature.side === -1)
          .map(({ feature, bounds }): StretchFaceDef => {
            const signedDepthMm = featureSignedDepthMm(feature);
            const s = featureSideSignZ(feature.side);
            const faceZ =
              s * (thicknessM / 2) +
              (s * Math.max(signedDepthMm, 0)) / 1000 +
              s * FEATURE_FACE_LIFT_M;
            return {
              id: `feature-${feature.id}`,
              centerM: [
                (bounds.minX + bounds.maxX) / 2000,
                (bounds.minZ + bounds.maxZ) / 2000,
                faceZ,
              ],
              rotation: s === 1 ? [0, 0, 0] : [0, Math.PI, 0],
              widthM: (bounds.maxX - bounds.minX) / 1000,
              heightM: (bounds.maxZ - bounds.minZ) / 1000,
              axis: [0, 0, s],
              label: featureDepthLabel(feature),
              onPreview: () => {},
              onCommit: (d) => commitFeatureDepth(feature, d),
            };
          }),
      ]
    : [];

  const materialTexture = useTiledProceduralTexture(
    wall.materialKey,
    wall.lengthMm / 500,
    wall.heightMm / 500,
  );

  const setGroupRef = (group: Group | null) => {
    groupRef.current = group;
    registerSceneRef(wall.id, group);
  };

  return (
    <>
      <group
        ref={setGroupRef}
        position={[wall.originX / 1000, (wall.geomZ ?? 0) / 1000, wall.originY / 1000]}
        rotation={[0, -wall.rotationDeg * DEG2RAD, 0]}
      >
        <group ref={drawAnchorRef} />
        <group ref={bodyRef}>
          <mesh
            geometry={geometry}
            rotation={isArcWall ? [-Math.PI / 2, 0, 0] : undefined}
            castShadow
            receiveShadow
            {...wallHandlers}
            onClick={handleClick}
            onPointerOver={(e) => {
              e.stopPropagation();
              document.body.style.cursor = penActive || drawActive ? 'crosshair' : 'pointer';
            }}
            onPointerOut={() => {
              document.body.style.cursor = 'auto';
              if (drawActive && drawIsSplit) hideSplitHover();
            }}
          >
            <meshStandardMaterial
              key={materialTexture ? (wall.materialKey ?? 'plain') : 'plain'}
              color={materialTexture ? '#ffffff' : (wall.colorHex ?? WALL_COLOR)}
              map={materialTexture ?? undefined}
              roughness={0.9}
              metalness={0.05}
            />
            {!presentation && (
              <Edges color={isSelected ? WALL_SELECTED : WALL_EDGE} threshold={15} />
            )}
          </mesh>
        </group>
        {featureItems.map((item) => (
          <WallFeatureObject
            key={item.feature.id}
            wall={wall}
            item={item}
            fallbackColor={wall.colorHex ?? WALL_COLOR}
            fallbackMap={materialTexture}
            interactive={interactive}
            thicknessM={thicknessM}
            presentation={presentation}
          />
        ))}
        {openingFrames.length > 0 && (
          <WallOpeningFrames
            frames={openingFrames}
            thicknessMm={wall.thicknessMm}
            quality={quality}
          />
        )}
        {drawActive && drawIsSplit && (
          <mesh ref={splitHoverRef} visible={false} raycast={() => null}>
            <boxGeometry args={[SPLIT_PREVIEW_WIDTH_M, 1, thicknessM + 0.02]} />
            <meshBasicMaterial color={SPLIT_COLOR} transparent opacity={0.8} depthWrite={false} />
          </mesh>
        )}
        {draft && <DraftPreview draft={draft} dims={drawDims()} />}
        {penLine && penLine.length >= 2 && (
          <Line points={penLine} color={REGION_COLOR} lineWidth={2} raycast={() => null} />
        )}
        {penLine?.map((p, i) => (
          <mesh key={i} position={p} raycast={() => null}>
            <sphereGeometry args={[0.03, 8, 8]} />
            <meshBasicMaterial color={REGION_COLOR} />
          </mesh>
        ))}
        {stretchActive && <StretchFaces faces={stretchFaces} />}
      </group>
      {vertexEditActive && (
        <FootprintCornerHandles
          box={{
            originX: wall.originX,
            originY: wall.originY,
            lengthMm: wall.lengthMm,
            crossMm: wall.thicknessMm,
            rotationDeg: wall.rotationDeg,
          }}
          topYM={
            ((wall.geomZ ?? 0) + Math.max(wall.heightMm, wall.heightEndMm ?? wall.heightMm)) / 1000
          }
          onCommit={(next) => {
            // For an arc wall the corner-box length is the CHORD (span): dragging it keeps the sweep
            // (curl shape) and scales the glass length + radius. For a straight wall it's the length.
            const shaped = isArcWall
              ? arcFromChordKeepingSweep(next.lengthMm, wall.geomArcSweepDeg ?? 1)
              : { lengthMm: next.lengthMm, geomArcRadiusMm: wall.geomArcRadiusMm ?? null };
            // Reject a corner resize that would grow the wall into a neighbour (the Stretch
            // tool clamps; the corner handles must not be a collision-free back door).
            const resized = buildWallFootprint(
              {
                ...wall,
                originX: next.originX,
                originY: next.originY,
                lengthMm: shaped.lengthMm,
                geomArcRadiusMm: shaped.geomArcRadiusMm,
                thicknessMm: next.crossMm,
              },
              0,
              0,
              next.rotationDeg,
            );
            if (penetratesAny(resized, planObstacles)) return;
            updateWall(wall.id, {
              originX: next.originX,
              originY: next.originY,
              lengthMm: shaped.lengthMm,
              thicknessMm: next.crossMm,
              ...(isArcWall ? { geomArcRadiusMm: shaped.geomArcRadiusMm } : {}),
            });
          }}
        />
      )}
      {curveEditActive && (
        <CurveBowHandle
          startX={wall.originX}
          startY={wall.originY}
          endX={wallCurveChord.endX}
          endY={wallCurveChord.endY}
          currentSagittaMm={wallCurveChord.sagittaMm}
          topYM={
            ((wall.geomZ ?? 0) + Math.max(wall.heightMm, wall.heightEndMm ?? wall.heightMm)) / 1000
          }
          onCommit={(sagittaMm) => {
            const chordMm = Math.hypot(
              wallCurveChord.endX - wall.originX,
              wallCurveChord.endY - wall.originY,
            );
            const chordDeg =
              (Math.atan2(wallCurveChord.endY - wall.originY, wallCurveChord.endX - wall.originX) *
                180) /
              Math.PI;
            // The glass length (lengthMm) stays fixed; the drag sets the sweep, radius derived.
            const arc = bowToArcKeepingLength(chordMm, chordDeg, sagittaMm, wall.lengthMm);
            updateWall(wall.id, {
              rotationDeg: arc.rotationDeg,
              geomArcRadiusMm: arc.geomArcRadiusMm,
              geomArcSweepDeg: arc.geomArcSweepDeg,
            });
          }}
        />
      )}
      {bendEditActive && (
        <BendHandle
          startX={wall.originX}
          startY={wall.originY}
          dirDeg={wall.rotationDeg}
          lengthMm={wall.lengthMm}
          bendAtMm={wall.bendAtMm ?? wall.lengthMm / 2}
          currentBendDeg={wall.bendAngleDeg ?? 0}
          topYM={
            ((wall.geomZ ?? 0) + Math.max(wall.heightMm, wall.heightEndMm ?? wall.heightMm)) / 1000
          }
          onCommit={(bendDeg) => {
            if (bendDeg === 0) {
              updateWall(wall.id, { bendAngleDeg: null });
              return;
            }
            updateWall(wall.id, {
              bendAtMm: wall.bendAtMm ?? Math.round(wall.lengthMm / 2),
              bendAngleDeg: bendDeg,
            });
          }}
        />
      )}
    </>
  );
}

interface WallFeatureObjectProps {
  wall: SceneWallState;
  item: WallFeatureItem;
  fallbackColor: string;
  fallbackMap: Texture | null;
  interactive: boolean;
  thicknessM: number;
  presentation: boolean;
}

function WallFeatureObject({
  wall,
  item,
  fallbackColor,
  fallbackMap,
  interactive,
  thicknessM,
  presentation,
}: WallFeatureObjectProps) {
  const { feature, outline, geometry } = item;
  const activeTool = useDesignerStore((s) => s.activeTool);
  const paintColor = useDesignerStore((s) => s.paintColor);
  const isSelected = useDesignerStore(
    (s) => s.selection.kind === 'wallFeature' && s.selection.featureId === feature.id,
  );
  const setSelection = useDesignerStore((s) => s.setSelection);
  const updateWallFeature = useDesignerStore((s) => s.updateWallFeature);
  const removeWallFeature = useDesignerStore((s) => s.removeWallFeature);

  const anchorRef = useRef<Group>(null);
  const previewRef = useRef<Group>(null);
  const lastDeltaRef = useRef({ x: 0, z: 0 });
  useLayoutEffect(() => {
    previewRef.current?.position.set(0, 0, 0);
  }, [feature.offsetMm, feature.centerZMm]);

  const regionGeometry = useMemo(() => {
    if (geometry || outline.length < 3) return null;
    return new ShapeGeometry(outlineToShape(outline));
  }, [geometry, outline]);
  useEffect(() => () => regionGeometry?.dispose(), [regionGeometry]);
  const regionPoints = useMemo<[number, number, number][]>(() => {
    const points = outline.map((p): [number, number, number] => [p.x / 1000, p.z / 1000, 0]);
    if (points.length > 1) points.push(points[0]);
    return points;
  }, [outline]);

  // Orient the outline line + click proxy onto whichever face the feature lives on. For front/back
  // this resolves to identity rotation at ±t/2 (unchanged from before); the four side faces get
  // their real plane via wallFaceFrame so top/bottom/left/right features are visible + selectable.
  const isSideFace = feature.side !== 1 && feature.side !== -1;
  const faceFrame = useMemo(() => {
    const dims: WallBoxDims = {
      lengthM: wall.lengthMm / 1000,
      heightM: Math.max(wall.heightMm, wall.heightEndMm ?? wall.heightMm) / 1000,
      thicknessM,
    };
    return wallFaceFrame(normalizeWallSide(feature.side), dims);
  }, [wall.lengthMm, wall.heightMm, wall.heightEndMm, thicknessM, feature.side]);
  const regionQuat = useMemo(() => {
    const w = new Vector3().crossVectors(faceFrame.uAxis, faceFrame.vAxis);
    return new Quaternion().setFromRotationMatrix(
      new Matrix4().makeBasis(faceFrame.uAxis, faceFrame.vAxis, w),
    );
  }, [faceFrame]);
  const regionPosition = useMemo<[number, number, number]>(
    () => [
      faceFrame.origin.x + faceFrame.normal.x * FACE_LIFT_M,
      faceFrame.origin.y + faceFrame.normal.y * FACE_LIFT_M,
      faceFrame.origin.z + faceFrame.normal.z * FACE_LIFT_M,
    ],
    [faceFrame],
  );

  const clampMove = (dxMm: number, dzMm: number) => {
    const bounds = item.bounds;
    const minTop =
      Math.min(wall.heightMm, wall.heightEndMm ?? wall.heightMm) - FEATURE_EDGE_MARGIN_MM;
    return {
      x: Math.round(
        Math.min(
          wall.lengthMm - FEATURE_EDGE_MARGIN_MM - bounds.maxX,
          Math.max(FEATURE_EDGE_MARGIN_MM - bounds.minX, dxMm),
        ),
      ),
      z: Math.round(
        Math.min(minTop - bounds.maxZ, Math.max(FEATURE_EDGE_MARGIN_MM / 2 - bounds.minZ, dzMm)),
      ),
    };
  };

  // Dragging uses the front-plane constraint, which only matches front/back features; side-face
  // features stay selectable + inspector-editable (offset/size/depth) without in-scene drag.
  const moveEnabled = interactive && activeTool === 'move' && !isSideFace;
  const drag = useDrag3D({
    constraint: { mode: 'panelPlane', targetRef: anchorRef },
    enabled: moveEnabled,
    onMove: (delta) => {
      const clamped = clampMove(delta.x, delta.y);
      lastDeltaRef.current = clamped;
      previewRef.current?.position.set(clamped.x / 1000, clamped.z / 1000, 0);
    },
    onCommit: () => {
      const clamped = lastDeltaRef.current;
      lastDeltaRef.current = { x: 0, z: 0 };
      if (clamped.x !== 0 || clamped.z !== 0) {
        updateWallFeature(wall.id, feature.id, {
          offsetMm: feature.offsetMm + clamped.x,
          centerZMm: feature.centerZMm + clamped.z,
        });
      } else {
        previewRef.current?.position.set(0, 0, 0);
      }
    },
  });

  const select = () =>
    setSelection({
      kind: 'wallFeature',
      runId: null,
      panelId: null,
      connectionId: null,
      hardwareId: null,
      wallId: wall.id,
      slabId: null,
      featureId: feature.id,
    });

  const handleClick = (e: ThreeEvent<MouseEvent>) => {
    e.stopPropagation();
    if (drag.consumeClick()) return;
    if (!interactive) return;
    if (activeTool === 'erase') {
      removeWallFeature(wall.id, feature.id);
      return;
    }
    if (activeTool === 'paint') {
      if (paintColor) updateWallFeature(wall.id, feature.id, { colorHex: paintColor.hex });
      return;
    }
    select();
  };

  const handlePointerDown = (e: ThreeEvent<PointerEvent>) => {
    if (moveEnabled && e.nativeEvent.button === 0) select();
    drag.handlers.onPointerDown(e);
  };

  const featureMap = feature.colorHex ? null : fallbackMap;

  if (activeTool === 'draw') {
    if (geometry) {
      return (
        <mesh geometry={geometry} castShadow receiveShadow raycast={() => null}>
          <meshStandardMaterial
            key={featureMap ? 'mapped' : 'plain'}
            color={featureMap ? '#ffffff' : (feature.colorHex ?? fallbackColor)}
            map={featureMap ?? undefined}
            roughness={0.85}
            metalness={0.05}
          />
          {!presentation && (
            <Edges color={isSelected ? FEATURE_SELECTED : WALL_EDGE} threshold={15} />
          )}
        </mesh>
      );
    }
    return (
      <group position={regionPosition} quaternion={regionQuat}>
        <Line
          points={regionPoints}
          color={feature.colorHex ?? REGION_COLOR}
          lineWidth={1.5}
          raycast={() => null}
        />
      </group>
    );
  }

  return (
    <group ref={anchorRef}>
      <group ref={previewRef}>
        {geometry ? (
          <mesh
            geometry={geometry}
            castShadow
            receiveShadow
            {...drag.handlers}
            onPointerDown={handlePointerDown}
            onClick={handleClick}
            onPointerOver={(e) => {
              e.stopPropagation();
              document.body.style.cursor = moveEnabled ? 'grab' : 'pointer';
            }}
            onPointerOut={() => {
              document.body.style.cursor = 'auto';
            }}
          >
            <meshStandardMaterial
              key={featureMap ? 'mapped' : 'plain'}
              color={featureMap ? '#ffffff' : (feature.colorHex ?? fallbackColor)}
              map={featureMap ?? undefined}
              roughness={0.85}
              metalness={0.05}
              emissive={isSelected ? FEATURE_SELECTED : '#000000'}
              emissiveIntensity={isSelected ? 0.15 : 0}
            />
            {!presentation && (
              <Edges color={isSelected ? FEATURE_SELECTED : WALL_EDGE} threshold={15} />
            )}
          </mesh>
        ) : (
          <group position={regionPosition} quaternion={regionQuat}>
            {!presentation && (
              <Line
                points={regionPoints}
                color={isSelected ? FEATURE_SELECTED : (feature.colorHex ?? REGION_COLOR)}
                lineWidth={isSelected ? 2.5 : 1.5}
                raycast={() => null}
              />
            )}
            {regionGeometry && (
              <mesh
                geometry={regionGeometry}
                {...drag.handlers}
                onPointerDown={handlePointerDown}
                onClick={handleClick}
                onPointerOver={(e) => {
                  e.stopPropagation();
                  document.body.style.cursor = moveEnabled ? 'grab' : 'pointer';
                }}
                onPointerOut={() => {
                  document.body.style.cursor = 'auto';
                }}
              >
                <meshBasicMaterial
                  transparent
                  opacity={isSelected ? 0.12 : 0}
                  color={FEATURE_SELECTED}
                  depthWrite={false}
                  side={DoubleSide}
                />
              </mesh>
            )}
          </group>
        )}
      </group>
    </group>
  );
}

function DraftPreview({ draft, dims }: { draft: DraftFeature; dims: WallBoxDims }) {
  // Orient the preview onto whichever face is being drawn (front/back resolve to the flat
  // XY plane unchanged); the outline (u,v) maps to the face's u/v axes, lifted along its normal.
  const frame = wallFaceFrame(draft.side, dims);
  const w = new Vector3().crossVectors(frame.uAxis, frame.vAxis);
  const previewQuat = new Quaternion().setFromRotationMatrix(
    new Matrix4().makeBasis(frame.uAxis, frame.vAxis, w),
  );
  const position: [number, number, number] = [
    frame.origin.x + frame.normal.x * FEATURE_FACE_LIFT_M,
    frame.origin.y + frame.normal.y * FEATURE_FACE_LIFT_M,
    frame.origin.z + frame.normal.z * FEATURE_FACE_LIFT_M,
  ];
  // Live size readout in the shared HUD while the shape is being drawn; cleared on unmount
  // (draw committed/cancelled → draft becomes null → this component unmounts).
  useEffect(() => {
    setDragReadout(formatDraftDimensionMm(draft));
    return () => setDragReadout(null);
  }, [draft]);
  const outline = useMemo(() => featureOutlineMm(draft), [draft]);
  const fillGeometry = useMemo(() => {
    if (draft.shape === 'free' || outline.length < 3) return null;
    if (draft.widthMm < 5 || draft.heightMm < 5) return null;
    return new ShapeGeometry(outlineToShape(outline));
  }, [draft.shape, draft.widthMm, draft.heightMm, outline]);
  useEffect(() => () => fillGeometry?.dispose(), [fillGeometry]);
  const linePoints = useMemo<[number, number, number][]>(() => {
    const points = outline.map((p): [number, number, number] => [p.x / 1000, p.z / 1000, 0]);
    if (draft.shape !== 'free' && points.length > 1) points.push(points[0]);
    return points;
  }, [outline, draft.shape]);
  if (outline.length < 2) return null;
  return (
    <group position={position} quaternion={previewQuat}>
      {fillGeometry && (
        <mesh geometry={fillGeometry} raycast={() => null}>
          <meshBasicMaterial
            color={REGION_COLOR}
            transparent
            opacity={DRAW_PREVIEW_OPACITY}
            depthWrite={false}
            side={DoubleSide}
          />
        </mesh>
      )}
      <Line points={linePoints} color={REGION_COLOR} lineWidth={1.5} raycast={() => null} />
    </group>
  );
}
