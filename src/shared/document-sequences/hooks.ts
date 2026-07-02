import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { documentSequencesApi } from './documentSequencesApi';
import type { ConfigureDocumentSequenceRequest } from './types';

export const DOCUMENT_SEQUENCES_KEY = ['settings', 'document-sequences'] as const;

export const useDocumentSequencesQuery = () =>
  useQuery({
    queryKey: DOCUMENT_SEQUENCES_KEY,
    queryFn: () => documentSequencesApi.list(),
    staleTime: 5 * 60 * 1000,
  });

export const useConfigureDocumentSequence = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (request: ConfigureDocumentSequenceRequest) =>
      documentSequencesApi.configure(request),
    onSuccess: () => qc.invalidateQueries({ queryKey: DOCUMENT_SEQUENCES_KEY }),
  });
};
