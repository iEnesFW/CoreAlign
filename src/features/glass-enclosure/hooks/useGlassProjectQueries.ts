import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { isApiError } from '@/shared/api/ApiError';
import { glassProjectsApi } from '../api/glassProjectsApi';
import { glassProjectKeys } from './projectKeys';
import type { ProjectsListParams } from '../model/project.types';
import type { AddManualBomLineInput, Optimize2DNestingInput } from '../model/engineering.types';

export const useGlassProjectsQuery = (params: ProjectsListParams) =>
  useQuery({
    queryKey: glassProjectKeys.list(params),
    queryFn: () => glassProjectsApi.list(params),
    placeholderData: (previous) => previous,
  });

export const useGlassProjectQuery = (id: string | null) =>
  useQuery({
    queryKey: glassProjectKeys.detail(id),
    queryFn: () => glassProjectsApi.getById(id as string),
    enabled: id !== null,
  });

export const useSceneLatestQuery = (id: string | null) =>
  useQuery({
    queryKey: glassProjectKeys.sceneLatest(id),
    queryFn: () => glassProjectsApi.getSceneLatest(id as string),
    enabled: id !== null,
  });

export const useSceneVersionsQuery = (id: string | null) =>
  useQuery({
    queryKey: glassProjectKeys.sceneVersions(id),
    queryFn: () => glassProjectsApi.getSceneVersions(id as string),
    enabled: id !== null,
  });

const invalidateProject = (qc: ReturnType<typeof useQueryClient>, id: string | null) => {
  if (id) qc.invalidateQueries({ queryKey: glassProjectKeys.detail(id) });
  qc.invalidateQueries({ queryKey: glassProjectKeys.lists() });
};

// A 404 from a run/panel mutation means the client scene references a run the server no longer
// has (undo/redo split-brain). Refetching replaces the scene with server truth via loadProject,
// instead of leaving dead ids that 404 on every later drag/panel/rebalance commit.
const healOn404 = (qc: ReturnType<typeof useQueryClient>, error: unknown, id: string) => {
  if (isApiError(error) && error.statusCode === 404) invalidateProject(qc, id);
};

export const useCreateProjectMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassProjectsApi.create,
    onSuccess: () => qc.invalidateQueries({ queryKey: glassProjectKeys.lists() }),
  });
};

export const useUpdateProjectHeaderMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      input,
    }: {
      id: string;
      input: Parameters<typeof glassProjectsApi.updateHeader>[1];
    }) => glassProjectsApi.updateHeader(id, input),
    onSuccess: (_, vars) => invalidateProject(qc, vars.id),
  });
};

export const useAssignProjectTeamMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      input,
    }: {
      id: string;
      input: Parameters<typeof glassProjectsApi.assignTeam>[1];
    }) => glassProjectsApi.assignTeam(id, input),
    onSuccess: (_, vars) => invalidateProject(qc, vars.id),
  });
};

export const useConfigureEnclosureMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      input,
    }: {
      id: string;
      input: Parameters<typeof glassProjectsApi.configureEnclosure>[1];
    }) => glassProjectsApi.configureEnclosure(id, input),
    onSuccess: (_, vars) => invalidateProject(qc, vars.id),
  });
};

export const useTransitionProjectStatusMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      input,
    }: {
      id: string;
      input: Parameters<typeof glassProjectsApi.transitionStatus>[1];
    }) => glassProjectsApi.transitionStatus(id, input),
    onSuccess: (_, vars) => invalidateProject(qc, vars.id),
  });
};

export const useDeleteProjectMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassProjectsApi.remove,
    onSuccess: () => qc.invalidateQueries({ queryKey: glassProjectKeys.lists() }),
  });
};

export const useAddRunMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      input,
    }: {
      id: string;
      input: Parameters<typeof glassProjectsApi.addRun>[1];
    }) => glassProjectsApi.addRun(id, input),
    onSuccess: (_, vars) => invalidateProject(qc, vars.id),
  });
};

export const useUpdateRunMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      runId,
      input,
    }: {
      id: string;
      runId: string;
      input: Parameters<typeof glassProjectsApi.updateRun>[2];
    }) => glassProjectsApi.updateRun(id, runId, input),
    onSuccess: (_, vars) => invalidateProject(qc, vars.id),
    onError: (error, vars) => healOn404(qc, error, vars.id),
  });
};

export const useRemoveRunMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, runId }: { id: string; runId: string }) =>
      glassProjectsApi.removeRun(id, runId),
    onSuccess: (_, vars) => invalidateProject(qc, vars.id),
    onError: (error, vars) => healOn404(qc, error, vars.id),
  });
};

