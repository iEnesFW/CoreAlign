import { useTranslation } from 'react-i18next';
import { safeRequest, safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { glassProjectsApi } from '../api/glassProjectsApi';
import { useDesignerStore } from '../model/designerStore';
import { developedLengthMm } from '../model/arcGeometry';
import { combinePanelHardware } from '../model/panelHardware';
import { createPanelFromTemplate } from '../model/panelDefaults';
import { moveWallWithAttachments, resolveAttachedRunIds } from '../model/wallAttachment';
import { blockedByLock, clampWallPatch } from '../model/sceneGuards';
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
  SceneHardwareItem,
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
  const addHardware = useDesignerStore((s) => s.addHardware);
  const updateRunMutation = useUpdateRunMutation();
  const removeRunMutation = useRemoveRunMutation();
  const rebalanceMutation = useRebalancePanelsMutation();
  const updatePanelMutation = useUpdatePanelMutation();
  const hardwareCatalog = useHardwareItemsQuery({ isActive: true }).data?.data ?? [];

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
    // WHY(C3-full): the server rebalance rebuilds the run's panels with fresh ids and no hardware,
    // and the refetch overwrites any local re-map — so capture each hardware item's ABSOLUTE position
    // along the run BEFORE the rebalance, and after the server round-trip re-map it onto the new
    // panels by position and persist. Everything is best-effort: on any missing data it bails (the
    // hardware is simply not restored — the RunInspector already confirmed the discard with the user).
    const oldRun = useDesignerStore.getState().scene.runs.find((r) => r.id === runId);
    const carried: { absX: number; item: SceneHardwareItem }[] = [];
    if (oldRun) {
      let acc = 0;
      for (const p of oldRun.panels) {
        const center = acc + p.widthMm / 2;
        for (const hw of p.hardware) carried.push({ absX: center + hw.offsetXmm, item: hw });
        acc += p.widthMm;
      }
    }

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
    if (carried.length === 0) return;

    const [resp] = await safeRequest(glassProjectsApi.getById(projectId));
    const fresh = resp?.data;
    const freshRun = fresh?.runs.find((r) => r.id === runId);
    if (!fresh || !freshRun || freshRun.panels.length === 0) return;
    useDesignerStore.getState().loadProject(fresh);

    // Map absolute positions to the NEW panel spans, clamp the offset into each pane, re-add + persist.
    let acc = 0;
    const spans = freshRun.panels.map((p) => {
      const start = acc;
      acc += p.widthMm;
      return { id: p.id, start, end: acc, center: start + p.widthMm / 2, width: p.widthMm };
    });
    const touched = new Set<string>();
    for (const c of carried) {
      const span =
        spans.find((s) => c.absX >= s.start && c.absX < s.end) ??
        (c.absX < spans[0].start ? spans[0] : spans[spans.length - 1]);
      const half = Math.max(0, span.width / 2 - c.item.widthMm / 2);
      addHardware(runId, span.id, {
        ...c.item,
        id: crypto.randomUUID(),
        offsetXmm: Math.max(-half, Math.min(c.absX - span.center, half)),
      });
      touched.add(span.id);
    }
    for (const panelId of touched) {
      const panel = useDesignerStore
        .getState()
        .scene.runs.find((r) => r.id === runId)
        ?.panels.find((p) => p.id === panelId);
      if (!panel) continue;
      await safeRequestWithNotify(
        enqueuePersist(() =>
          updatePanelMutation.mutateAsync({
            id: projectId,
            runId,
            panelId,
            input: {
              ...toPanelInput(panel),
              hardware: combinePanelHardware(panel, hardwareCatalog),
            },
          }),
        ),
      );
    }
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
  const commitWallPatch = (wall: SceneWallState, rawPatch: Partial<SceneWallState>) => {
    const store = useDesignerStore.getState();
    // WHY the gate runs HERE too: the pose branch below writes the wall through applyScenePatch,
    // which is a raw scene swap — it never sees `blockedByLock` or `clampWallPatch`. So a LOCKED
    // wall could still be moved and rotated from the inspector and the transform toolbar, and the
    // shape floors / opening re-fit were skipped on exactly the edits that reshape the wall.
    if (blockedByLock(wall, rawPatch)) return;
    const patch = clampWallPatch(wall, rawPatch);
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
