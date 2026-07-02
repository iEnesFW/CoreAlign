import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { recurringInvoicesApi } from '../api/recurringInvoicesApi';
import type {
  CreateRecurringInvoiceInput,
  RecurringInvoiceListParams,
  UpdateRecurringInvoiceInput,
} from '../model/recurringInvoice.types';

const ROOT = ['recurring-invoices'] as const;

export const useRecurringInvoicesQuery = (params: RecurringInvoiceListParams) =>
  useQuery({
    queryKey: [...ROOT, 'list', params] as const,
    queryFn: () => recurringInvoicesApi.list(params),
    placeholderData: (previous) => previous,
    staleTime: 30 * 1000,
  });

export const useRecurringInvoiceQuery = (id: string | null) =>
  useQuery({
    queryKey: [...ROOT, 'detail', id] as const,
    queryFn: () => recurringInvoicesApi.getById(id as string),
    enabled: id !== null,
  });

const useInvalidate = () => {
  const qc = useQueryClient();
  return () => {
    qc.invalidateQueries({ queryKey: ROOT });
    qc.invalidateQueries({ queryKey: ['invoices'] });
  };
};

export const useCreateRecurringInvoice = () => {
  const invalidate = useInvalidate();
  return useMutation({
    mutationFn: (input: CreateRecurringInvoiceInput) => recurringInvoicesApi.create(input),
    onSuccess: invalidate,
  });
};

export const useUpdateRecurringInvoice = () => {
  const invalidate = useInvalidate();
  return useMutation({
    mutationFn: (input: UpdateRecurringInvoiceInput) => recurringInvoicesApi.update(input),
    onSuccess: invalidate,
  });
};

export const usePauseRecurringInvoice = () => {
  const invalidate = useInvalidate();
  return useMutation({
    mutationFn: (id: string) => recurringInvoicesApi.pause(id),
    onSuccess: invalidate,
  });
};

export const useResumeRecurringInvoice = () => {
  const invalidate = useInvalidate();
  return useMutation({
    mutationFn: (id: string) => recurringInvoicesApi.resume(id),
    onSuccess: invalidate,
  });
};

export const useCancelRecurringInvoice = () => {
  const invalidate = useInvalidate();
  return useMutation({
    mutationFn: (id: string) => recurringInvoicesApi.cancel(id),
    onSuccess: invalidate,
  });
};

export const useRunRecurringInvoiceNow = () => {
  const invalidate = useInvalidate();
  return useMutation({
    mutationFn: (id: string) => recurringInvoicesApi.runNow(id),
    onSuccess: invalidate,
  });
};
