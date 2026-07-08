import { useTranslation } from 'react-i18next';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { useDesignerStore } from '../model/designerStore';
import { developedLengthMm } from '../model/arcGeometry';
import { combinePanelHardware } from '../model/panelHardware';
import { createPanelFromTemplate } from '../model/panelDefaults';
import { moveWallWithAttachments, resolveAttachedRunIds } from '../model/wallAttachment';
import { enqueuePersist } from '../model/persistQueue';
import {
  useAddPanelMutation,
  useRebalancePanelsMutation,
  useRemovePanelMutation,
  useRemoveRunMutation,
  useSetRunPanelsMutation,
  useUpdatePanelMutation,
  useUpdateRunMutation,
} from './useGlassProjectQueries';
import { useHardwareItemsQuery } from './useGlassEnclosureQueries';
import type {
  ScenePanelState,
  SceneRunState,
  SceneWallState,
  SetRunPanelsInput,
  UpdatePanelInput,
  UpdateRunInput,
} from '../model/project.types';
import type { GlassOpeningType } from '../model/glassEnclosure.types';

const toRunInput = (run: SceneRunState): UpdateRunInput => ({
  label: run.label,
  lengthMm: run.lengthMm,
  heightMm: run.heightMm,
  originX: run.originX,
  originY: run.originY,
  rotationDeg: run.rotationDeg,
  profileSystemId: run.profileSystemId,
  colorId: run.colorId,
  hasTopDrip: run.hasTopDrip,
  hasBottomThreshold: run.hasBottomThreshold,
  geomZ: run.geomZ ?? null,
  // Round-trip notes/geomTiltDeg (carried off the DTO) — hard-coding null here WIPED the
  // server values on every designer-driven run update.
  geomTiltDeg: run.geomTiltDeg ?? null,
  geomArcRadiusMm: run.geomArcRadiusMm ?? null,
  geomArcSweepDeg: run.geomArcSweepDeg ?? null,
  arcGlassBent: run.arcGlassBent ?? false,
  notes: run.notes ?? null,
});

const toPanelInput = (panel: Omit<ScenePanelState, 'panelIndex'>): UpdatePanelInput => ({
  widthMm: panel.widthMm,
  openingType: panel.openingType,
  glassTypeId: panel.glassTypeId,
  hasHandle: panel.hasHandle,
  hasLock: panel.hasLock,
  hasBrushSeal: panel.hasBrushSeal,
  notes: panel.notes ?? null,
  // Shape lives on structured columns (projectToScene hydrates these on reload); omitting
  // them here silently dropped manual shaping on the next load. cornerNotchMm is blob-only
  // (not on the DTO) and rides the scene-json rescue instead.
  heightMm: panel.heightMm ?? null,
  topShape: panel.topShape ?? null,
  topRightHeightMm: panel.topRightHeightMm ?? null,
  archRiseMm: panel.archRiseMm ?? null,
  cornerRadiiMm: panel.cornerRadiiMm ?? null,
  shapeKind: panel.shapeKind ?? null,
  shapePointsJson: panel.shapePointsJson ?? null,
});

export const useRunEntityActions = () => {
  const { t } = useTranslation();
  const projectId = useDesignerStore((s) => s.projectId);
  const removeRunLocal = useDesignerStore((s) => s.removeRun);
  const rebalancePanelsLocal = useDesignerStore((s) => s.rebalancePanels);
  const updateRunMutation = useUpdateRunMutation();
  const removeRunMutation = useRemoveRunMutation();
  const rebalanceMutation = useRebalancePanelsMutation();

  const persistRun = async (run: SceneRunState) => {
    if (!projectId) return;
    await safeRequestWithNotify(
      enqueuePersist(() =>
        updateRunMutation.mutateAsync({ id: projectId, runId: run.id, input: toRunInput(run) }),
      ),
    );
  };

  const deleteRun = async (runId: string) => {
    if (!projectId) return;
    removeRunLocal(runId);
    await safeRequestWithNotify(
      enqueuePersist(() => removeRunMutation.mutateAsync({ id: projectId, runId })),
      {
        successMessage: t('GlassEnclosure.Designer.RunDeleted', { defaultValue: 'Run deleted' }),
        showSuccessNotification: true,
      },
    );
  };

  const rebalance = async (
    runId: string,
    count: number,
    openingType: GlassOpeningType,
    glassTypeId: string,
  ) => {
    if (!projectId) return;
    rebalancePanelsLocal(runId, count, openingType, glassTypeId);
    await safeRequestWithNotify(
      enqueuePersist(() =>
        rebalanceMutation.mutateAsync({
          id: projectId,
          runId,
          input: {
            panelCount: count,
            defaultOpeningType: openingType,
            defaultGlassTypeId: glassTypeId,
          },
        }),
      ),
    );
  };

  return { persistRun, deleteRun, rebalance };
};

