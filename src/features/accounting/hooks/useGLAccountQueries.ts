import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { glAccountApi } from '../api/glAccountApi';
import type {
  CreateGLAccountRequest,
  GLAccountListParams,
  UpdateGLAccountRequest,
} from '../model/glAccount.types';

const TREE_KEY = ['accounting', 'gl-accounts', 'tree'] as const;
const listKey = (params: GLAccountListParams) =>
  ['accounting', 'gl-accounts', 'list', params] as const;
const detailKey = (id: string) => ['accounting', 'gl-accounts', 'detail', id] as const;

export const useGLAccountTree = () =>
  useQuery({
    queryKey: TREE_KEY,
    queryFn: () => glAccountApi.tree(),
    staleTime: 5 * 60 * 1000,
  });

export const useGLAccountList = (params: GLAccountListParams) =>
  useQuery({
    queryKey: listKey(params),
    queryFn: () => glAccountApi.list(params),
    staleTime: 5 * 60 * 1000,
  });

export const useGLAccount = (id: string | undefined) =>
  useQuery({
    queryKey: detailKey(id ?? ''),
    queryFn: () => glAccountApi.getById(id as string),
    enabled: !!id,
    staleTime: 5 * 60 * 1000,
  });

const invalidate = (qc: ReturnType<typeof useQueryClient>) => {
  qc.invalidateQueries({ queryKey: ['accounting', 'gl-accounts'] });
};

export const useCreateGLAccount = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateGLAccountRequest) => glAccountApi.create(request),
    onSuccess: () => invalidate(qc),
  });
};

export const useUpdateGLAccount = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (request: UpdateGLAccountRequest) => glAccountApi.update(request),
    onSuccess: () => invalidate(qc),
  });
};

export const useSetGLAccountActive = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      glAccountApi.setActive(id, isActive),
    onSuccess: () => invalidate(qc),
  });
};

export const useDeleteGLAccount = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => glAccountApi.remove(id),
    onSuccess: () => invalidate(qc),
  });
};

export const useSeedTurkishChart = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => glAccountApi.seedTurkish(),
    onSuccess: () => invalidate(qc),
  });
};
