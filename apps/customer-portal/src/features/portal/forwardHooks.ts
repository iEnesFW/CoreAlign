import { useMutation } from '@tanstack/react-query';
import { portalForwardApi, type ForwardDocumentInput } from './forwardApi';

export const useForwardDocument = () =>
  useMutation({
    mutationFn: (input: ForwardDocumentInput) => portalForwardApi.forward(input),
  });
