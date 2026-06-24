import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  CreateEmployeeInput,
  DeductionInput,
  Employee,
  EmployeeListItem,
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

const BASE = '/employees';
const INVALIDATION = [/\/employees/i, /\/payroll-runs/i] as const;

const mutate = <T>(p: Promise<{ data: ApiResponse<T> }>) =>
  p.then((r) => {
    invalidateHttpCache(INVALIDATION);
    return r.data;
  });

export const employeesApi = {
  search: (params: EmployeeListParams) =>
    cachedGet<ApiResponse<PagedResult<EmployeeListItem>>>(apiClient, BASE, { params }),

  getById: (id: string) => cachedGet<ApiResponse<Employee>>(apiClient, `${BASE}/${id}`),

  create: (input: CreateEmployeeInput) =>
    mutate(apiClient.post<ApiResponse<Employee>>(BASE, input)),

  update: (input: UpdateEmployeeInput) =>
    mutate(apiClient.put<ApiResponse<Employee>>(`${BASE}/${input.id}`, input)),

  updateBaseSalary: (input: UpdateBaseSalaryInput) =>
    mutate(apiClient.put<ApiResponse<Employee>>(`${BASE}/${input.id}/base-salary`, input)),

  terminate: (input: TerminateEmployeeInput) =>
    mutate(apiClient.post<ApiResponse<Employee>>(`${BASE}/${input.id}/terminate`, input)),

  leave: (input: LeaveInput) =>
    mutate(apiClient.post<ApiResponse<Employee>>(`${BASE}/${input.id}/leave`, input)),

  returnFromLeave: (input: ReturnFromLeaveInput) =>
    mutate(apiClient.post<ApiResponse<Employee>>(`${BASE}/${input.id}/return-from-leave`, input)),

  addComponent: (input: SalaryComponentInput) =>
    mutate(apiClient.post<ApiResponse<Employee>>(`${BASE}/${input.id}/components`, input)),

  updateComponent: (input: UpdateSalaryComponentInput) =>
    mutate(
      apiClient.put<ApiResponse<Employee>>(
        `${BASE}/${input.id}/components/${input.componentId}`,
        input,
      ),
    ),

  removeComponent: (id: string, componentId: string) =>
    mutate(apiClient.delete<ApiResponse<Employee>>(`${BASE}/${id}/components/${componentId}`)),

  addDeduction: (input: DeductionInput) =>
    mutate(apiClient.post<ApiResponse<Employee>>(`${BASE}/${input.id}/deductions`, input)),

  updateDeduction: (input: UpdateDeductionInput) =>
    mutate(
      apiClient.put<ApiResponse<Employee>>(
        `${BASE}/${input.id}/deductions/${input.deductionId}`,
        input,
      ),
    ),

  removeDeduction: (id: string, deductionId: string) =>
    mutate(apiClient.delete<ApiResponse<Employee>>(`${BASE}/${id}/deductions/${deductionId}`)),
};
