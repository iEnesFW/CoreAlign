import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import {
  SceneViewport,
  SnapGuideOverlay,
  isCtrlPressed,
  isShiftPressed,
  setDragReadout,
  trackModifierKeys,
} from '@/shared/three-engine';
import { RunGroup } from './builders/RunGroup';
import { ArcRunGroup } from './builders/ArcRunGroup';
import { ConnectionPosts } from './builders/ConnectionPosts';
import { WallObject } from './builders/WallObject';
import { SlabObject } from './builders/SlabObject';
import { PolygonSurfaceObject } from './builders/PolygonSurfaceObject';
import { PenController } from './interaction/PenController';
import { DragReadoutOverlay } from './interaction/DragReadoutOverlay';
import { PitchedGreenhouseGeometry } from './geometries/PitchedGreenhouseGeometry';
import { PolygonFacadeGeometry } from './geometries/PolygonFacadeGeometry';
import { PlacementController } from './interaction/PlacementController';
import { PasteController } from './interaction/PasteController';
import { MarqueeController } from './interaction/MarqueeController';
import { MeasureController } from './interaction/MeasureController';
import { pointInPolygonMm } from './interaction/pointInPolygon';
import { multiSelectionHas } from './interaction/multiMove';
import { parsePolygonVertices } from '../model/polygonGeometry';
import {
  arcEndLocal,
  arcPointAt,
  developedLengthMm,
  isRealArc,
  radiusFromChordSweep,
  resolveArc,
} from '../model/arcGeometry';
import { curvedSlabFrame, curvedSlabPlanColumnsMm } from './builders/curvedSlabGeometry';
import { runViolatesCatalog } from '../model/catalogValidation';
import { polygonSelfIntersects } from '../model/polygonValidation';
import { registerExportRoot } from '../model/sceneExport';
import { queueToast } from '@/shared/api/toastQueue';
import { useDesignerStore } from '../model/designerStore';
import { useViewerAppearance } from '../model/viewerAppearance';
import { usePanelEntityActions, useRunEntityActions } from '../hooks/useDesignerEntityActions';
import { useAddRunMutation } from '../hooks/useGlassProjectQueries';
import { enqueuePersist } from '../model/persistQueue';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import {
  buildRunFootprint,
  buildSlabFootprint,
  buildSurfaceFootprint,
  buildWallFootprint,
  isFloating,
  normalizePlanAngleDeg,
  penetratesAny,
  RUN_PLAN_THICKNESS_MM,
} from './interaction/planCollision';
import { computeNeighbourShrink, type StretchBody } from '../model/pushResize';
import { findAttachedWallIds, resolveAttachedRunIds } from '../model/wallAttachment';
import { splitPanelsAtLength } from '../model/panelSplit';
import { panelIsShaped } from '../model/panelOutline';
import { clampHardwareOffsets, glassClampHeightMm } from '../model/hardwarePlacement';
import { rotatePlanPointDeg } from './interaction/planTransform';
import { wallFaceFrame, type WallFeatureSide } from './builders/wallFaces';
import {
  FEATURE_EDGE_MARGIN_MM,
  featureFitsWall,
  featureOutlineMm,
  outlineFitsRect,
  sanitizeFreeOutline,
} from '../model/wallFeatureGeometry';
import type { PlanFootprint } from './interaction/planCollision';
import type {
  PlanMoveDelta,
  PlanSnapPoint,
  PlanSnapSegment,
  PlanSnapTargets,
} from './interaction/planSnap';
import type { PlanRotationCommit } from './interaction/useObjectGestures';
import type { RunStretchPatch } from './builders/RunGroup';
import type {
  PlacementRunDraft,
  PlacementSlabDraft,
  PlacementWallDraft,
} from './interaction/PlacementController';
import type { PasteGhostSpec } from './interaction/PasteController';
import type { ColorOptionDto, GlassTypeDto, ProfileSystemDto } from '../model/glassEnclosure.types';
import type {
  SceneHardwareItem,
  SceneRunState,
  SceneSlabState,
  SceneSurfaceState,
  SceneWallState,
  WallFeatureSideValue,
} from '../model/project.types';

interface DesignerCanvasProps {
  profileSystems: ProfileSystemDto[];
  glassTypes: GlassTypeDto[];
  colors: ColorOptionDto[];
}

const SNAP_GRID_MM = 5;
const STICKY_SNAP_MM = 25;
const MIN_HARDWARE_MM = 8;
const FLOATING_GAP_MM = 50;
const MIN_RUN_LENGTH_MM = 100;
const DEG2RAD = Math.PI / 180;
const PANEL_TARGET_WIDTH_MM = 600;
const PEN_FACE_CLOSE_MM = 200;
const PEN_FACE_MIN_MM = 80;

const buildPlanSnapTargets = (
  walls: SceneWallState[],
  runs: SceneRunState[],
  slabs: SceneSlabState[],
  surfaces: SceneSurfaceState[],
): PlanSnapTargets => {
  const points: PlanSnapPoint[] = [];
  const segments: PlanSnapSegment[] = [];
  const addLineTargets = (
    ownerId: string,
    x: number,
    y: number,
    lengthMm: number,
    rotationDeg: number,
    halfWidthMm: number,
  ) => {
    const rad = rotationDeg * DEG2RAD;
    const dirX = Math.cos(rad);
    const dirY = Math.sin(rad);
    const endX = x + lengthMm * dirX;
    const endY = y + lengthMm * dirY;
    const midX = x + (lengthMm / 2) * dirX;
    const midY = y + (lengthMm / 2) * dirY;
    const nx = -dirY * halfWidthMm;
    const ny = dirX * halfWidthMm;
    points.push(
      { ownerId, x, y },
      { ownerId, x: endX, y: endY },
      { ownerId, x: midX, y: midY },
      { ownerId, x: x + nx, y: y + ny },
      { ownerId, x: x - nx, y: y - ny },
      { ownerId, x: endX + nx, y: endY + ny },
      { ownerId, x: endX - nx, y: endY - ny },
      { ownerId, x: midX + nx, y: midY + ny },
      { ownerId, x: midX - nx, y: midY - ny },
    );
    segments.push(
      { ownerId, x1: x + nx, y1: y + ny, x2: endX + nx, y2: endY + ny },
      { ownerId, x1: x - nx, y1: y - ny, x2: endX - nx, y2: endY - ny },
    );
  };
  // Arc bodies (CHORD-INVARIANT): the true fixed ends come from arcEndLocal on the RAW stored
  // radius (exactly what the renderer draws — no clamp), plus the apex and the chord midpoint.
  // The CHORD is the honest alignment segment for an arc (its faces are not straight lines).
  // rotationDeg alone is a PHANTOM here — it's the rolled start tangent, not the chord direction.
  const addArcTargets = (
    ownerId: string,
    originX: number,
    originY: number,
    rotationDeg: number,
    radiusMm: number,
    sweepDeg: number,
  ) => {
    const rad = rotationDeg * DEG2RAD;
    const cos = Math.cos(rad);
    const sin = Math.sin(rad);
    const resolved = resolveArc(radiusMm, sweepDeg);
    const e = arcEndLocal(resolved.radiusMm, sweepDeg);
    const apex = arcPointAt(resolved.radiusMm, resolved.direction, resolved.sweepRad / 2);
    const toWorld = (lx: number, ly: number) => ({
      x: originX + lx * cos - ly * sin,
      y: originY + lx * sin + ly * cos,
    });
    const end = toWorld(e.xMm, e.yMm);
    const mid = toWorld(apex.x, apex.z);
    points.push(
      { ownerId, x: originX, y: originY },
      { ownerId, x: end.x, y: end.y },
      { ownerId, x: mid.x, y: mid.y },
      { ownerId, x: (originX + end.x) / 2, y: (originY + end.y) / 2 },
    );
    segments.push({ ownerId, x1: originX, y1: originY, x2: end.x, y2: end.y });
  };
  for (const wall of walls) {
    if (isRealArc(wall.geomArcRadiusMm, wall.geomArcSweepDeg)) {
      addArcTargets(
        wall.id,
        wall.originX,
        wall.originY,
        wall.rotationDeg,
        radiusFromChordSweep(wall.lengthMm, wall.geomArcRadiusMm, wall.geomArcSweepDeg),
        wall.geomArcSweepDeg ?? 1,
      );
      continue;
    }
    addLineTargets(
      wall.id,
      wall.originX,
      wall.originY,
      wall.lengthMm,
      wall.rotationDeg,
      wall.thicknessMm / 2,
    );
  }
  for (const run of runs) {
    if (isRealArc(run.geomArcRadiusMm, run.geomArcSweepDeg)) {
      addArcTargets(
        run.id,
        run.originX,
        run.originY,
        run.rotationDeg,
        radiusFromChordSweep(run.lengthMm, run.geomArcRadiusMm, run.geomArcSweepDeg),
        run.geomArcSweepDeg ?? 1,
      );
      continue;
    }
    addLineTargets(
      run.id,
      run.originX,
      run.originY,
      run.lengthMm,
      run.rotationDeg,
      RUN_PLAN_THICKNESS_MM / 2,
    );
  }
  for (const slab of slabs) {
    const rad = slab.rotationDeg * DEG2RAD;
    const cos = Math.cos(rad);
    const sin = Math.sin(rad);
    const corner = (lx: number, ly: number): PlanSnapPoint => ({
      ownerId: slab.id,
      x: slab.originX + lx * cos - ly * sin,
      y: slab.originY + lx * sin + ly * cos,
    });
    // A plan-curved slab's real edges are the sampled band (same columns as the mesh) — the flat
    // rect corners are phantoms it bows away from. Emit the band's true ends/apexes + the front
    // chord as the alignment segment.
    if (isRealArc(slab.geomArcRadiusMm, slab.geomArcSweepDeg)) {
      const columns = curvedSlabPlanColumnsMm(
        slab.lengthMm,
        slab.depthMm,
        slab.geomArcRadiusMm ?? 0,
        slab.geomArcSweepDeg ?? 1,
        slab.slabArcAxis ?? 'length',
      );
      const first = columns[0];
      const last = columns[columns.length - 1];
      const midCol = columns[Math.floor(columns.length / 2)];
      const fs = corner(first.front.x, first.front.z);
      const fe = corner(last.front.x, last.front.z);
      points.push(
        fs,
        fe,
        corner(midCol.front.x, midCol.front.z),
        corner(first.back.x, first.back.z),
        corner(last.back.x, last.back.z),
        corner(midCol.back.x, midCol.back.z),
      );
      segments.push({ ownerId: slab.id, x1: fs.x, y1: fs.y, x2: fe.x, y2: fe.y });
      continue;
    }
    const corners = [
      corner(0, 0),
      corner(slab.lengthMm, 0),
      corner(slab.lengthMm, slab.depthMm),
      corner(0, slab.depthMm),
    ];
    points.push(...corners);
    for (let i = 0; i < corners.length; i += 1) {
      const a = corners[i];
      const b = corners[(i + 1) % corners.length];
      points.push({ ownerId: slab.id, x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 });
      segments.push({ ownerId: slab.id, x1: a.x, y1: a.y, x2: b.x, y2: b.y });
    }
  }
  for (const surface of surfaces) {
    const pts = surface.points;
    for (let i = 0; i < pts.length; i += 1) {
      const a = pts[i];
      const b = pts[(i + 1) % pts.length];
      points.push({ ownerId: surface.id, x: a.x, y: a.y });
      segments.push({ ownerId: surface.id, x1: a.x, y1: a.y, x2: b.x, y2: b.y });
    }
  }
  return { points, segments };
};

