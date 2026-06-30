import { create } from 'zustand';
import type {
  CornerRadiiMm,
  GlassProjectDto,
  GlassValidationFindingDto,
  RunFrameEdges,
  SceneCameraState,
  SceneConnectionState,
  SceneHardwareItem,
  ScenePanelState,
  SceneRunState,
  SceneSlabState,
  SceneState,
  SceneSurfaceState,
  SceneWallFeature,
  SceneWallOpening,
  SceneWallState,
} from './project.types';
import type { GlassOpeningType } from './glassEnclosure.types';
import type { CornerFillMode } from './multiAutofill';
import { MIN_PANEL_MM, cascadePanelWidths } from './panelResize';
import { chordFromRadiusSweep } from './arcGeometry';
import type { QualityPreset } from '@/shared/three-engine';

export type { QualityPreset };

export interface DesignerSelection {
  kind:
    | 'run'
    | 'panel'
    | 'connection'
    | 'hardware'
    | 'wall'
    | 'slab'
    | 'wallFeature'
    | 'slabFeature'
    | 'surface'
    | null;
  runId: string | null;
  panelId: string | null;
  connectionId: string | null;
  hardwareId?: string | null;
  wallId?: string | null;
  slabId?: string | null;
  featureId?: string | null;
  surfaceId?: string | null;
}

export type DesignerClipboard =
  | { kind: 'panel'; panel: ScenePanelState }
  | { kind: 'hardware'; item: SceneHardwareItem }
  | { kind: 'run'; run: SceneRunState }
  | { kind: 'wall'; wall: SceneWallState }
  | { kind: 'slab'; slab: SceneSlabState };

export type DesignerTool =
  | 'select'
  | 'multiselect'
  | 'move'
  | 'rotate'
  | 'stretch'
  | 'draw'
  | 'paint'
  | 'erase'
  | 'measure';

export type MultiSelectKind = 'run' | 'wall' | 'slab';

export interface MultiSelectRef {
  kind: MultiSelectKind;
  id: string;
}

export interface MultiSelection {
  runIds: string[];
  wallIds: string[];
  slabIds: string[];
  order: MultiSelectRef[];
}

export const EMPTY_MULTI_SELECTION: MultiSelection = {
  runIds: [],
  wallIds: [],
  slabIds: [],
  order: [],
};

const EMPTY_SELECTION: DesignerSelection = {
  kind: null,
  runId: null,
  panelId: null,
  connectionId: null,
  hardwareId: null,
  wallId: null,
  slabId: null,
  featureId: null,
  surfaceId: null,
};

export type PlacementKind = 'run' | 'wall' | 'floor' | 'roof' | 'pen';

export interface PenFacePoint {
  x: number;
  z: number;
}

export interface PenFaceSession {
  hostKind: 'wall' | 'slab';
  hostId: string;
  // Which of the host's six faces the pen is drawing on (slabs use front/back only). The points
  // below are in that face's in-plane (u,v) mm.
  side: 'front' | 'back' | 'top' | 'bottom' | 'left' | 'right';
  points: PenFacePoint[];
  cursor: PenFacePoint | null;
}

export type WallDrawShape =
  | 'rect'
  | 'circle'
  | 'ellipse'
  | 'triangle'
  | 'polygon'
  | 'free'
  | 'split';

export interface PaintColor {
  id: string | null;
  hex: string;
}

interface DesignerState {
  projectId: string | null;
  project: GlassProjectDto | null;
  scene: SceneState;
  selection: DesignerSelection;
  quality: QualityPreset;
  showAnnotations: boolean;
  presentationMode: boolean;
  layerVisibility: { runs: boolean; walls: boolean; slabs: boolean; surfaces: boolean };
  isDirty: boolean;
  history: SceneState[];
  historyIndex: number;
  validation: GlassValidationFindingDto[];
  clipboard: DesignerClipboard | null;
  pasteArmed: boolean;
  activeTool: DesignerTool;
  placement: PlacementKind | null;
  paintColor: PaintColor | null;
  paintMaterial: string | null;
  drawShape: WallDrawShape;
  cornerFillMode: CornerFillMode;
  multiSelection: MultiSelection;
  penFace: PenFaceSession | null;
  transformHandlesActive: boolean;
  stackOnDrop: boolean;

  setActiveTool: (tool: DesignerTool) => void;
  toggleTransformHandles: () => void;
  setTransformHandles: (active: boolean) => void;
  toggleStackOnDrop: () => void;
  setStackOnDrop: (active: boolean) => void;
  setPlacement: (placement: PlacementKind | null) => void;
  setPaintColor: (color: PaintColor | null) => void;
  setPaintMaterial: (materialKey: string | null) => void;
  setDrawShape: (shape: WallDrawShape) => void;
  setCornerFillMode: (mode: CornerFillMode) => void;
  toggleMultiSelect: (kind: MultiSelectKind, id: string) => void;
  setMultiSelect: (selection: MultiSelection) => void;
  clearMultiSelect: () => void;
  setPenFace: (session: PenFaceSession | null) => void;
  setPenFaceCursor: (hostId: string, cursor: PenFacePoint | null) => void;
  applyScenePatch: (updater: (scene: SceneState) => SceneState) => void;
  setClipboard: (clipboard: DesignerClipboard | null) => void;
  setPasteArmed: (armed: boolean) => void;
  setRunGlassBent: (runId: string, bent: boolean) => void;
  setRunFrame: (
    runId: string,
    patch: { frameEdges?: RunFrameEdges | null; hasMullions?: boolean | null },
  ) => void;

