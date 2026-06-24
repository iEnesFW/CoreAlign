import { useMutation, useQueryClient } from '@tanstack/react-query';
import { fiscalYearCloseApi } from '../api/fiscalYearCloseApi';

const invalidate = (qc: ReturnType<typeof useQueryClient>) => {
  qc.invalidateQueries({ queryKey: ['accounting', 'journal-entries'] });
  qc.invalidateQueries({ queryKey: ['accounting', 'trial-balance'] });
};

export const useCloseFiscalYear = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (year: number) => fiscalYearCloseApi.close(year),
    onSuccess: () => invalidate(qc),
  });
};

export const useOpenNextFiscalYear = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (year: number) => fiscalYearCloseApi.openNext(year),
    onSuccess: () => invalidate(qc),
  });
};

export const useReverseFiscalYearClose = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (year: number) => fiscalYearCloseApi.reverseClose(year),
    onSuccess: () => invalidate(qc),
  });
};
