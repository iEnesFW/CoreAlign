import { useTranslation } from 'react-i18next';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { useDesignerStore } from '../model/designerStore';
import { createPanelFromTemplate } from '../model/panelDefaults';
import { enqueuePersist } from '../model/persistQueue';
import {
  useAddPanelMutation,
  useRebalancePanelsMutation,
  useRemovePanelMutation,
  useRemoveRunMutation,
  useUpdatePanelMutation,
  useUpdateRunMutation,
} from './useGlassProjectQueries';
import type {
  ScenePanelState,
  SceneRunState,
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
  geomArcRadiusMm: run.geomArcRadiusMm ?? null,
  geomArcSweepDeg: run.geomArcSweepDeg ?? null,
  arcGlassBent: run.arcGlassBent ?? false,
  notes: null,
});

const toPanelInput = (panel: Omit<ScenePanelState, 'panelIndex'>): UpdatePanelInput => ({
  widthMm: panel.widthMm,
  openingType: panel.openingType,
  glassTypeId: panel.glassTypeId,
  hasHandle: panel.hasHandle,
  hasLock: panel.hasLock,
  hasBrushSeal: panel.hasBrushSeal,
  notes: null,
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
    const widthMm = run
      ? Math.max(100, Math.round(run.lengthMm / (run.panels.length + 1)))
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

  return { createPanel, createPanelFrom, persistPanel, deletePanel };
};

export const useDesignerEntityActions = () => ({
  ...useRunEntityActions(),
  ...usePanelEntityActions(),
});
