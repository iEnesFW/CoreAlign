import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { glPostingMapApi } from '../api/glPostingMapApi';
import type { ConfigureGLPostingMapRequest } from '../model/glPostingMap.types';

const KEY = ['accounting', 'gl-posting-map'] as const;

export const useGLPostingMapQuery = () =>
  useQuery({
    queryKey: KEY,
    queryFn: () => glPostingMapApi.list(),
    staleTime: 5 * 60 * 1000,
  });

export const useConfigureGLPostingMapping = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (request: ConfigureGLPostingMapRequest) => glPostingMapApi.configure(request),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
};
