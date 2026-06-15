import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { orderTemplatesApi } from '../api/orderTemplatesApi';
import type {
  CreateOrderTemplateInput,
  OrderTemplateListParams,
  UpdateOrderTemplateInput,
} from '../model/orderTemplate.types';

const KEYS = {
  list: (params: OrderTemplateListParams) => ['order-templates', 'list', params] as const,
  one: (id: string) => ['order-templates', 'one', id] as const,
};

export const useOrderTemplatesQuery = (params: OrderTemplateListParams) =>
  useQuery({
    queryKey: KEYS.list(params),
    queryFn: () => orderTemplatesApi.list(params),
    placeholderData: (previous) => previous,
    staleTime: 15_000,
  });

export const useOrderTemplateQuery = (id: string | undefined) =>
  useQuery({
    queryKey: id ? KEYS.one(id) : ['order-templates', 'one', 'empty'],
    queryFn: () => orderTemplatesApi.getById(id!),
    enabled: Boolean(id),
  });

export const useCreateOrderTemplateMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateOrderTemplateInput) => orderTemplatesApi.create(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['order-templates'] }),
  });
};

export const useUpdateOrderTemplateMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateOrderTemplateInput) => orderTemplatesApi.update(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['order-templates'] }),
  });
};

export const useDeleteOrderTemplateMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => orderTemplatesApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['order-templates'] }),
  });
};

export const useRunOrderTemplateNowMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => orderTemplatesApi.runNow(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['order-templates'] }),
  });
};
