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
import { arcEndLocal, arcPointAt, effectiveArcRadiusMm } from '../model/arcGeometry';
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
import { findAttachedWallIds } from '../model/wallAttachment';
import { rotatePlanPointDeg } from './interaction/planTransform';
import { wallFaceFrame, type WallFeatureSide } from './builders/wallFaces';
import {
  FEATURE_EDGE_MARGIN_MM,
  FREE_SIMPLIFY_TOLERANCE_MM,
  featureFitsWall,
  featureOutlineMm,
  outlineFitsRect,
  simplifyFreePoints,
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
  for (const wall of walls) {
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
    if (run.geomArcRadiusMm && run.geomArcRadiusMm > 0) {
      const rad = run.rotationDeg * DEG2RAD;
      const cos = Math.cos(rad);
      const sin = Math.sin(rad);
      const dir = (run.geomArcSweepDeg ?? 1) < 0 ? -1 : 1;
      const radius = effectiveArcRadiusMm(run.lengthMm, run.geomArcRadiusMm);
      const sweepRad = Math.min(run.lengthMm / radius, Math.PI * 2);
      const e = arcEndLocal(run.lengthMm, run.geomArcRadiusMm, run.geomArcSweepDeg ?? 1);
      const apex = arcPointAt(radius, dir, sweepRad / 2);
      const toWorld = (lx: number, ly: number) => ({
        x: run.originX + lx * cos - ly * sin,
        y: run.originY + lx * sin + ly * cos,
      });
      const end = toWorld(e.xMm, e.yMm);
      const mid = toWorld(apex.x, apex.z);
      points.push(
        { ownerId: run.id, x: run.originX, y: run.originY },
        { ownerId: run.id, x: end.x, y: end.y },
        { ownerId: run.id, x: mid.x, y: mid.y },
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
  const activeTool = useDesignerStore((s) => s.activeTool);
  const placement = useDesignerStore((s) => s.placement);
  const paintColor = useDesignerStore((s) => s.paintColor);
  const paintMaterial = useDesignerStore((s) => s.paintMaterial);
  const multiSelection = useDesignerStore((s) => s.multiSelection);
  const toggleMultiSelect = useDesignerStore((s) => s.toggleMultiSelect);
  const setMultiSelect = useDesignerStore((s) => s.setMultiSelect);
  const setPlacement = useDesignerStore((s) => s.setPlacement);
  const onPenFaceFinishRef = useRef<() => void>(() => {});
  const { appearance } = useViewerAppearance();
  const { createPanelFrom, persistPanel, deletePanel } = usePanelEntityActions();
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
  }, [scene, addHardware]);

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
      offsetYmm: clampMm(clipboard.item.offsetYmm, run.heightMm / 2),
    };
    addHardware(runId, panelId, clone);
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
    pts = simplifyFreePoints(
      pts.map((p) => ({ x: p.x, z: p.z })),
      FREE_SIMPLIFY_TOLERANCE_MM,
    );
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
    if (polygonSelfIntersects(pts.map((p) => ({ x: p.x, y: p.z })))) {
      queueToast({
        dedupeKey: 'glass-pen-self-intersect',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.Pen.SelfIntersect', {
          defaultValue: 'Çizim kendisiyle kesişiyor; geçerli bir alan oluşturulamadı.',
        }),
      });
      return;
    }
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
    // Stored side uses 1/-1 for front/back, the string for the four side faces (slab → 1/-1).
    const featureSide: WallFeatureSideValue =
      session.side === 'front' ? 1 : session.side === 'back' ? -1 : session.side;
    const feature = {
      id: crypto.randomUUID(),
      shape: 'free' as const,
      // A pen-drawn shape defaults to a NON-CUTTING outline (flush recess, no depth): it shows as
      // a line and the user chooses hole / recess+depth / protrusion in the inspector.
      mode: 'recess' as const,
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
      if (wall.geomArcRadiusMm && wall.geomArcRadiusMm > 0) {
        queueToast({
          dedupeKey: 'glass-arc-no-feature',
          variant: 'warning',
          description: t('GlassEnclosure.Designer.Pen.ArcNoFeature', {
            defaultValue: 'Kavisli duvara henüz açıklık/şekil çizilemiyor.',
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
      if (slab.kind === 'roof' && (slab.arcRiseMm ?? 0) > 0) {
        queueToast({
          dedupeKey: 'glass-arc-no-feature',
          variant: 'warning',
          description: t('GlassEnclosure.Designer.Pen.ArcNoFeature', {
            defaultValue: 'Kavisli yüzeye henüz açıklık/şekil çizilemiyor.',
          }),
        });
        return;
      }
      if (!outlineFitsRect(outline, slab.lengthMm, slab.depthMm, FEATURE_EDGE_MARGIN_MM)) {
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

  const onPenFaceClick = (
    hostKind: 'wall' | 'slab',
    hostId: string,
    side: WallFeatureSide,
    pt: { x: number; z: number },
  ) => {
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
      if (before.get(p.id) !== p.widthMm) void persistPanel(runId, p);
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
    for (const id of ms.runIds) persistFreshRun(id);
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
    updateRun(runId, patch);
    persistFreshRun(runId);
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
    const pivotX = wall.originX + (wall.lengthMm / 2) * Math.cos(rad);
    const pivotY = wall.originY + (wall.lengthMm / 2) * Math.sin(rad);
    const groupIds = new Set(groupWallIds);
    const movingRunIds = new Set(attachedRunIds);
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
    }));
    for (const runId of attachedRunIds) persistFreshRun(runId);
  };

  const handleMarquee = (polygonMm: { x: number; y: number }[]) => {
    const state = useDesignerStore.getState().scene;
    const runIds = state.runs
      .filter((run) => {
        const rad = run.rotationDeg * DEG2RAD;
        const center = {
          x: run.originX + (run.lengthMm / 2) * Math.cos(rad),
          y: run.originY + (run.lengthMm / 2) * Math.sin(rad),
        };
        return pointInPolygonMm(center, polygonMm);
      })
      .map((run) => run.id);
    const wallIds = (state.walls ?? [])
      .filter((wall) => {
        const rad = wall.rotationDeg * DEG2RAD;
        const center = {
          x: wall.originX + (wall.lengthMm / 2) * Math.cos(rad),
          y: wall.originY + (wall.lengthMm / 2) * Math.sin(rad),
        };
        return pointInPolygonMm(center, polygonMm);
      })
      .map((wall) => wall.id);
    const slabIds = (state.slabs ?? [])
      .filter((slab) => {
        const rad = slab.rotationDeg * DEG2RAD;
        const cos = Math.cos(rad);
        const sin = Math.sin(rad);
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
            rotationDeg: draft.rotationDeg,
            panelCount: Math.max(1, Math.ceil(draft.lengthMm / PANEL_TARGET_WIDTH_MM)),
            label: `${t('GlassEnclosure.Designer.DefaultRunLabel', { defaultValue: 'Hat' })} ${
              runCount + 1
            }`,
            colorId: colors[0]?.id ?? null,
            hasTopDrip: true,
            hasBottomThreshold: false,
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
    const edgeY = Math.max(0, run.heightMm / 2 - item.heightMm / 2);
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
    const edgeY = Math.max(0, run.heightMm / 2 - nextH / 2);
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
      if (run.geomArcRadiusMm && run.geomArcRadiusMm > 0) {
        return (
          <ArcRunGroup
            key={run.id}
            radiusMm={run.geomArcRadiusMm}
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
          defaultValue: 'YapÄ±ÅŸtÄ±r: hedef panele tÄ±kla Â· Esc iptal',
        })
      : clipboard?.kind === 'panel'
        ? t('GlassEnclosure.Designer.PastePanelHint', {
            defaultValue: 'YapÄ±ÅŸtÄ±r: hedef hatta veya panele tÄ±kla Â· Esc iptal',
          })
        : t('GlassEnclosure.Designer.PastePlaceHint', {
            defaultValue: 'YapÄ±ÅŸtÄ±r: konuma tÄ±kla Â· Esc iptal',
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
          <PenController snapTargets={snapTargets} onFinish={handlePenFinish} />
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