export const usePanelEntityActions = () => {
  const { t } = useTranslation();
  const projectId = useDesignerStore((s) => s.projectId);
  const removePanelLocal = useDesignerStore((s) => s.removePanel);
  const addPanelMutation = useAddPanelMutation();
  const updatePanelMutation = useUpdatePanelMutation();
  const removePanelMutation = useRemovePanelMutation();
  const setRunPanelsMutation = useSetRunPanelsMutation();
  const hardwareCatalog = useHardwareItemsQuery({ isActive: true }).data?.data ?? [];

  const createPanelFrom = async (runId: string, source: Omit<ScenePanelState, 'panelIndex'>) => {
    if (!projectId) return null;
    const [response] = await safeRequestWithNotify(
      enqueuePersist(() =>
        addPanelMutation.mutateAsync({ id: projectId, runId, input: toPanelInput(source) }),
      ),
      {
        successMessage: t('GlassEnclosure.Designer.PanelAdded', { defaultValue: 'Panel added' }),
        showSuccessNotification: true,
      },
    );
    return response?.data ?? null;
  };

  const createPanel = async (
    runId: string,
    template: ScenePanelState | undefined,
    fallbackGlassTypeId: string,
  ) => {
    const run = useDesignerStore.getState().scene.runs.find((r) => r.id === runId);
    const base = createPanelFromTemplate(template, fallbackGlassTypeId);
    // Panels divide the DEVELOPED length (radius·sweep on an arc run), not the chord.
    const widthMm = run
      ? Math.max(
          100,
          Math.round(
            developedLengthMm(run.lengthMm, run.geomArcRadiusMm, run.geomArcSweepDeg) /
              (run.panels.length + 1),
          ),
        )
      : base.widthMm;
    return createPanelFrom(runId, { ...base, widthMm });
  };

  const persistPanel = async (runId: string, panel: ScenePanelState) => {
    if (!projectId) return;
    await safeRequestWithNotify(
      enqueuePersist(() =>
        updatePanelMutation.mutateAsync({
          id: projectId,
          runId,
          panelId: panel.id,
          input: toPanelInput(panel),
        }),
      ),
    );
  };

  // WHY: divide adds a panel mid-array — the append-only AddPanel can't; this reconciles the run's
  // whole panel set by id (keeps existing ids + their hardware, adds the new split half, reindexes).
  const persistRunPanels = async (runId: string) => {
    if (!projectId) return;
    const run = useDesignerStore.getState().scene.runs.find((r) => r.id === runId);
    if (!run) return;
    const input: SetRunPanelsInput = {
      panels: run.panels.map((p) => ({
        id: p.id,
        widthMm: p.widthMm,
        openingType: p.openingType,
        glassTypeId: p.glassTypeId,
      })),
    };
    await safeRequestWithNotify(
      enqueuePersist(() => setRunPanelsMutation.mutateAsync({ id: projectId, runId, input })),
    );
  };

  // WHY: hardware persists on a DELIBERATE hardware change only — general panel writes omit it (null = don't touch) so a load-race transient [] can never wipe the structural rows.
  const persistPanelHardware = async (runId: string, panelId: string) => {
    if (!projectId) return;
    const run = useDesignerStore.getState().scene.runs.find((r) => r.id === runId);
    const panel = run?.panels.find((p) => p.id === panelId);
    if (!panel) return;
    await safeRequestWithNotify(
      enqueuePersist(() =>
        updatePanelMutation.mutateAsync({
          id: projectId,
          runId,
          panelId,
          input: { ...toPanelInput(panel), hardware: combinePanelHardware(panel, hardwareCatalog) },
        }),
      ),
    );
  };

  const deletePanel = async (runId: string, panelId: string) => {
    if (!projectId) return;
    removePanelLocal(runId, panelId);
    await safeRequestWithNotify(
      enqueuePersist(() => removePanelMutation.mutateAsync({ id: projectId, runId, panelId })),
      {
        successMessage: t('GlassEnclosure.Designer.PanelDeleted', {
          defaultValue: 'Panel deleted',
        }),
        showSuccessNotification: true,
      },
    );
  };

  return {
    createPanel,
    createPanelFrom,
    persistPanel,
    persistPanelHardware,
    persistRunPanels,
    deletePanel,
  };
};

export const useWallEntityActions = () => {
  const { persistRun } = useRunEntityActions();

  // WHY: numeric/inspector wall edits must co-move + persist attached glass exactly like the drag path (onCommitWallMove) — otherwise the glass is left behind (audit §2b) or snaps back on refetch.
  const commitWallPatch = (wall: SceneWallState, patch: Partial<SceneWallState>) => {
    const store = useDesignerStore.getState();
    const after = { ...wall, ...patch };
    const poseChanged =
      after.originX !== wall.originX ||
      after.originY !== wall.originY ||
      after.rotationDeg !== wall.rotationDeg;
    if (!poseChanged) {
      store.updateWall(wall.id, patch);
      return;
    }
    const attachedIds = new Set(resolveAttachedRunIds(wall, store.scene.runs));
    const movedById = new Map(
      moveWallWithAttachments(
        wall,
        after,
        store.scene.runs.filter((r) => attachedIds.has(r.id)),
      ).map((r) => [r.id, r] as const),
    );
    store.applyScenePatch((s) => ({
      ...s,
      walls: (s.walls ?? []).map((w) => (w.id === wall.id ? { ...w, ...patch } : w)),
      runs: s.runs.map((r) => movedById.get(r.id) ?? r),
    }));
    for (const id of attachedIds) {
      const fresh = useDesignerStore.getState().scene.runs.find((r) => r.id === id);
      if (fresh) void persistRun(fresh);
    }
  };

  return { commitWallPatch };
};

export const useDesignerEntityActions = () => ({
  ...useRunEntityActions(),
  ...usePanelEntityActions(),
});