  addWall: (wall: SceneWallState) => void;
  updateWall: (wallId: string, patch: Partial<SceneWallState>) => void;
  removeWall: (wallId: string) => void;
  addWallOpening: (wallId: string, opening: SceneWallOpening) => void;
  updateWallOpening: (wallId: string, openingId: string, patch: Partial<SceneWallOpening>) => void;
  removeWallOpening: (wallId: string, openingId: string) => void;
  addWallFeature: (wallId: string, feature: SceneWallFeature) => void;
  updateWallFeature: (wallId: string, featureId: string, patch: Partial<SceneWallFeature>) => void;
  removeWallFeature: (wallId: string, featureId: string) => void;
  splitWall: (wallId: string, alongMm: number) => void;
  addSlabFeature: (slabId: string, feature: SceneWallFeature) => void;
  updateSlabFeature: (slabId: string, featureId: string, patch: Partial<SceneWallFeature>) => void;
  removeSlabFeature: (slabId: string, featureId: string) => void;
  addSlab: (slab: SceneSlabState) => void;
  updateSlab: (slabId: string, patch: Partial<SceneSlabState>) => void;
  removeSlab: (slabId: string) => void;
  addSurface: (surface: SceneSurfaceState) => void;
  updateSurface: (surfaceId: string, patch: Partial<SceneSurfaceState>) => void;
  removeSurface: (surfaceId: string) => void;
  resizePanelEdge: (runId: string, panelId: string, neighborId: string, deltaMm: number) => void;

  loadProject: (project: GlassProjectDto) => void;
  applyScene: (scene: SceneState) => void;
  exportScene: () => SceneState;
  markSaved: () => void;
  setSelection: (selection: DesignerSelection) => void;
  setQuality: (quality: QualityPreset) => void;
  toggleAnnotations: () => void;
  toggleLayer: (key: 'runs' | 'walls' | 'slabs' | 'surfaces') => void;
  togglePresentation: () => void;
  setValidation: (findings: GlassValidationFindingDto[]) => void;

  beginTransaction: () => void;
  commitTransaction: () => void;
  commitAutofillTransaction: (before: SceneState, freshProject: GlassProjectDto) => void;

  addRun: (
    run: Omit<SceneRunState, 'orderIndex' | 'panels'> & { panels?: ScenePanelState[] },
  ) => void;
  updateRun: (runId: string, patch: Partial<SceneRunState>) => void;
  applyRunPatches: (
    patches: Array<
      { id: string } & Partial<Pick<SceneRunState, 'lengthMm' | 'heightMm' | 'originX' | 'originY'>>
    >,
  ) => void;
  removeRun: (runId: string) => void;
  reorderRuns: (orderedRunIds: string[]) => void;

  addPanel: (runId: string, panel: Omit<ScenePanelState, 'panelIndex'>) => void;
  updatePanel: (runId: string, panelId: string, patch: Partial<ScenePanelState>) => void;
  removePanel: (runId: string, panelId: string) => void;
  rebalancePanels: (
    runId: string,
    count: number,
    openingType: GlassOpeningType,
    glassTypeId: string,
  ) => void;

  addHardware: (runId: string, panelId: string, item: SceneHardwareItem) => void;
  updateHardware: (
    runId: string,
    panelId: string,
    hardwareId: string,
    patch: Partial<SceneHardwareItem>,
  ) => void;
  removeHardware: (runId: string, panelId: string, hardwareId: string) => void;
  mergeHardwareFromScene: (scene: SceneState) => void;

  addConnection: (connection: SceneConnectionState) => void;
  updateConnection: (connectionId: string, patch: Partial<SceneConnectionState>) => void;
  removeConnection: (connectionId: string) => void;

  setCamera: (camera: SceneCameraState) => void;

  updatePitchedRoof: (patch: {
    roofPitchDeg?: number | null;
    ridgeHeightMm?: number | null;
    eaveHeightMm?: number | null;
  }) => void;

  undo: () => void;
  redo: () => void;
  canUndo: () => boolean;
  canRedo: () => boolean;

  reset: () => void;
}

const SCHEMA_VERSION = 1;
const HISTORY_LIMIT = 100;
const MIN_SPLIT_SEGMENT_MM = 100;

const emptyScene = (): SceneState => ({
  runs: [],
  connections: [],
  walls: [],
  slabs: [],
  surfaces: [],
  camera: null,
  metadata: { schemaVersion: SCHEMA_VERSION, savedAt: new Date().toISOString() },
});

const cloneScene = (scene: SceneState): SceneState => structuredClone(scene);