const snapMm = (value: number) => Math.round(value / SNAP_GRID_MM) * SNAP_GRID_MM;

const clampMm = (value: number, limit: number) => Math.min(limit, Math.max(-limit, value));

const stickyMm = (value: number, targets: number[]) => {
  for (const target of targets) {
    if (Math.abs(value - target) <= STICKY_SNAP_MM) return Math.round(target);
  }
  return value;
};

export function DesignerCanvas({ profileSystems, glassTypes, colors }: DesignerCanvasProps) {
  const { t } = useTranslation();
  const scene = useDesignerStore((s) => s.scene);
  const quality = useDesignerStore((s) => s.quality);
  const showAnnotations = useDesignerStore((s) => s.showAnnotations);
  const presentation = useDesignerStore((s) => s.presentationMode);
  const layerVisibility = useDesignerStore((s) => s.layerVisibility);
  const selection = useDesignerStore((s) => s.selection);
  const setSelection = useDesignerStore((s) => s.setSelection);
  const updateHardware = useDesignerStore((s) => s.updateHardware);
  const addHardware = useDesignerStore((s) => s.addHardware);
  const removeHardware = useDesignerStore((s) => s.removeHardware);
  const clipboard = useDesignerStore((s) => s.clipboard);
  const pasteArmed = useDesignerStore((s) => s.pasteArmed);
  const setPasteArmed = useDesignerStore((s) => s.setPasteArmed);
  const project = useDesignerStore((s) => s.project);
  const projectId = useDesignerStore((s) => s.projectId);
  const resizePanelEdge = useDesignerStore((s) => s.resizePanelEdge);
  const updateRun = useDesignerStore((s) => s.updateRun);
  const applyRunPatches = useDesignerStore((s) => s.applyRunPatches);
  const updateWall = useDesignerStore((s) => s.updateWall);
  const updateSlab = useDesignerStore((s) => s.updateSlab);
  const setCamera = useDesignerStore((s) => s.setCamera);
  const removeWall = useDesignerStore((s) => s.removeWall);
  const removeSlab = useDesignerStore((s) => s.removeSlab);
  const addWall = useDesignerStore((s) => s.addWall);
  const addSlab = useDesignerStore((s) => s.addSlab);
  const addSurface = useDesignerStore((s) => s.addSurface);
  const addWallFeature = useDesignerStore((s) => s.addWallFeature);
  const addSlabFeature = useDesignerStore((s) => s.addSlabFeature);
  const penFace = useDesignerStore((s) => s.penFace);
  const setPenFace = useDesignerStore((s) => s.setPenFace);
  const penIntent = useDesignerStore((s) => s.penIntent);
  const penMode = useDesignerStore((s) => s.penMode);
  const setRunPanels = useDesignerStore((s) => s.setRunPanels);
  const activeTool = useDesignerStore((s) => s.activeTool);
  const placement = useDesignerStore((s) => s.placement);
  const placementShape = useDesignerStore((s) => s.placementShape);
  const paintColor = useDesignerStore((s) => s.paintColor);
  const paintMaterial = useDesignerStore((s) => s.paintMaterial);
  const multiSelection = useDesignerStore((s) => s.multiSelection);
  const toggleMultiSelect = useDesignerStore((s) => s.toggleMultiSelect);
  const setMultiSelect = useDesignerStore((s) => s.setMultiSelect);
  const setPlacement = useDesignerStore((s) => s.setPlacement);
  const onPenFaceFinishRef = useRef<() => void>(() => {});
  const { appearance } = useViewerAppearance();
  const { createPanelFrom, persistPanel, persistRunPanels, persistPanelHardware, deletePanel } =
    usePanelEntityActions();
  const { persistRun, deleteRun } = useRunEntityActions();
  const addRunMutation = useAddRunMutation();

  const [cursor, setCursor] = useState<{ x: number; y: number } | null>(null);
  const pendingHardwareRef = useRef<{
    runId: string;
    panelId: string;
    items: SceneHardwareItem[];
  } | null>(null);

  useEffect(() => {
    const pending = pendingHardwareRef.current;
    if (!pending) return;
    const run = scene.runs.find((r) => r.id === pending.runId);
    const panel = run?.panels.find((p) => p.id === pending.panelId);
    if (!panel) return;
    pendingHardwareRef.current = null;
    for (const item of pending.items) addHardware(pending.runId, pending.panelId, item);
    void persistPanelHardware(pending.runId, pending.panelId);
  }, [scene, addHardware, persistPanelHardware]);

  useEffect(() => {
    if (!placement) return;
    const onKeyDown = (e: KeyboardEvent) => {
      const target = e.target as HTMLElement | null;
      if (
        target instanceof HTMLInputElement ||
        target instanceof HTMLTextAreaElement ||
        target instanceof HTMLSelectElement ||
        target?.isContentEditable
      )
        return;
      if (e.key === 'Escape') {
        useDesignerStore.getState().setPenFace(null);
        setPlacement(null);
      } else if (e.key === 'Enter' && placement === 'pen') {
        e.preventDefault();
        onPenFaceFinishRef.current();
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [placement, setPlacement]);

  useEffect(() => {
    if (placement !== 'pen') useDesignerStore.getState().setPenFace(null);
  }, [placement]);

  // The pen draw readout is set live in WallObject/SlabObject while a face session is open;
  // clear it once the session ends (committed, finished, or tool switched away).
  useEffect(() => {
    if (!penFace) setDragReadout(null);
  }, [penFace]);

  useEffect(() => trackModifierKeys(), []);

  useEffect(() => {
    const resetCursor = () => {
      document.body.style.cursor = 'auto';
    };
    window.addEventListener('pointercancel', resetCursor);
    window.addEventListener('blur', resetCursor);
    return () => {
      window.removeEventListener('pointercancel', resetCursor);
      window.removeEventListener('blur', resetCursor);
    };
  }, []);

  const isMultiSelectClick = () =>
    activeTool === 'multiselect' ||
    (activeTool === 'select' && (isCtrlPressed() || isShiftPressed()));

  const systemMap = useMemo(() => new Map(profileSystems.map((s) => [s.id, s])), [profileSystems]);
  const glassMap = useMemo(() => new Map(glassTypes.map((g) => [g.id, g])), [glassTypes]);
  const colorMap = useMemo(() => new Map(colors.map((c) => [c.id, c])), [colors]);
  const snapTargets = useMemo(
    () =>
      buildPlanSnapTargets(scene.walls ?? [], scene.runs, scene.slabs ?? [], scene.surfaces ?? []),
    [scene.walls, scene.runs, scene.slabs, scene.surfaces],
  );
  const solidFootprints = useMemo<PlanFootprint[]>(
    () => [
      ...(scene.walls ?? []).map((wall) => buildWallFootprint(wall, 0, 0, wall.rotationDeg)),
      ...scene.runs.map((run) => buildRunFootprint(run, 0, 0, run.rotationDeg)),
      ...(scene.slabs ?? []).map((slab) => buildSlabFootprint(slab, 0, 0, slab.rotationDeg)),
    ],
    [scene.walls, scene.runs, scene.slabs],
  );
  const planObstacles = solidFootprints;
  const supportFootprints = useMemo<PlanFootprint[]>(
    () => [...solidFootprints, ...(scene.surfaces ?? []).map((s) => buildSurfaceFootprint(s))],
    [solidFootprints, scene.surfaces],
  );
  const placementObstacles = planObstacles;
  const runObstacles = planObstacles;
  const wallObstacles = planObstacles;
  const floatingCount = useMemo(() => {
    let count = 0;
    for (const slab of scene.slabs ?? []) {
      if (
        isFloating(
          buildSlabFootprint(slab, 0, 0, slab.rotationDeg),
          supportFootprints,
          FLOATING_GAP_MM,
        )
      )
        count += 1;
    }
    for (const surface of scene.surfaces ?? []) {
      if (isFloating(buildSurfaceFootprint(surface), supportFootprints, FLOATING_GAP_MM))
        count += 1;
    }
    return count;
  }, [scene.slabs, scene.surfaces, supportFootprints]);

  const catalogViolations = useMemo(
    () =>
      scene.runs.reduce((c, run) => c + (runViolatesCatalog(run, systemMap, glassMap) ? 1 : 0), 0),
    [scene.runs, systemMap, glassMap],
  );

  const interactionsEnabled = !pasteArmed && !placement;

  const clearSingleSelection = () =>
    setSelection({
      kind: null,
      runId: null,
      panelId: null,
      connectionId: null,
      hardwareId: null,
      wallId: null,
      slabId: null,
      featureId: null,
    });

  const clearSelection = () => {
    clearSingleSelection();
    useDesignerStore.getState().clearMultiSelect();
  };

  const persistFreshRun = (runId: string) => {
    const fresh = useDesignerStore.getState().scene.runs.find((r) => r.id === runId);
    if (fresh) void persistRun(fresh);
  };

  const paintRun = (runId: string) => {
    // WHY: a glass run carries only a catalog colorId — it has no material-texture target (unlike
    // walls/slabs). Painting a MATERIAL onto glass was a silent no-op; tell the user instead.
    if (paintMaterial) {
      queueToast({
        dedupeKey: 'glass-paint-run-material',
        variant: 'info',
        description: t('GlassEnclosure.Designer.Paint.MaterialNotOnGlass', {
          defaultValue: 'Malzeme dokusu cama uygulanamaz — cam için bir renk seçin.',
        }),
      });
      return;
    }
    if (!paintColor) return;
    updateRun(runId, { colorId: paintColor.id });
    persistFreshRun(runId);
  };

  const pastePanel = async (runId: string) => {
    if (!clipboard || clipboard.kind !== 'panel') return;
    setPasteArmed(false);
    const source = clipboard.panel;
    const created = await createPanelFrom(runId, {
      ...source,
      id: crypto.randomUUID(),
      hardware: [],
    });
    if (created && source.hardware.length > 0) {
      pendingHardwareRef.current = {
        runId,
        panelId: created.id,
        items: source.hardware.map((h) => ({ ...h, id: crypto.randomUUID() })),
      };
    }
  };

  const pasteHardware = (runId: string, panelId: string) => {
    if (!clipboard || clipboard.kind !== 'hardware') return;
    setPasteArmed(false);
    const run = scene.runs.find((r) => r.id === runId);
    const panel = run?.panels.find((p) => p.id === panelId);
    if (!run || !panel) return;
    const clone: SceneHardwareItem = {
      ...clipboard.item,
      id: crypto.randomUUID(),
      offsetXmm: clampMm(clipboard.item.offsetXmm, panel.widthMm / 2),
      offsetYmm: clampMm(
        clipboard.item.offsetYmm,
        glassClampHeightMm(panel.heightMm, run.heightMm) / 2,
      ),
    };
    addHardware(runId, panelId, clone);
    void persistPanelHardware(runId, panelId);
    setSelection({ kind: 'hardware', runId, panelId, connectionId: null, hardwareId: clone.id });
  };

  const onSelectRun = (runId: string) => {
    if (placement) return;
    if (isMultiSelectClick()) {
      clearSingleSelection();
      toggleMultiSelect('run', runId);
      return;
    }
    if (activeTool === 'paint') {
      paintRun(runId);
      return;
    }
    if (activeTool === 'erase') {
      void deleteRun(runId);
      clearSelection();
      return;
    }
    if (pasteArmed && clipboard?.kind === 'panel') {
      void pastePanel(runId);
      return;
    }
    setSelection({ kind: 'run', runId, panelId: null, connectionId: null, hardwareId: null });
  };
  const onSelectPanel = (runId: string, panelId: string) => {
    if (placement) return;
    if (isMultiSelectClick()) {
      clearSingleSelection();
      toggleMultiSelect('run', runId);
      return;
    }
    if (activeTool === 'paint') {
      paintRun(runId);
      return;
    }
    if (activeTool === 'erase') {
      void deletePanel(runId, panelId);
      clearSelection();
      return;
    }
    if (pasteArmed && clipboard) {
      if (clipboard.kind === 'hardware') pasteHardware(runId, panelId);
      else void pastePanel(runId);
      return;
    }
    setSelection({ kind: 'panel', runId, panelId, connectionId: null, hardwareId: null });
  };
  const onSelectHardware = (runId: string, panelId: string, hardwareId: string) => {
    if (placement) return;
    if (isMultiSelectClick()) {
      clearSingleSelection();
      toggleMultiSelect('run', runId);
      return;
    }
    if (activeTool === 'paint') {
      paintRun(runId);
      return;
    }
    if (activeTool === 'erase') {
      removeHardware(runId, panelId, hardwareId);
      clearSelection();
      return;
    }
    if (pasteArmed && clipboard) {
      if (clipboard.kind === 'hardware') pasteHardware(runId, panelId);
      else void pastePanel(runId);
      return;
    }
    setSelection({ kind: 'hardware', runId, panelId, connectionId: null, hardwareId });
  };
  const onSelectWall = (wallId: string) => {
    if (placement) return;
    if (isMultiSelectClick()) {
      clearSingleSelection();
      toggleMultiSelect('wall', wallId);
      return;
    }
    if (activeTool === 'paint') {
      if (paintMaterial) updateWall(wallId, { materialKey: paintMaterial, colorHex: null });
      else if (paintColor) updateWall(wallId, { colorHex: paintColor.hex, materialKey: null });
      return;
    }
    if (activeTool === 'erase') {
      removeWall(wallId);
      clearSelection();
      return;
    }
    if (pasteArmed) return;
    setSelection({
      kind: 'wall',
      runId: null,
      panelId: null,
      connectionId: null,
      hardwareId: null,
      wallId,
    });
  };
  const onSelectSurface = (surfaceId: string) => {
    if (placement || pasteArmed) return;
    setSelection({
      kind: 'surface',
      runId: null,
      panelId: null,
      connectionId: null,
      hardwareId: null,
      wallId: null,
      slabId: null,
      surfaceId,
    });
  };

  const commitPenFace = (session: NonNullable<typeof penFace>) => {
    setPenFace(null);
    let pts = session.points;
    if (pts.length > 1) {
      const f = pts[0];
      const l = pts[pts.length - 1];
      if (Math.hypot(f.x - l.x, f.z - l.z) < 1) pts = pts.slice(0, -1);
    }
    // Pen points are deliberately CLICKED vertices (plus shift-arc tessellations) — running RDP
    // over them deleted intentional corners under the tolerance, so the committed shape was never
    // 1:1 with the drawing. Only near-identical consecutive points are dropped.
    pts = pts
      .map((p) => ({ x: p.x, z: p.z }))
      .filter((p, i, arr) => i === 0 || Math.hypot(p.x - arr[i - 1].x, p.z - arr[i - 1].z) > 1.5);
    if (pts.length < 3) {
      queueToast({
        dedupeKey: 'glass-pen-too-small',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.Pen.TooSmall', {
          defaultValue: 'Çizilen şekil çok küçük veya geçersiz.',
        }),
      });
      return;
    }
    // WHY: same strong repair the drag-draw path uses (collinear/vertex-touch aware + tail/head trim)
    // — the weak polygonSelfIntersects missed pinched-vertex loops and let a non-manifold outline
    // reach the CSG cutter. Bounds below are derived from the SANITIZED outline, not the raw points.
    const sanitized = sanitizeFreeOutline(pts);
    if (!sanitized) {
      queueToast({
        dedupeKey: 'glass-pen-self-intersect',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.Pen.SelfIntersect', {
          defaultValue: 'Çizim kendisiyle kesişiyor; geçerli bir alan oluşturulamadı.',
        }),
      });
      return;
    }
    pts = sanitized;
    const xs = pts.map((p) => p.x);
    const zs = pts.map((p) => p.z);
    const minX = Math.min(...xs);
    const maxX = Math.max(...xs);
    const minZ = Math.min(...zs);
    const maxZ = Math.max(...zs);
    const offsetMm = Math.round((minX + maxX) / 2);
    const centerZMm = Math.round((minZ + maxZ) / 2);
    const widthMm = Math.round(maxX - minX);
    const heightMm = Math.round(maxZ - minZ);
    if (widthMm < PEN_FACE_MIN_MM || heightMm < PEN_FACE_MIN_MM) {
      queueToast({
        dedupeKey: 'glass-pen-too-small',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.Pen.TooSmall', {
          defaultValue: 'Çizilen şekil çok küçük veya geçersiz.',
        }),
      });
      return;
    }
    if (penIntent === 'glassPanel') {
      queueToast({
        dedupeKey: 'glass-pen-glasspanel',
        variant: 'info',
        description: t('GlassEnclosure.Designer.Pen.GlassPanelDeferred', {
          defaultValue:
            'Cam paneli çizimi yakında — henüz şekilli serbest cam nesnesi desteklenmiyor.',
        }),
      });
      return;
    }
    if (penIntent === 'divide') {
      if (session.hostKind !== 'wall') {
        queueToast({
          dedupeKey: 'glass-pen-divide-wall-only',
          variant: 'warning',
          description: t('GlassEnclosure.Designer.Pen.DivideWallOnly', {
            defaultValue: 'Bölme yalnızca cam bağlı bir duvara çizilebilir.',
          }),
        });
        return;
      }
      const divideWall = (scene.walls ?? []).find((w) => w.id === session.hostId);
      if (!divideWall) return;
      const attachedRunIds = resolveAttachedRunIds(divideWall, scene.runs);
      const hostRun = scene.runs.find((r) => attachedRunIds.includes(r.id));
      if (!hostRun) {
        queueToast({
          dedupeKey: 'glass-pen-divide-no-run',
          variant: 'warning',
          description: t('GlassEnclosure.Designer.Pen.DivideNoRun', {
            defaultValue: 'Bu duvara bağlı bir cam hattı bulunamadı.',
          }),
        });
        return;
      }
      // Divide only makes sense for a straight run of rectangular panes — a shaped/hole-fill pane
      // (triangle, arch, free outline) can't be cut into two rectangles without destroying its shape.
      if (hostRun.panels.some((p) => panelIsShaped(p) || Boolean(p.shapePointsJson))) {
        queueToast({
          dedupeKey: 'glass-pen-divide-shaped',
          variant: 'warning',
          description: t('GlassEnclosure.Designer.Pen.DivideShaped', {
            defaultValue:
              'Şekilli / boşluk-dolgu cam bölünemez — yalnız düz dikdörtgen hatlar bölünebilir.',
          }),
        });
        return;
      }
      // WHY: fraction = offsetMm/lengthMm uses the CHORD while totalWidth is the DEVELOPED length —
      // on an arc run the cut would land at the wrong physical position. Reject (latent: today the
      // divide host resolves via a straight wall, but guard explicitly).
      if (isRealArc(hostRun.geomArcRadiusMm, hostRun.geomArcSweepDeg)) {
        queueToast({
          dedupeKey: 'glass-pen-divide-arc',
          variant: 'warning',
          description: t('GlassEnclosure.Designer.Pen.DivideArc', {
            defaultValue: 'Kavisli hat bölünemez — yalnız düz hatlar bölünebilir.',
          }),
        });
        return;
      }
      const totalWidth = hostRun.panels.reduce((sum, p) => sum + p.widthMm, 0);
      const fraction = Math.min(1, Math.max(0, offsetMm / Math.max(1, divideWall.lengthMm)));
      const split = splitPanelsAtLength(hostRun.panels, fraction * totalWidth, () =>
        crypto.randomUUID(),
      );
      if (!split) {
        queueToast({
          dedupeKey: 'glass-pen-divide-invalid',
          variant: 'warning',
          description: t('GlassEnclosure.Designer.Pen.DivideInvalid', {
            defaultValue: 'Bu konumda bölme yapılamıyor (paneller çok dar kalır).',
          }),
        });
        return;
      }
      setRunPanels(hostRun.id, split);
      void persistRunPanels(hostRun.id);
      queueToast({
        dedupeKey: 'glass-pen-divided',
        variant: 'success',
        description: t('GlassEnclosure.Designer.Pen.Divided', {
          defaultValue: 'Cam hattı çizim çizgisinden bölündü.',
        }),
      });
      return;
    }
    // Stored side uses 1/-1 for front/back, the string for the four side faces (slab → 1/-1).
    const featureSide: WallFeatureSideValue =
      session.side === 'front' ? 1 : session.side === 'back' ? -1 : session.side;
    // A curved wall only renders cutting features (hole / recess+depth) through its CSG path, so a
    // shape drawn there defaults to a through HOLE (the common opening). A flat wall/slab keeps the
    // NON-CUTTING outline (flush recess): the user then picks hole / recess+depth / protrude.
    const penHostWall =
      session.hostKind === 'wall'
        ? (scene.walls ?? []).find((w) => w.id === session.hostId)
        : undefined;
    const penHostIsCurved = Boolean(
      isRealArc(penHostWall?.geomArcRadiusMm, penHostWall?.geomArcSweepDeg),
    );
    const feature = {
      id: crypto.randomUUID(),
      shape: 'free' as const,
      mode: penHostIsCurved ? ('hole' as const) : ('recess' as const),
      side: featureSide,
      offsetMm,
      centerZMm,
      widthMm,
      heightMm,
      depthMm: 0,
      points: pts.map((p) => ({ x: Math.round(p.x - offsetMm), z: Math.round(p.z - centerZMm) })),
      colorHex: null,
    };
    const outline = featureOutlineMm(feature);
    if (session.hostKind === 'wall') {
      const wall = (scene.walls ?? []).find((w) => w.id === session.hostId);
      if (!wall) return;
      // Curved walls now carve features via CSG, but a BENT (L) wall reuses the straight face frame
      // and renders no features yet — block drawing there so a pen shape isn't silently lost.
      if (wall.bendAngleDeg && Math.abs(wall.bendAngleDeg) >= 1) {
        queueToast({
          dedupeKey: 'glass-bent-no-feature',
          variant: 'warning',
          description: t('GlassEnclosure.Designer.Pen.ArcNoFeature', {
            defaultValue: 'Şekilli (kavisli/eğimli) yüzeye henüz açıklık/şekil çizilemiyor.',
          }),
        });
        return;
      }
      // Front/back fit against the wall length×height; a side face fits against that face's own
      // bounds (uMax×vMax from wallFaceFrame) — the outline is already in the face's (u,v).
      const fits =
        session.side === 'front' || session.side === 'back'
          ? featureFitsWall(wall, outline)
          : (() => {
              const frame = wallFaceFrame(session.side, {
                lengthM: wall.lengthMm / 1000,
                heightM: Math.max(wall.heightMm, wall.heightEndMm ?? wall.heightMm) / 1000,
                thicknessM: wall.thicknessMm / 1000,
              });
              const m = FEATURE_EDGE_MARGIN_MM / 2;
              return (
                minX >= m &&
                maxX <= frame.uMaxM * 1000 - m &&
                minZ >= m &&
                maxZ <= frame.vMaxM * 1000 - m
              );
            })();
      if (!fits) {
        queueToast({
          dedupeKey: 'glass-pen-no-fit',
          variant: 'warning',
          description: t('GlassEnclosure.Designer.Pen.DoesNotFit', {
            defaultValue: 'Şekil yüzeye sığmıyor — kenarlardan biraz içeride çizin.',
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
    } else {
      const slab = (scene.slabs ?? []).find((s) => s.id === session.hostId);
      if (!slab) return;
      // Plan-arc slabs carve + render features now (#6b); only barrel/pitch remain deferred.
      if ((slab.arcRiseMm ?? 0) > 0 || (slab.pitchRiseMm ?? 0) > 0) {
        queueToast({
          dedupeKey: 'glass-arc-no-feature',
          variant: 'warning',
          description: t('GlassEnclosure.Designer.Pen.ArcNoFeature', {
            defaultValue: 'Şekilli (kavisli/eğimli) yüzeye henüz açıklık/şekil çizilemiyor.',
          }),
        });
        return;
      }
      // ARC slab: pen coordinates live in the developed (s,c) frame — fit against that domain.
      const slabFits = isRealArc(slab.geomArcRadiusMm, slab.geomArcSweepDeg)
        ? (() => {
            const frame = curvedSlabFrame(
              slab.lengthMm,
              slab.depthMm,
              slab.geomArcRadiusMm ?? 0,
              slab.geomArcSweepDeg ?? 1,
              slab.slabArcAxis ?? 'length',
            );
            return outlineFitsRect(
              outline,
              frame.developedMm,
              frame.acrossMm,
              FEATURE_EDGE_MARGIN_MM,
            );
          })()
        : outlineFitsRect(outline, slab.lengthMm, slab.depthMm, FEATURE_EDGE_MARGIN_MM);
      if (!slabFits) {
        queueToast({
          dedupeKey: 'glass-pen-no-fit',
          variant: 'warning',
          description: t('GlassEnclosure.Designer.Pen.DoesNotFit', {
            defaultValue: 'Şekil yüzeye sığmıyor — kenarlardan biraz içeride çizin.',
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
    }
  };

  // Block the pen SESSION on a barrel/pitched slab up-front (not only at commit) so the user
  // isn't led through drawing a shape that would then be rejected. Plan-arc slabs now carve and
  // render features (#6b), so they draw freely.
  const shapedSlabPenBlocked = (hostKind: 'wall' | 'slab', hostId: string): boolean => {
    if (hostKind !== 'slab') return false;
    const slab = (useDesignerStore.getState().scene.slabs ?? []).find((s) => s.id === hostId);
    if (!slab) return false;
    const blocked = (slab.arcRiseMm ?? 0) > 0 || (slab.pitchRiseMm ?? 0) > 0;
    if (blocked) {
      queueToast({
        dedupeKey: 'glass-arc-no-feature',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.Pen.ArcNoFeature', {
          defaultValue: 'Şekilli (kavisli/eğimli) yüzeye henüz açıklık/şekil çizilemiyor.',
        }),
      });
    }
    return blocked;
  };

  const onPenFaceClick = (
    hostKind: 'wall' | 'slab',
    hostId: string,
    side: WallFeatureSide,
    pt: { x: number; z: number },
  ) => {
    if (shapedSlabPenBlocked(hostKind, hostId)) return;
    const session = useDesignerStore.getState().penFace;
    if (!session || session.hostId !== hostId) {
      setPenFace({ hostKind, hostId, side, points: [pt], cursor: pt });
      return;
    }
    const first = session.points[0];
    if (
      session.points.length >= 3 &&
      Math.hypot(pt.x - first.x, pt.z - first.z) <= PEN_FACE_CLOSE_MM
    ) {
      commitPenFace(session);
      return;
    }
    const prev = session.points[session.points.length - 1];
    if (prev && Math.hypot(pt.x - prev.x, pt.z - prev.z) < 1) return;
    setPenFace({ ...session, points: [...session.points, pt], cursor: pt });
  };

  const onPenFaceArc = (
    hostKind: 'wall' | 'slab',
    hostId: string,
    side: WallFeatureSide,
    pts: { x: number; z: number }[],
  ) => {
    if (pts.length === 0) return;
    if (shapedSlabPenBlocked(hostKind, hostId)) return;
    const session = useDesignerStore.getState().penFace;
    const cursor = pts[pts.length - 1];
    if (!session || session.hostId !== hostId) {
      setPenFace({ hostKind, hostId, side, points: pts, cursor });
      return;
    }
    setPenFace({ ...session, points: [...session.points, ...pts], cursor });
  };

  const onPenFaceFinish = () => {
    const session = useDesignerStore.getState().penFace;
    if (session) commitPenFace(session);
  };

  useEffect(() => {
    onPenFaceFinishRef.current = onPenFaceFinish;
  });

  const handlePenFinish = (pointsMm: { x: number; y: number }[]) => {
    setPlacement(null);
    if (polygonSelfIntersects(pointsMm)) {
      queueToast({
        dedupeKey: 'glass-pen-self-intersect',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.Pen.SelfIntersect', {
          defaultValue: 'Çizim kendisiyle kesişiyor; geçerli bir yüzey oluşturulamadı.',
        }),
      });
      return;
    }
    const surface: SceneSurfaceState = {
      id: crypto.randomUUID(),
      kind: 'floor',
      points: pointsMm.map((p) => ({ x: Math.round(p.x), y: Math.round(p.y) })),
      elevationMm: 0,
      thicknessMm: 120,
      colorHex: null,
      materialKey: null,
    };
    addSurface(surface);
    setSelection({
      kind: 'surface',
      runId: null,
      panelId: null,
      connectionId: null,
      hardwareId: null,
      wallId: null,
      slabId: null,
      surfaceId: surface.id,
    });
  };

  const onSelectSlab = (slabId: string) => {
    if (placement) return;
    if (isMultiSelectClick()) {
      clearSingleSelection();
      toggleMultiSelect('slab', slabId);
      return;
    }
    if (activeTool === 'paint') {
      if (paintMaterial) updateSlab(slabId, { materialKey: paintMaterial, colorHex: null });
      else if (paintColor) updateSlab(slabId, { colorHex: paintColor.hex, materialKey: null });
      return;
    }
    if (activeTool === 'erase') {
      removeSlab(slabId);
      clearSelection();
      return;
    }
    if (pasteArmed) return;
    setSelection({
      kind: 'slab',
      runId: null,
      panelId: null,
      connectionId: null,
      hardwareId: null,
      wallId: null,
      slabId,
    });
  };

  const onResizePanel = (runId: string, panelId: string, deltaMm: number) => {
    const run = scene.runs.find((r) => r.id === runId);
    if (!run) return;
    const index = run.panels.findIndex((p) => p.id === panelId);
    if (index < 0) return;
    const neighbor = run.panels[index + 1] ?? run.panels[index - 1];
    if (!neighbor) return;
    const before = new Map(run.panels.map((p) => [p.id, p.widthMm]));
    resizePanelEdge(runId, panelId, neighbor.id, snapMm(deltaMm));
    const freshRun = useDesignerStore.getState().scene.runs.find((r) => r.id === runId);
    for (const p of freshRun?.panels ?? []) {
      if (before.get(p.id) === p.widthMm) continue;
      // WHY: a narrowed pane leaves hardware placed near the old edge overhanging the glass — re-clamp
      // each item's offset to the new width (and persist the hardware, not just the panel width).
      let hardwareClamped = false;
      for (const hw of p.hardware) {
        const clamped = clampHardwareOffsets(
          p.widthMm,
          glassClampHeightMm(p.heightMm, run.heightMm),
          hw,
        );
        if (clamped.offsetXmm !== hw.offsetXmm || clamped.offsetYmm !== hw.offsetYmm) {
          updateHardware(runId, p.id, hw.id, clamped);
          hardwareClamped = true;
        }
      }
      void persistPanel(runId, p);
      if (hardwareClamped) void persistPanelHardware(runId, p.id);
    }
  };

  const commitGroupMove = (
    dxMm: number,
    dyMm: number,
    extraRunIds: string[] = [],
    extraWallIds: string[] = [],
  ) => {
    const ms = useDesignerStore.getState().multiSelection;
    const runSet = new Set([...ms.runIds, ...extraRunIds]);
    const wallSet = new Set([...ms.wallIds, ...extraWallIds]);
    const slabSet = new Set(ms.slabIds);
    useDesignerStore.getState().applyScenePatch((s) => ({
      ...s,
      runs: s.runs.map((r) =>
        runSet.has(r.id)
          ? { ...r, originX: Math.round(r.originX + dxMm), originY: Math.round(r.originY + dyMm) }
          : r,
      ),
      walls: (s.walls ?? []).map((w) =>
        wallSet.has(w.id)
          ? { ...w, originX: Math.round(w.originX + dxMm), originY: Math.round(w.originY + dyMm) }
          : w,
      ),
      slabs: (s.slabs ?? []).map((sl) =>
        slabSet.has(sl.id)
          ? {
              ...sl,
              originX: Math.round(sl.originX + dxMm),
              originY: Math.round(sl.originY + dyMm),
            }
          : sl,
      ),
    }));
    // Persist the FULL moved set — a wall-attached run carried via extraRunIds is moved in the
    // scene patch above, and skipping its persist would snap the glass back on the next refetch.
    for (const id of runSet) persistFreshRun(id);
  };

  const onMoveRun = (runId: string, delta: PlanMoveDelta) => {
    if (delta.dxMm === 0 && delta.dyMm === 0) return;
    if (multiSelectionHas(useDesignerStore.getState().multiSelection, 'run', runId)) {
      commitGroupMove(delta.dxMm, delta.dyMm);
      return;
    }
    const run = scene.runs.find((r) => r.id === runId);
    if (!run) return;
    updateRun(runId, {
      originX: Math.round(run.originX + delta.dxMm),
      originY: Math.round(run.originY + delta.dyMm),
    });
    persistFreshRun(runId);
  };

  const onStackRun = (runId: string, delta: PlanMoveDelta, geomZMm: number) => {
    const run = scene.runs.find((r) => r.id === runId);
    if (!run) return;
    updateRun(runId, {
      originX: Math.round(run.originX + delta.dxMm),
      originY: Math.round(run.originY + delta.dyMm),
      geomZ: geomZMm,
    });
    persistFreshRun(runId);
  };

  const onMoveSlab = (slabId: string, delta: PlanMoveDelta) => {
    if (delta.dxMm === 0 && delta.dyMm === 0) return;
    if (multiSelectionHas(useDesignerStore.getState().multiSelection, 'slab', slabId)) {
      commitGroupMove(delta.dxMm, delta.dyMm);
      return;
    }
    const slab = (scene.slabs ?? []).find((s) => s.id === slabId);
    if (!slab) return;
    updateSlab(slabId, {
      originX: Math.round(slab.originX + delta.dxMm),
      originY: Math.round(slab.originY + delta.dyMm),
    });
  };

  const onRotateRun = (runId: string, commit: PlanRotationCommit) => {
    updateRun(runId, {
      rotationDeg: commit.rotationDeg,
      originX: commit.originX,
      originY: commit.originY,
    });
    persistFreshRun(runId);
  };

  const onStretchRun = (runId: string, patch: RunStretchPatch) => {
    // The server validator rejects GeomArcRadiusMm < 100 — without this gate the bow/corner
    // handles could commit a tiny-radius arc that APPEARS to apply, then silently reverts when
    // the persist 400s (RunArcSection has the same guard for its inspector inputs).
    if (typeof patch.geomArcRadiusMm === 'number' && patch.geomArcRadiusMm < 100) {
      queueToast({
        dedupeKey: 'glass-arc-radius-too-small',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.Arc.RadiusTooSmall', {
          defaultValue:
            'Bu ölçüler {{r}} mm yarıçap üretiyor — minimum 100 mm. Kirişi büyütün veya oku küçültün.',
          r: patch.geomArcRadiusMm,
        }),
      });
      return;
    }
    const beforeWidths = new Map(
      (scene.runs.find((r) => r.id === runId)?.panels ?? []).map((p) => [p.id, p.widthMm]),
    );
    updateRun(runId, patch);
    persistFreshRun(runId);
    // A length/arc change rescales the panel widths (withClampedRunLength). Persist the changed
    // panels so the server stays consistent with the run; otherwise the next reload re-normalizes
    // them and an arc panel's glass jumps (surfaced by toggling its hardware checkboxes).
    const fresh = useDesignerStore.getState().scene.runs.find((r) => r.id === runId);
    fresh?.panels.forEach((p) => {
      if (beforeWidths.get(p.id) !== p.widthMm) void persistPanel(runId, p);
    });
  };

  const onPushStretchRun = (
    runId: string,
    face: 'start' | 'end',
    targetMm: number,
    clampedMm: number,
    blockerId: string,
  ): boolean => {
    const run = scene.runs.find((r) => r.id === runId);
    const blocker = scene.runs.find((r) => r.id === blockerId);
    if (!run || !blocker) return false;
    const toBody = (r: SceneRunState): StretchBody => ({
      id: r.id,
      originX: r.originX,
      originY: r.originY,
      rotationDeg: r.rotationDeg,
      lengthMm: r.lengthMm,
      minLengthMm: MIN_RUN_LENGTH_MM,
    });
    const res = computeNeighbourShrink(toBody(run), face, toBody(blocker), targetMm, clampedMm);
    let selfGrow = res.selfGrowMm;
    let neighbour = res.neighbour ?? null;
    const rad = run.rotationDeg * DEG2RAD;
    const dirX = Math.cos(rad);
    const dirY = Math.sin(rad);
    if (neighbour) {
      const attachedWalls = new Set(findAttachedWallIds(run, scene.walls ?? []));
      const others = solidFootprints.filter(
        (o) => o.ownerId !== runId && o.ownerId !== blockerId && !attachedWalls.has(o.ownerId),
      );
      const aGrown = buildRunFootprint(
        { ...run, lengthMm: run.lengthMm + selfGrow },
        face === 'start' ? -selfGrow * dirX : 0,
        face === 'start' ? -selfGrow * dirY : 0,
        run.rotationDeg,
      );
      if (penetratesAny(aGrown, others)) {
        selfGrow = clampedMm;
        neighbour = null;
      }
    }
    const newLen = Math.max(MIN_RUN_LENGTH_MM, Math.round(run.lengthMm + selfGrow));
    if (newLen === run.lengthMm) return false;
    const aPatch: { id: string } & RunStretchPatch = { id: runId, lengthMm: newLen };
    if (face === 'start') {
      const shift = newLen - run.lengthMm;
      aPatch.originX = Math.round(run.originX - shift * dirX);
      aPatch.originY = Math.round(run.originY - shift * dirY);
    }
    const patches: Array<{ id: string } & RunStretchPatch> = [aPatch];
    if (neighbour) {
      patches.push({
        id: neighbour.id,
        lengthMm: neighbour.newLengthMm,
        originX: neighbour.newOriginX,
        originY: neighbour.newOriginY,
      });
    }
    applyRunPatches(patches);
    persistFreshRun(runId);
    if (neighbour) persistFreshRun(neighbour.id);
    return true;
  };

  const onCommitWallMove = (
    wallId: string,
    delta: PlanMoveDelta,
    attachedRunIds: string[],
    groupWallIds: string[],
  ) => {
    const state = useDesignerStore.getState();
    const wall = (state.scene.walls ?? []).find((w) => w.id === wallId);
    if (!wall) return;
    if (multiSelectionHas(state.multiSelection, 'wall', wallId)) {
      commitGroupMove(delta.dxMm, delta.dyMm, attachedRunIds, groupWallIds);
      return;
    }
    const movingWallIds = new Set([wallId, ...groupWallIds]);
    const movingRunIds = new Set(attachedRunIds);
    state.applyScenePatch((sceneState) => ({
      ...sceneState,
      walls: (sceneState.walls ?? []).map((w) =>
        movingWallIds.has(w.id)
          ? {
              ...w,
              originX: Math.round(w.originX + delta.dxMm),
              originY: Math.round(w.originY + delta.dyMm),
            }
          : w,
      ),
      runs: sceneState.runs.map((r) =>
        movingRunIds.has(r.id)
          ? {
              ...r,
              originX: Math.round(r.originX + delta.dxMm),
              originY: Math.round(r.originY + delta.dyMm),
            }
          : r,
      ),
    }));
    for (const runId of attachedRunIds) persistFreshRun(runId);
  };

  // Bare wall stack-on-top: move it and write the resting elevation so it can sit
  // on top of another wall/body instead of interpenetrating.
  const onStackWall = (wallId: string, delta: PlanMoveDelta, geomZMm: number) => {
    useDesignerStore.getState().applyScenePatch((s) => ({
      ...s,
      walls: (s.walls ?? []).map((w) =>
        w.id === wallId
          ? {
              ...w,
              originX: Math.round(w.originX + delta.dxMm),
              originY: Math.round(w.originY + delta.dyMm),
              geomZ: geomZMm,
            }
          : w,
      ),
    }));
  };

  const onCommitWallRotate = (
    wallId: string,
    commit: PlanRotationCommit,
    attachedRunIds: string[],
    groupWallIds: string[],
  ) => {
    const state = useDesignerStore.getState();
    const wall = (state.scene.walls ?? []).find((w) => w.id === wallId);
    if (!wall) return;
    const rad = wall.rotationDeg * DEG2RAD;
    // The commit pivot must equal the gesture/preview pivot (the adapter's CHORD midpoint) — for
    // an arc wall the straight origin+length/2 midpoint is a phantom (rotationDeg is the rolled
    // start tangent), so attached runs would rotate about a different point than the preview.
    const halfSweepRad = isRealArc(wall.geomArcRadiusMm, wall.geomArcSweepDeg)
      ? (((wall.geomArcSweepDeg ?? 0) / 2) * Math.PI) / 180
      : 0;
    const endHalfX = (wall.lengthMm * Math.cos(halfSweepRad)) / 2;
    const endHalfY = (wall.lengthMm * Math.sin(halfSweepRad)) / 2;
    const pivotX = wall.originX + endHalfX * Math.cos(rad) - endHalfY * Math.sin(rad);
    const pivotY = wall.originY + endHalfX * Math.sin(rad) + endHalfY * Math.cos(rad);
    // WHY: a multi-selection rotate previews every sibling orbiting the pivot, but the commit only
    // rotated group-walls + attached runs — the rest snapped back. When the pivot wall is part of the
    // multi-selection, rotate the selected walls/runs/slabs too so relative poses are preserved for
    // rotation exactly like commitGroupMove preserves them for translation.
    const ms = state.multiSelection;
    const isMulti = multiSelectionHas(ms, 'wall', wallId);
    const groupIds = new Set([...groupWallIds, ...(isMulti ? ms.wallIds : [])]);
    groupIds.delete(wallId);
    const movingRunIds = new Set([...attachedRunIds, ...(isMulti ? ms.runIds : [])]);
    const movingSlabIds = new Set(isMulti ? ms.slabIds : []);
    state.applyScenePatch((sceneState) => ({
      ...sceneState,
      walls: (sceneState.walls ?? []).map((w) => {
        if (w.id === wallId) {
          return {
            ...w,
            rotationDeg: commit.rotationDeg,
            originX: commit.originX,
            originY: commit.originY,
          };
        }
        if (!groupIds.has(w.id)) return w;
        const origin = rotatePlanPointDeg(w.originX, w.originY, pivotX, pivotY, commit.sweepDeg);
        return {
          ...w,
          originX: Math.round(origin.x),
          originY: Math.round(origin.y),
          rotationDeg: normalizePlanAngleDeg(w.rotationDeg + commit.sweepDeg),
        };
      }),
      runs: sceneState.runs.map((r) => {
        if (!movingRunIds.has(r.id)) return r;
        const origin = rotatePlanPointDeg(r.originX, r.originY, pivotX, pivotY, commit.sweepDeg);
        return {
          ...r,
          originX: Math.round(origin.x),
          originY: Math.round(origin.y),
          rotationDeg: normalizePlanAngleDeg(r.rotationDeg + commit.sweepDeg),
        };
      }),
      slabs: (sceneState.slabs ?? []).map((sl) => {
        if (!movingSlabIds.has(sl.id)) return sl;
        const origin = rotatePlanPointDeg(sl.originX, sl.originY, pivotX, pivotY, commit.sweepDeg);
        return {
          ...sl,
          originX: Math.round(origin.x),
          originY: Math.round(origin.y),
          rotationDeg: normalizePlanAngleDeg((sl.rotationDeg ?? 0) + commit.sweepDeg),
        };
      }),
    }));
    for (const runId of movingRunIds) persistFreshRun(runId);
  };

  // Membership centre of a straight OR arc body: for an arc the straight-line midpoint is a
  // phantom (rotationDeg is the rolled tangent) — use the CHORD midpoint from the true end.
  const planBodyCenterMm = (body: {
    originX: number;
    originY: number;
    rotationDeg: number;
    lengthMm: number;
    geomArcRadiusMm?: number | null;
    geomArcSweepDeg?: number | null;
  }) => {
    const rad = body.rotationDeg * DEG2RAD;
    if (isRealArc(body.geomArcRadiusMm, body.geomArcSweepDeg)) {
      const e = arcEndLocal(body.geomArcRadiusMm ?? 0, body.geomArcSweepDeg ?? 1);
      return {
        x: body.originX + (e.xMm / 2) * Math.cos(rad) - (e.yMm / 2) * Math.sin(rad),
        y: body.originY + (e.xMm / 2) * Math.sin(rad) + (e.yMm / 2) * Math.cos(rad),
      };
    }
    return {
      x: body.originX + (body.lengthMm / 2) * Math.cos(rad),
      y: body.originY + (body.lengthMm / 2) * Math.sin(rad),
    };
  };

  const handleMarquee = (polygonMm: { x: number; y: number }[]) => {
    const state = useDesignerStore.getState().scene;
    const runIds = state.runs
      .filter((run) => pointInPolygonMm(planBodyCenterMm(run), polygonMm))
      .map((run) => run.id);
    const wallIds = (state.walls ?? [])
      .filter((wall) => pointInPolygonMm(planBodyCenterMm(wall), polygonMm))
      .map((wall) => wall.id);
    const slabIds = (state.slabs ?? [])
      .filter((slab) => {
        const rad = slab.rotationDeg * DEG2RAD;
        const cos = Math.cos(rad);
        const sin = Math.sin(rad);
        // A plan-curved slab bows AWAY from its flat rect — test the real band's mid-column
        // (between the front and back apex) instead of the phantom rect centre.
        if (isRealArc(slab.geomArcRadiusMm, slab.geomArcSweepDeg)) {
          const columns = curvedSlabPlanColumnsMm(
            slab.lengthMm,
            slab.depthMm,
            slab.geomArcRadiusMm ?? 0,
            slab.geomArcSweepDeg ?? 1,
            slab.slabArcAxis ?? 'length',
          );
          const mid = columns[Math.floor(columns.length / 2)];
          const lx = (mid.front.x + mid.back.x) / 2;
          const lz = (mid.front.z + mid.back.z) / 2;
          const center = {
            x: slab.originX + lx * cos - lz * sin,
            y: slab.originY + lx * sin + lz * cos,
          };
          return pointInPolygonMm(center, polygonMm);
        }
        const center = {
          x: slab.originX + (slab.lengthMm / 2) * cos - (slab.depthMm / 2) * sin,
          y: slab.originY + (slab.lengthMm / 2) * sin + (slab.depthMm / 2) * cos,
        };
        return pointInPolygonMm(center, polygonMm);
      })
      .map((slab) => slab.id);
    setMultiSelect({ runIds, wallIds, slabIds, order: [] });
  };

  const placeWall = (draft: PlacementWallDraft) => {
    const wall: SceneWallState = {
      id: crypto.randomUUID(),
      originX: draft.originX,
      originY: draft.originY,
      rotationDeg: draft.rotationDeg,
      lengthMm: draft.lengthMm,
      heightMm: draft.heightMm,
      heightEndMm: null,
      thicknessMm: draft.thicknessMm,
      colorHex: null,
      openings: [],
      geomArcRadiusMm: placementShape === 'curved' ? draft.lengthMm : null,
      geomArcSweepDeg: placementShape === 'curved' ? 60 : null,
    };
    addWall(wall);
    setPlacement(null);
    setSelection({
      kind: 'wall',
      runId: null,
      panelId: null,
      connectionId: null,
      hardwareId: null,
      wallId: wall.id,
    });
  };

  const placeSlab = (kind: 'floor' | 'roof', draft: PlacementSlabDraft) => {
    const slab: SceneSlabState = {
      id: crypto.randomUUID(),
      kind,
      originX: draft.originX,
      originY: draft.originY,
      rotationDeg: 0,
      lengthMm: draft.lengthMm,
      depthMm: draft.depthMm,
      thicknessMm: draft.thicknessMm,
      elevationMm: draft.elevationMm,
      colorHex: null,
      arcRiseMm:
        placementShape === 'curved' ? Math.max(150, Math.round(draft.depthMm * 0.15)) : null,
    };
    addSlab(slab);
    setPlacement(null);
    setSelection({
      kind: 'slab',
      runId: null,
      panelId: null,
      connectionId: null,
      hardwareId: null,
      wallId: null,
      slabId: slab.id,
    });
  };

  const placeRun = async (draft: PlacementRunDraft) => {
    setPlacement(null);
    if (!projectId || profileSystems.length === 0) return;
    const runCount = useDesignerStore.getState().scene.runs.length;
    // Curved placement creates the run AS AN ARC in one call (the pasteRunAt pattern): the server
    // then sizes the panels from the DEVELOPED length. A straight create + arc patch left the
    // server panels summing to the CHORD (BOM glass under-measured ~4.5% at 60°), and the old
    // local patch targeted runs[runs.length-1] — the wrong run before the refetch landed.
    // rotationDeg rolls by −sweep/2 so the chord stays along the placement direction; at 60° the
    // radius equals the chord (2·r·sin30° = r).
    const curved = placementShape === 'curved';
    const developedMm = curved
      ? developedLengthMm(draft.lengthMm, draft.lengthMm, 60)
      : draft.lengthMm;
    await safeRequestWithNotify(
      enqueuePersist(() =>
        addRunMutation.mutateAsync({
          id: projectId,
          input: {
            lengthMm: draft.lengthMm,
            heightMm: draft.heightMm,
            profileSystemId: profileSystems[0].id,
            originX: draft.originX,
            originY: draft.originY,
            rotationDeg: curved
              ? Math.round((draft.rotationDeg - 30) * 100) / 100
              : draft.rotationDeg,
            panelCount: Math.max(1, Math.ceil(developedMm / PANEL_TARGET_WIDTH_MM)),
            label: `${t('GlassEnclosure.Designer.DefaultRunLabel', { defaultValue: 'Hat' })} ${
              runCount + 1
            }`,
            colorId: colors[0]?.id ?? null,
            hasTopDrip: true,
            hasBottomThreshold: false,
            geomZ: 0,
            geomArcRadiusMm: curved ? draft.lengthMm : null,
            geomArcSweepDeg: curved ? 60 : null,
            arcGlassBent: false,
            notes: null,
          },
        }),
      ),
      { successMessage: t('GlassEnclosure.Designer.RunAdded', { defaultValue: 'Hat eklendi' }) },
    );
  };

  const pasteSpec = useMemo<PasteGhostSpec | null>(() => {
    if (!pasteArmed || !clipboard) return null;
    if (clipboard.kind === 'run') {
      const zMin = clipboard.run.geomZ ?? 0;
      return {
        lengthMm: clipboard.run.lengthMm,
        halfWidthMm: RUN_PLAN_THICKNESS_MM / 2,
        zMinMm: zMin,
        zMaxMm: zMin + clipboard.run.heightMm,
        rotationDeg: clipboard.run.rotationDeg,
      };
    }
    if (clipboard.kind === 'wall') {
      const zMin = clipboard.wall.geomZ ?? 0;
      return {
        lengthMm: clipboard.wall.lengthMm,
        halfWidthMm: clipboard.wall.thicknessMm / 2,
        zMinMm: zMin,
        zMaxMm:
          zMin +
          Math.max(clipboard.wall.heightMm, clipboard.wall.heightEndMm ?? clipboard.wall.heightMm),
        rotationDeg: clipboard.wall.rotationDeg,
      };
    }
    if (clipboard.kind === 'slab') {
      return {
        lengthMm: clipboard.slab.lengthMm,
        halfWidthMm: clipboard.slab.depthMm / 2,
        zMinMm: clipboard.slab.elevationMm,
        zMaxMm: clipboard.slab.elevationMm + clipboard.slab.thicknessMm,
        rotationDeg: clipboard.slab.rotationDeg,
      };
    }
    return null;
  }, [pasteArmed, clipboard]);

  const pasteRunAt = async (source: SceneRunState, centerX: number, centerY: number) => {
    if (!projectId) return;
    const rad = source.rotationDeg * DEG2RAD;
    const originX = Math.round(centerX - (source.lengthMm / 2) * Math.cos(rad));
    const originY = Math.round(centerY - (source.lengthMm / 2) * Math.sin(rad));
    const runCount = useDesignerStore.getState().scene.runs.length;
    const [response] = await safeRequestWithNotify(
      enqueuePersist(() =>
        addRunMutation.mutateAsync({
          id: projectId,
          input: {
            lengthMm: source.lengthMm,
            heightMm: source.heightMm,
            profileSystemId: source.profileSystemId,
            originX,
            originY,
            rotationDeg: source.rotationDeg,
            panelCount: Math.max(1, source.panels.length),
            label: `${source.label} ${runCount + 1}`,
            colorId: source.colorId,
            hasTopDrip: source.hasTopDrip,
            hasBottomThreshold: source.hasBottomThreshold,
            geomZ: source.geomZ ?? null,
            geomArcRadiusMm: source.geomArcRadiusMm ?? null,
            geomArcSweepDeg: source.geomArcSweepDeg ?? null,
            arcGlassBent: source.arcGlassBent ?? false,
            notes: null,
          },
        }),
      ),
      { successMessage: t('GlassEnclosure.Designer.RunAdded', { defaultValue: 'Hat eklendi' }) },
    );
    const created = response?.data;
    if (!created) return;
    const createdPanels = [...created.panels].sort((a, b) => a.panelIndex - b.panelIndex);
    const sourcePanels = [...source.panels].sort((a, b) => a.panelIndex - b.panelIndex);
    for (let i = 0; i < Math.min(createdPanels.length, sourcePanels.length); i += 1) {
      void persistPanel(created.id, {
        ...sourcePanels[i],
        id: createdPanels[i].id,
        panelIndex: i,
        hardware: [],
      });
    }
  };

  const handlePasteAt = (centerX: number, centerY: number) => {
    if (!clipboard) return;
    setPasteArmed(false);
    if (clipboard.kind === 'wall') {
      const wall = clipboard.wall;
      const rad = wall.rotationDeg * DEG2RAD;
      const clone: SceneWallState = {
        ...structuredClone(wall),
        id: crypto.randomUUID(),
        originX: Math.round(centerX - (wall.lengthMm / 2) * Math.cos(rad)),
        originY: Math.round(centerY - (wall.lengthMm / 2) * Math.sin(rad)),
        openings: (wall.openings ?? []).map((o) => ({ ...o, id: crypto.randomUUID() })),
        features: (wall.features ?? []).map((f) => ({ ...f, id: crypto.randomUUID() })),
      };
      addWall(clone);
      setSelection({
        kind: 'wall',
        runId: null,
        panelId: null,
        connectionId: null,
        hardwareId: null,
        wallId: clone.id,
      });
      return;
    }
    if (clipboard.kind === 'slab') {
      const slab = clipboard.slab;
      const rad = slab.rotationDeg * DEG2RAD;
      const cos = Math.cos(rad);
      const sin = Math.sin(rad);
      const clone: SceneSlabState = {
        ...structuredClone(slab),
        id: crypto.randomUUID(),
        originX: Math.round(centerX - (slab.lengthMm / 2) * cos + (slab.depthMm / 2) * sin),
        originY: Math.round(centerY - (slab.lengthMm / 2) * sin - (slab.depthMm / 2) * cos),
        features: (slab.features ?? []).map((f) => ({ ...f, id: crypto.randomUUID() })),
      };
      addSlab(clone);
      setSelection({
        kind: 'slab',
        runId: null,
        panelId: null,
        connectionId: null,
        hardwareId: null,
        wallId: null,
        slabId: clone.id,
      });
      return;
    }
    if (clipboard.kind === 'run') void pasteRunAt(clipboard.run, centerX, centerY);
  };

  const onDragHardware = (
    runId: string,
    panelId: string,
    hardwareId: string,
    delta: { dx: number; dy: number; dz: number },
  ) => {
    const run = scene.runs.find((r) => r.id === runId);
    const panel = run?.panels.find((p) => p.id === panelId);
    const item = panel?.hardware.find((h) => h.id === hardwareId);
    if (!run || !panel || !item) return;
    const edgeX = Math.max(0, panel.widthMm / 2 - item.widthMm / 2);
    const edgeY = Math.max(
      0,
      glassClampHeightMm(panel.heightMm, run.heightMm) / 2 - item.heightMm / 2,
    );
    let offsetXmm = stickyMm(snapMm(item.offsetXmm + delta.dx), [0, edgeX, -edgeX]);
    let offsetYmm = stickyMm(snapMm(item.offsetYmm + delta.dy), [0, edgeY, -edgeY]);
    for (const cornerX of [edgeX, -edgeX]) {
      for (const cornerY of [edgeY, -edgeY]) {
        if (Math.hypot(offsetXmm - cornerX, offsetYmm - cornerY) <= STICKY_SNAP_MM * 1.6) {
          offsetXmm = Math.round(cornerX);
          offsetYmm = Math.round(cornerY);
        }
      }
    }
    updateHardware(runId, panelId, hardwareId, {
      offsetXmm: clampMm(offsetXmm, edgeX),
      offsetYmm: clampMm(offsetYmm, edgeY),
      offsetZmm: snapMm(item.offsetZmm + delta.dz),
    });
  };

  const onResizeHardware = (
    runId: string,
    panelId: string,
    hardwareId: string,
    widthMm: number,
    heightMm: number,
  ) => {
    const run = scene.runs.find((r) => r.id === runId);
    const panel = run?.panels.find((p) => p.id === panelId);
    const item = panel?.hardware.find((h) => h.id === hardwareId);
    if (!run || !panel || !item) return;
    const nextW = Math.min(
      Math.round(panel.widthMm),
      Math.max(MIN_HARDWARE_MM, Math.round(widthMm)),
    );
    const nextH = Math.min(
      Math.round(run.heightMm),
      Math.max(MIN_HARDWARE_MM, Math.round(heightMm)),
    );
    // A larger item must still fit centred within the panel, so re-clamp the offset to the new edge.
    const edgeX = Math.max(0, panel.widthMm / 2 - nextW / 2);
    const edgeY = Math.max(0, glassClampHeightMm(panel.heightMm, run.heightMm) / 2 - nextH / 2);
    updateHardware(runId, panelId, hardwareId, {
      widthMm: nextW,
      heightMm: nextH,
      offsetXmm: clampMm(item.offsetXmm, edgeX),
      offsetYmm: clampMm(item.offsetYmm, edgeY),
    });
  };

  const geometryMode = project?.geometryMode ?? 'Planar';

  const renderGeometry = (): ReactNode => {
    if (project && geometryMode === 'FreeForm') {
      const vertices = parsePolygonVertices(project.polygonVerticesJson);
      if (vertices.length >= 3) {
        return (
          <PolygonFacadeGeometry
            vertices={vertices}
            heightMm={project.eaveHeightMm ?? 2600}
            quality={quality}
            glassTypes={glassMap}
            colors={colorMap}
          />
        );
      }
    }
    if (project && geometryMode === 'Pitched' && project.enclosureSubtype === 'Greenhouse') {
      return (
        <PitchedGreenhouseGeometry
          runs={scene.runs}
          roofPitchDeg={project.roofPitchDeg ?? 15}
          ridgeHeightMm={project.ridgeHeightMm ?? 3000}
          eaveHeightMm={project.eaveHeightMm ?? 2200}
          quality={quality}
          glassTypes={glassMap}
          colors={colorMap}
        />
      );
    }

    return scene.runs.map((run) => {
      const sharedProps = {
        run,
        system: systemMap.get(run.profileSystemId),
        color: run.colorId ? colorMap.get(run.colorId) : undefined,
        glassTypes: glassMap,
        quality,
        showAnnotations: showAnnotations && !presentation,
        selectedPanelId: selection.panelId,
        selectedRunId: multiSelection.runIds.includes(run.id) ? run.id : selection.runId,
        selectedHardwareId: selection.hardwareId ?? null,
        onSelectRun,
        onSelectPanel,
        onSelectHardware,
        onDragHardware,
        onResizeHardware,
      };
      if (isRealArc(run.geomArcRadiusMm, run.geomArcSweepDeg)) {
        return (
          <ArcRunGroup
            key={run.id}
            {...sharedProps}
            onMoveRun={interactionsEnabled ? onMoveRun : undefined}
            onRotateRun={interactionsEnabled ? onRotateRun : undefined}
            onStretchRun={interactionsEnabled ? onStretchRun : undefined}
            onStackRun={interactionsEnabled ? onStackRun : undefined}
            snapTargets={snapTargets}
            obstacles={runObstacles}
            supports={supportFootprints}
          />
        );
      }
      return (
        <RunGroup
          key={run.id}
          {...sharedProps}
          onResizePanel={onResizePanel}
          onMoveRun={interactionsEnabled ? onMoveRun : undefined}
          onRotateRun={interactionsEnabled ? onRotateRun : undefined}
          onStretchRun={interactionsEnabled ? onStretchRun : undefined}
          onPushStretchRun={interactionsEnabled ? onPushStretchRun : undefined}
          onStackRun={interactionsEnabled ? onStackRun : undefined}
          snapTargets={snapTargets}
          obstacles={runObstacles}
          supports={supportFootprints}
        />
      );
    });
  };

  const renderWalls = () =>
    (scene.walls ?? []).map((wall) => (
      <WallObject
        key={wall.id}
        wall={wall}
        isSelected={
          (selection.kind === 'wall' && selection.wallId === wall.id) ||
          multiSelection.wallIds.includes(wall.id)
        }
        onSelect={onSelectWall}
        snapTargets={snapTargets}
        obstacles={wallObstacles}
        supports={supportFootprints}
        interactive={interactionsEnabled}
        onCommitMove={onCommitWallMove}
        onStackWall={interactionsEnabled ? onStackWall : undefined}
        onCommitRotate={onCommitWallRotate}
        penActive={placement === 'pen'}
        onPenFaceClick={onPenFaceClick}
        onPenFaceArc={onPenFaceArc}
        onPenFaceFinish={onPenFaceFinish}
      />
    ));

  const renderSurfaces = () =>
    (scene.surfaces ?? []).map((surface) => (
      <PolygonSurfaceObject
        key={surface.id}
        surface={surface}
        isSelected={selection.kind === 'surface' && selection.surfaceId === surface.id}
        interactive={interactionsEnabled}
        penActive={placement === 'pen'}
        supports={supportFootprints}
        onSelect={onSelectSurface}
      />
    ));

  const renderSlabs = () =>
    (scene.slabs ?? []).map((slab) => (
      <SlabObject
        key={slab.id}
        slab={slab}
        isSelected={
          (selection.kind === 'slab' && selection.slabId === slab.id) ||
          multiSelection.slabIds.includes(slab.id)
        }
        onSelect={onSelectSlab}
        snapTargets={snapTargets}
        obstacles={planObstacles}
        supports={supportFootprints}
        interactive={interactionsEnabled}
        onCommitMove={onMoveSlab}
        penActive={placement === 'pen'}
        onPenFaceClick={onPenFaceClick}
        onPenFaceArc={onPenFaceArc}
        onPenFaceFinish={onPenFaceFinish}
      />
    ));

  const pasteHint =
    clipboard?.kind === 'hardware'
      ? t('GlassEnclosure.Designer.PasteHardwareHint', {
          defaultValue: 'Yapıştır: hedef panele tıkla · Esc iptal',
        })
      : clipboard?.kind === 'panel'
        ? t('GlassEnclosure.Designer.PastePanelHint', {
            defaultValue: 'Yapıştır: hedef hatta veya panele tıkla · Esc iptal',
          })
        : t('GlassEnclosure.Designer.PastePlaceHint', {
            defaultValue: 'Yapıştır: konuma tıkla · Esc iptal',
          });

  const cursorClass = pasteArmed
    ? 'cursor-copy'
    : activeTool === 'paint' || activeTool === 'draw' || activeTool === 'measure' || placement
      ? 'cursor-crosshair'
      : '';

  return (
    <div
      className={`relative h-full w-full ${cursorClass}`}
      onPointerMove={(e) => {
        if (!pasteArmed) return;
        const rect = e.currentTarget.getBoundingClientRect();
        setCursor({ x: e.clientX - rect.left, y: e.clientY - rect.top });
      }}
    >
      <SceneViewport
        quality={quality}
        presentation={presentation}
        appearance={appearance}
        initialCamera={scene.camera ?? undefined}
        onCameraChange={setCamera}
        onPointerMissed={clearSelection}
      >
        <group ref={registerExportRoot}>
          {layerVisibility.runs && renderGeometry()}
          {layerVisibility.runs && scene.connections.length > 0 && (
            <ConnectionPosts
              connections={scene.connections}
              runs={scene.runs}
              colors={colorMap}
              quality={quality}
            />
          )}
          {layerVisibility.walls && renderWalls()}
          {layerVisibility.slabs && renderSlabs()}
          {layerVisibility.surfaces && renderSurfaces()}
        </group>
        <SnapGuideOverlay />
        {placement === 'pen' && !penFace && (
          <PenController snapTargets={snapTargets} onFinish={handlePenFinish} mode={penMode} />
        )}
        {placement && placement !== 'pen' && (
          <PlacementController
            placement={placement}
            runs={scene.runs}
            walls={scene.walls ?? []}
            snapTargets={snapTargets}
            obstacles={placementObstacles}
            onPlaceWall={placeWall}
            onPlaceRun={(draft) => void placeRun(draft)}
            onPlaceSlab={placeSlab}
          />
        )}
        {!placement && pasteSpec && (
          <PasteController
            spec={pasteSpec}
            snapTargets={snapTargets}
            obstacles={planObstacles}
            onPlace={handlePasteAt}
          />
        )}
        {!placement && !pasteSpec && activeTool === 'multiselect' && (
          <MarqueeController onSelect={handleMarquee} />
        )}
        {!placement && !pasteSpec && activeTool === 'measure' && (
          <MeasureController snapTargets={snapTargets} />
        )}
      </SceneViewport>
      {pasteArmed && cursor && (
        <div
          className="pointer-events-none absolute z-20 rounded-md bg-primary-600/95 px-2.5 py-1 text-xs font-medium text-white shadow-lg"
          style={{ left: cursor.x + 14, top: cursor.y + 14 }}
        >
          {pasteHint}
        </div>
      )}
      <DragReadoutOverlay />
      {(floatingCount > 0 || catalogViolations > 0) && (
        <div className="pointer-events-none absolute bottom-3 left-3 z-20 flex flex-col gap-1.5">
          {floatingCount > 0 && (
            <div className="rounded-md bg-warning-500/95 px-2.5 py-1 text-xs font-medium text-white shadow-lg">
              {t('GlassEnclosure.Designer.FloatingWarning', {
                defaultValue: '⚠ {{count}} nesne desteksiz (boşlukta)',
                count: floatingCount,
              })}
            </div>
          )}
          {catalogViolations > 0 && (
            <div className="rounded-md bg-danger-600/95 px-2.5 py-1 text-xs font-medium text-white shadow-lg">
              {t('GlassEnclosure.Designer.CatalogWarning', {
                defaultValue: '⚠ {{count}} hat katalog limitini aşıyor (panel ölçü/ağırlık)',
                count: catalogViolations,
              })}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
