import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Boxes,
  ClipboardList,
  FileSpreadsheet,
  LayoutGrid,
  Layers,
  Scissors,
  Sigma,
} from 'lucide-react';
import { DesignerToolbar } from '@/features/glass-enclosure/ui/DesignerToolbar';
import { ValidationPanel } from '@/features/glass-enclosure/ui/ValidationPanel';
import { LiveCostPreview } from '@/features/glass-enclosure/ui/LiveCostPreview';
import { CuttingReportView } from '@/features/glass-enclosure/ui/CuttingReportView';
import { TechnicalSummaryReport } from '@/features/glass-enclosure/ui/TechnicalSummaryReport';
import { QuoteSummaryView } from '@/features/glass-enclosure/ui/QuoteSummaryView';
import { CommercePanel } from '@/features/glass-enclosure/ui/CommercePanel';
import { FieldSurveyForm } from '@/features/glass-enclosure/ui/FieldSurveyForm';
import { ExportMenu } from '@/features/glass-enclosure/ui/ExportMenu';
import { useDesignerStore } from '@/features/glass-enclosure/model/designerStore';
import { DesignerShell } from '@/features/glass-enclosure/designer/layout';
import {
  RunsPanel,
  CanvasPanel,
  InspectorPanel,
  BOMPanel,
  SelectionSummary,
} from '@/features/glass-enclosure/designer/panels';
import {
  useAddConnectionMutation,
  useAddRunMutation,
  useCuttingReportQuery,
  useGenerateCuttingPlanMutation,
  useProjectBOMQuery,
  useRecomputeBOMMutation,
  useGlassProjectQuery,
  useSaveSceneMutation,
  useSceneLatestQuery,
  useTechnicalSummaryQuery,
  useUpdateRunMutation,
  useValidateProjectMutation,
} from '@/features/glass-enclosure/hooks/useGlassProjectQueries';
import {
  useColorOptionsQuery,
  useGlassTypesQuery,
  useHardwareItemsQuery,
  useHardwareKitsQuery,
  useProfileSystemsQuery,
  useSettingsQuery,
} from '@/features/glass-enclosure/hooks/useGlassEnclosureQueries';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { logger } from '@/shared/lib/logger';
import { snapAngleDeg } from '@/features/glass-enclosure/model/angleSnap';
import { useSceneSync } from '@/features/glass-enclosure/hooks/useSceneSync';
import { useSceneAutosave } from '@/features/glass-enclosure/hooks/useSceneAutosave';
import { useDesignerEntityActions } from '@/features/glass-enclosure/hooks/useDesignerEntityActions';
import { useMultiSelectionDelete } from '@/features/glass-enclosure/hooks/useMultiSelectionDelete';
import { useWallAutofill } from '@/features/glass-enclosure/hooks/useWallAutofill';
import { enqueuePersist } from '@/features/glass-enclosure/model/persistQueue';
import type { DesignerTool, PlacementKind } from '@/features/glass-enclosure/model/designerStore';
import type { SceneState } from '@/features/glass-enclosure/model/project.types';

type DesignerViewMode = 'split' | '2d' | '3d' | 'cutting' | 'engineering' | 'quote' | 'survey';

const TOOL_SHORTCUTS: Record<string, DesignerTool> = {
  v: 'select',
  l: 'multiselect',
  m: 'move',
  r: 'rotate',
  s: 'stretch',
  d: 'draw',
  b: 'paint',
  e: 'erase',
  k: 'measure',
};

const PLACEMENT_SHORTCUTS: Record<string, PlacementKind> = {
  '1': 'run',
  '2': 'wall',
  '3': 'floor',
  '4': 'roof',
  p: 'pen',
};

const parseSceneSnapshot = (json: string): SceneState | null => {
  try {
    return JSON.parse(json) as SceneState;
  } catch {
    return null;
  }
};

