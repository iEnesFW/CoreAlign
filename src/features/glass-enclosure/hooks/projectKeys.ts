import type { ProjectsListParams } from '../model/project.types';

export const glassProjectKeys = {
  all: ['glass-projects'] as const,
  lists: () => [...glassProjectKeys.all, 'list'] as const,
  list: (params: ProjectsListParams) => [...glassProjectKeys.lists(), params] as const,
  details: () => [...glassProjectKeys.all, 'detail'] as const,
  detail: (id: string | null) => [...glassProjectKeys.details(), id] as const,
  scenes: (id: string | null) => [...glassProjectKeys.all, 'scene', id] as const,
  sceneLatest: (id: string | null) => [...glassProjectKeys.scenes(id), 'latest'] as const,
  sceneVersions: (id: string | null) => [...glassProjectKeys.scenes(id), 'versions'] as const,
  validation: (id: string | null) => [...glassProjectKeys.all, 'validation', id] as const,
  bom: (id: string | null) => [...glassProjectKeys.all, 'bom', id] as const,
  cuttingPlan: (id: string | null) => [...glassProjectKeys.all, 'cutting-plan', id] as const,
  technicalSummary: (id: string | null) =>
    [...glassProjectKeys.all, 'technical-summary', id] as const,
  shareTokens: (id: string | null) => [...glassProjectKeys.all, 'share-tokens', id] as const,
  workOrders: (id: string | null) => [...glassProjectKeys.all, 'work-orders', id] as const,
  notifications: (id: string | null) => [...glassProjectKeys.all, 'notifications', id] as const,
};
