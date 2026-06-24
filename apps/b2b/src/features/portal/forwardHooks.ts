import { useMutation } from '@tanstack/react-query';
import { dealerForwardApi, type ForwardDocumentInput } from './forwardApi';

export const useForwardDocument = () =>
  useMutation({
    mutationFn: (input: ForwardDocumentInput) => dealerForwardApi.forward(input),
  });