export const useRebalancePanelsMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      runId,
      input,
    }: {
      id: string;
      runId: string;
      input: Parameters<typeof glassProjectsApi.rebalancePanels>[2];
    }) => glassProjectsApi.rebalancePanels(id, runId, input),
    onSuccess: (_, vars) => invalidateProject(qc, vars.id),
    onError: (error, vars) => healOn404(qc, error, vars.id),
  });
};

export const useSetRunPanelsMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      runId,
      input,
    }: {
      id: string;
      runId: string;
      input: Parameters<typeof glassProjectsApi.setRunPanels>[2];
    }) => glassProjectsApi.setRunPanels(id, runId, input),
    onSuccess: (_, vars) => invalidateProject(qc, vars.id),
    onError: (error, vars) => healOn404(qc, error, vars.id),
  });
};

export const useAddPanelMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      runId,
      input,
    }: {
      id: string;
      runId: string;
      input: Parameters<typeof glassProjectsApi.addPanel>[2];
    }) => glassProjectsApi.addPanel(id, runId, input),
    onSuccess: (_, vars) => invalidateProject(qc, vars.id),
    onError: (error, vars) => healOn404(qc, error, vars.id),
  });
};

export const useUpdatePanelMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      runId,
      panelId,
      input,
    }: {
      id: string;
      runId: string;
      panelId: string;
      input: Parameters<typeof glassProjectsApi.updatePanel>[3];
    }) => glassProjectsApi.updatePanel(id, runId, panelId, input),
    onSuccess: (_, vars) => invalidateProject(qc, vars.id),
    onError: (error, vars) => healOn404(qc, error, vars.id),
  });
};

export const useRemovePanelMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, runId, panelId }: { id: string; runId: string; panelId: string }) =>
      glassProjectsApi.removePanel(id, runId, panelId),
    onSuccess: (_, vars) => invalidateProject(qc, vars.id),
    onError: (error, vars) => healOn404(qc, error, vars.id),
  });
};

export const useSaveSceneMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      input,
    }: {
      id: string;
      input: Parameters<typeof glassProjectsApi.saveScene>[1];
    }) => glassProjectsApi.saveScene(id, input),
    onSuccess: (_, vars) => {
      qc.invalidateQueries({ queryKey: glassProjectKeys.sceneLatest(vars.id) });
      qc.invalidateQueries({ queryKey: glassProjectKeys.sceneVersions(vars.id) });
    },
  });
};

export const useValidateProjectMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassProjectsApi.validate,
    onSuccess: (_, id) => qc.invalidateQueries({ queryKey: glassProjectKeys.validation(id) }),
  });
};

export const useAddConnectionMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      input,
    }: {
      id: string;
      input: Parameters<typeof glassProjectsApi.addConnection>[1];
    }) => glassProjectsApi.addConnection(id, input),
    onSuccess: (_, vars) => invalidateProject(qc, vars.id),
  });
};

export const useUpdateConnectionMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      connectionId,
      input,
    }: {
      id: string;
      connectionId: string;
      input: Parameters<typeof glassProjectsApi.updateConnection>[2];
    }) => glassProjectsApi.updateConnection(id, connectionId, input),
    onSuccess: (_, vars) => invalidateProject(qc, vars.id),
  });
};

export const useRemoveConnectionMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, connectionId }: { id: string; connectionId: string }) =>
      glassProjectsApi.removeConnection(id, connectionId),
    onSuccess: (_, vars) => invalidateProject(qc, vars.id),
  });
};

export const useProjectBOMQuery = (id: string | null) =>
  useQuery({
    queryKey: glassProjectKeys.bom(id),
    queryFn: () => glassProjectsApi.getBOM(id as string),
    enabled: id !== null,
  });

export const useRecomputeBOMMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassProjectsApi.recomputeBOM,
    onSuccess: (_, id) => qc.invalidateQueries({ queryKey: glassProjectKeys.bom(id) }),
  });
};

const invalidateBom = (qc: ReturnType<typeof useQueryClient>, id: string) => {
  qc.invalidateQueries({ queryKey: glassProjectKeys.bom(id) });
  invalidateProject(qc, id);
};

export const useOverrideBomLinePriceMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      lineId,
      unitPriceOverride,
    }: {
      id: string;
      lineId: string;
      unitPriceOverride: number | null;
    }) => glassProjectsApi.overrideBomLinePrice(id, lineId, unitPriceOverride),
    onSuccess: (_, vars) => invalidateBom(qc, vars.id),
  });
};

