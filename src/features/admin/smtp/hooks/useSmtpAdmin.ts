import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { smtpAdminApi } from '../api/smtpAdminApi';
import { smtpKeys } from './smtpKeys';
import type { SafeResult } from '@/shared/lib/safeRequest';
import type { SmtpSettings, UpsertSmtpInput } from '../smtp.types';

const unwrapSafe = async <T>(promise: Promise<SafeResult<T>>): Promise<T> => {
  const [data, error] = await promise;
  if (error) {
    throw error;
  }
  return data as T;
};

export const useSmtpSettingsQuery = () =>
  useQuery({
    queryKey: smtpKeys.settings(),
    queryFn: () => unwrapSafe<SmtpSettings>(smtpAdminApi.get()),
    staleTime: 60 * 1000,
  });

export const useUpsertSmtpMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: UpsertSmtpInput) => unwrapSafe(smtpAdminApi.upsert(body)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: smtpKeys.settings() });
    },
  });
};

export const useTestSmtpMutation = () =>
  useMutation({
    mutationFn: (toAddress: string) => unwrapSafe(smtpAdminApi.test(toAddress)),
  });