const projectToScene = (project: GlassProjectDto, prev?: SceneState): SceneState => {
  const prevHardware = new Map<string, SceneHardwareItem[]>();
  const prevNotch = new Map<string, CornerRadiiMm>();
  const prevBent = new Map<string, boolean>();
  const prevFrame = new Map<string, RunFrameEdges>();
  const prevMullions = new Map<string, boolean>();
  const prevCustomColor = new Map<string, string>();
  if (prev) {
    for (const r of prev.runs) {
      if (r.arcGlassBent) prevBent.set(r.id, true);
      // customColorHex lives only in the blob (not the run DTO), so carry it across a structured
      // re-fetch from the previous scene like arcGlassBent / frameEdges.
      if (r.customColorHex) prevCustomColor.set(r.id, r.customColorHex);
      // frameEdges / hasMullions live only in the blob (not the run DTO), so they must
      // be carried across a structured re-fetch from the previous scene, like arcGlassBent.
      if (r.frameEdges) prevFrame.set(r.id, r.frameEdges);
      if (r.hasMullions === false) prevMullions.set(r.id, false);
      for (const p of r.panels) {
        if (p.hardware?.length) prevHardware.set(p.id, p.hardware);
        // cornerNotchMm is a blob-only panel field (not on the DTO), so carry it across a
        // structured re-fetch from the previous scene like hardware does.
        if (p.cornerNotchMm) prevNotch.set(p.id, p.cornerNotchMm);
      }
    }
  }
  return {
    metadata: { schemaVersion: SCHEMA_VERSION, savedAt: project.updatedAtUtc },
    camera: prev?.camera ?? null,
    walls: prev?.walls ?? [],
    slabs: prev?.slabs ?? [],
    surfaces: prev?.surfaces ?? [],
    runs: project.runs.map((run) => ({
      id: run.id,
      orderIndex: run.orderIndex,
      label: run.label,
      // CHORD-INVARIANT migration: lengthMm is the chord (the fixed span = 2·radius·sin(sweep/2)).
      // Recovers it from the stored radius+sweep — idempotent, and converts old arc-length data
      // (which stored the developed length) back to the chord. The rendered arc is unchanged (it
      // reads radius+sweep), only lengthMm's meaning shifts to the chord for the handles/inspector.
      lengthMm: chordFromRadiusSweep(
        Math.max(run.lengthMm, run.panels.length * MIN_PANEL_MM),
        run.geomArcRadiusMm,
        run.geomArcSweepDeg,
      ),
      heightMm: run.heightMm,
      originX: run.originX,
      originY: run.originY,
      rotationDeg: run.rotationDeg,
      profileSystemId: run.profileSystemId,
      colorId: run.colorId,
      customColorHex: prevCustomColor.get(run.id) ?? null,
      hasTopDrip: run.hasTopDrip,
      hasBottomThreshold: run.hasBottomThreshold,
      geomZ: run.geomZ ?? null,
      geomArcRadiusMm: run.geomArcRadiusMm ?? null,
      geomArcSweepDeg: run.geomArcSweepDeg ?? null,
      arcGlassBent: run.arcGlassBent ?? prevBent.get(run.id) ?? false,
      frameEdges: prevFrame.get(run.id) ?? null,
      hasMullions: prevMullions.has(run.id) ? false : null,
      panels: normalizePanelWidths(
        run.panels.map((panel) => ({
          id: panel.id,
          panelIndex: panel.panelIndex,
          widthMm: panel.widthMm,
          openingType: panel.openingType,
          glassTypeId: panel.glassTypeId,
          hasHandle: panel.hasHandle,
          hasLock: panel.hasLock,
          hasBrushSeal: panel.hasBrushSeal,
          heightMm: panel.heightMm ?? null,
          topShape: panel.topShape ?? null,
          topRightHeightMm: panel.topRightHeightMm ?? null,
          archRiseMm: panel.archRiseMm ?? null,
          cornerRadiiMm: panel.cornerRadiiMm ?? undefined,
          cornerNotchMm: prevNotch.get(panel.id) ?? undefined,
          shapeKind: panel.shapeKind ?? null,
          shapePointsJson: panel.shapePointsJson ?? null,
          hardware: prevHardware.get(panel.id) ?? [],
        })),
        Math.max(run.lengthMm, run.panels.length * MIN_PANEL_MM),
      ),
    })),
    connections: project.connections.map((c) => ({
      id: c.id,
      runAId: c.runAId,
      runBId: c.runBId,
      jointAngleDeg: c.jointAngleDeg,
      mitreCutDeg: c.mitreCutDeg,
      usesCornerPost: c.usesCornerPost,
      cornerProfileId: c.cornerProfileId,
    })),
  };
};

const reindexRuns = (runs: SceneRunState[]): SceneRunState[] =>
  runs.map((run, index) => ({ ...run, orderIndex: index }));

const reindexPanels = (panels: ScenePanelState[]): ScenePanelState[] =>
  panels.map((panel, index) => ({ ...panel, panelIndex: index }));

// A run with more than one panel can't carry a shaped pane — shaping the middle of a 3-panel
// run leaves its siblings rectangular, which is not a real product. Strip every shape field so
// a multi-panel run is always rectangular (the inspector also hides shape controls there).
const stripPanelShape = (panel: ScenePanelState): ScenePanelState => ({
  ...panel,
  topShape: null,
  topRightHeightMm: null,
  archRiseMm: null,
  cornerRadiiMm: undefined,
  cornerNotchMm: undefined,
  shapeKind: null,
  shapePointsJson: null,
});

export const distributePanelWidths = (
  panels: ScenePanelState[],
  lengthMm: number,
): ScenePanelState[] => {
  const count = panels.length;
  if (count === 0) return panels;
  if (lengthMm <= count * MIN_PANEL_MM) {
    return panels.map((panel) =>
      panel.widthMm === MIN_PANEL_MM ? panel : { ...panel, widthMm: MIN_PANEL_MM },
    );
  }
  const rawTotal = panels.reduce((sum, panel) => sum + panel.widthMm, 0);
  const widths = panels.map((panel, index) => {
    if (index === count - 1) return 0;
    const share = rawTotal > 0 ? panel.widthMm / rawTotal : 1 / count;
    return Math.max(MIN_PANEL_MM, Math.round(share * lengthMm));
  });
  widths[count - 1] = lengthMm - widths.reduce((a, b) => a + b, 0);
  while (widths[count - 1] < MIN_PANEL_MM) {
    let widest = 0;
    for (let i = 1; i < count - 1; i += 1) if (widths[i] > widths[widest]) widest = i;
    const take = Math.min(MIN_PANEL_MM - widths[count - 1], widths[widest] - MIN_PANEL_MM);
    if (take <= 0) break;
    widths[widest] -= take;
    widths[count - 1] += take;
  }
  return panels.map((panel, index) => ({ ...panel, widthMm: widths[index] }));
};

const withClampedRunLength = (run: SceneRunState, lengthMm: number): SceneRunState => {
  const clamped = Math.max(run.panels.length * MIN_PANEL_MM, Math.round(lengthMm));
  return { ...run, lengthMm: clamped, panels: distributePanelWidths(run.panels, clamped) };
};

const normalizePanelWidths = (panels: ScenePanelState[], lengthMm: number): ScenePanelState[] => {
  const sum = panels.reduce((acc, panel) => acc + panel.widthMm, 0);
  return sum === lengthMm ? panels : distributePanelWidths(panels, lengthMm);
};

