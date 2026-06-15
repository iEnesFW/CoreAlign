import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { tagsApi } from '../api/tagsApi';
import type { CreateTagInput, UpdateTagInput } from '../model/tag.types';

const FIVE_MINUTES = 5 * 60 * 1000;

export const tagKeys = {
  all: ['tags'] as const,
  list: (isActive?: boolean) => [...tagKeys.all, 'list', { isActive }] as const,
};

export const useTagsQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: tagKeys.list(isActive),
    queryFn: () => tagsApi.list(isActive),
    staleTime: FIVE_MINUTES,
  });

const invalidateTags = (qc: ReturnType<typeof useQueryClient>) => {
  qc.invalidateQueries({ queryKey: tagKeys.all });
  qc.invalidateQueries({ queryKey: ['customers'] });
};

export const useCreateTag = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateTagInput) => tagsApi.create(input),
    onSuccess: () => invalidateTags(qc),
  });
};

export const useUpdateTag = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateTagInput) => tagsApi.update(input),
    onSuccess: () => invalidateTags(qc),
  });
};

export const useDeleteTag = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => tagsApi.remove(id),
    onSuccess: () => invalidateTags(qc),
  });
};
