import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { serviceTicketApi, type ServiceTicketListParams } from '../api/warrantyApi';
import type {
  AssignServiceTicketInput,
  CreateServiceTicketInput,
  ResolveServiceTicketInput,
} from '../model/warranty.types';

export const useServiceTicketsQuery = (params: ServiceTicketListParams) =>
  useQuery({
    queryKey: ['service-tickets', 'list', params] as const,
    queryFn: () => serviceTicketApi.list(params),
    staleTime: 30 * 1000,
  });

export const useMyServiceTicketsQuery = (customerId: string | undefined) =>
  useQuery({
    queryKey: ['service-tickets', 'mine', customerId] as const,
    queryFn: () => serviceTicketApi.listMine(customerId!),
    enabled: Boolean(customerId),
    staleTime: 30 * 1000,
  });

export const useCreateServiceTicket = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateServiceTicketInput) => serviceTicketApi.create(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['service-tickets'] }),
  });
};

export const useAssignServiceTicket = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: AssignServiceTicketInput) => serviceTicketApi.assign(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['service-tickets'] }),
  });
};

export const useResolveServiceTicket = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: ResolveServiceTicketInput) => serviceTicketApi.resolve(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['service-tickets'] }),
  });
};
