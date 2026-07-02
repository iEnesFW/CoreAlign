import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { paymentsApi, type PaymentsSearchParams } from '../api/paymentsApi';
import type {
  ApplyPaymentInput,
  ApplyPaymentLine,
  CreatePaymentInput,
} from '../model/payment.types';

const FOURTY_FIVE_SECONDS = 45 * 1000;

export const usePaymentsSearch = (params: PaymentsSearchParams) =>
  useQuery({
    queryKey: ['payments', 'search', params] as const,
    queryFn: () => paymentsApi.search(params),
    placeholderData: (prev) => prev,
    staleTime: FOURTY_FIVE_SECONDS,
  });

export const usePaymentQuery = (id: string | null) =>
  useQuery({
    queryKey: ['payments', 'detail', id] as const,
    queryFn: () => paymentsApi.getById(id as string),
    enabled: id !== null,
  });

export const usePaymentsByCustomer = (customerId: string | null) =>
  useQuery({
    queryKey: ['payments', 'by-customer', customerId] as const,
    queryFn: () => paymentsApi.getByCustomer(customerId as string),
    enabled: customerId !== null,
    staleTime: FOURTY_FIVE_SECONDS,
  });

export const usePaymentsByInvoice = (invoiceId: string | null) =>
  useQuery({
    queryKey: ['payments', 'by-invoice', invoiceId] as const,
    queryFn: () => paymentsApi.getByInvoice(invoiceId as string),
    enabled: invoiceId !== null,
    staleTime: FOURTY_FIVE_SECONDS,
  });

export const useCustomerLedger = (
  customerId: string | null,
  fromUtc?: string,
  toUtc?: string,
  page = 1,
  pageSize = 50,
) =>
  useQuery({
    queryKey: ['payments', 'ledger', customerId, fromUtc, toUtc, page, pageSize] as const,
    queryFn: () => paymentsApi.getLedger(customerId as string, fromUtc, toUtc, page, pageSize),
    enabled: customerId !== null,
    staleTime: FOURTY_FIVE_SECONDS,
  });

export const useCustomerAging = (customerId: string | null) =>
  useQuery({
    queryKey: ['payments', 'aging', customerId] as const,
    queryFn: () => paymentsApi.getAging(customerId as string),
    enabled: customerId !== null,
    staleTime: FOURTY_FIVE_SECONDS,
  });

export const useOpenInvoicesForCustomer = (customerId: string | null) =>
  useQuery({
    queryKey: ['payments', 'open-invoices', customerId] as const,
    queryFn: () => paymentsApi.getOpenInvoices(customerId as string),
    enabled: customerId !== null,
    staleTime: FOURTY_FIVE_SECONDS,
  });

const invalidate = (qc: ReturnType<typeof useQueryClient>) => {
  qc.invalidateQueries({ queryKey: ['payments'] });
  qc.invalidateQueries({ queryKey: ['invoices'] });
  qc.invalidateQueries({ queryKey: ['customers'] });
};

export const useCreatePayment = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreatePaymentInput) => paymentsApi.create(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useConfirmPayment = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => paymentsApi.confirm(id),
    onSuccess: () => invalidate(qc),
  });
};

export const useApplyPayment = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: ApplyPaymentInput) => paymentsApi.apply(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useOffsetCustomerAdvance = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, applications }: { id: string; applications: ApplyPaymentLine[] }) =>
      paymentsApi.offsetAdvance(id, applications),
    onSuccess: () => invalidate(qc),
  });
};

export const useApplyPaymentFifo = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => paymentsApi.applyFifo(id),
    onSuccess: () => invalidate(qc),
  });
};

export const useUnapplyPayment = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (params: { id: string; applicationId: string }) =>
      paymentsApi.unapply(params.id, params.applicationId),
    onSuccess: () => invalidate(qc),
  });
};

export const useVoidPayment = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (params: { id: string; reason?: string | null }) =>
      paymentsApi.voidPayment(params.id, params.reason),
    onSuccess: () => invalidate(qc),
  });
};
