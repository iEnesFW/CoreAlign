import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { feedbackApi, feedbackThreadApi, type FeedbackListParams } from '../api/feedbackApi';
import type {
  AddFeedbackCommentInput,
  CreateFeedbackInput,
  UpdateFeedbackStatusInput,
} from '../model/feedback.types';

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

export const useUploadFeedbackAttachment = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, file }: { id: string; file: File }) =>
      feedbackApi.uploadAttachment(id, file),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['feedback'] }),
  });
};

export const useFeedbackTicketQuery = (id: string | null) =>
  useQuery({
    queryKey: ['feedback', 'detail', id] as const,
    queryFn: () => feedbackApi.getById(id as string),
    enabled: Boolean(id),
  });

export const useFeedbackCommentsQuery = (ticketId: string | null) =>
  useQuery({
    queryKey: ['feedback', 'comments', ticketId] as const,
    queryFn: () => feedbackThreadApi.listComments(ticketId as string),
    enabled: Boolean(ticketId),
  });

export const useFeedbackAttachmentsQuery = (ticketId: string | null) =>
  useQuery({
    queryKey: ['feedback', 'attachments', ticketId] as const,
    queryFn: () => feedbackThreadApi.listAttachments(ticketId as string),
    enabled: Boolean(ticketId),
  });

export const useAddFeedbackComment = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: AddFeedbackCommentInput) => feedbackThreadApi.addComment(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['feedback'] }),
  });
};

export const useUploadFeedbackAttachments = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ ticketId, files }: { ticketId: string; files: File[] }) =>
      feedbackThreadApi.uploadAttachments(ticketId, files),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['feedback'] }),
  });
};

export const useDeleteFeedbackAttachment = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ ticketId, attachmentId }: { ticketId: string; attachmentId: string }) =>
      feedbackThreadApi.deleteAttachment(ticketId, attachmentId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['feedback'] }),
  });
};
