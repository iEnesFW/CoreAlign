import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';
import type {
  BalanceSheetReportDto,
  IncomeStatementReportDto,
  ReconciliationReportDto,
} from '../model/financialStatement.types';

export const financialStatementApi = {
  balanceSheet: (asOf: string) =>
    apiClient
      .get<ApiResponse<BalanceSheetReportDto>>('/accounting/balance-sheet', { params: { asOf } })
      .then((r) => r.data),

  incomeStatement: (params: { fromDate: string; toDate: string }) =>
    apiClient
      .get<ApiResponse<IncomeStatementReportDto>>('/accounting/income-statement', { params })
      .then((r) => r.data),

  reconciliation: (asOf: string) =>
    apiClient
      .get<ApiResponse<ReconciliationReportDto>>('/accounting/reconciliation', { params: { asOf } })
      .then((r) => r.data),
};
