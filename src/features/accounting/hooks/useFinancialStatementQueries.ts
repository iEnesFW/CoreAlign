import { useQuery } from '@tanstack/react-query';
import { financialStatementApi } from '../api/financialStatementApi';

const balanceSheetKey = (asOf: string) => ['accounting', 'balance-sheet', asOf] as const;
const incomeStatementKey = (params: { fromDate: string; toDate: string }) =>
  ['accounting', 'income-statement', params] as const;
const reconciliationKey = (asOf: string) => ['accounting', 'reconciliation', asOf] as const;

export const useBalanceSheetQuery = (asOf: string) =>
  useQuery({
    queryKey: balanceSheetKey(asOf),
    queryFn: () => financialStatementApi.balanceSheet(asOf),
    staleTime: 60 * 1000,
    enabled: !!asOf,
  });

export const useIncomeStatementQuery = (params: { fromDate: string; toDate: string }) =>
  useQuery({
    queryKey: incomeStatementKey(params),
    queryFn: () => financialStatementApi.incomeStatement(params),
    staleTime: 60 * 1000,
    enabled: !!params.fromDate && !!params.toDate,
  });

export const useReconciliationQuery = (asOf: string) =>
  useQuery({
    queryKey: reconciliationKey(asOf),
    queryFn: () => financialStatementApi.reconciliation(asOf),
    staleTime: 60 * 1000,
    enabled: !!asOf,
  });
