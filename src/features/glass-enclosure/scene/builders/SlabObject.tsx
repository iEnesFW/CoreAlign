import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { Edges, Line } from '@react-three/drei';
import { useThree } from '@react-three/fiber';
import { useTranslation } from 'react-i18next';
import { DoubleSide, ExtrudeGeometry, Raycaster, ShapeGeometry, Vector3 } from 'three';
import type { ThreeEvent } from '@react-three/fiber';
import type { Group, Mesh, Texture, BufferGeometry } from 'three';
import {
  isShiftPressed,
  setDragReadout,
  stickyDimensionMm,
  useDrag3D,
  useTiledProceduralTexture,
} from '@/shared/three-engine';
import { isAltPressed } from '@/shared/three-engine';
import { queueToast } from '@/shared/api/toastQueue';
import { useObjectGestures } from '../interaction/useObjectGestures';
import { notifyStackUnavailable } from '../interaction/stackFeedback';
import { StretchFaces } from '../interaction/StretchFaces';
import { FootprintCornerHandles } from '../interaction/FootprintCornerHandles';
import { setBodyPreview } from '../interaction/bodyPreview';
import { registerSceneRef } from '../interaction/sceneRefs';
import { captureMultiSnapshots, multiSelectionHas } from '../interaction/multiMove';
import {
  captureMultiBodies,
  EMPTY_MULTI_BODIES,
  multiBodyFootprints,
} from '../interaction/multiMoveFootprints';
import type { MultiMoveBodies } from '../interaction/multiMoveFootprints';
import { collectHeightLevels, snapToLevels } from '../interaction/levelSnap';
import { arcMetricsFromBulge, chordBulgeMm, tessellateArc } from '../interaction/penArc';
import { previewSnapshotsMove } from '../interaction/attachedRunPreview';
import { EMPTY_SNAP_TARGETS, filterSnapTargets } from '../interaction/planSnap';
import {
  buildSlabFootprint,
  clampPlanStretch,
  liftToClearMm,
  penetratesAny,
  restElevationMm,
  stackableSupports,
  restsOnSupportAtMm,
} from '../interaction/planCollision';
import { filletedShapeMm, outlineToPath, outlineToShape } from './surfaceFeatureShapes';
import { buildBarrelRoofGeometry } from './barrelRoofGeometry';
import {
  curvedSlabFrame,
  curvedSlabMapOutlineMm,
  curvedSlabPickSc,
  curvedSlabPlanOutlineMm,
  curvedSlabPointAt,
} from './curvedSlabGeometry';
import { buildPitchedRoofGeometry } from './pitchedRoofGeometry';
import { edgeColorFor } from './edgeColor';
import { bowFromArc, isRealArc } from '../../model/arcGeometry';
import { arcCommitKeepingEnds } from '../../geometry/arcCommit';
import { commitArcOrWarn } from '../../geometry/arcCommitFeedback';
import type { CurvablePose } from '../../geometry/curvature';
import { edgeArcOutline, hasEdgeArc } from '../../model/edgeArcOutline';
import { ArcSweepHandle } from '../interaction/ArcSweepHandle';
import type { WallFeatureSide } from './wallFaces';
import type { AttachedRunSnapshot } from '../interaction/attachedRunPreview';
import type { PlanMoveDelta } from '../interaction/planSnap';
import { useDesignerStore } from '../../model/designerStore';
import { useSlabEntityActions } from '../../hooks/useDesignerEntityActions';
import {
  FEATURE_EDGE_MARGIN_MM,
  FREE_SAMPLE_STEP_MM,
  FREE_SIMPLIFY_TOLERANCE_MM,
  MIN_FEATURE_SIZE_MM,
  composeSurfaceFeatures,
  featureOutlineMm,
  formatDraftDimensionMm,
  outlineBoundsMm,
  outlineFitsRect,
  sanitizeFreeOutline,
  shrinkOutlineMm,
  simplifyFreePoints,
} from '../../model/wallFeatureGeometry';
import type { PlanGestureAdapter } from '../interaction/useObjectGestures';
import type { StretchFaceDef } from '../interaction/StretchFaces';
import type { PlanPoint, PlanSnapTargets } from '../interaction/planSnap';
import type { PlanFootprint } from '../interaction/planCollision';
import type { ComposedFeature, FeatureOutlineSpec } from '../../model/wallFeatureGeometry';
import { featureSideSignZ } from '../../model/project.types';
import type {
  SceneSlabState,
  SceneWallFeature,
  SceneWallFeaturePoint,
} from '../../model/project.types';