const typedRunKey = (run: SceneRunState) =>
  JSON.stringify([
    run.id,
    run.orderIndex,
    run.label,
    run.lengthMm,
    run.heightMm,
    run.originX,
    run.originY,
    run.rotationDeg,
    run.profileSystemId,
    run.colorId,
    run.hasTopDrip,
    run.hasBottomThreshold,
    run.geomZ ?? null,
    run.geomArcRadiusMm ?? null,
    run.geomArcSweepDeg ?? null,
    run.arcGlassBent ?? false,
    run.frameEdges ?? null,
    run.hasMullions ?? null,
    run.panels.map((p) => [
      p.id,
      p.panelIndex,
      p.widthMm,
      p.openingType,
      p.glassTypeId,
      p.hasHandle,
      p.hasLock,
      p.hasBrushSeal,
    ]),
  ]);

const typedSceneEqual = (a: SceneState, b: SceneState) =>
  a.runs.length === b.runs.length &&
  a.runs.every((run, i) => typedRunKey(run) === typedRunKey(b.runs[i])) &&
  JSON.stringify(a.connections) === JSON.stringify(b.connections);

const selectionStillValid = (selection: DesignerSelection, scene: SceneState) => {
  if (!selection.kind) return false;
  if (selection.kind === 'wall') return (scene.walls ?? []).some((w) => w.id === selection.wallId);
  if (selection.kind === 'wallFeature') {
    return (scene.walls ?? []).some(
      (w) =>
        w.id === selection.wallId && (w.features ?? []).some((f) => f.id === selection.featureId),
    );
  }
  if (selection.kind === 'slabFeature') {
    return (scene.slabs ?? []).some(
      (s) =>
        s.id === selection.slabId && (s.features ?? []).some((f) => f.id === selection.featureId),
    );
  }
  if (selection.kind === 'slab') return (scene.slabs ?? []).some((s) => s.id === selection.slabId);
  if (selection.kind === 'surface')
    return (scene.surfaces ?? []).some((s) => s.id === selection.surfaceId);
  if (selection.kind === 'connection')
    return scene.connections.some((c) => c.id === selection.connectionId);
  const run = scene.runs.find((r) => r.id === selection.runId);
  if (!run) return false;
  if (selection.kind === 'run') return true;
  const panel = run.panels.find((p) => p.id === selection.panelId);
  if (!panel) return false;
  if (selection.kind === 'panel') return true;
  return panel.hardware.some((h) => h.id === selection.hardwareId);
};

const pushHistory = (state: DesignerState, nextScene: SceneState): Partial<DesignerState> => {
  const base =
    state.history.length === 0
      ? [cloneScene(state.scene)]
      : state.history.slice(0, state.historyIndex + 1);
  const next = [...base, cloneScene(nextScene)];
  while (next.length > HISTORY_LIMIT) next.shift();
  return {
    scene: nextScene,
    history: next,
    historyIndex: next.length - 1,
    isDirty: true,
  };
};

const refreshMetadata = (scene: SceneState): SceneState => ({
  ...scene,
  metadata: { ...scene.metadata, schemaVersion: SCHEMA_VERSION, savedAt: new Date().toISOString() },
});

