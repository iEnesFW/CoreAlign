import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { incomingInvoicesApi } from '../api/incomingInvoicesApi';
import { incomingInvoiceKeys } from './incomingInvoiceKeys';
import type {
  IgnoreIncomingInvoiceInput,
  IncomingInvoiceListParams,
  ProcessIncomingInvoiceInput,
} from '../model/incomingInvoice.types';

export const useIncomingInvoicesQuery = (params: IncomingInvoiceListParams) =>
  useQuery({
    queryKey: incomingInvoiceKeys.list(params),
    queryFn: () => incomingInvoicesApi.list(params),
    placeholderData: (previous) => previous,
    staleTime: 30 * 1000,
  });

export const useIncomingInvoiceQuery = (id: string | null) =>
  useQuery({
    queryKey: incomingInvoiceKeys.detail(id),
    queryFn: () => incomingInvoicesApi.getById(id as string),
    enabled: id !== null,
  });

const useInvalidate = () => {
  const qc = useQueryClient();
  return () => {
    qc.invalidateQueries({ queryKey: incomingInvoiceKeys.all });
    qc.invalidateQueries({ queryKey: ['vendor-bills'] });
  };
};

export const useProcessIncomingInvoice = () => {
  const invalidate = useInvalidate();
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: ProcessIncomingInvoiceInput }) =>
      incomingInvoicesApi.process(id, input),
    onSuccess: invalidate,
  });
};

export const useIgnoreIncomingInvoice = () => {
  const invalidate = useInvalidate();
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: IgnoreIncomingInvoiceInput }) =>
      incomingInvoicesApi.ignore(id, input),
    onSuccess: invalidate,
  });
};
