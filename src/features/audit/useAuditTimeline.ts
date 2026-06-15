import { useQuery } from '@tanstack/react-query';
import { auditApi } from './auditApi';

export const useAuditTimelineQuery = (entityType: string, entityId: string | undefined) =>
  useQuery({
    queryKey: ['audit', entityType, entityId ?? ''],
    queryFn: () => auditApi.timeline(entityType, entityId as string),
    enabled: !!entityId,
    staleTime: 30 * 1000,
  });