export const useDesignerStore = create<DesignerState>((set, get) => ({
  projectId: null,
  project: null,
  scene: emptyScene(),
  selection: { kind: null, runId: null, panelId: null, connectionId: null },
  quality: 'high',
  showAnnotations: true,
  presentationMode: false,
  layerVisibility: { runs: true, walls: true, slabs: true, surfaces: true },
  isDirty: false,
  history: [],
  historyIndex: -1,
  validation: [],
  clipboard: null,
  pasteArmed: false,
  activeTool: 'select',
  placement: null,
  paintColor: null,
  paintMaterial: null,
  drawShape: 'rect',
  cornerFillMode: 'auto',
  multiSelection: EMPTY_MULTI_SELECTION,
  penFace: null,
  transformHandlesActive: false,
  stackOnDrop: false,

  setActiveTool: (activeTool) =>
    set((s) => ({
      activeTool,
      placement: null,
      multiSelection:
        activeTool === 'multiselect' ||
        activeTool === 'move' ||
        activeTool === 'rotate' ||
        activeTool === 'select'
          ? s.multiSelection
          : EMPTY_MULTI_SELECTION,
    })),
  toggleTransformHandles: () => set((s) => ({ transformHandlesActive: !s.transformHandlesActive })),
  setTransformHandles: (transformHandlesActive) => set({ transformHandlesActive }),
  toggleStackOnDrop: () => set((s) => ({ stackOnDrop: !s.stackOnDrop })),
  setStackOnDrop: (stackOnDrop) => set({ stackOnDrop }),
  setPlacement: (placement) =>
    set(placement === null ? { placement } : { placement, activeTool: 'select' }),
  setPaintColor: (paintColor) => set({ paintColor, paintMaterial: null }),
  setPaintMaterial: (paintMaterial) => set({ paintMaterial, paintColor: null }),
  setDrawShape: (drawShape) => set({ drawShape }),
  setCornerFillMode: (cornerFillMode) => set({ cornerFillMode }),
  toggleMultiSelect: (kind, id) =>
    set((s) => {
      const key = kind === 'run' ? 'runIds' : kind === 'wall' ? 'wallIds' : 'slabIds';
      const current = s.multiSelection[key];
      const removing = current.includes(id);
      return {
        selection: EMPTY_SELECTION,
        multiSelection: {
          ...s.multiSelection,
          [key]: removing ? current.filter((item) => item !== id) : [...current, id],
          order: removing
            ? s.multiSelection.order.filter((ref) => !(ref.kind === kind && ref.id === id))
            : [...s.multiSelection.order, { kind, id }],
        },
      };
    }),
  setMultiSelect: (multiSelection) =>
    set({
      selection: EMPTY_SELECTION,
      multiSelection: {
        ...multiSelection,
        order:
          multiSelection.order.length > 0
            ? multiSelection.order
            : [
                ...multiSelection.runIds.map((id): MultiSelectRef => ({ kind: 'run', id })),
                ...multiSelection.wallIds.map((id): MultiSelectRef => ({ kind: 'wall', id })),
                ...multiSelection.slabIds.map((id): MultiSelectRef => ({ kind: 'slab', id })),
              ],
      },
    }),
  clearMultiSelect: () =>
    set((s) =>
      s.multiSelection.order.length === 0 &&
      s.multiSelection.runIds.length === 0 &&
      s.multiSelection.wallIds.length === 0 &&
      s.multiSelection.slabIds.length === 0
        ? {}
        : { multiSelection: EMPTY_MULTI_SELECTION },
    ),
  setPenFace: (penFace) => set({ penFace }),
  setPenFaceCursor: (hostId, cursor) =>
    set((s) =>
      s.penFace && s.penFace.hostId === hostId ? { penFace: { ...s.penFace, cursor } } : {},
    ),

  applyScenePatch: (updater) => {
    const current = get();
    const next = updater(cloneScene(current.scene));
    set(pushHistory(current, next));
  },
  setClipboard: (clipboard) => set({ clipboard, pasteArmed: false }),
  setPasteArmed: (pasteArmed) => set({ pasteArmed }),

  setRunGlassBent: (runId, bent) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      runs: current.scene.runs.map((run) =>
        run.id === runId ? { ...run, arcGlassBent: bent } : run,
      ),
    };
    set(pushHistory(current, next));
  },

  setRunFrame: (runId, patch) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      runs: current.scene.runs.map((run) =>
        run.id === runId
          ? {
              ...run,
              frameEdges: patch.frameEdges !== undefined ? patch.frameEdges : run.frameEdges,
              hasMullions: patch.hasMullions !== undefined ? patch.hasMullions : run.hasMullions,
            }
          : run,
      ),
    };
    set(pushHistory(current, next));
  },

  addWall: (wall) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      walls: [...(current.scene.walls ?? []), wall],
    };
    set(pushHistory(current, next));
  },

  updateWall: (wallId, patch) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      walls: (current.scene.walls ?? []).map((wall) =>
        wall.id === wallId ? { ...wall, ...patch } : wall,
      ),
    };
    set(pushHistory(current, next));
  },

  removeWall: (wallId) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      walls: (current.scene.walls ?? []).filter((wall) => wall.id !== wallId),
    };
    set(pushHistory(current, next));
  },

  addWallOpening: (wallId, opening) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      walls: (current.scene.walls ?? []).map((wall) =>
        wall.id === wallId ? { ...wall, openings: [...(wall.openings ?? []), opening] } : wall,
      ),
    };
    set(pushHistory(current, next));
  },

  updateWallOpening: (wallId, openingId, patch) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      walls: (current.scene.walls ?? []).map((wall) =>
        wall.id === wallId
          ? {
              ...wall,
              openings: (wall.openings ?? []).map((opening) =>
                opening.id === openingId ? { ...opening, ...patch } : opening,
              ),
            }
          : wall,
      ),
    };
    set(pushHistory(current, next));
  },

  removeWallOpening: (wallId, openingId) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      walls: (current.scene.walls ?? []).map((wall) =>
        wall.id === wallId
          ? { ...wall, openings: (wall.openings ?? []).filter((o) => o.id !== openingId) }
          : wall,
      ),
    };
    set(pushHistory(current, next));
  },

  addWallFeature: (wallId, feature) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      walls: (current.scene.walls ?? []).map((wall) =>
        wall.id === wallId ? { ...wall, features: [...(wall.features ?? []), feature] } : wall,
      ),
    };
    set(pushHistory(current, next));
  },

  updateWallFeature: (wallId, featureId, patch) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      walls: (current.scene.walls ?? []).map((wall) =>
        wall.id === wallId
          ? {
              ...wall,
              features: (wall.features ?? []).map((feature) =>
                feature.id === featureId ? { ...feature, ...patch } : feature,
              ),
            }
          : wall,
      ),
    };
    set(pushHistory(current, next));
  },

  removeWallFeature: (wallId, featureId) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      walls: (current.scene.walls ?? []).map((wall) =>
        wall.id === wallId
          ? { ...wall, features: (wall.features ?? []).filter((f) => f.id !== featureId) }
          : wall,
      ),
    };
    set(pushHistory(current, next));
  },

  splitWall: (wallId, alongMm) => {
    const current = get();
    const walls = current.scene.walls ?? [];
    const wall = walls.find((w) => w.id === wallId);
    if (!wall) return;
    const along = Math.round(alongMm);
    if (along < MIN_SPLIT_SEGMENT_MM || along > wall.lengthMm - MIN_SPLIT_SEGMENT_MM) return;
    const rad = (wall.rotationDeg * Math.PI) / 180;
    const ratio = along / wall.lengthMm;
    const heightEnd = wall.heightEndMm ?? null;
    const heightAtSplit =
      heightEnd === null
        ? wall.heightMm
        : Math.round(wall.heightMm + (heightEnd - wall.heightMm) * ratio);
    const openings = wall.openings ?? [];
    const features = wall.features ?? [];
    const groupId = wall.groupId ?? crypto.randomUUID();
    const first: SceneWallState = {
      ...wall,
      groupId,
      lengthMm: along,
      heightEndMm: heightEnd === null ? null : heightAtSplit,
      openings: openings.filter((o) => o.offsetMm <= along),
      features: features.filter((f) => f.offsetMm <= along),
    };
    const second: SceneWallState = {
      ...wall,
      groupId,
      id: crypto.randomUUID(),
      originX: Math.round(wall.originX + along * Math.cos(rad)),
      originY: Math.round(wall.originY + along * Math.sin(rad)),
      lengthMm: wall.lengthMm - along,
      heightMm: heightAtSplit,
      heightEndMm: heightEnd,
      openings: openings
        .filter((o) => o.offsetMm > along)
        .map((o) => ({ ...o, offsetMm: o.offsetMm - along })),
      features: features
        .filter((f) => f.offsetMm > along)
        .map((f) => ({ ...f, offsetMm: f.offsetMm - along })),
    };
    const next: SceneState = {
      ...current.scene,
      walls: walls.flatMap((w) => (w.id === wallId ? [first, second] : [w])),
    };
    set(pushHistory(current, next));
  },

  addSlab: (slab) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      slabs: [...(current.scene.slabs ?? []), slab],
    };
    set(pushHistory(current, next));
  },

  addSlabFeature: (slabId, feature) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      slabs: (current.scene.slabs ?? []).map((slab) =>
        slab.id === slabId ? { ...slab, features: [...(slab.features ?? []), feature] } : slab,
      ),
    };
    set(pushHistory(current, next));
  },

  updateSlabFeature: (slabId, featureId, patch) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      slabs: (current.scene.slabs ?? []).map((slab) =>
        slab.id === slabId
          ? {
              ...slab,
              features: (slab.features ?? []).map((feature) =>
                feature.id === featureId ? { ...feature, ...patch } : feature,
              ),
            }
          : slab,
      ),
    };
    set(pushHistory(current, next));
  },

  removeSlabFeature: (slabId, featureId) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      slabs: (current.scene.slabs ?? []).map((slab) =>
        slab.id === slabId
          ? { ...slab, features: (slab.features ?? []).filter((f) => f.id !== featureId) }
          : slab,
      ),
    };
    set(pushHistory(current, next));
  },

  updateSlab: (slabId, patch) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      slabs: (current.scene.slabs ?? []).map((slab) =>
        slab.id === slabId ? { ...slab, ...patch } : slab,
      ),
    };
    set(pushHistory(current, next));
  },

  removeSlab: (slabId) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      slabs: (current.scene.slabs ?? []).filter((slab) => slab.id !== slabId),
    };
    set(pushHistory(current, next));
  },

  addSurface: (surface) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      surfaces: [...(current.scene.surfaces ?? []), surface],
    };
    set(pushHistory(current, next));
  },

  updateSurface: (surfaceId, patch) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      surfaces: (current.scene.surfaces ?? []).map((surface) =>
        surface.id === surfaceId ? { ...surface, ...patch } : surface,
      ),
    };
    set(pushHistory(current, next));
  },

  removeSurface: (surfaceId) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      surfaces: (current.scene.surfaces ?? []).filter((surface) => surface.id !== surfaceId),
    };
    set(pushHistory(current, next));
  },

  resizePanelEdge: (runId, panelId, neighborId, deltaMm) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      runs: current.scene.runs.map((run) => {
        if (run.id !== runId) return run;
        const i = run.panels.findIndex((p) => p.id === panelId);
        const j = run.panels.findIndex((p) => p.id === neighborId);
        if (i < 0 || j < 0) return run;
        const widths = cascadePanelWidths(
          run.panels.map((p) => p.widthMm),
          i,
          j,
          deltaMm,
        );
        return {
          ...run,
          panels: run.panels.map((p, k) =>
            widths[k] !== p.widthMm ? { ...p, widthMm: widths[k] } : p,
          ),
        };
      }),
    };
    set(pushHistory(current, next));
  },

  loadProject: (project) => {
    const current = get();
    const nextScene = projectToScene(project, current.scene);
    if (current.projectId === project.id) {
      if (typedSceneEqual(current.scene, nextScene)) {
        set({ project });
        return;
      }
      set({
        project,
        scene: nextScene,
        selection: selectionStillValid(current.selection, nextScene)
          ? current.selection
          : { kind: null, runId: null, panelId: null, connectionId: null },
        isDirty: current.isDirty,
      });
      return;
    }
    set({
      projectId: project.id,
      project,
      scene: nextScene,
      selection: { kind: null, runId: null, panelId: null, connectionId: null },
      isDirty: false,
      history: [],
      historyIndex: -1,
      validation: [],
      clipboard: null,
      pasteArmed: false,
    });
  },

  applyScene: (scene) =>
    set({
      scene: cloneScene(scene),
      isDirty: false,
      history: [],
      historyIndex: -1,
    }),

  exportScene: () => refreshMetadata(get().scene),
  markSaved: () => set({ isDirty: false }),

  setSelection: (selection) =>
    set((s) => {
      const hasMulti =
        s.multiSelection.runIds.length +
          s.multiSelection.wallIds.length +
          s.multiSelection.slabIds.length >
        0;
      return selection.kind && hasMulti
        ? { selection, multiSelection: EMPTY_MULTI_SELECTION }
        : { selection };
    }),
  setQuality: (quality) => set({ quality }),
  toggleAnnotations: () => set((s) => ({ showAnnotations: !s.showAnnotations })),
  toggleLayer: (key) =>
    set((s) => ({ layerVisibility: { ...s.layerVisibility, [key]: !s.layerVisibility[key] } })),
  togglePresentation: () => set((s) => ({ presentationMode: !s.presentationMode })),
  setValidation: (findings) => set({ validation: findings }),

  beginTransaction: () => {
    const current = get();
    if (current.history.length > 0) return;
    set({ history: [cloneScene(current.scene)], historyIndex: 0 });
  },
  commitTransaction: () => set({ isDirty: true }),

  // WHY: autofill creates runs/connections via the server CRUD endpoints (not the local store), so
  // nothing lands in the undo history and Ctrl+Z/Y do nothing. Record the operation as a single
  // [before, after] history pair: undo reverts to `before` and the existing scene→server reconciler
  // (syncSceneToServer) removes the created runs; redo replays `after` and re-creates them.
  commitAutofillTransaction: (before, freshProject) => {
    const current = get();
    const after = projectToScene(freshProject, before);
    const trimmed =
      current.historyIndex >= 0 ? current.history.slice(0, current.historyIndex + 1) : [];
    const withBaseline =
      trimmed.length > 0 && typedSceneEqual(trimmed[trimmed.length - 1], before)
        ? trimmed
        : [...trimmed, cloneScene(before)];
    const next = [...withBaseline, cloneScene(after)];
    set({
      project: freshProject,
      scene: cloneScene(after),
      history: next,
      historyIndex: next.length - 1,
      isDirty: false,
    });
  },

  addRun: (run) => {
    const current = get();
    const newRun: SceneRunState = {
      ...run,
      orderIndex: current.scene.runs.length,
      panels: run.panels ?? [],
    };
    const next: SceneState = {
      ...current.scene,
      runs: [...current.scene.runs, newRun],
    };
    set(pushHistory(current, next));
  },

  updateRun: (runId, patch) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      runs: current.scene.runs.map((run) => {
        if (run.id !== runId) return run;
        const merged = { ...run, ...patch };
        if (patch.lengthMm !== undefined && patch.lengthMm !== run.lengthMm) {
          return withClampedRunLength(merged, merged.lengthMm);
        }
        return merged;
      }),
    };
    set(pushHistory(current, next));
  },

  applyRunPatches: (patches) => {
    const current = get();
    const map = new Map(patches.map((p) => [p.id, p]));
    const next: SceneState = {
      ...current.scene,
      runs: current.scene.runs.map((run) => {
        const patch = map.get(run.id);
        if (!patch) return run;
        const merged = { ...run, ...patch };
        if (patch.lengthMm !== undefined && patch.lengthMm !== run.lengthMm) {
          return withClampedRunLength(merged, merged.lengthMm);
        }
        return merged;
      }),
    };
    set(pushHistory(current, next));
  },

  removeRun: (runId) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      runs: reindexRuns(current.scene.runs.filter((r) => r.id !== runId)),
      connections: current.scene.connections.filter(
        (c) => c.runAId !== runId && c.runBId !== runId,
      ),
    };
    set(pushHistory(current, next));
  },

  reorderRuns: (orderedRunIds) => {
    const current = get();
    const runById = new Map(current.scene.runs.map((r) => [r.id, r]));
    const reordered = orderedRunIds
      .map((id) => runById.get(id))
      .filter((r): r is SceneRunState => r !== undefined);
    const next: SceneState = { ...current.scene, runs: reindexRuns(reordered) };
    set(pushHistory(current, next));
  },

  addPanel: (runId, panel) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      runs: current.scene.runs.map((run) => {
        if (run.id !== runId) return run;
        const nextPanel: ScenePanelState = { ...panel, panelIndex: run.panels.length };
        // Adding a panel makes the run multi-panel → it can no longer be shaped, so strip any
        // shape from the existing panes and the new one alike.
        const multi = run.panels.length >= 1;
        const existing = multi ? run.panels.map(stripPanelShape) : run.panels;
        return {
          ...run,
          panels: [...existing, multi ? stripPanelShape(nextPanel) : nextPanel],
          lengthMm: run.lengthMm + nextPanel.widthMm,
        };
      }),
    };
    set(pushHistory(current, next));
  },

  updatePanel: (runId, panelId, patch) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      runs: current.scene.runs.map((run) => {
        if (run.id !== runId) return run;
        const panels = run.panels.map((panel) =>
          panel.id === panelId ? { ...panel, ...patch } : panel,
        );
        if (patch.widthMm !== undefined) {
          const lengthMm = panels.reduce((sum, panel) => sum + panel.widthMm, 0);
          return { ...run, panels, lengthMm };
        }
        return { ...run, panels };
      }),
    };
    set(pushHistory(current, next));
  },

  removePanel: (runId, panelId) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      runs: current.scene.runs.map((run) => {
        if (run.id !== runId) return run;
        const removed = run.panels.find((p) => p.id === panelId);
        const panels = reindexPanels(run.panels.filter((p) => p.id !== panelId));
        const lengthMm =
          removed && panels.length > 0 ? Math.max(1, run.lengthMm - removed.widthMm) : run.lengthMm;
        return { ...run, panels, lengthMm };
      }),
    };
    set(pushHistory(current, next));
  },

  rebalancePanels: (runId, count, openingType, glassTypeId) => {
    const current = get();
    const safeCount = Math.max(1, Math.floor(count));
    const next: SceneState = {
      ...current.scene,
      runs: current.scene.runs.map((run) => {
        if (run.id !== runId) return run;
        const base = Math.floor(run.lengthMm / safeCount);
        const panels: ScenePanelState[] = Array.from({ length: safeCount }, (_, i) => ({
          id: crypto.randomUUID(),
          panelIndex: i,
          widthMm: i === safeCount - 1 ? run.lengthMm - base * (safeCount - 1) : base,
          openingType,
          glassTypeId,
          hasHandle: false,
          hasLock: false,
          hasBrushSeal: false,
          hardware: [],
        }));
        return { ...run, panels };
      }),
    };
    set(pushHistory(current, next));
  },

  addHardware: (runId, panelId, item) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      runs: current.scene.runs.map((run) => {
        if (run.id !== runId) return run;
        return {
          ...run,
          panels: run.panels.map((panel) =>
            panel.id === panelId ? { ...panel, hardware: [...panel.hardware, item] } : panel,
          ),
        };
      }),
    };
    set(pushHistory(current, next));
  },

  updateHardware: (runId, panelId, hardwareId, patch) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      runs: current.scene.runs.map((run) => {
        if (run.id !== runId) return run;
        return {
          ...run,
          panels: run.panels.map((panel) =>
            panel.id === panelId
              ? {
                  ...panel,
                  hardware: panel.hardware.map((h) =>
                    h.id === hardwareId ? { ...h, ...patch } : h,
                  ),
                }
              : panel,
          ),
        };
      }),
    };
    set(pushHistory(current, next));
  },

  removeHardware: (runId, panelId, hardwareId) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      runs: current.scene.runs.map((run) => {
        if (run.id !== runId) return run;
        return {
          ...run,
          panels: run.panels.map((panel) =>
            panel.id === panelId
              ? { ...panel, hardware: panel.hardware.filter((h) => h.id !== hardwareId) }
              : panel,
          ),
        };
      }),
    };
    set(pushHistory(current, next));
  },

  mergeHardwareFromScene: (scene) => {
    const current = get();
    const hwByPanel = new Map<string, SceneHardwareItem[]>();
    const notchByPanel = new Map<string, CornerRadiiMm>();
    const bentByRun = new Map<string, boolean>();
    const frameByRun = new Map<string, RunFrameEdges>();
    const mullionsByRun = new Map<string, boolean>();
    const colorByRun = new Map<string, string>();
    for (const r of scene.runs) {
      if (r.arcGlassBent) bentByRun.set(r.id, true);
      if (r.frameEdges) frameByRun.set(r.id, r.frameEdges);
      if (r.hasMullions === false) mullionsByRun.set(r.id, false);
      if (r.customColorHex) colorByRun.set(r.id, r.customColorHex);
      for (const p of r.panels) {
        if (p.hardware?.length) hwByPanel.set(p.id, p.hardware);
        if (p.cornerNotchMm) notchByPanel.set(p.id, p.cornerNotchMm);
      }
    }
    // CHORD-INVARIANT migration: lengthMm is the chord (= 2·radius·sin(sweep/2)); recover it from the
    // stored radius+sweep on load (idempotent; converts old arc-length data back to the chord).
    const snapshotWalls = (scene.walls ?? []).map((w) =>
      w.geomArcRadiusMm && w.geomArcRadiusMm > 0
        ? {
            ...w,
            lengthMm: chordFromRadiusSweep(w.lengthMm, w.geomArcRadiusMm, w.geomArcSweepDeg),
          }
        : w,
    );
    const snapshotSlabs = scene.slabs ?? [];
    const snapshotSurfaces = scene.surfaces ?? [];
    if (
      hwByPanel.size === 0 &&
      notchByPanel.size === 0 &&
      bentByRun.size === 0 &&
      frameByRun.size === 0 &&
      mullionsByRun.size === 0 &&
      colorByRun.size === 0 &&
      snapshotWalls.length === 0 &&
      snapshotSlabs.length === 0 &&
      snapshotSurfaces.length === 0
    )
      return;
    const next: SceneState = {
      ...current.scene,
      walls: snapshotWalls.length > 0 ? snapshotWalls : (current.scene.walls ?? []),
      slabs: snapshotSlabs.length > 0 ? snapshotSlabs : (current.scene.slabs ?? []),
      surfaces: snapshotSurfaces.length > 0 ? snapshotSurfaces : (current.scene.surfaces ?? []),
      runs: current.scene.runs.map((run) => ({
        ...run,
        arcGlassBent: bentByRun.get(run.id) ?? run.arcGlassBent ?? false,
        frameEdges: frameByRun.get(run.id) ?? run.frameEdges ?? null,
        hasMullions: mullionsByRun.has(run.id) ? false : (run.hasMullions ?? true),
        customColorHex: colorByRun.get(run.id) ?? run.customColorHex ?? null,
        panels: run.panels.map((panel) => {
          const hw = hwByPanel.get(panel.id);
          const notch = notchByPanel.get(panel.id);
          if (!hw && !notch) return panel;
          return {
            ...panel,
            ...(hw ? { hardware: hw } : {}),
            ...(notch ? { cornerNotchMm: notch } : {}),
          };
        }),
      })),
    };
    set({ scene: next });
  },

  addConnection: (connection) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      connections: [...current.scene.connections, connection],
    };
    set(pushHistory(current, next));
  },

  updateConnection: (connectionId, patch) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      connections: current.scene.connections.map((c) =>
        c.id === connectionId ? { ...c, ...patch } : c,
      ),
    };
    set(pushHistory(current, next));
  },

  removeConnection: (connectionId) => {
    const current = get();
    const next: SceneState = {
      ...current.scene,
      connections: current.scene.connections.filter((c) => c.id !== connectionId),
    };
    set(pushHistory(current, next));
  },

  setCamera: (camera) => set((s) => ({ scene: { ...s.scene, camera } })),

  updatePitchedRoof: (patch) =>
    set((s) => {
      if (!s.project) return {};
      const next: GlassProjectDto = {
        ...s.project,
        roofPitchDeg: patch.roofPitchDeg ?? s.project.roofPitchDeg,
        ridgeHeightMm: patch.ridgeHeightMm ?? s.project.ridgeHeightMm,
        eaveHeightMm: patch.eaveHeightMm ?? s.project.eaveHeightMm,
      };
      return { project: next, isDirty: true };
    }),

  undo: () => {
    const current = get();
    if (current.historyIndex <= 0) return;
    const nextIndex = current.historyIndex - 1;
    set({
      scene: cloneScene(current.history[nextIndex]),
      historyIndex: nextIndex,
      isDirty: true,
    });
  },

  redo: () => {
    const current = get();
    if (current.historyIndex >= current.history.length - 1) return;
    const nextIndex = current.historyIndex + 1;
    set({
      scene: cloneScene(current.history[nextIndex]),
      historyIndex: nextIndex,
      isDirty: true,
    });
  },

  canUndo: () => get().historyIndex > 0,
  canRedo: () => get().historyIndex < get().history.length - 1,

  reset: () =>
    set({
      projectId: null,
      project: null,
      scene: emptyScene(),
      selection: { kind: null, runId: null, panelId: null, connectionId: null },
      isDirty: false,
      history: [],
      historyIndex: -1,
      validation: [],
    }),
}));
