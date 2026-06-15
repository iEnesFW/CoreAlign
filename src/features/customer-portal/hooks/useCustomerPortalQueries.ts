import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  customerPortalApi,
  type MyInvoicesListParams,
  type MyProjectsListParams,
} from '../api/customerPortalApi';
import type {
  CreateMyServiceTicketInput,
  InitiatePaymentInput,
} from '../model/customerPortal.types';

const CP_KEY = ['customer-portal'] as const;

export const useMyWarrantiesQuery = () =>
  useQuery({
    queryKey: [...CP_KEY, 'warranties'] as const,
    queryFn: () => customerPortalApi.listMyWarranties(),
    staleTime: 60 * 1000,
  });

export const useMyWarrantyQuery = (id: string | undefined) =>
  useQuery({
    queryKey: [...CP_KEY, 'warranties', 'detail', id] as const,
    queryFn: () => customerPortalApi.getMyWarranty(id!),
    enabled: Boolean(id),
    staleTime: 60 * 1000,
  });

export const useMyServiceTicketsQuery = () =>
  useQuery({
    queryKey: [...CP_KEY, 'service-tickets'] as const,
    queryFn: () => customerPortalApi.listMyServiceTickets(),
    staleTime: 60 * 1000,
  });

export const useMyServiceTicketQuery = (id: string | undefined) =>
  useQuery({
    queryKey: [...CP_KEY, 'service-tickets', 'detail', id] as const,
    queryFn: () => customerPortalApi.getMyServiceTicket(id!),
    enabled: Boolean(id),
    staleTime: 60 * 1000,
  });

export const useCreateMyServiceTicket = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateMyServiceTicketInput) =>
      customerPortalApi.createMyServiceTicket(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: [...CP_KEY, 'service-tickets'] }),
  });
};

export const useMyInvoicesQuery = (params: MyInvoicesListParams) =>
  useQuery({
    queryKey: [...CP_KEY, 'invoices', 'list', params] as const,
    queryFn: () => customerPortalApi.listMyInvoices(params),
    staleTime: 60 * 1000,
  });

export const useMyInvoiceQuery = (id: string | undefined) =>
  useQuery({
    queryKey: [...CP_KEY, 'invoices', 'detail', id] as const,
    queryFn: () => customerPortalApi.getMyInvoice(id!),
    enabled: Boolean(id),
    staleTime: 60 * 1000,
  });

export const useMyPaymentsQuery = () =>
  useQuery({
    queryKey: [...CP_KEY, 'payments'] as const,
    queryFn: () => customerPortalApi.listMyPayments(),
    staleTime: 60 * 1000,
  });

export const useMyPaymentQuery = (id: string | undefined) =>
  useQuery({
    queryKey: [...CP_KEY, 'payments', 'detail', id] as const,
    queryFn: () => customerPortalApi.getMyPayment(id!),
    enabled: Boolean(id),
    staleTime: 60 * 1000,
  });

export const useInitiatePayment = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: InitiatePaymentInput) => customerPortalApi.initiatePayment(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: [...CP_KEY, 'payments'] }),
  });
};

export const useMyProjectsQuery = (params: MyProjectsListParams) =>
  useQuery({
    queryKey: [...CP_KEY, 'projects', 'list', params] as const,
    queryFn: () => customerPortalApi.listMyProjects(params),
    staleTime: 2 * 60 * 1000,
  });

export const useMyProjectInstallationStatusQuery = (id: string | undefined) =>
  useQuery({
    queryKey: [...CP_KEY, 'projects', 'installation', id] as const,
    queryFn: () => customerPortalApi.getProjectInstallationStatus(id!),
    enabled: Boolean(id),
    staleTime: 30 * 1000,
  });

export const downloadInvoicePdf = (id: string) => customerPortalApi.downloadInvoicePdf(id);
