import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';

export interface EntityAuditLogDto {
  id: string;
  entityType: string;
  entityId: string;
  action: 'Create' | 'Update' | 'Delete';
  beforeJson?: string | null;
  afterJson?: string | null;
  userId?: string | null;
  changedAtUtc: string;
  correlationId?: string | null;
  rollingHash: string;
  sequence: number;
}

export const auditApi = {
  timeline: (entityType: string, entityId: string) =>
    apiClient
      .get<ApiResponse<EntityAuditLogDto[]>>(`/audit/entity/${entityType}/${entityId}`)
      .then((r) => r.data),
};