export function GlassProjectDesignerPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const projectQuery = useGlassProjectQuery(id ?? null);
  const profileSystemsQuery = useProfileSystemsQuery();
  const glassTypesQuery = useGlassTypesQuery();
  const colorsQuery = useColorOptionsQuery();
  const hardwareItemsQuery = useHardwareItemsQuery();
  const hardwareKitsQuery = useHardwareKitsQuery();
  const settingsQuery = useSettingsQuery();
  const saveMutation = useSaveSceneMutation();
  const validateMutation = useValidateProjectMutation();
  const addRunMutation = useAddRunMutation();
  const updateRunMutation = useUpdateRunMutation();
  const addConnectionMutation = useAddConnectionMutation();
  const [viewMode, setViewMode] = useState<DesignerViewMode>('3d');
  const cuttingReportQuery = useCuttingReportQuery(id ?? null);
  const technicalSummaryQuery = useTechnicalSummaryQuery(id ?? null);
  const generateCuttingMutation = useGenerateCuttingPlanMutation();
  const bomQuery = useProjectBOMQuery(id ?? null);
  const recomputeBomMutation = useRecomputeBOMMutation();
  const sceneLatestQuery = useSceneLatestQuery(id ?? null);

  const loadProject = useDesignerStore((s) => s.loadProject);
  const exportScene = useDesignerStore((s) => s.exportScene);
  const setCamera = useDesignerStore((s) => s.setCamera);
  const mergeHardwareFromScene = useDesignerStore((s) => s.mergeHardwareFromScene);
  const setValidation = useDesignerStore((s) => s.setValidation);
  const { syncSceneToServer } = useSceneSync();
  useSceneAutosave(id ?? null);
  const hardwareMergedProjectRef = useRef<string | null>(null);

  const handleUndo = useCallback(async () => {
    const state = useDesignerStore.getState();
    if (!state.canUndo() || !state.project) return;
    state.undo();
    const target = useDesignerStore.getState().scene;
    await safeRequestWithNotify(syncSceneToServer(state.project, target), {
      showSuccessNotification: false,
    });
  }, [syncSceneToServer]);

  const handleRedo = useCallback(async () => {
    const state = useDesignerStore.getState();
    if (!state.canRedo() || !state.project) return;
    state.redo();
    const target = useDesignerStore.getState().scene;
    await safeRequestWithNotify(syncSceneToServer(state.project, target), {
      showSuccessNotification: false,
    });
  }, [syncSceneToServer]);

  const handleCopy = useCallback(() => {
    const state = useDesignerStore.getState();
    const { selection, scene } = state;
    const run = scene.runs.find((r) => r.id === selection.runId);
    if (selection.kind === 'panel' && run) {
      const panel = run.panels.find((p) => p.id === selection.panelId);
      if (panel) state.setClipboard({ kind: 'panel', panel: structuredClone(panel) });
      return;
    }
    if (selection.kind === 'hardware' && run) {
      const panel = run.panels.find((p) => p.id === selection.panelId);
      const item = panel?.hardware.find((h) => h.id === selection.hardwareId);
      if (item) state.setClipboard({ kind: 'hardware', item: structuredClone(item) });
      return;
    }
    if (selection.kind === 'run' && run) {
      state.setClipboard({ kind: 'run', run: structuredClone(run) });
      return;
    }
    if (selection.kind === 'wall' && selection.wallId) {
      const wall = (scene.walls ?? []).find((w) => w.id === selection.wallId);
      if (wall) state.setClipboard({ kind: 'wall', wall: structuredClone(wall) });
      return;
    }
    if (selection.kind === 'slab' && selection.slabId) {
      const slab = (scene.slabs ?? []).find((s) => s.id === selection.slabId);
      if (slab) state.setClipboard({ kind: 'slab', slab: structuredClone(slab) });
    }
  }, []);

  const handleArmPaste = useCallback(() => {
    const state = useDesignerStore.getState();
    if (state.clipboard) state.setPasteArmed(true);
  }, []);

  const handleDuplicate = useCallback(() => {
    const OFFSET_MM = 300;
    const state = useDesignerStore.getState();
    const { selection, scene } = state;
    if (selection.kind === 'wall' && selection.wallId) {
      const wall = (scene.walls ?? []).find((w) => w.id === selection.wallId);
      if (!wall) return;
      const clone = {
        ...structuredClone(wall),
        id: crypto.randomUUID(),
        originX: wall.originX + OFFSET_MM,
        originY: wall.originY + OFFSET_MM,
        openings: (wall.openings ?? []).map((o) => ({ ...o, id: crypto.randomUUID() })),
        features: (wall.features ?? []).map((f) => ({ ...f, id: crypto.randomUUID() })),
      };
      state.addWall(clone);
      state.setSelection({
        kind: 'wall',
        runId: null,
        panelId: null,
        connectionId: null,
        hardwareId: null,
        wallId: clone.id,
      });
      return;
    }
    if (selection.kind === 'slab' && selection.slabId) {
      const slab = (scene.slabs ?? []).find((s) => s.id === selection.slabId);
      if (!slab) return;
      const clone = {
        ...structuredClone(slab),
        id: crypto.randomUUID(),
        originX: slab.originX + OFFSET_MM,
        originY: slab.originY + OFFSET_MM,
        features: (slab.features ?? []).map((f) => ({ ...f, id: crypto.randomUUID() })),
      };
      state.addSlab(clone);
      state.setSelection({
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
    if (selection.kind === 'surface' && selection.surfaceId) {
      const surface = (scene.surfaces ?? []).find((s) => s.id === selection.surfaceId);
      if (!surface) return;
      const clone = {
        ...structuredClone(surface),
        id: crypto.randomUUID(),
        points: surface.points.map((p) => ({ x: p.x + OFFSET_MM, y: p.y + OFFSET_MM })),
      };
      state.addSurface(clone);
      state.setSelection({
        kind: 'surface',
        runId: null,
        panelId: null,
        connectionId: null,
        hardwareId: null,
        wallId: null,
        slabId: null,
        surfaceId: clone.id,
      });
      return;
    }
    if (selection.kind === 'run' || selection.kind === 'panel' || selection.kind === 'hardware') {
      handleCopy();
      handleArmPaste();
    }
  }, [handleCopy, handleArmPaste]);

  const { deleteRun, deletePanel, persistRun } = useDesignerEntityActions();
  const { deleteMultiSelection } = useMultiSelectionDelete();

  const handleDeleteSelection = useCallback(() => {
    if (deleteMultiSelection() > 0) return;
    const state = useDesignerStore.getState();
    const sel = state.selection;
    const clear = () =>
      state.setSelection({
        kind: null,
        runId: null,
        panelId: null,
        connectionId: null,
        hardwareId: null,
        wallId: null,
      });
    if (sel.kind === 'panel' && sel.runId && sel.panelId) {
      void deletePanel(sel.runId, sel.panelId);
      state.setSelection({
        kind: 'run',
        runId: sel.runId,
        panelId: null,
        connectionId: null,
        hardwareId: null,
      });
    } else if (sel.kind === 'run' && sel.runId) {
      void deleteRun(sel.runId);
      clear();
    } else if (sel.kind === 'hardware' && sel.runId && sel.panelId && sel.hardwareId) {
      state.removeHardware(sel.runId, sel.panelId, sel.hardwareId);
      state.setSelection({
        kind: 'panel',
        runId: sel.runId,
        panelId: sel.panelId,
        connectionId: null,
        hardwareId: null,
      });
    } else if (sel.kind === 'wallFeature' && sel.wallId && sel.featureId) {
      state.removeWallFeature(sel.wallId, sel.featureId);
      state.setSelection({
        kind: 'wall',
        runId: null,
        panelId: null,
        connectionId: null,
        hardwareId: null,
        wallId: sel.wallId,
      });
    } else if (sel.kind === 'slabFeature' && sel.slabId && sel.featureId) {
      state.removeSlabFeature(sel.slabId, sel.featureId);
      state.setSelection({
        kind: 'slab',
        runId: null,
        panelId: null,
        connectionId: null,
        hardwareId: null,
        wallId: null,
        slabId: sel.slabId,
      });
    } else if (sel.kind === 'wall' && sel.wallId) {
      state.removeWall(sel.wallId);
      clear();
    } else if (sel.kind === 'slab' && sel.slabId) {
      state.removeSlab(sel.slabId);
      clear();
    } else if (sel.kind === 'surface' && sel.surfaceId) {
      state.removeSurface(sel.surfaceId);
      clear();
    } else if (sel.kind === 'connection' && sel.connectionId) {
      state.removeConnection(sel.connectionId);
      clear();
    }
  }, [deleteRun, deletePanel, deleteMultiSelection]);

  // Arrow-key nudge of the selected placed object in the plan (X/Y mm). Runs need the
  // structured persist (origin lives on the DTO); walls/slabs/surfaces ride the scene blob.
  const handleNudge = useCallback(
    (dxMm: number, dyMm: number) => {
      const state = useDesignerStore.getState();
      const sel = state.selection;
      if (sel.kind === 'run' && sel.runId) {
        const run = state.scene.runs.find((r) => r.id === sel.runId);
        if (!run) return;
        state.updateRun(sel.runId, { originX: run.originX + dxMm, originY: run.originY + dyMm });
        const updated = useDesignerStore.getState().scene.runs.find((r) => r.id === sel.runId);
        if (updated) void persistRun(updated);
      } else if (sel.kind === 'wall' && sel.wallId) {
        const wall = (state.scene.walls ?? []).find((w) => w.id === sel.wallId);
        if (wall)
          state.updateWall(sel.wallId, {
            originX: wall.originX + dxMm,
            originY: wall.originY + dyMm,
          });
      } else if (sel.kind === 'slab' && sel.slabId) {
        const slab = (state.scene.slabs ?? []).find((s) => s.id === sel.slabId);
        if (slab)
          state.updateSlab(sel.slabId, {
            originX: slab.originX + dxMm,
            originY: slab.originY + dyMm,
          });
      } else if (sel.kind === 'surface' && sel.surfaceId) {
        const surface = (state.scene.surfaces ?? []).find((s) => s.id === sel.surfaceId);
        if (surface)
          state.updateSurface(sel.surfaceId, {
            points: surface.points.map((p) => ({ x: p.x + dxMm, y: p.y + dyMm })),
          });
      }
    },
    [persistRun],
  );

  const project = projectQuery.data?.data ?? null;
  const profileSystems = useMemo(
    () => profileSystemsQuery.data?.data ?? [],
    [profileSystemsQuery.data?.data],
  );
  const glassTypes = useMemo(() => glassTypesQuery.data?.data ?? [], [glassTypesQuery.data?.data]);
  const colors = useMemo(() => colorsQuery.data?.data ?? [], [colorsQuery.data?.data]);
  const hardwareItems = useMemo(
    () => hardwareItemsQuery.data?.data ?? [],
    [hardwareItemsQuery.data?.data],
  );
  const hardwareKits = useMemo(
    () => hardwareKitsQuery.data?.data ?? [],
    [hardwareKitsQuery.data?.data],
  );
  const settings = settingsQuery.data?.data ?? null;

  useEffect(() => {
    if (project) loadProject(project);
  }, [project, loadProject]);

  useEffect(() => {
    if (!project) return;
    if (hardwareMergedProjectRef.current === project.id) return;
    const snapshotJson = sceneLatestQuery.data?.data?.sceneJson;
    if (!snapshotJson) return;
    const parsed = parseSceneSnapshot(snapshotJson);
    if (!parsed) return;
    hardwareMergedProjectRef.current = project.id;
    mergeHardwareFromScene(parsed);
    if (parsed.camera) setCamera(parsed.camera);
  }, [project, sceneLatestQuery.data?.data?.sceneJson, mergeHardwareFromScene, setCamera]);

  const handleSave = useCallback(async () => {
    if (!id) return;
    const sceneSnapshot = exportScene();
    await safeRequestWithNotify(
      enqueuePersist(() =>
        saveMutation.mutateAsync({
          id,
          input: {
            sceneJson: JSON.stringify(sceneSnapshot),
            cameraStateJson: sceneSnapshot.camera ? JSON.stringify(sceneSnapshot.camera) : null,
            label: null,
          },
        }),
      ),
      { successMessage: t('GlassEnclosure.Designer.SaveSuccess') },
    );
  }, [id, exportScene, saveMutation, t]);

  const handleValidate = useCallback(async () => {
    if (!id) return;
    const [response] = await safeRequestWithNotify(validateMutation.mutateAsync(id), {
      successMessage: t('GlassEnclosure.Designer.ValidationDone'),
    });
    if (response?.data) setValidation(response.data.findings);
  }, [id, validateMutation, setValidation, t]);

  const ensureCanvasVisible = useCallback(() => {
    setViewMode((mode) => (mode === 'split' || mode === '3d' ? mode : '3d'));
  }, []);

  const armPlacement = useCallback(
    (kind: PlacementKind) => {
      ensureCanvasVisible();
      const state = useDesignerStore.getState();
      state.setPlacement(state.placement === kind ? null : kind);
    },
    [ensureCanvasVisible],
  );

  const handleAddRun = useCallback(() => {
    armPlacement('run');
  }, [armPlacement]);

  const { autofill } = useWallAutofill();

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      const target = e.target as HTMLElement | null;
      if (
        target instanceof HTMLInputElement ||
        target instanceof HTMLTextAreaElement ||
        target instanceof HTMLSelectElement ||
        target?.isContentEditable
      )
        return;
      const meta = e.ctrlKey || e.metaKey;
      if (meta && e.key === 'z' && !e.shiftKey) {
        e.preventDefault();
        void handleUndo();
      } else if (meta && (e.key === 'y' || (e.shiftKey && e.key === 'z'))) {
        e.preventDefault();
        void handleRedo();
      } else if (meta && e.key === 's') {
        e.preventDefault();
        handleSave();
      } else if (meta && e.key === 'c') {
        handleCopy();
      } else if (meta && e.key === 'd') {
        e.preventDefault();
        handleDuplicate();
      } else if (meta && e.key === 'v') {
        e.preventDefault();
        handleArmPaste();
      } else if (e.key === 'Delete') {
        e.preventDefault();
        handleDeleteSelection();
      } else if (!meta && !e.altKey && e.key.startsWith('Arrow')) {
        const step = e.shiftKey ? 1 : 10;
        if (e.key === 'ArrowLeft') {
          e.preventDefault();
          handleNudge(-step, 0);
        } else if (e.key === 'ArrowRight') {
          e.preventDefault();
          handleNudge(step, 0);
        } else if (e.key === 'ArrowUp') {
          e.preventDefault();
          handleNudge(0, -step);
        } else if (e.key === 'ArrowDown') {
          e.preventDefault();
          handleNudge(0, step);
        }
      } else if (e.key === 'Escape') {
        const state = useDesignerStore.getState();
        const hasMulti =
          state.multiSelection.runIds.length +
            state.multiSelection.wallIds.length +
            state.multiSelection.slabIds.length >
          0;
        if (state.pasteArmed || state.placement) {
          state.setPasteArmed(false);
          state.setPlacement(null);
        } else if (hasMulti) {
          state.clearMultiSelect();
        } else if (state.selection.kind) {
          state.setSelection({
            kind: null,
            runId: null,
            panelId: null,
            connectionId: null,
            hardwareId: null,
            wallId: null,
            slabId: null,
          });
        } else {
          state.setActiveTool('select');
        }
      } else if (!meta && !e.altKey) {
        const key = e.key.toLowerCase();
        const tool = TOOL_SHORTCUTS[key];
        const placementKind = PLACEMENT_SHORTCUTS[key];
        if (tool) {
          e.preventDefault();
          useDesignerStore.getState().setActiveTool(tool);
        } else if (placementKind) {
          e.preventDefault();
          armPlacement(placementKind);
        } else if (key === 'f') {
          e.preventDefault();
          void autofill();
        }
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [
    handleUndo,
    handleRedo,
    handleSave,
    handleCopy,
    handleArmPaste,
    handleDuplicate,
    handleDeleteSelection,
    handleNudge,
    armPlacement,
    autofill,
  ]);

  const isLoading =
    projectQuery.isLoading || profileSystemsQuery.isLoading || glassTypesQuery.isLoading;

  const handlePlan2DAddRun = useCallback(
    async (start: { x: number; y: number }, end: { x: number; y: number }) => {
      if (!id || profileSystems.length === 0) return;
      const dx = end.x - start.x;
      const dy = end.y - start.y;
      const length = Math.round(Math.hypot(dx, dy));
      const rotationDeg = snapAngleDeg((Math.atan2(dy, dx) * 180) / Math.PI);
      const defaultSystem = profileSystems[0];
      const runIndex = useDesignerStore.getState().scene.runs.length + 1;
      const [response] = await safeRequestWithNotify(
        enqueuePersist(() =>
          addRunMutation.mutateAsync({
            id,
            input: {
              lengthMm: length,
              heightMm: 2400,
              profileSystemId: defaultSystem.id,
              originX: start.x,
              originY: start.y,
              rotationDeg,
              label: t('GlassEnclosure.Designer.DefaultRunLabel', {
                defaultValue: `Run ${runIndex}`,
              }),
              colorId: colors[0]?.id ?? null,
              hasTopDrip: true,
              hasBottomThreshold: false,
              notes: null,
            },
          }),
        ),
        { successMessage: t('GlassEnclosure.Designer.RunAdded') },
      );
      if (response?.data) logger.info('plan2d.run-added', { id, runId: response.data.id });
    },
    [id, profileSystems, colors, addRunMutation, t],
  );

  const handlePlan2DUpdateRun = useCallback(
    async (
      runId: string,
      geometry: { lengthMm: number; originX: number; originY: number; rotationDeg: number },
    ) => {
      if (!id) return;
      const run = useDesignerStore.getState().scene.runs.find((r) => r.id === runId);
      if (!run) return;
      await safeRequestWithNotify(
        enqueuePersist(() =>
          updateRunMutation.mutateAsync({
            id,
            runId,
            input: {
              lengthMm: geometry.lengthMm,
              heightMm: run.heightMm,
              originX: geometry.originX,
              originY: geometry.originY,
              rotationDeg: geometry.rotationDeg,
              label: run.label,
              profileSystemId: run.profileSystemId,
              colorId: run.colorId,
              hasTopDrip: run.hasTopDrip,
              hasBottomThreshold: run.hasBottomThreshold,
              geomArcRadiusMm: run.geomArcRadiusMm ?? null,
              geomArcSweepDeg: run.geomArcSweepDeg ?? null,
              arcGlassBent: run.arcGlassBent ?? false,
              notes: null,
            },
          }),
        ),
        { showSuccessNotification: false },
      );
    },
    [id, updateRunMutation],
  );

  const handleAddConnectionCandidate = useCallback(
    async (runAId: string, runBId: string) => {
      if (!id) return;
      const existing = useDesignerStore
        .getState()
        .scene.connections.find(
          (c) =>
            (c.runAId === runAId && c.runBId === runBId) ||
            (c.runAId === runBId && c.runBId === runAId),
        );
      if (existing) {
        useDesignerStore.getState().setSelection({
          kind: 'connection',
          runId: null,
          panelId: null,
          connectionId: existing.id,
        });
        return;
      }
      const [response] = await safeRequestWithNotify(
        enqueuePersist(() =>
          addConnectionMutation.mutateAsync({
            id,
            input: {
              runAId,
              runBId,
              jointAngleDeg: 90,
              mitreCutDeg: 45,
              usesCornerPost: false,
              cornerProfileId: null,
            },
          }),
        ),
        { successMessage: t('GlassEnclosure.Connection.Added') },
      );
      if (response?.data) {
        useDesignerStore.getState().setSelection({
          kind: 'connection',
          runId: null,
          panelId: null,
          connectionId: response.data.id,
        });
      }
    },
    [id, addConnectionMutation, t],
  );

  const handleRecomputeBom = useCallback(async () => {
    if (!id) return;
    await safeRequestWithNotify(recomputeBomMutation.mutateAsync(id), {
      successMessage: t('GlassEnclosure.Quote.Recomputed'),
    });
  }, [id, recomputeBomMutation, t]);

  if (!id || isLoading || !project) {
    return (
      <div className="flex h-full items-center justify-center text-sm text-slate-500">
        {t('Common.Loading')}
      </div>
    );
  }

  const handleBack = () => navigate(-1);

  const canvasView: 'split' | '2d' | '3d' =
    viewMode === '2d' || viewMode === '3d' ? viewMode : 'split';

  const isReportMode =
    viewMode === 'cutting' ||
    viewMode === 'engineering' ||
    viewMode === 'quote' ||
    viewMode === 'survey';

  const canvasSlot = isReportMode ? (
    <div className="flex h-full flex-1 overflow-hidden">
      {viewMode === 'cutting' && (
        <div className="flex-1 overflow-auto bg-white dark:bg-slate-950">
          <CuttingReportView
            report={cuttingReportQuery.data?.data ?? null}
            onRegenerate={async () => {
              if (!id) return;
              await safeRequestWithNotify(generateCuttingMutation.mutateAsync(id), {
                successMessage: t('GlassEnclosure.Cutting.Generated'),
              });
            }}
            isGenerating={generateCuttingMutation.isPending}
          />
        </div>
      )}
      {viewMode === 'engineering' && (
        <div className="flex-1 overflow-auto bg-white p-4 dark:bg-slate-950">
          <TechnicalSummaryReport summary={technicalSummaryQuery.data?.data ?? null} />
        </div>
      )}
      {viewMode === 'quote' && (
        <div className="flex-1 overflow-auto bg-slate-50 dark:bg-slate-950">
          <QuoteSummaryView
            project={project}
            bom={bomQuery.data?.data ?? null}
            isLoading={bomQuery.isLoading}
            onRecompute={handleRecomputeBom}
            isRecomputing={recomputeBomMutation.isPending}
          />
        </div>
      )}
      {viewMode === 'survey' && (
        <div className="flex-1 overflow-auto bg-slate-50 dark:bg-slate-950">
          <FieldSurveyForm
            projectId={id}
            defaultFloorNumber={project.floorNumber}
            defaultBuildingHeightM={project.buildingHeightM}
          />
        </div>
      )}
    </div>
  ) : (
    <CanvasPanel
      view={canvasView}
      profileSystems={profileSystems}
      glassTypes={glassTypes}
      colors={colors}
      onAddRunFromPlan={handlePlan2DAddRun}
      onUpdateRunGeometry={handlePlan2DUpdateRun}
      onSelectConnectionCandidate={handleAddConnectionCandidate}
    />
  );

  const runsSlot = (
    <div className="flex h-full min-h-0 flex-col">
      <div className="flex min-h-0 flex-1 flex-col">
        <RunsPanel embedded onAddRun={handleAddRun} isAdding={addRunMutation.isPending} />
      </div>
      <div className="max-h-[42%] shrink-0 overflow-auto border-t border-slate-200 dark:border-slate-700">
        <SelectionSummary profileSystems={profileSystems} glassTypes={glassTypes} colors={colors} />
      </div>
    </div>
  );

  const costSlot = (
    <div className="space-y-3">
      <LiveCostPreview
        profileSystems={profileSystems}
        glassTypes={glassTypes}
        colors={colors}
        hardwareItems={hardwareItems}
        hardwareKits={hardwareKits}
        settings={settings}
        floorNumber={project.floorNumber}
      />
      <div className="border-t border-slate-200 pt-3 dark:border-slate-700">
        <ValidationPanel />
      </div>
    </div>
  );

  const commerceSlot = (
    <div className="border-t border-slate-200 pt-3 dark:border-slate-700">
      <CommercePanel project={project} />
    </div>
  );

  const inspectorSlot = (
    <InspectorPanel
      projectId={id}
      profileSystems={profileSystems}
      glassTypes={glassTypes}
      colors={colors}
      floorNumber={project.floorNumber}
      buildingHeightM={project.buildingHeightM}
      costSlot={costSlot}
      commerceSlot={commerceSlot}
    />
  );

  const bomSlot = (
    <BOMPanel
      project={project}
      bom={bomQuery.data?.data ?? null}
      isLoading={bomQuery.isLoading}
      onRecompute={handleRecomputeBom}
      isRecomputing={recomputeBomMutation.isPending}
    />
  );

  const toolbarSlot = (
    <>
      <DesignerToolbar
        onAddRun={handleAddRun}
        onSave={handleSave}
        onValidate={handleValidate}
        onUndo={() => void handleUndo()}
        onRedo={() => void handleRedo()}
        isSaving={saveMutation.isPending}
        isValidating={validateMutation.isPending}
      />
      <div className="flex flex-wrap items-center gap-1 border-b border-slate-200 bg-white px-3 py-1.5 dark:border-slate-700 dark:bg-slate-900">
        <ViewModeButton
          active={viewMode === 'split'}
          onClick={() => setViewMode('split')}
          icon={<Layers size={14} />}
          label={t('GlassEnclosure.Designer.ViewSplit')}
        />
        <ViewModeButton
          active={viewMode === '2d'}
          onClick={() => setViewMode('2d')}
          icon={<LayoutGrid size={14} />}
          label={t('GlassEnclosure.Designer.ViewPlan')}
        />
        <ViewModeButton
          active={viewMode === '3d'}
          onClick={() => setViewMode('3d')}
          icon={<Boxes size={14} />}
          label={t('GlassEnclosure.Designer.View3D')}
        />
        <span className="mx-1 h-5 w-px bg-slate-300 dark:bg-slate-700" />
        <ViewModeButton
          active={viewMode === 'cutting'}
          onClick={() => setViewMode('cutting')}
          icon={<Scissors size={14} />}
          label={t('GlassEnclosure.Designer.ViewCutting')}
        />
        <ViewModeButton
          active={viewMode === 'engineering'}
          onClick={() => setViewMode('engineering')}
          icon={<Sigma size={14} />}
          label={t('GlassEnclosure.Designer.ViewEngineering')}
        />
        <ViewModeButton
          active={viewMode === 'quote'}
          onClick={() => setViewMode('quote')}
          icon={<FileSpreadsheet size={14} />}
          label={t('GlassEnclosure.Designer.ViewQuote')}
        />
        <ViewModeButton
          active={viewMode === 'survey'}
          onClick={() => setViewMode('survey')}
          icon={<ClipboardList size={14} />}
          label={t('GlassEnclosure.Designer.ViewSurvey')}
        />
        <div className="ml-auto">
          <ExportMenu />
        </div>
      </div>
    </>
  );

  const headerSubtitle = `${project.code} · ${project.customerName ?? '—'} · ${t(
    `GlassEnclosure.Status.${project.status}` as never,
  )}`;

  const headerRight = (
    <span>
      v{project.currentSceneVersion} ·{' '}
      {t('GlassEnclosure.Designer.PanelCount', { count: project.totalPanels }).toLowerCase()} ·{' '}
      {project.totalAreaM2.toFixed(2)} m²
    </span>
  );

  return (
    <DesignerShell
      headerTitle={project.projectName}
      headerSubtitle={headerSubtitle}
      headerRight={headerRight}
      toolbarSlot={toolbarSlot}
      onBack={handleBack}
      runsSlot={runsSlot}
      canvasSlot={canvasSlot}
      inspectorSlot={inspectorSlot}
      bomSlot={bomSlot}
      sidePanelsDefaultCollapsed={isReportMode}
    />
  );
}

const ViewModeButton = ({
  active,
  onClick,
  icon,
  label,
}: {
  active: boolean;
  onClick: () => void;
  icon: React.ReactNode;
  label: string;
}) => (
  <button
    type="button"
    onClick={onClick}
    className={`inline-flex items-center gap-1.5 rounded-md px-2.5 py-1 text-xs font-medium transition ${
      active
        ? 'bg-primary-600 text-white'
        : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800'
    }`}
    aria-pressed={active}
  >
    {icon}
    {label}
  </button>
);

export default GlassProjectDesignerPage;
