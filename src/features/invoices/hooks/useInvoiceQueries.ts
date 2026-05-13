import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { invoicesApi } from '../api/invoicesApi';
import { invoiceKeys } from './invoiceKeys';
import { orderKeys } from '@/features/orders/hooks/orderKeys';
import type { GenerateInvoiceRequest, InvoiceListParams } from '../model/invoice.types';

export const useInvoicesQuery = (params: InvoiceListParams, options?: { enabled?: boolean }) =>
  useQuery({
    queryKey: invoiceKeys.list(params),
    queryFn: () => invoicesApi.list(params),
    placeholderData: (previous) => previous,
    enabled: options?.enabled ?? true,
  });

export const useInvoiceQuery = (id: string | null) =>
  useQuery({
    queryKey: invoiceKeys.detail(id),
    queryFn: () => invoicesApi.getById(id as string),
    enabled: id !== null,
  });

export const useInvoicesByOrderQuery = (orderId: string | null) =>
  useQuery({
    queryKey: invoiceKeys.byOrder(orderId),
    queryFn: () => invoicesApi.getByOrder(orderId as string),
    enabled: orderId !== null,
  });

interface GenerateArgs {
  orderId: string;
  request?: GenerateInvoiceRequest;
}

export const useGenerateInvoiceFromOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ orderId, request }: GenerateArgs) =>
      invoicesApi.generateFromOrder(orderId, request),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: invoiceKeys.lists() });
      queryClient.invalidateQueries({ queryKey: invoiceKeys.byOrder(vars.orderId) });
      queryClient.invalidateQueries({ queryKey: orderKeys.detail(vars.orderId) });
    },
  });
};

export const useMarkInvoicePaid = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => invoicesApi.markPaid(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: invoiceKeys.lists() });
      queryClient.invalidateQueries({ queryKey: invoiceKeys.detail(id) });
    },
  });
};

export const useCancelInvoice = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => invoicesApi.cancel(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: invoiceKeys.lists() });
      queryClient.invalidateQueries({ queryKey: invoiceKeys.detail(id) });
    },
  });
};
