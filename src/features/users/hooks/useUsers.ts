import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { usersApi } from '../api/usersApi';
import type {
  InviteUserInput,
  SetUserActiveInput,
  UpdateUserRolesInput,
} from '../model/user.types';

export const useUsersQuery = () =>
  useQuery({
    queryKey: ['users', 'list'] as const,
    queryFn: () => usersApi.list(),
    staleTime: 30 * 1000,
  });

export const useRolesQuery = () =>
  useQuery({
    queryKey: ['users', 'roles'] as const,
    queryFn: () => usersApi.listRoles(),
    staleTime: 5 * 60 * 1000,
  });

const invalidate = (qc: ReturnType<typeof useQueryClient>) =>
  qc.invalidateQueries({ queryKey: ['users'] });

export const useInviteUser = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: InviteUserInput) => usersApi.invite(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useUpdateUserRoles = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateUserRolesInput) => usersApi.updateRoles(input),
    onSuccess: () => invalidate(qc),
  });
};

export const useSetUserActive = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: SetUserActiveInput) => usersApi.setActive(input),
    onSuccess: () => invalidate(qc),
  });
};