interface SlabObjectProps {
  slab: SceneSlabState;
  isSelected: boolean;
  onSelect: (slabId: string) => void;
  snapTargets?: PlanSnapTargets;
  obstacles?: PlanFootprint[];
  supports?: PlanFootprint[];
  interactive?: boolean;
  onCommitMove?: (slabId: string, delta: PlanMoveDelta) => void;
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

const FACE_PICK_RAYCASTER = new Raycaster();

const FLOOR_COLOR = '#b7bfc7';
const ROOF_COLOR = '#8c98a4';
const SELECTED_EDGE = '#1d4ed8';
const SLAB_EDGE = '#cbd5e1';
const REGION_COLOR = '#2563eb';
const DEG2RAD = Math.PI / 180;
const HALF_PI = Math.PI / 2;
const PEN_FREE_STEP_MM = 40;
const MIN_PLAN_MM = 100;
const MIN_THICKNESS_MM = 50;
const FACE_LIFT_M = 0.002;
const FEATURE_FACE_LIFT_M = 0.004;
const PLUG_INSET_MM = 1;
const MIN_PLUG_DEPTH_M = 0.003;
const HOLE_THRESHOLD_MM = 5;
const DRAW_PREVIEW_OPACITY = 0.35;
const EMPTY_OBSTACLES: PlanFootprint[] = [];
const TMP_VEC = new Vector3();

interface DraftFeature extends FeatureOutlineSpec {
  side: 1 | -1;
}

interface SlabFeatureItem extends ComposedFeature {
  geometry: ExtrudeGeometry | null;
}

const buildSlabGeometries = (
  slab: SceneSlabState,
  cutFeatures = true,
): { body: BufferGeometry; featureItems: SlabFeatureItem[] } => {
  const thicknessMm = slab.thicknessMm;
  const thicknessM = thicknessMm / 1000;
  const orient = (geometry: ExtrudeGeometry, extraYM: number) => {
    geometry.rotateX(HALF_PI);
    geometry.translate(0, thicknessM + extraYM, 0);
    return geometry;
  };

  // PLAN arc (curves left/right like a wall): the bend is purely in PLAN and the cut is vertical
  // along Y, so the curved body is an EXTRUDE of the sampled band outline — and features cut
  // exactly like the flat slab (shape.holes), with each (s,c) outline forward-mapped into plan.
  // #6b: features are fully supported on plan-arc slabs (barrel/pitch remain deferred).
  if (isRealArc(slab.geomArcRadiusMm, slab.geomArcSweepDeg)) {
    const frame = curvedSlabFrame(
      slab.lengthMm,
      slab.depthMm,
      slab.geomArcRadiusMm ?? 0,
      slab.geomArcSweepDeg ?? 1,
      slab.slabArcAxis ?? 'length',
    );
    const shape = outlineToShape(
      curvedSlabPlanOutlineMm(
        slab.lengthMm,
        slab.depthMm,
        slab.geomArcRadiusMm ?? 0,
        slab.geomArcSweepDeg ?? 1,
        slab.slabArcAxis ?? 'length',
      ),
    );
    const composed = composeSurfaceFeatures(
      slab.features ?? [],
      (outline) =>
        outlineFitsRect(outline, frame.developedMm, frame.acrossMm, FEATURE_EDGE_MARGIN_MM),
      [],
      thicknessMm,
    );
    const featureItems: SlabFeatureItem[] = [];
    for (const baseItem of composed) {
      const item =
        cutFeatures || baseItem.kind === 'protrude'
          ? baseItem
          : { ...baseItem, kind: 'outline' as const, cut: false };
      const mappedOutline = curvedSlabMapOutlineMm(frame, item.outline);
      if (item.cut) shape.holes.push(outlineToPath(mappedOutline));
      let geometry: ExtrudeGeometry | null = null;
      if (item.kind === 'plug') {
        const plugDepthM = Math.max(MIN_PLUG_DEPTH_M, (thicknessMm - item.feature.depthMm) / 1000);
        geometry = orient(
          new ExtrudeGeometry(
            outlineToShape(
              curvedSlabMapOutlineMm(frame, shrinkOutlineMm(item.outline, PLUG_INSET_MM)),
            ),
            { depth: plugDepthM, bevelEnabled: false },
          ),
          item.feature.side === 1 ? -item.feature.depthMm / 1000 : 0,
        );
      } else if (item.kind === 'protrude') {
        const depthM = Math.max(0.002, item.feature.depthMm / 1000);
        geometry = orient(
          new ExtrudeGeometry(outlineToShape(mappedOutline), {
            depth: depthM,
            bevelEnabled: false,
          }),
          item.feature.side === 1 ? depthM : -thicknessM,
        );
      }
      // outline is overridden with the PLAN-mapped polyline so the selection decal / region line
      // renders ON the band; bounds stay in (s,c) for the move clamp.
      featureItems.push({ ...item, outline: mappedOutline, geometry });
    }
    const body = orient(new ExtrudeGeometry(shape, { depth: thicknessM, bevelEnabled: false }), 0);
    return { body, featureItems };
  }

  // Barrel (single-curvature) slab — a curved sheet (roof OR floor), already in the slab's
  // oriented frame. Surface features are not projected onto the curve in this version (#6b).
  if (slab.arcRiseMm && slab.arcRiseMm > 0) {
    return {
      body: buildBarrelRoofGeometry(slab.lengthMm, slab.depthMm, slab.arcRiseMm, slab.thicknessMm),
      featureItems: [],
    };
  }
  // Pitched/gable slab — two sloped planes meeting at a ridge (symmetric, triangular gable ends)
  // or a single slope (monopitch). Mutually exclusive with the barrel; features deferred like #6b.
  if (slab.pitchRiseMm && slab.pitchRiseMm > 0) {
    return {
      body: buildPitchedRoofGeometry(
        slab.lengthMm,
        slab.depthMm,
        slab.pitchRiseMm,
        slab.pitchType ?? 'symmetric',
        slab.thicknessMm,
      ),
      featureItems: [],
    };
  }

  // WHY: single-edge arc bows one rect edge of a FLAT slab; mutually exclusive with the whole-body
  // arc/barrel/pitch above (they return first). Features deferred here like barrel/pitch.
  if (hasEdgeArc(slab.geomEdgeArc)) {
    const outline = edgeArcOutline(slab.lengthMm, slab.depthMm, slab.geomEdgeArc ?? {});
    if (outline.length >= 3) {
      const shape = outlineToShape(outline.map((p) => ({ x: p.x, z: p.y })));
      const body = orient(
        new ExtrudeGeometry(shape, { depth: thicknessM, bevelEnabled: false }),
        0,
      );
      return { body, featureItems: [] };
    }
  }

  const radii = slab.cornerRadiiMm ?? {};
  const shape = filletedShapeMm(
    [
      { x: 0, z: 0 },
      { x: slab.lengthMm, z: 0 },
      { x: slab.lengthMm, z: slab.depthMm },
      { x: 0, z: slab.depthMm },
    ],
    [radii.bl ?? 0, radii.br ?? 0, radii.tr ?? 0, radii.tl ?? 0],
  );
  const composed = composeSurfaceFeatures(
    slab.features ?? [],
    (outline) => outlineFitsRect(outline, slab.lengthMm, slab.depthMm, FEATURE_EDGE_MARGIN_MM),
    [],
    thicknessMm,
  );
  const featureItems: SlabFeatureItem[] = [];
  for (const baseItem of composed) {
    const item =
      cutFeatures || baseItem.kind === 'protrude'
        ? baseItem
        : { ...baseItem, kind: 'outline' as const, cut: false };
    if (item.cut) shape.holes.push(outlineToPath(item.outline));
    let geometry: ExtrudeGeometry | null = null;
    if (item.kind === 'plug') {
      const plugDepthM = Math.max(MIN_PLUG_DEPTH_M, (thicknessMm - item.feature.depthMm) / 1000);
      geometry = orient(
        new ExtrudeGeometry(outlineToShape(shrinkOutlineMm(item.outline, PLUG_INSET_MM)), {
          depth: plugDepthM,
          bevelEnabled: false,
        }),
        item.feature.side === 1 ? -item.feature.depthMm / 1000 : 0,
      );
    } else if (item.kind === 'protrude') {
      const depthM = Math.max(0.002, item.feature.depthMm / 1000);
      geometry = orient(
        new ExtrudeGeometry(outlineToShape(item.outline), { depth: depthM, bevelEnabled: false }),
        item.feature.side === 1 ? depthM : -thicknessM,
      );
    }
    featureItems.push({ ...item, geometry });
  }
  const body = orient(new ExtrudeGeometry(shape, { depth: thicknessM, bevelEnabled: false }), 0);
  return { body, featureItems };
};

const clampValue = (value: number, min: number, max: number) => Math.min(max, Math.max(min, value));

export function SlabObject({
  slab,
  isSelected,
  onSelect,
  snapTargets,
  obstacles,
  supports,
  interactive = true,
  onCommitMove,
  penActive = false,
  onPenFaceClick,
  onPenFaceArc,
  onPenFaceFinish,
}: SlabObjectProps) {
  const { t } = useTranslation();
  const activeTool = useDesignerStore((s) => s.activeTool);
  const drawShape = useDesignerStore((s) => s.drawShape);
  const presentation = useDesignerStore((s) => s.presentationMode);
  const penFace = useDesignerStore((s) => s.penFace);
  const penMode = useDesignerStore((s) => s.penMode);
  const setPenFaceCursor = useDesignerStore((s) => s.setPenFaceCursor);
  const sceneRef = useDesignerStore((s) => s.scene);
  const multiSelection = useDesignerStore((s) => s.multiSelection);
  // Floor moves carry the walls/glass/roofs resting on them; commitSlabPatch persists the runs
  // that rode along (server entities — otherwise the next refetch snaps the glass back).
  const { commitSlabPatch: updateSlab } = useSlabEntityActions();
  const paintColor = useDesignerStore((s) => s.paintColor);
  const paintMaterial = useDesignerStore((s) => s.paintMaterial);
  const addSlabFeature = useDesignerStore((s) => s.addSlabFeature);
  const updateSlabFeature = useDesignerStore((s) => s.updateSlabFeature);
  const setSelection = useDesignerStore((s) => s.setSelection);
  const multiSiblingsRef = useRef<AttachedRunSnapshot[]>([]);
  const lengthM = slab.lengthMm / 1000;
  const depthM = slab.depthMm / 1000;
  const thicknessM = slab.thicknessMm / 1000;
  const elevationM = slab.elevationMm / 1000;

  const transformActive = useDesignerStore((s) => s.transformHandlesActive);
  // A plan-arc (geomArc*, curves like a wall), barrel (arcRise) or pitched (pitchRise) slab is a
  // non-flat sheet: its footprint stretch + corner handles don't apply (resized via the curve
  // handle / inspector). The plan curve bows along slabArcAxis (length or depth).
  const isArcSlab = isRealArc(slab.geomArcRadiusMm, slab.geomArcSweepDeg);
  const isBarrelRoof = (slab.arcRiseMm ?? 0) > 0 || (slab.pitchRiseMm ?? 0) > 0;
  const isShapedSlab = isArcSlab || isBarrelRoof;
  // The stretch TOOL stays live on shaped slabs — gating the whole toolset on flatness read as
  // "stretch is broken after any curve". Flat slabs keep all six faces + feature depths; shaped
  // slabs keep the curvature-independent thickness/elevation faces, and a PLAN-ARC slab also gets
  // the two chord-end faces (keep-sweep resize, like the wall/run ends).
  const stretchToolOn = activeTool === 'stretch' && interactive && !slab.locked;
  const stretchActive = stretchToolOn && !isShapedSlab;
  const shapedStretchActive = stretchToolOn && isShapedSlab;
  // Corner handles stay available on a PLAN-ARC slab (its logical rect is still authoritative — the
  // bent axis' chord ends sit exactly on the rect corners); resizing re-derives the radius for the
  // new chord below. Barrel/pitched (up-curve) slabs are resized via their rise instead.
  const vertexEditActive =
    transformActive && isSelected && interactive && !slab.locked && !isBarrelRoof;
  // The plan-arc (bow) handle: on a flat slab (to START a plan curve) or an already plan-curved one
  // (to re-adjust), but NOT a barrel/pitched (up-curve) slab — mutually exclusive.
  const curveEditActive =
    transformActive && isSelected && interactive && !slab.locked && !isBarrelRoof;
  // The bent axis' chord (world plan mm): for 'length' it runs along the slab's local X
  // (rotationDeg); for 'depth' along local Z (rotationDeg + 90°). Both ends stay FIXED while bowing.
  const slabArcAxis = slab.slabArcAxis ?? 'length';
  const slabRotRad = slab.rotationDeg * DEG2RAD;
  const chordLenMm = slabArcAxis === 'length' ? slab.lengthMm : slab.depthMm;
  const chordDirX = slabArcAxis === 'length' ? Math.cos(slabRotRad) : -Math.sin(slabRotRad);
  const chordDirY = slabArcAxis === 'length' ? Math.sin(slabRotRad) : Math.cos(slabRotRad);
  const slabChordEndX = slab.originX + chordLenMm * chordDirX;
  const slabChordEndY = slab.originY + chordLenMm * chordDirY;
  const slabCurrentSagittaMm = isArcSlab
    ? bowFromArc(chordLenMm, slab.geomArcRadiusMm ?? 0, slab.geomArcSweepDeg ?? 0)
    : 0;
  // The slab as the shared curvature writer sees it: the BENT AXIS is the chord (length or depth),
  // and the pose is SYMMETRIC — rotationDeg stays the axis direction because the mesh builder
  // (curvedSlabPlanColumnsMm) owns the mirror. Rolling it like a run/wall would swing the deck.
  const slabArcPose = (chordMm = chordLenMm): CurvablePose => ({
    lengthMm: chordMm,
    rotationDeg: slab.rotationDeg,
    geomArcRadiusMm: slab.geomArcRadiusMm,
    geomArcSweepDeg: slab.geomArcSweepDeg,
  });
  // Developed (s,c) frame of a plan-curved slab: ALL pen/draw/feature coordinates live in it, so
  // the surface never "behaves flat" — picks invert plan→(s,c), previews/cuts map (s,c)→plan.
  const arcFrame = useMemo(
    () =>
      isArcSlab
        ? curvedSlabFrame(
            slab.lengthMm,
            slab.depthMm,
            slab.geomArcRadiusMm ?? 0,
            slab.geomArcSweepDeg ?? 1,
            slabArcAxis,
          )
        : null,
    [
      isArcSlab,
      slab.lengthMm,
      slab.depthMm,
      slab.geomArcRadiusMm,
      slab.geomArcSweepDeg,
      slabArcAxis,
    ],
  );
  // WHY: always cut features even while stretching — suppressing the cut during the Stretch
  // tool (where depth is given) hid the recess/hole on the slab face until the tool was left.
  const { body, featureItems } = useMemo(() => buildSlabGeometries(slab, true), [slab]);
  useEffect(
    () => () => {
      body.dispose();
      for (const item of featureItems) item.geometry?.dispose();
    },
    [body, featureItems],
  );

  const groupRef = useRef<Group>(null);
  const bodyRef = useRef<Group>(null);
  const faceMeshRef = useRef<Mesh>(null);
  // WHY: during a captured pen drag R3F hands back the CAPTURE-TIME intersection whenever the fresh
  // ray misses the mesh, so a cursor that briefly leaves the slab face recorded a spike back to the
  // stroke origin. Re-pick against the body; a miss means SKIP the sample, never store a stale one.
  const freshFaceHitPoint = (e: ThreeEvent<PointerEvent>): Vector3 | null => {
    const mesh = faceMeshRef.current;
    if (!mesh) return e.point;
    FACE_PICK_RAYCASTER.ray.copy(e.ray);
    const hits = FACE_PICK_RAYCASTER.intersectObject(mesh, false);
    return hits.length > 0 ? hits[0].point : null;
  };
  const drawSessionRef = useRef<{
    x0: number;
    z0: number;
    side: 1 | -1;
    points: SceneWallFeaturePoint[];
  } | null>(null);
  const draftRef = useRef<DraftFeature | null>(null);
  const [draft, setDraftState] = useState<DraftFeature | null>(null);
  const planObstacles = obstacles ?? EMPTY_OBSTACLES;

  const setDraft = (value: DraftFeature | null) => {
    draftRef.current = value;
    setDraftState(value);
  };

  // Exclude co-moving multi-selection members too — their stale pre-move endpoints must not
  // act as snap targets while the group drags.
  const filteredTargets = useMemo<PlanSnapTargets>(() => {
    if (!snapTargets) return EMPTY_SNAP_TARGETS;
    const excluded = new Set<string>([slab.id]);
    if (multiSelectionHas(multiSelection, 'slab', slab.id)) {
      for (const id of multiSelection.runIds) excluded.add(id);
      for (const id of multiSelection.wallIds) excluded.add(id);
      for (const id of multiSelection.slabIds) excluded.add(id);
    }
    return filterSnapTargets(snapTargets, excluded);
  }, [snapTargets, slab.id, multiSelection]);

  const rad = slab.rotationDeg * DEG2RAD;
  const dirX = Math.cos(rad);
  const dirY = Math.sin(rad);
  const corner = (lx: number, ly: number): PlanPoint => ({
    x: slab.originX + lx * dirX - ly * dirY,
    y: slab.originY + lx * dirY + ly * dirX,
  });
  const moveProbes: PlanPoint[] = [
    corner(0, 0),
    corner(slab.lengthMm, 0),
    corner(slab.lengthMm, slab.depthMm),
    corner(0, slab.depthMm),
  ];

  const isMultiMember = multiSelectionHas(multiSelection, 'slab', slab.id);
  const multiBodiesRef = useRef<MultiMoveBodies>(EMPTY_MULTI_BODIES);
  const canStack = !isMultiMember;
  const supportFootprints = supports ?? EMPTY_OBSTACLES;
  // Explicit stack rests on whatever is overlapped; precise auto-stack on what's under the centre;
  // a plain drag keeps the slab's current elevation (fallback = its own base, never forced down).
  const baseElevMm = slab.elevationMm;
  // WHY the supports are filtered: a floor slab overlaps in plan EVERY wall standing on it, and
  // restElevationMm has no "is it above me" guard, so an Alt-drag (or the "stack on drop" toggle)
  // resolved the top of its own wall — 2.6 m up — and the plate teleported out from under the
  // scene. Cargo is not ground; a roof can still climb onto a taller wall beside it.
  const stackSupports = useMemo(
    () => stackableSupports(supportFootprints, slab.elevationMm + slab.thicknessMm),
    [supportFootprints, slab.elevationMm, slab.thicknessMm],
  );
  const restElevationAt = (dxMm: number, dyMm: number) =>
    restElevationMm(
      buildSlabFootprint(slab, dxMm, dyMm, slab.rotationDeg),
      stackSupports,
      baseElevMm,
    );
  const centerXMm = slab.originX + (slab.lengthMm / 2) * dirX - (slab.depthMm / 2) * dirY;
  const centerYMm = slab.originY + (slab.lengthMm / 2) * dirY + (slab.depthMm / 2) * dirX;
  // A slab is PLACED, not dropped: it keeps the height it was authored at and rises onto anything
  // it would otherwise pass through.
  //
  // WHY not the gravity resolver here (it was, and it produced two separate field reports): its
  // fallback is the GROUND. A floor plate is authored at −thickness so its top is flush with grade,
  // so the fallback ratcheted it up by its own thickness the instant it was nudged — everything
  // standing on it was then inside it, and users read the plate poking out of the ground as its
  // thickness having changed. A roof legitimately spans open air, so the same fallback slammed it
  // to the ground as soon as it left its walls. Neither body is falling.
  const centerRestAt = (dxMm: number, dyMm: number) =>
    liftToClearMm(
      buildSlabFootprint(slab, dxMm, dyMm, slab.rotationDeg),
      supportFootprints,
      baseElevMm,
      slab.thicknessMm,
    );
  const restingAtStart = restsOnSupportAtMm(
    buildSlabFootprint(slab, 0, 0, slab.rotationDeg),
    supportFootprints,
    baseElevMm,
    5,
  );

  // While co-moving a multi-selection, sibling members travel with this slab, so
  // their footprints must not register as collisions during the drag.
  const gestureObstacles = useMemo(() => {
    if (!isMultiMember) return planObstacles;
    const coMoving = new Set<string>([
      ...multiSelection.slabIds,
      ...multiSelection.runIds,
      ...multiSelection.wallIds,
    ]);
    return planObstacles.filter((o) => !coMoving.has(o.ownerId));
  }, [planObstacles, isMultiMember, multiSelection]);

  const adapter: PlanGestureAdapter = {
    originXMm: slab.originX,
    originYMm: slab.originY,
    rotationDeg: slab.rotationDeg,
    baseYM: elevationM,
    centerXMm,
    centerYMm,
    moveProbes,
    footprintAt: (dxMm, dyMm, rotationDeg) => {
      const own = buildSlabFootprint(slab, dxMm, dyMm, rotationDeg);
      if (rotationDeg !== slab.rotationDeg) return own;
      return [own, ...multiBodyFootprints(multiBodiesRef.current, dxMm, dyMm)];
    },
    altLiftYMAt: canStack ? (dxMm, dyMm) => restElevationAt(dxMm, dyMm) / 1000 : undefined,
    centerLiftYMAt: canStack ? (dxMm, dyMm) => centerRestAt(dxMm, dyMm) / 1000 : undefined,
    restingAtStart,
    // A roof/floor moves freely and rests on top of whatever it overlaps, so it can be dragged onto
    // another object instead of locking against it (the collision-slide that walls/runs still use).
    freeMove: canStack,
  };

  const gestures = useObjectGestures({
    adapter,
    groupRef,
    enabled: interactive && !slab.locked,
    selectedForDrag: isSelected && !slab.locked,
    snapTargets: filteredTargets,
    obstacles: gestureObstacles,
    onPick: () => onSelect(slab.id),
    onGestureStart: () => {
      multiSiblingsRef.current = isMultiMember
        ? captureMultiSnapshots(sceneRef, multiSelection, { kind: 'slab', id: slab.id })
        : [];
      // Exclusion and motion must be symmetric: gestureObstacles drops the co-movers, so the clamp
      // only sees them if they are added to the MOVING set too (WallObject's rule, shared here).
      multiBodiesRef.current = captureMultiBodies(sceneRef, multiSelection, {
        kind: 'slab',
        id: slab.id,
      });
    },
    onMovePreview: (delta) =>
      previewSnapshotsMove(multiSiblingsRef.current, delta.dxMm, delta.dyMm),
    onMoveCommit: (delta, meta) => {
      // A stack (explicit or precise centre-over) rests at stackElevMm; a plain lateral drag
      // (null) keeps the slab's current elevation.
      if (canStack && meta.stackElevMm !== null) {
        updateSlab(slab.id, {
          originX: Math.round(slab.originX + delta.dxMm),
          originY: Math.round(slab.originY + delta.dyMm),
          elevationMm: meta.stackElevMm,
        });
        return;
      }
      // WHY: an Alt-drag that cannot stack must say why — silently sliding sideways reads as a bug.
      if (isMultiMember && isAltPressed()) notifyStackUnavailable(t);
      onCommitMove?.(slab.id, delta);
    },
    onRotateCommit: (commit) =>
      updateSlab(slab.id, {
        rotationDeg: commit.rotationDeg,
        originX: commit.originX,
        originY: commit.originY,
      }),
  });

  const drawActive = interactive && activeTool === 'draw' && drawShape !== 'split';

  const localPointMm = (point: Vector3): { x: number; z: number; side: 1 | -1 } | null => {
    const group = groupRef.current;
    if (!group) return null;
    TMP_VEC.copy(point);
    group.worldToLocal(TMP_VEC);
    const side: 1 | -1 = TMP_VEC.y >= thicknessM / 2 ? 1 : -1;
    // ARC slab: invert the plan hit into the developed (s,c) frame — the raw plan coords range
    // outside the flat rect (the bulge) and would otherwise clamp to a phantom rectangle.
    if (arcFrame) {
      const sc = curvedSlabPickSc(arcFrame, TMP_VEC.x * 1000, TMP_VEC.z * 1000);
      return { x: sc.s, z: sc.c, side };
    }
    return { x: TMP_VEC.x * 1000, z: TMP_VEC.z * 1000, side };
  };

  const clampDrawPoint = (xMm: number, zMm: number): SceneWallFeaturePoint =>
    arcFrame
      ? {
          x: clampValue(xMm, FEATURE_EDGE_MARGIN_MM, arcFrame.developedMm - FEATURE_EDGE_MARGIN_MM),
          z: clampValue(zMm, FEATURE_EDGE_MARGIN_MM, arcFrame.acrossMm - FEATURE_EDGE_MARGIN_MM),
        }
      : {
          x: clampValue(xMm, FEATURE_EDGE_MARGIN_MM, slab.lengthMm - FEATURE_EDGE_MARGIN_MM),
          z: clampValue(zMm, FEATURE_EDGE_MARGIN_MM, slab.depthMm - FEATURE_EDGE_MARGIN_MM),
        };

  const commitDraft = (spec: DraftFeature) => {
    // Plan-arc slabs now carve + render features (#6b); only barrel/pitch remain deferred.
    if (isBarrelRoof) {
      queueToast({
        dedupeKey: 'glass-arc-no-feature',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.Pen.ArcNoFeature', {
          defaultValue: 'Kavisli yüzeye henüz açıklık/şekil çizilemiyor.',
        }),
      });
      return;
    }
    if (spec.widthMm < MIN_FEATURE_SIZE_MM || spec.heightMm < MIN_FEATURE_SIZE_MM) {
      queueToast({
        dedupeKey: 'glass-slab-feature-fit',
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
      const simplified = sanitizeFreeOutline(
        simplifyFreePoints(absolute, FREE_SIMPLIFY_TOLERANCE_MM),
      );
      if (!simplified) {
        queueToast({
          dedupeKey: 'glass-slab-feature-fit',
          variant: 'warning',
          description: t('GlassEnclosure.Designer.WallFeature.SelfIntersecting', {
            defaultValue: 'Çizim kendini kesiyor — şekli tek parça, kesişmeden çizin.',
          }),
        });
        return;
      }
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
      // Default a drawn shape to a NON-CUTTING outline (flush recess, no depth): it shows as a
      // line and the user chooses hole / recess+depth / protrusion in the inspector.
      mode: 'recess',
      side: spec.side,
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
    const fits = arcFrame
      ? outlineFitsRect(outline, arcFrame.developedMm, arcFrame.acrossMm, FEATURE_EDGE_MARGIN_MM)
      : outlineFitsRect(outline, slab.lengthMm, slab.depthMm, FEATURE_EDGE_MARGIN_MM);
    if (!fits) {
      queueToast({
        dedupeKey: 'glass-slab-feature-fit',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.WallFeature.DoesNotFitSlab', {
          defaultValue: 'Alan zemin/çatı sınırlarına sığmıyor.',
        }),
      });
      return;
    }
    addSlabFeature(slab.id, feature);
    setSelection({
      kind: 'slabFeature',
      runId: null,
      panelId: null,
      connectionId: null,
      hardwareId: null,
      wallId: null,
      slabId: slab.id,
      featureId: feature.id,
    });
  };

