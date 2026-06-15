import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { feedbackApi, type FeedbackListParams } from '../api/feedbackApi';
import type { CreateFeedbackInput, UpdateFeedbackStatusInput } from '../model/feedback.types';

export const useFeedbackListQuery = (params: FeedbackListParams) =>
  useQuery({
    queryKey: ['feedback', 'list', params] as const,
    queryFn: () => feedbackApi.list(params),
    staleTime: 30 * 1000,
  });

export const useCreateFeedback = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateFeedbackInput) => feedbackApi.create(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['feedback'] }),
  });
};

export const useUpdateFeedbackStatus = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateFeedbackStatusInput) => feedbackApi.updateStatus(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['feedback'] }),
  });
};
