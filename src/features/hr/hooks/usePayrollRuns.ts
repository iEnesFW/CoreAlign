import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { payrollRunsApi } from '../api/payrollRunsApi';
import { payslipsApi } from '../api/payslipsApi';
import type { CreatePayrollRunInput, PayrollRunListParams } from '../model/payroll.types';

export type PayrollRunActionType = 'calculate' | 'approve' | 'reopen' | 'post' | 'pay';

export const usePayrollRunsQuery = (params: PayrollRunListParams) =>
  useQuery({
    queryKey: ['payroll-runs', 'list', params] as const,
    queryFn: () => payrollRunsApi.search(params),
    staleTime: 30 * 1000,
  });

export const usePayrollRunQuery = (id: string | null) =>
  useQuery({
    queryKey: ['payroll-runs', 'detail', id] as const,
    queryFn: () => payrollRunsApi.getById(id as string),
    enabled: id !== null,
  });

export const usePayrollRunPayslipsQuery = (id: string | null) =>
  useQuery({
    queryKey: ['payroll-runs', 'payslips', id] as const,
    queryFn: () => payrollRunsApi.getPayslips(id as string),
    enabled: id !== null,
  });

export const usePayslipQuery = (id: string | null) =>
  useQuery({
    queryKey: ['payslips', 'detail', id] as const,
    queryFn: () => payslipsApi.getById(id as string),
    enabled: id !== null,
  });

const invalidate = (qc: ReturnType<typeof useQueryClient>) => {
  qc.invalidateQueries({ queryKey: ['payroll-runs'] });
  qc.invalidateQueries({ queryKey: ['payslips'] });
};

export const useCreatePayrollRun = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreatePayrollRunInput) => payrollRunsApi.create(input),
    onSuccess: () => invalidate(qc),
  });
};

export const usePayrollRunAction = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, action }: { id: string; action: PayrollRunActionType }) => {
      if (action === 'calculate') return payrollRunsApi.calculate(id);
      if (action === 'approve') return payrollRunsApi.approve(id);
      if (action === 'reopen') return payrollRunsApi.reopen(id);
      if (action === 'post') return payrollRunsApi.post(id);
      return payrollRunsApi.pay(id);
    },
    onSuccess: () => invalidate(qc),
  });
};
