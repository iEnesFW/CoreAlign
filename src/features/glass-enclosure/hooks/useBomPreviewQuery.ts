import { useQuery } from '@tanstack/react-query';
import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue';
import { glassProjectsApi } from '../api/glassProjectsApi';

// Live cost preview off the backend BOM (single source of truth). The preview composes the
// PERSISTED scene, so the revision (designer history index) is debounced past the ~1200ms scene
// autosave — a refetch then reads the just-saved scene rather than one mid-edit.
export const useBomPreviewQuery = (
  projectId: string | null,
  revision: number,
  enabled: boolean,
) => {
  const settledRevision = useDebouncedValue(revision, 1500);
  return useQuery({
    queryKey: ['glass-projects', projectId, 'bom-preview', settledRevision],
    queryFn: () => glassProjectsApi.getBomPreview(projectId as string),
    enabled: enabled && Boolean(projectId),
    staleTime: 60_000,
    gcTime: 120_000,
  });
};
