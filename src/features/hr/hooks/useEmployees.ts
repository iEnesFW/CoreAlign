import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { employeesApi } from '../api/employeesApi';
import type {
  CreateEmployeeInput,
  DeductionInput,
  EmployeeListParams,
  LeaveInput,
  ReturnFromLeaveInput,
  SalaryComponentInput,
  TerminateEmployeeInput,
  UpdateBaseSalaryInput,
  UpdateDeductionInput,
  UpdateEmployeeInput,
  UpdateSalaryComponentInput,
} from '../model/employee.types';

export const useEmployeesQuery = (params: EmployeeListParams) =>
  useQuery({
    queryKey: ['employees', 'list', params] as const,
    queryFn: () => employeesApi.search(params),
    staleTime: 30 * 1000,
  });

export const useEmployeeQuery = (id: string | null) =>
  useQuery({
    queryKey: ['employees', 'detail', id] as const,
    queryFn: () => employeesApi.getById(id as string),
    enabled: id !== null,
  });

const invalidate = (qc: ReturnType<typeof useQueryClient>) =>
  qc.invalidateQueries({ queryKey: ['employees'] });

export const useCreateEmployee = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateEmployeeInput) => employeesApi.create(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useUpdateEmployee = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateEmployeeInput) => employeesApi.update(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useUpdateBaseSalary = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateBaseSalaryInput) => employeesApi.updateBaseSalary(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useTerminateEmployee = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: TerminateEmployeeInput) => employeesApi.terminate(input),
    onSuccess: () => invalidate(qc),
  });
};

export const usePutEmployeeOnLeave = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: LeaveInput) => employeesApi.leave(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useReturnFromLeave = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: ReturnFromLeaveInput) => employeesApi.returnFromLeave(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useAddSalaryComponent = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: SalaryComponentInput) => employeesApi.addComponent(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useUpdateSalaryComponent = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateSalaryComponentInput) => employeesApi.updateComponent(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useRemoveSalaryComponent = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, componentId }: { id: string; componentId: string }) =>
      employeesApi.removeComponent(id, componentId),
    onSuccess: () => invalidate(qc),
  });
};

export const useAddDeduction = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: DeductionInput) => employeesApi.addDeduction(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useUpdateDeduction = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateDeductionInput) => employeesApi.updateDeduction(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useRemoveDeduction = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, deductionId }: { id: string; deductionId: string }) =>
      employeesApi.removeDeduction(id, deductionId),
    onSuccess: () => invalidate(qc),
  });
};