export const useAddManualBomLineMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: AddManualBomLineInput }) =>
      glassProjectsApi.addManualBomLine(id, input),
    onSuccess: (_, vars) => invalidateBom(qc, vars.id),
  });
};

export const useDeleteManualBomLineMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, lineId }: { id: string; lineId: string }) =>
      glassProjectsApi.deleteManualBomLine(id, lineId),
    onSuccess: (_, vars) => invalidateBom(qc, vars.id),
  });
};

export const usePushBomLinePriceToCatalogMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, lineId }: { id: string; lineId: string }) =>
      glassProjectsApi.pushBomLinePriceToCatalog(id, lineId),
    onSuccess: (_, vars) => invalidateBom(qc, vars.id),
  });
};

export const useCuttingReportQuery = (id: string | null) =>
  useQuery({
    queryKey: glassProjectKeys.cuttingPlan(id),
    queryFn: () => glassProjectsApi.getCuttingReport(id as string),
    enabled: id !== null,
  });

export const useGenerateCuttingPlanMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassProjectsApi.generateCuttingPlan,
    onSuccess: (_, id) => qc.invalidateQueries({ queryKey: glassProjectKeys.cuttingPlan(id) }),
  });
};

export const useOptimize2DNestingMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (args: { id: string; input: Optimize2DNestingInput }) =>
      glassProjectsApi.optimize2DNesting(args.id, args.input),
    onSuccess: (_, args) =>
      qc.invalidateQueries({ queryKey: glassProjectKeys.cuttingPlan(args.id) }),
  });
};

export const useTechnicalSummaryQuery = (id: string | null) =>
  useQuery({
    queryKey: glassProjectKeys.technicalSummary(id),
    queryFn: () => glassProjectsApi.getTechnicalSummary(id as string),
    enabled: id !== null,
  });

export const useShareTokensQuery = (id: string | null) =>
  useQuery({
    queryKey: glassProjectKeys.shareTokens(id),
    queryFn: () => glassProjectsApi.listShareTokens(id as string),
    enabled: id !== null,
  });

export const useGenerateShareTokenMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, overrideTtlDays }: { id: string; overrideTtlDays: number | null }) =>
      glassProjectsApi.generateShareToken(id, overrideTtlDays),
    onSuccess: (_, vars) =>
      qc.invalidateQueries({ queryKey: glassProjectKeys.shareTokens(vars.id) }),
  });
};

export const useConvertToOrderMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => glassProjectsApi.convertToOrder(id),
    onSuccess: (_, id) => invalidateProject(qc, id),
  });
};

export const useWorkOrdersQuery = (id: string | null) =>
  useQuery({
    queryKey: glassProjectKeys.workOrders(id),
    queryFn: () => glassProjectsApi.listWorkOrders(id as string),
    enabled: id !== null,
  });

export const useReleaseToProductionMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      input,
    }: {
      id: string;
      input: Parameters<typeof glassProjectsApi.releaseToProduction>[1];
    }) => glassProjectsApi.releaseToProduction(id, input),
    onSuccess: (_, vars) => {
      qc.invalidateQueries({ queryKey: glassProjectKeys.workOrders(vars.id) });
      invalidateProject(qc, vars.id);
    },
  });
};

export const useNotificationHistoryQuery = (id: string | null) =>
  useQuery({
    queryKey: glassProjectKeys.notifications(id),
    queryFn: () => glassProjectsApi.listNotificationHistory(id as string),
    enabled: id !== null,
  });

export const useUpdateWorkOrderStatusMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      workOrderId,
      status,
    }: {
      workOrderId: string;
      status: string;
      projectId: string;
    }) => glassProjectsApi.updateWorkOrderStatus(workOrderId, status),
    onSuccess: (_, vars) => {
      qc.invalidateQueries({ queryKey: glassProjectKeys.workOrders(vars.projectId) });
      invalidateProject(qc, vars.projectId);
    },
  });
};

export const useRecordDefectMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      workOrderId,
      defectNotes,
    }: {
      workOrderId: string;
      defectNotes: string;
      projectId: string;
    }) => glassProjectsApi.recordWorkOrderDefect(workOrderId, defectNotes),
    onSuccess: (_, vars) => {
      qc.invalidateQueries({ queryKey: glassProjectKeys.workOrders(vars.projectId) });
      qc.invalidateQueries({ queryKey: glassProjectKeys.notifications(vars.projectId) });
    },
  });
};