  const worldToLocalDelta = (dxMm: number, dzMm: number) => ({
    x: dxMm * dirX + dzMm * dirY,
    z: -dxMm * dirY + dzMm * dirX,
  });

  // ARC slab: linear plan deltas diverge from the cursor on the bend — track the latest MESH hit
  // in (s,c) instead (same pattern as the curved wall's draw fix).
  const drawArcScRef = useRef<SceneWallFeaturePoint | null>(null);

  const drawDrag = useDrag3D({
    constraint: { mode: 'ground' },
    enabled: drawActive,
    onMove: (delta) => {
      const session = drawSessionRef.current;
      if (!session) return;
      if (delta.x === 0 && delta.z === 0 && !(arcFrame && drawArcScRef.current)) {
        setDraft(null);
        return;
      }
      const local = worldToLocalDelta(delta.x, delta.z);
      const cur =
        arcFrame && drawArcScRef.current
          ? drawArcScRef.current
          : clampDrawPoint(session.x0 + local.x, session.z0 + local.z);
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
      const local = localPointMm(e.point);
      if (local) {
        const start = clampDrawPoint(local.x, local.z);
        drawSessionRef.current = { x0: start.x, z0: start.z, side: local.side, points: [start] };
        drawArcScRef.current = null;
      }
    }
    drawDrag.handlers.onPointerDown(e);
  };

  const handleDrawPointerMove = (e: ThreeEvent<PointerEvent>) => {
    if (arcFrame && drawSessionRef.current) {
      const local = localPointMm(e.point);
      if (local) drawArcScRef.current = clampDrawPoint(local.x, local.z);
    }
    drawDrag.handlers.onPointerMove(e);
  };

  const penFacePoint = (point: Vector3): { x: number; z: number; side: 1 | -1 } | null => {
    const local = localPointMm(point);
    if (!local) return null;
    const clamped = clampDrawPoint(local.x, local.z);
    return { x: clamped.x, z: clamped.z, side: local.side };
  };

  const penArcRef = useRef<{ active: boolean; end: { x: number; z: number } } | null>(null);
  const penFreehandRef = useRef(false);
  const penSuppressClickRef = useRef(false);
  const [penArcPreview, setPenArcPreview] = useState<{ x: number; z: number }[] | null>(null);
  const getThree = useThree((s) => s.get);
  const setOrbitEnabled = (value: boolean) => {
    const controls = getThree().controls as unknown as { enabled: boolean } | null;
    if (controls) controls.enabled = value;
  };

  const handlePenDown = (e: ThreeEvent<PointerEvent>) => {
    if (e.nativeEvent.button !== 0) return;
    // WHY here and not in PenController: see WallObject — that controller reads a hit as a PLAN
    // (x, z) position, which is wrong for anything but the ground. penFacePoint gives the slab's
    // own surface coordinates.
    if (penMode === 'freehand' && !isShiftPressed()) {
      const start = penFacePoint(e.point);
      if (!start) return;
      penFreehandRef.current = true;
      penSuppressClickRef.current = true;
      setOrbitEnabled(false);
      (e.target as Element | null)?.setPointerCapture?.(e.pointerId);
      onPenFaceClick?.('slab', slab.id, start.side === 1 ? 'front' : 'back', {
        x: start.x,
        z: start.z,
      });
      return;
    }
    const session = useDesignerStore.getState().penFace;
    if (!isShiftPressed() || !session || session.hostId !== slab.id || session.points.length < 1)
      return;
    const local = penFacePoint(e.point);
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
    if (penFreehandRef.current) {
      penFreehandRef.current = false;
      setOrbitEnabled(true);
      (e.target as Element | null)?.releasePointerCapture?.(e.pointerId);
      onPenFaceFinish?.();
      return;
    }
    const arc = penArcRef.current;
    penArcRef.current = null;
    setPenArcPreview(null);
    setOrbitEnabled(true);
    if (!arc?.active) return;
    (e.target as Element | null)?.releasePointerCapture?.(e.pointerId);
    const session = useDesignerStore.getState().penFace;
    const local = penFacePoint(e.point);
    if (!session || !local) return;
    penSuppressClickRef.current = true;
    const anchor = session.points[session.points.length - 1];
    const bulge = chordBulgeMm(
      { x: anchor.x, y: anchor.z },
      { x: arc.end.x, y: arc.end.z },
      { x: local.x, y: local.z },
    );
    const pts = tessellateArc(
      { x: anchor.x, y: anchor.z },
      { x: arc.end.x, y: arc.end.z },
      bulge,
    ).map((p) => ({ x: Math.round(p.x), z: Math.round(p.y) }));
    onPenFaceArc?.('slab', slab.id, session.side, pts);
  };

  const handlePenMove = (e: ThreeEvent<PointerEvent>) => {
    if (penFreehandRef.current) {
      const fresh = freshFaceHitPoint(e);
      const local = fresh ? penFacePoint(fresh) : null;
      if (!local) return;
      const session = useDesignerStore.getState().penFace;
      const last = session?.points[session.points.length - 1];
      if (last && Math.hypot(local.x - last.x, local.z - last.z) < PEN_FREE_STEP_MM) return;
      onPenFaceArc?.('slab', slab.id, local.side === 1 ? 'front' : 'back', [
        { x: Math.round(local.x), z: Math.round(local.z) },
      ]);
      return;
    }
    const arc = penArcRef.current;
    // Mid-drag samples are dropped on a miss; a plain hover has no capture to go stale.
    const point = freshFaceHitPoint(e) ?? (arc?.active ? null : e.point);
    const local = point ? penFacePoint(point) : null;
    if (!local) return;
    if (arc?.active) {
      const session = useDesignerStore.getState().penFace;
      const anchor = session?.points[session.points.length - 1];
      if (anchor) {
        const bulge = chordBulgeMm(
          { x: anchor.x, y: anchor.z },
          { x: arc.end.x, y: arc.end.z },
          { x: local.x, y: local.z },
        );
        const preview = tessellateArc(
          { x: anchor.x, y: anchor.z },
          { x: arc.end.x, y: arc.end.z },
          bulge,
        );
        setPenArcPreview([anchor, ...preview.map((p) => ({ x: p.x, z: p.y }))]);
        const { radiusMm, angleDeg } = arcMetricsFromBulge(
          { x: anchor.x, y: anchor.z },
          { x: arc.end.x, y: arc.end.z },
          bulge,
        );
        setDragReadout(
          radiusMm > 0
            ? `R ${Math.round(radiusMm)} mm · ${Math.round(angleDeg)}°`
            : `${Math.round(Math.hypot(arc.end.x - anchor.x, arc.end.z - anchor.z))} mm`,
        );
      }
      return;
    }
    if (e.nativeEvent.detail > 0) return;
    const session = useDesignerStore.getState().penFace;
    const anchor = session?.points[session.points.length - 1];
    if (anchor && session?.hostId === slab.id) {
      setDragReadout(`${Math.round(Math.hypot(local.x - anchor.x, local.z - anchor.z))} mm`);
    }
    setPenFaceCursor(slab.id, { x: local.x, z: local.z });
  };

  const handleClick = (event: ThreeEvent<MouseEvent>) => {
    event.stopPropagation();
    if (penActive) {
      if (penSuppressClickRef.current) {
        penSuppressClickRef.current = false;
        return;
      }
      if (event.nativeEvent.detail > 1) return;
      const local = penFacePoint(event.point);
      if (local)
        onPenFaceClick?.('slab', slab.id, local.side === 1 ? 'front' : 'back', {
          x: local.x,
          z: local.z,
        });
      return;
    }
    if (gestures.consumeClick() || drawDrag.consumeClick()) return;
    if (activeTool === 'draw') return;
    if (activeTool === 'paint' && interactive) {
      if (paintMaterial) updateSlab(slab.id, { materialKey: paintMaterial, colorHex: null });
      else if (paintColor) updateSlab(slab.id, { colorHex: paintColor.hex, materialKey: null });
      return;
    }
    onSelect(slab.id);
  };

  const slabHandlers = penActive
    ? {
        onPointerDown: handlePenDown,
        onPointerMove: handlePenMove,
        onPointerUp: handlePenUp,
        onPointerCancel: cancelPenArc,
        onDoubleClick: () => onPenFaceFinish?.(),
      }
    : drawActive
      ? {
          onPointerDown: handleDrawPointerDown,
          onPointerMove: handleDrawPointerMove,
          onPointerUp: drawDrag.handlers.onPointerUp,
          onPointerCancel: drawDrag.handlers.onPointerCancel,
        }
      : gestures.handlers;

  const penLine = useMemo<[number, number, number][] | null>(() => {
    if (!penActive || !penFace || penFace.hostId !== slab.id) return null;
    const pts = penArcPreview ? [...penArcPreview] : [...penFace.points];
    if (!penArcPreview && penFace.cursor) pts.push(penFace.cursor);
    if (pts.length < 1) return null;
    const yFace = penFace.side === 'front' ? thicknessM + FACE_LIFT_M : -FACE_LIFT_M;
    // ARC slab: points are in (s,c) — forward-map onto the band so the line follows the curve.
    if (arcFrame) {
      return pts.map((p): [number, number, number] => {
        const plan = curvedSlabPointAt(arcFrame, p.x, p.z);
        return [plan.x / 1000, yFace, plan.z / 1000];
      });
    }
    return pts.map((p): [number, number, number] => [p.x / 1000, yFace, p.z / 1000]);
  }, [penActive, penFace, penArcPreview, slab.id, thicknessM, arcFrame]);

  // ARC slab draw preview: the flat SlabDraftPreview would hover off the band — render the draft
  // outline forward-mapped onto the curved surface instead (commit is exact; this matches it).
  const arcDraftLine = useMemo<[number, number, number][] | null>(() => {
    if (!draft || !arcFrame) return null;
    const outline = featureOutlineMm(draft);
    if (outline.length < 3) return null;
    const yFace = draft.side === 1 ? thicknessM + FACE_LIFT_M : -FACE_LIFT_M;
    const pts = curvedSlabMapOutlineMm(arcFrame, outline).map((p): [number, number, number] => [
      p.x / 1000,
      yFace,
      p.z / 1000,
    ]);
    pts.push(pts[0]);
    return pts;
  }, [draft, arcFrame, thicknessM]);

  const stickyDelta = (base: number, deltaMm: number) => stickyDimensionMm(base + deltaMm) - base;
  const heightLevels = collectHeightLevels(sceneRef, slab.id);
  const levelDelta = (base: number, deltaMm: number) =>
    snapToLevels(base + deltaMm, heightLevels) - base;
  const topDelta = (deltaMm: number) =>
    snapToLevels(slab.elevationMm + slab.thicknessMm + deltaMm, heightLevels) -
    (slab.elevationMm + slab.thicknessMm);

  const resetBody = () => {
    bodyRef.current?.scale.set(1, 1, 1);
    bodyRef.current?.position.set(0, 0, 0);
  };
  useLayoutEffect(
    () => resetBody(),
    [slab.lengthMm, slab.depthMm, slab.thicknessMm, slab.elevationMm],
  );

  const previewLength = (deltaMm: number, fromStart: boolean) => {
    const scale = Math.max(0.05, (slab.lengthMm + deltaMm) / slab.lengthMm);
    setBodyPreview(bodyRef, [scale, 1, 1], [fromStart ? -deltaMm / 1000 : 0, 0, 0]);
  };

  const previewDepth = (deltaMm: number, fromNear: boolean) => {
    const scale = Math.max(0.05, (slab.depthMm + deltaMm) / slab.depthMm);
    setBodyPreview(bodyRef, [1, 1, scale], [0, 0, fromNear ? -deltaMm / 1000 : 0]);
  };

  const previewThickness = (deltaMm: number) => {
    const scale = Math.max(0.05, (slab.thicknessMm + deltaMm) / slab.thicknessMm);
    setBodyPreview(bodyRef, [1, scale, 1], [0, 0, 0]);
  };

  const previewElevation = (deltaMm: number) => {
    setBodyPreview(bodyRef, [1, 1, 1], [0, -deltaMm / 1000, 0]);
  };

  const commitLength = (deltaMm: number, fromStart: boolean) => {
    const target = stickyDelta(slab.lengthMm, deltaMm);
    const clamped = clampPlanStretch(
      (d) =>
        buildSlabFootprint(
          { ...slab, lengthMm: slab.lengthMm + d },
          fromStart ? -d * dirX : 0,
          fromStart ? -d * dirY : 0,
          slab.rotationDeg,
        ),
      planObstacles,
      target,
    );
    const next = Math.max(MIN_PLAN_MM, Math.round(slab.lengthMm + clamped));
    if (next === slab.lengthMm) {
      resetBody();
      return;
    }
    if (!fromStart) {
      updateSlab(slab.id, { lengthMm: next });
      return;
    }
    const shift = next - slab.lengthMm;
    updateSlab(slab.id, {
      lengthMm: next,
      originX: Math.round(slab.originX - shift * dirX),
      originY: Math.round(slab.originY - shift * dirY),
    });
  };

  const commitDepth = (deltaMm: number, fromNear: boolean) => {
    const target = stickyDelta(slab.depthMm, deltaMm);
    const clamped = clampPlanStretch(
      (d) =>
        buildSlabFootprint(
          { ...slab, depthMm: slab.depthMm + d },
          fromNear ? d * dirY : 0,
          fromNear ? -d * dirX : 0,
          slab.rotationDeg,
        ),
      planObstacles,
      target,
    );
    const next = Math.max(MIN_PLAN_MM, Math.round(slab.depthMm + clamped));
    if (next === slab.depthMm) {
      resetBody();
      return;
    }
    if (!fromNear) {
      updateSlab(slab.id, { depthMm: next });
      return;
    }
    const shift = next - slab.depthMm;
    updateSlab(slab.id, {
      depthMm: next,
      originX: Math.round(slab.originX + shift * dirY),
      originY: Math.round(slab.originY - shift * dirX),
    });
  };

  const commitThickness = (deltaMm: number) => {
    const target = topDelta(deltaMm);
    const clamped = clampPlanStretch(
      (d) =>
        buildSlabFootprint({ ...slab, thicknessMm: slab.thicknessMm + d }, 0, 0, slab.rotationDeg),
      planObstacles,
      target,
    );
    const next = Math.max(MIN_THICKNESS_MM, Math.round(slab.thicknessMm + clamped));
    if (next === slab.thicknessMm) {
      resetBody();
      return;
    }
    updateSlab(slab.id, { thicknessMm: next });
  };

  const commitElevation = (deltaMm: number) => {
    const target = levelDelta(slab.elevationMm, -deltaMm);
    const clamped = clampPlanStretch(
      (d) =>
        buildSlabFootprint({ ...slab, elevationMm: slab.elevationMm + d }, 0, 0, slab.rotationDeg),
      planObstacles,
      target,
    );
    const next = Math.round(slab.elevationMm + clamped);
    if (next === slab.elevationMm) {
      resetBody();
      return;
    }
    updateSlab(slab.id, { elevationMm: next });
  };

  const featureSignedDepthMm = (feature: SceneWallFeature) => {
    if (feature.mode === 'protrude') return feature.depthMm;
    if (feature.mode === 'hole') return -slab.thicknessMm;
    return -feature.depthMm;
  };

  const previewArcChord = (deltaMm: number, fromStart: boolean) => {
    const isLength = slabArcAxis === 'length';
    const current = isLength ? slab.lengthMm : slab.depthMm;
    const scale = Math.max(0.05, (current + deltaMm) / current);
    // Scale ONLY the bent axis — the commit leaves the perpendicular plan dimension untouched, so
    // a uniform [s,1,s] scale on a metres-wide slab previews the wrong axis growing (then snapping
    // back). The bulge flattens slightly in preview; the commit's geometry is exact.
    const scaleVec: [number, number, number] = isLength ? [scale, 1, 1] : [1, 1, scale];
    const offset: [number, number, number] = fromStart
      ? isLength
        ? [-deltaMm / 1000, 0, 0]
        : [0, 0, -deltaMm / 1000]
      : [0, 0, 0];
    setBodyPreview(bodyRef, scaleVec, offset);
  };

  const commitArcChord = (deltaMm: number, fromStart: boolean) => {
    const isLength = slabArcAxis === 'length';
    const current = isLength ? slab.lengthMm : slab.depthMm;
    const next = Math.max(MIN_PLAN_MM, Math.round(current + stickyDelta(current, deltaMm)));
    if (next === current) {
      resetBody();
      return;
    }
    const arc = commitArcOrWarn(slabArcPose(), { kind: 'chordResize', chordMm: next }, t, {
      pose: 'symmetric',
    });
    if (!arc) {
      resetBody();
      return;
    }
    const shift = fromStart ? next - current : 0;
    const patch: Partial<SceneSlabState> = {
      ...(isLength ? { lengthMm: next } : { depthMm: next }),
      geomArcRadiusMm: arc.geomArcRadiusMm,
      geomArcSweepDeg: arc.geomArcSweepDeg,
      ...(fromStart
        ? isLength
          ? {
              originX: Math.round(slab.originX - shift * dirX),
              originY: Math.round(slab.originY - shift * dirY),
            }
          : {
              originX: Math.round(slab.originX + shift * dirY),
              originY: Math.round(slab.originY - shift * dirX),
            }
        : {}),
    };
    const candidate = { ...slab, ...patch };
    if (penetratesAny(buildSlabFootprint(candidate, 0, 0, slab.rotationDeg), planObstacles)) {
      resetBody();
      return;
    }
    updateSlab(slab.id, patch);
  };

  const commitFeatureDepth = (feature: SceneWallFeature, deltaMm: number) => {
    const thicknessMm = slab.thicknessMm;
    const signed = stickyDimensionMm(featureSignedDepthMm(feature) + deltaMm);
    if (signed <= -(thicknessMm - HOLE_THRESHOLD_MM)) {
      updateSlabFeature(slab.id, feature.id, { mode: 'hole', depthMm: thicknessMm });
    } else if (signed < 0) {
      updateSlabFeature(slab.id, feature.id, { mode: 'recess', depthMm: -signed });
    } else if (signed > 0) {
      updateSlabFeature(slab.id, feature.id, { mode: 'protrude', depthMm: signed });
    } else {
      updateSlabFeature(slab.id, feature.id, { mode: 'recess', depthMm: 0 });
    }
  };

  const labelMm = (value: number) => `${Math.round(value)} mm`;
  const lengthLabel = (d: number) =>
    labelMm(Math.max(MIN_PLAN_MM, slab.lengthMm + stickyDelta(slab.lengthMm, d)));
  const depthLabel = (d: number) =>
    labelMm(Math.max(MIN_PLAN_MM, slab.depthMm + stickyDelta(slab.depthMm, d)));
  const thicknessLabel = (d: number) =>
    labelMm(Math.max(MIN_THICKNESS_MM, slab.thicknessMm + topDelta(d)));
  const elevationLabel = (d: number) =>
    labelMm(slab.elevationMm + levelDelta(slab.elevationMm, -d));
  const featureDepthLabel = (feature: SceneWallFeature) => (d: number) => {
    const signed = stickyDimensionMm(featureSignedDepthMm(feature) + d);
    if (signed <= -(slab.thicknessMm - HOLE_THRESHOLD_MM)) {
      return t('GlassEnclosure.Designer.Tool.ModeHole', { defaultValue: 'Boşluk (delik)' });
    }
    return labelMm(Math.abs(signed));
  };

  const materialTexture = useTiledProceduralTexture(
    slab.materialKey,
    slab.lengthMm / 500,
    slab.depthMm / 500,
  );

  const stretchFaces: StretchFaceDef[] = stretchActive
    ? [
        {
          id: 'far-x',
          centerM: [lengthM + FACE_LIFT_M, thicknessM / 2, depthM / 2],
          rotation: [0, HALF_PI, 0],
          widthM: depthM,
          heightM: thicknessM,
          axis: [1, 0, 0],
          label: lengthLabel,
          onPreview: (d) => previewLength(stickyDelta(slab.lengthMm, d), false),
          onCommit: (d) => commitLength(d, false),
        },
        {
          id: 'near-x',
          centerM: [-FACE_LIFT_M, thicknessM / 2, depthM / 2],
          rotation: [0, -HALF_PI, 0],
          widthM: depthM,
          heightM: thicknessM,
          axis: [-1, 0, 0],
          label: lengthLabel,
          onPreview: (d) => previewLength(stickyDelta(slab.lengthMm, d), true),
          onCommit: (d) => commitLength(d, true),
        },
        {
          id: 'far-z',
          centerM: [lengthM / 2, thicknessM / 2, depthM + FACE_LIFT_M],
          rotation: [0, 0, 0],
          widthM: lengthM,
          heightM: thicknessM,
          axis: [0, 0, 1],
          label: depthLabel,
          onPreview: (d) => previewDepth(stickyDelta(slab.depthMm, d), false),
          onCommit: (d) => commitDepth(d, false),
        },
        {
          id: 'near-z',
          centerM: [lengthM / 2, thicknessM / 2, -FACE_LIFT_M],
          rotation: [0, Math.PI, 0],
          widthM: lengthM,
          heightM: thicknessM,
          axis: [0, 0, -1],
          label: depthLabel,
          onPreview: (d) => previewDepth(stickyDelta(slab.depthMm, d), true),
          onCommit: (d) => commitDepth(d, true),
        },
        {
          id: 'top',
          centerM: [lengthM / 2, thicknessM + FACE_LIFT_M, depthM / 2],
          rotation: [-HALF_PI, 0, 0],
          widthM: lengthM,
          heightM: depthM,
          axis: [0, 1, 0],
          label: thicknessLabel,
          onPreview: (d) => previewThickness(topDelta(d)),
          onCommit: commitThickness,
        },
        {
          id: 'bottom',
          centerM: [lengthM / 2, -FACE_LIFT_M, depthM / 2],
          rotation: [HALF_PI, 0, 0],
          widthM: lengthM,
          heightM: depthM,
          axis: [0, -1, 0],
          label: elevationLabel,
          onPreview: (d) => previewElevation(-levelDelta(slab.elevationMm, -d)),
          onCommit: commitElevation,
        },
        ...featureItems.map(({ feature, bounds }): StretchFaceDef => {
          const signedDepthMm = featureSignedDepthMm(feature);
          const s = featureSideSignZ(feature.side);
          const outwardM = Math.max(signedDepthMm, 0) / 1000 + FEATURE_FACE_LIFT_M;
          const faceY = s === 1 ? thicknessM + outwardM : -outwardM;
          return {
            id: `feature-${feature.id}`,
            centerM: [
              (bounds.minX + bounds.maxX) / 2000,
              faceY,
              (bounds.minZ + bounds.maxZ) / 2000,
            ],
            rotation: s === 1 ? [-HALF_PI, 0, 0] : [HALF_PI, 0, 0],
            widthM: (bounds.maxX - bounds.minX) / 1000,
            heightM: (bounds.maxZ - bounds.minZ) / 1000,
            axis: [0, s, 0],
            label: featureDepthLabel(feature),
            onPreview: () => {},
            onCommit: (d) => commitFeatureDepth(feature, d),
          };
        }),
      ]
    : [];

  // Shaped (plan-arc / barrel / pitched) slabs keep the curvature-independent thickness +
  // elevation faces (the top plane lifts above the rise so the raised body can't occlude it);
  // a PLAN-ARC slab additionally gets its two chord-end faces as a keep-sweep resize.
  const shapeRiseM = Math.max(slab.arcRiseMm ?? 0, slab.pitchRiseMm ?? 0) / 1000;
  // A PLAN-ARC slab bends AWAY from the flat rect (for one sweep sign the whole band is outside
  // it), so the grab faces must sit on the ACTUAL bent band via curvedSlabPointAt — anchored on
  // the rect they float in empty space. Barrel/pitch slabs are flat in plan, so they keep the rect.
  const bandAnchors = arcFrame
    ? {
        start: curvedSlabPointAt(arcFrame, 0, arcFrame.acrossMm / 2),
        end: curvedSlabPointAt(arcFrame, arcFrame.developedMm, arcFrame.acrossMm / 2),
        apex: curvedSlabPointAt(arcFrame, arcFrame.developedMm / 2, arcFrame.acrossMm / 2),
      }
    : null;
  const arcChordEndFaces: StretchFaceDef[] =
    isArcSlab && bandAnchors
      ? slabArcAxis === 'length'
        ? [
            {
              id: 'arc-end',
              centerM: [bandAnchors.end.x / 1000, thicknessM / 2, bandAnchors.end.z / 1000],
              rotation: [0, HALF_PI, 0],
              widthM: depthM,
              heightM: thicknessM,
              axis: [1, 0, 0],
              label: lengthLabel,
              onPreview: (d) => previewArcChord(stickyDelta(slab.lengthMm, d), false),
              onCommit: (d) => commitArcChord(d, false),
            },
            {
              id: 'arc-start',
              centerM: [bandAnchors.start.x / 1000, thicknessM / 2, bandAnchors.start.z / 1000],
              rotation: [0, -HALF_PI, 0],
              widthM: depthM,
              heightM: thicknessM,
              axis: [-1, 0, 0],
              label: lengthLabel,
              onPreview: (d) => previewArcChord(stickyDelta(slab.lengthMm, d), true),
              onCommit: (d) => commitArcChord(d, true),
            },
          ]
        : [
            {
              id: 'arc-end',
              centerM: [bandAnchors.end.x / 1000, thicknessM / 2, bandAnchors.end.z / 1000],
              rotation: [0, 0, 0],
              widthM: lengthM,
              heightM: thicknessM,
              axis: [0, 0, 1],
              label: depthLabel,
              onPreview: (d) => previewArcChord(stickyDelta(slab.depthMm, d), false),
              onCommit: (d) => commitArcChord(d, false),
            },
            {
              id: 'arc-start',
              centerM: [bandAnchors.start.x / 1000, thicknessM / 2, bandAnchors.start.z / 1000],
              rotation: [0, Math.PI, 0],
              widthM: lengthM,
              heightM: thicknessM,
              axis: [0, 0, -1],
              label: depthLabel,
              onPreview: (d) => previewArcChord(stickyDelta(slab.depthMm, d), true),
              onCommit: (d) => commitArcChord(d, true),
            },
          ]
      : [];
  const shapedTopCenter: [number, number, number] = bandAnchors
    ? [bandAnchors.apex.x / 1000, thicknessM + shapeRiseM + FACE_LIFT_M, bandAnchors.apex.z / 1000]
    : [lengthM / 2, thicknessM + shapeRiseM + FACE_LIFT_M, depthM / 2];
  const shapedBottomCenter: [number, number, number] = bandAnchors
    ? [bandAnchors.apex.x / 1000, -FACE_LIFT_M, bandAnchors.apex.z / 1000]
    : [lengthM / 2, -FACE_LIFT_M, depthM / 2];
  const shapedStretchFaces: StretchFaceDef[] = shapedStretchActive
    ? [
        {
          id: 'top',
          centerM: shapedTopCenter,
          rotation: [-HALF_PI, 0, 0],
          widthM: lengthM,
          heightM: depthM,
          axis: [0, 1, 0],
          label: thicknessLabel,
          onPreview: (d) => previewThickness(topDelta(d)),
          onCommit: commitThickness,
        },
        {
          id: 'bottom',
          centerM: shapedBottomCenter,
          rotation: [HALF_PI, 0, 0],
          widthM: lengthM,
          heightM: depthM,
          axis: [0, -1, 0],
          label: elevationLabel,
          onPreview: (d) => previewElevation(-levelDelta(slab.elevationMm, -d)),
          onCommit: commitElevation,
        },
        ...arcChordEndFaces,
      ]
    : [];

  const setGroupRef = (group: Group | null) => {
    groupRef.current = group;
    registerSceneRef(slab.id, group);
  };

  return (
    <>
      <group
        ref={setGroupRef}
        position={[slab.originX / 1000, elevationM, slab.originY / 1000]}
        rotation={[0, -slab.rotationDeg * DEG2RAD, 0]}
      >
        <group ref={bodyRef}>
          <mesh
            ref={faceMeshRef}
            geometry={body}
            castShadow
            receiveShadow
            {...slabHandlers}
            onClick={handleClick}
            onPointerOver={(e) => {
              e.stopPropagation();
              document.body.style.cursor = penActive || drawActive ? 'crosshair' : 'pointer';
            }}
            onPointerOut={() => {
              document.body.style.cursor = 'auto';
            }}
          >
            <meshStandardMaterial
              key={materialTexture ? (slab.materialKey ?? 'plain') : 'plain'}
              color={
                materialTexture
                  ? '#ffffff'
                  : (slab.colorHex ?? (slab.kind === 'roof' ? ROOF_COLOR : FLOOR_COLOR))
              }
              map={materialTexture ?? undefined}
              roughness={0.85}
              metalness={0.05}
              side={isArcSlab ? DoubleSide : undefined}
            />
            {!presentation && (
              <Edges
                color={
                  isSelected
                    ? SELECTED_EDGE
                    : edgeColorFor(materialTexture ? null : slab.colorHex, SLAB_EDGE)
                }
                threshold={15}
              />
            )}
          </mesh>
        </group>
        {featureItems.map((item) => (
          <SlabFeatureObject
            key={item.feature.id}
            slab={slab}
            item={item}
            fallbackColor={slab.colorHex ?? (slab.kind === 'roof' ? ROOF_COLOR : FLOOR_COLOR)}
            fallbackMap={materialTexture}
            interactive={interactive}
            thicknessM={thicknessM}
            presentation={presentation}
            boundsAlongMm={arcFrame ? arcFrame.developedMm : slab.lengthMm}
            boundsAcrossMm={arcFrame ? arcFrame.acrossMm : slab.depthMm}
            worldToLocalDelta={
              arcFrame
                ? (dxMm: number, dzMm: number) => {
                    // Convert the plan drag into an EXACT (s,c) delta by inverse-mapping the
                    // feature's anchor moved by the plan delta (linear addition drifts on a bend).
                    const local = worldToLocalDelta(dxMm, dzMm);
                    const anchor = curvedSlabPointAt(
                      arcFrame,
                      item.feature.offsetMm,
                      item.feature.centerZMm,
                    );
                    const sc = curvedSlabPickSc(arcFrame, anchor.x + local.x, anchor.z + local.z);
                    return {
                      x: sc.s - item.feature.offsetMm,
                      z: sc.c - item.feature.centerZMm,
                    };
                  }
                : worldToLocalDelta
            }
          />
        ))}
        {draft && !arcFrame && <SlabDraftPreview draft={draft} thicknessM={thicknessM} />}
        {arcDraftLine && (
          <Line points={arcDraftLine} color={REGION_COLOR} lineWidth={2} raycast={() => null} />
        )}
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
        {shapedStretchActive && <StretchFaces faces={shapedStretchFaces} />}
      </group>
      {vertexEditActive && (
        <FootprintCornerHandles
          box={{
            // WHY: a slab's depth extends one-sided (local 0..depth), but boxCornersMm centres the
            // cross on ±depth/2 (correct for walls/runs, whose body is centred). Shift the box
            // origin onto the depth centreline so the handles land on the slab's real corners —
            // same offset buildSlabFootprint uses for the (proven) collision footprint.
            originX: slab.originX - Math.sin(slab.rotationDeg * DEG2RAD) * (slab.depthMm / 2),
            originY: slab.originY + Math.cos(slab.rotationDeg * DEG2RAD) * (slab.depthMm / 2),
            lengthMm: slab.lengthMm,
            crossMm: slab.depthMm,
            rotationDeg: slab.rotationDeg,
          }}
          topYM={(slab.elevationMm + slab.thicknessMm) / 1000}
          previewOutline={
            isArcSlab
              ? (next) => {
                  // Preview the ACTUAL curved band the commit would produce (re-derived radius on
                  // the new chord), not a dashed phantom rectangle detached from the bow.
                  const nr = next.rotationDeg * DEG2RAD;
                  const cosN = Math.cos(nr);
                  const sinN = Math.sin(nr);
                  const backHalf = next.crossMm / 2;
                  const nextOriginX = next.originX + Math.sin(nr) * backHalf;
                  const nextOriginY = next.originY - Math.cos(nr) * backHalf;
                  const bentChordMm = slabArcAxis === 'length' ? next.lengthMm : next.crossMm;
                  // Preview through the WRITER so the dashed band is byte-for-byte what a release
                  // would store.
                  const previewArc = arcCommitKeepingEnds(
                    slabArcPose(Math.max(100, bentChordMm)),
                    { kind: 'chordResize', chordMm: Math.max(100, bentChordMm) },
                    { pose: 'symmetric' },
                  ).patch;
                  const outline = curvedSlabPlanOutlineMm(
                    next.lengthMm,
                    next.crossMm,
                    previewArc?.geomArcRadiusMm ?? slab.geomArcRadiusMm ?? 0,
                    previewArc?.geomArcSweepDeg ?? slab.geomArcSweepDeg ?? 1,
                    slabArcAxis,
                  );
                  const topY = (slab.elevationMm + slab.thicknessMm) / 1000;
                  const pts = outline.map((p): [number, number, number] => [
                    (nextOriginX + p.x * cosN - p.z * sinN) / 1000,
                    topY,
                    (nextOriginY + p.x * sinN + p.z * cosN) / 1000,
                  ]);
                  pts.push(pts[0]);
                  return pts;
                }
              : undefined
          }
          onCommit={(next) => {
            // Convert the centreline box origin back to the slab's one-sided (corner) origin.
            const nr = next.rotationDeg * DEG2RAD;
            const backHalf = next.crossMm / 2;
            const originX = Math.round(next.originX + Math.sin(nr) * backHalf);
            const originY = Math.round(next.originY - Math.cos(nr) * backHalf);
            // A corner resize on a plan-arc slab changes the bent axis' CHORD: the shared writer
            // keeps the curl angle (sweep) and re-derives the radius so the arc ends stay pinned to
            // the new corners — the same rule the wall/run end handles use.
            const bentChordMm = slabArcAxis === 'length' ? next.lengthMm : next.crossMm;
            let arcPatch: Partial<SceneSlabState> = {};
            if (isArcSlab) {
              const arc = commitArcOrWarn(
                slabArcPose(bentChordMm),
                { kind: 'chordResize', chordMm: bentChordMm },
                t,
                { pose: 'symmetric' },
              );
              if (!arc) return;
              arcPatch = {
                geomArcRadiusMm: arc.geomArcRadiusMm,
                geomArcSweepDeg: arc.geomArcSweepDeg,
              };
            }
            // Reject a corner resize that would grow the slab into a neighbour — checked against
            // the SAME band that will be committed (the re-derived radius), not the stale one.
            const resized = buildSlabFootprint(
              {
                ...slab,
                originX,
                originY,
                lengthMm: next.lengthMm,
                depthMm: next.crossMm,
                ...arcPatch,
              },
              0,
              0,
              next.rotationDeg,
            );
            if (penetratesAny(resized, planObstacles)) return;
            updateSlab(slab.id, {
              originX,
              originY,
              lengthMm: next.lengthMm,
              depthMm: next.crossMm,
              ...arcPatch,
            });
          }}
        />
      )}
      {curveEditActive && (
        <ArcSweepHandle
          startX={slab.originX}
          startY={slab.originY}
          endX={slabChordEndX}
          endY={slabChordEndY}
          currentSagittaMm={slabCurrentSagittaMm}
          topYM={(slab.elevationMm + slab.thicknessMm) / 1000}
          onCommit={(sagittaMm) => {
            // SYMMETRIC plan bow: the bent axis' two ends stay FIXED and rotationDeg is UNTOUCHED
            // (the mesh builder owns the mirror). The shared writer supplies the quantised sweep,
            // the chord-consistent radius, the straighten deadzone and the server-radius guard;
            // the sign pair (+sagitta → NEGATIVE sweep) is the same contract runs/walls use, so the
            // re-grab handle rests on the visible apex and the deck bows on the dragged side.
            // Features SURVIVE the curve (#6b): the arc branch carves + renders them in the
            // developed (s,c) frame, so nothing needs dropping.
            const arc = commitArcOrWarn(slabArcPose(), { kind: 'bow', sagittaMm }, t, {
              pose: 'symmetric',
            });
            if (!arc) return;
            updateSlab(slab.id, {
              geomArcRadiusMm: arc.geomArcRadiusMm,
              geomArcSweepDeg: arc.geomArcSweepDeg,
              ...(arc.geomArcRadiusMm === null
                ? {}
                : { slabArcAxis, arcRiseMm: null, pitchRiseMm: null }),
            });
          }}
        />
      )}
    </>
  );
}

interface SlabFeatureObjectProps {
  slab: SceneSlabState;
  item: SlabFeatureItem;
  fallbackColor: string;
  fallbackMap: Texture | null;
  interactive: boolean;
  thicknessM: number;
  presentation: boolean;
  // Feature-space bounds for the move clamp: the flat rect on a flat slab, the DEVELOPED
  // (s ∈ developedMm, c ∈ acrossMm) domain on a plan-arc slab.
  boundsAlongMm: number;
  boundsAcrossMm: number;
  worldToLocalDelta: (dxMm: number, dzMm: number) => { x: number; z: number };
}

function SlabFeatureObject({
  slab,
  item,
  fallbackColor,
  fallbackMap,
  interactive,
  thicknessM,
  presentation,
  boundsAlongMm,
  boundsAcrossMm,
  worldToLocalDelta,
}: SlabFeatureObjectProps) {
  const { feature, outline, geometry } = item;
  const activeTool = useDesignerStore((s) => s.activeTool);
  const paintColor = useDesignerStore((s) => s.paintColor);
  const isSelected = useDesignerStore(
    (s) => s.selection.kind === 'slabFeature' && s.selection.featureId === feature.id,
  );
  const setSelection = useDesignerStore((s) => s.setSelection);
  const updateSlabFeature = useDesignerStore((s) => s.updateSlabFeature);
  const removeSlabFeature = useDesignerStore((s) => s.removeSlabFeature);

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
  const faceY = feature.side === 1 ? thicknessM + FACE_LIFT_M : -FACE_LIFT_M;
  const regionPoints = useMemo<[number, number, number][]>(() => {
    const points = outline.map((p): [number, number, number] => [p.x / 1000, faceY, p.z / 1000]);
    if (points.length > 1) points.push(points[0]);
    return points;
  }, [outline, faceY]);

  const clampMove = (dxMm: number, dzMm: number) => {
    const bounds = item.bounds;
    return {
      x: Math.round(
        Math.min(
          boundsAlongMm - FEATURE_EDGE_MARGIN_MM - bounds.maxX,
          Math.max(FEATURE_EDGE_MARGIN_MM - bounds.minX, dxMm),
        ),
      ),
      z: Math.round(
        Math.min(
          boundsAcrossMm - FEATURE_EDGE_MARGIN_MM - bounds.maxZ,
          Math.max(FEATURE_EDGE_MARGIN_MM - bounds.minZ, dzMm),
        ),
      ),
    };
  };

  const moveEnabled = interactive && activeTool === 'move';
  const drag = useDrag3D({
    constraint: { mode: 'ground' },
    enabled: moveEnabled,
    onMove: (delta) => {
      const local = worldToLocalDelta(delta.x, delta.z);
      const clamped = clampMove(local.x, local.z);
      lastDeltaRef.current = clamped;
      previewRef.current?.position.set(clamped.x / 1000, 0, clamped.z / 1000);
    },
    onCommit: () => {
      const clamped = lastDeltaRef.current;
      lastDeltaRef.current = { x: 0, z: 0 };
      if (clamped.x !== 0 || clamped.z !== 0) {
        updateSlabFeature(slab.id, feature.id, {
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
      kind: 'slabFeature',
      runId: null,
      panelId: null,
      connectionId: null,
      hardwareId: null,
      wallId: null,
      slabId: slab.id,
      featureId: feature.id,
    });

  const handleClick = (e: ThreeEvent<MouseEvent>) => {
    e.stopPropagation();
    if (drag.consumeClick()) return;
    if (!interactive) return;
    if (activeTool === 'erase') {
      removeSlabFeature(slab.id, feature.id);
      return;
    }
    if (activeTool === 'paint') {
      if (paintColor) updateSlabFeature(slab.id, feature.id, { colorHex: paintColor.hex });
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
            <Edges
              color={
                isSelected
                  ? SELECTED_EDGE
                  : edgeColorFor(featureMap ? null : (feature.colorHex ?? slab.colorHex), SLAB_EDGE)
              }
              threshold={15}
            />
          )}
        </mesh>
      );
    }
    return (
      <Line
        points={regionPoints}
        color={feature.colorHex ?? REGION_COLOR}
        lineWidth={1.5}
        raycast={() => null}
      />
    );
  }

  return (
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
            emissive={isSelected ? SELECTED_EDGE : '#000000'}
            emissiveIntensity={isSelected ? 0.15 : 0}
          />
          {!presentation && (
            <Edges
              color={
                isSelected
                  ? SELECTED_EDGE
                  : edgeColorFor(featureMap ? null : (feature.colorHex ?? slab.colorHex), SLAB_EDGE)
              }
              threshold={15}
            />
          )}
        </mesh>
      ) : (
        <>
          {!presentation && (
            <Line
              points={regionPoints}
              color={isSelected ? SELECTED_EDGE : (feature.colorHex ?? REGION_COLOR)}
              lineWidth={isSelected ? 2.5 : 1.5}
              raycast={() => null}
            />
          )}
          {regionGeometry && (
            <mesh
              geometry={regionGeometry}
              position={[0, faceY, 0]}
              rotation={[HALF_PI, 0, 0]}
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
                color={SELECTED_EDGE}
                depthWrite={false}
                side={DoubleSide}
              />
            </mesh>
          )}
        </>
      )}
    </group>
  );
}

function SlabDraftPreview({ draft, thicknessM }: { draft: DraftFeature; thicknessM: number }) {
  const faceY = draft.side === 1 ? thicknessM + FEATURE_FACE_LIFT_M : -FEATURE_FACE_LIFT_M;
  // Live size readout in the shared HUD while drawing; cleared when the draft unmounts.
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
    const points = outline.map((p): [number, number, number] => [p.x / 1000, faceY, p.z / 1000]);
    if (draft.shape !== 'free' && points.length > 1) points.push(points[0]);
    return points;
  }, [outline, draft.shape, faceY]);
  if (outline.length < 2) return null;
  return (
    <>
      {fillGeometry && (
        <mesh
          geometry={fillGeometry}
          position={[0, faceY, 0]}
          rotation={[HALF_PI, 0, 0]}
          raycast={() => null}
        >
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
    </>
  );
}
