import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { payrollParametersApi } from '../api/payrollParametersApi';
import type {
  CreatePayrollParametersInput,
  UpdatePayrollParametersInput,
} from '../model/parameters.types';

export const usePayrollParametersQuery = () =>
  useQuery({
    queryKey: ['payroll-parameters', 'list'] as const,
    queryFn: () => payrollParametersApi.list(),
    staleTime: 5 * 60 * 1000,
  });

export const usePayrollParametersDetailQuery = (id: string | null) =>
  useQuery({
    queryKey: ['payroll-parameters', 'detail', id] as const,
    queryFn: () => payrollParametersApi.getById(id as string),
    enabled: id !== null,
  });

const invalidate = (qc: ReturnType<typeof useQueryClient>) =>
  qc.invalidateQueries({ queryKey: ['payroll-parameters'] });

export const useCreatePayrollParameters = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreatePayrollParametersInput) => payrollParametersApi.create(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useUpdatePayrollParameters = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdatePayrollParametersInput) => payrollParametersApi.update(input),
    onSuccess: () => invalidate(qc),
  });
};
