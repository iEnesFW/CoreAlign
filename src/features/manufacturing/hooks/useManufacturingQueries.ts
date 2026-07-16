import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { operatorsApi, routingsApi, workCentersApi } from '../api/manufacturingApi';
import type {
  AssignRoutingToProductInput,
  CreateRoutingInput,
  CreateWorkCenterInput,
  CreateWorkCenterOperatorInput,
  RoutingStatus,
  SetRoutingStepsInput,
  UpdateRoutingInput,
  UpdateWorkCenterInput,
  UpdateWorkCenterOperatorInput,
} from '../model/manufacturing.types';

export const manufacturingKeys = {
  routings: (status?: RoutingStatus) => ['production-routings', status ?? 'all'] as const,
  routing: (id: string | null) => ['production-routings', 'detail', id] as const,
  workCenters: (includeInactive: boolean) => ['work-centers', includeInactive] as const,
  operators: (workCenterId?: string, employeeId?: string) =>
    ['work-center-operators', workCenterId ?? 'all', employeeId ?? 'all'] as const,
  jobs: (status?: string, productId?: string) =>
    ['production-jobs', status ?? 'all', productId ?? 'all'] as const,
  job: (id: string | null) => ['production-jobs', 'detail', id] as const,
};

export const useRoutingsQuery = (status?: RoutingStatus) =>
  useQuery({
    queryKey: manufacturingKeys.routings(status),
    queryFn: async () => (await routingsApi.list(status)).data ?? [],
    staleTime: 30_000,
  });

export const useRoutingQuery = (id: string | null) =>
  useQuery({
    queryKey: manufacturingKeys.routing(id),
    queryFn: async () => (id ? ((await routingsApi.getById(id)).data ?? null) : null),
    enabled: !!id,
  });

export const useWorkCentersQuery = (includeInactive = false) =>
  useQuery({
    queryKey: manufacturingKeys.workCenters(includeInactive),
    queryFn: async () => (await workCentersApi.list(includeInactive)).data ?? [],
    staleTime: 60_000,
  });

export const useOperatorsQuery = (workCenterId?: string, employeeId?: string) =>
  useQuery({
    queryKey: manufacturingKeys.operators(workCenterId, employeeId),
    queryFn: async () => (await operatorsApi.list(workCenterId, employeeId)).data ?? [],
    staleTime: 30_000,
  });

const useInvalidateRoutings = () => {
  const qc = useQueryClient();
  return () => qc.invalidateQueries({ queryKey: ['production-routings'] });
};

export const useCreateRouting = () => {
  const invalidate = useInvalidateRoutings();
  return useMutation({
    mutationFn: (input: CreateRoutingInput) => routingsApi.create(input),
    onSuccess: invalidate,
  });
};

export const useUpdateRouting = () => {
  const invalidate = useInvalidateRoutings();
  return useMutation({
    mutationFn: (input: UpdateRoutingInput) => routingsApi.update(input),
    onSuccess: invalidate,
  });
};

export const useSetRoutingSteps = () => {
  const invalidate = useInvalidateRoutings();
  return useMutation({
    mutationFn: (input: SetRoutingStepsInput) => routingsApi.setSteps(input),
    onSuccess: invalidate,
  });
};

export const useRoutingTransition = () => {
  const invalidate = useInvalidateRoutings();
  return useMutation({
    mutationFn: ({ id, action }: { id: string; action: 'activate' | 'archive' | 'restore' }) =>
      routingsApi.transition(id, action),
    onSuccess: invalidate,
  });
};

export const useDeleteRouting = () => {
  const invalidate = useInvalidateRoutings();
  return useMutation({
    mutationFn: (id: string) => routingsApi.remove(id),
    onSuccess: invalidate,
  });
};

export const useAssignRoutingToProduct = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: AssignRoutingToProductInput) => routingsApi.assignToProduct(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['products'] }),
  });
};

// --- Production Jobs ---

export const useJobsQuery = (status?: any, productId?: string) =>
  useQuery({
    queryKey: manufacturingKeys.jobs(status, productId),
    queryFn: async () => {
      const { productionJobsApi } = await import('../api/manufacturingApi');
      return (await productionJobsApi.list(status, productId)).data ?? [];
    },
    staleTime: 10_000,
  });

export const useJobQuery = (id: string | null) =>
  useQuery({
    queryKey: manufacturingKeys.job(id),
    queryFn: async () => {
      if (!id) return null;
      const { productionJobsApi } = await import('../api/manufacturingApi');
      return (await productionJobsApi.getById(id)).data ?? null;
    },
    enabled: !!id,
  });

const useInvalidateJobs = () => {
  const qc = useQueryClient();
  return () => qc.invalidateQueries({ queryKey: ['production-jobs'] });
};

export const useCreateJob = () => {
  const invalidate = useInvalidateJobs();
  return useMutation({
    mutationFn: async (input: any) => {
      const { productionJobsApi } = await import('../api/manufacturingApi');
      return productionJobsApi.create(input);
    },
    onSuccess: invalidate,
  });
};

export const useReleaseJob = () => {
  const invalidate = useInvalidateJobs();
  return useMutation({
    mutationFn: async ({ id, input }: { id: string; input: any }) => {
      const { productionJobsApi } = await import('../api/manufacturingApi');
      return productionJobsApi.release(id, input);
    },
    onSuccess: invalidate,
  });
};

export const useStartJobStep = () => {
  const invalidate = useInvalidateJobs();
  return useMutation({
    mutationFn: async ({
      id,
      stepNumber,
      input,
    }: {
      id: string;
      stepNumber: number;
      input: any;
    }) => {
      const { productionJobsApi } = await import('../api/manufacturingApi');
      return productionJobsApi.startStep(id, stepNumber, input);
    },
    onSuccess: invalidate,
  });
};

export const useFinishJobStep = () => {
  const invalidate = useInvalidateJobs();
  return useMutation({
    mutationFn: async ({
      id,
      stepNumber,
      input,
    }: {
      id: string;
      stepNumber: number;
      input: any;
    }) => {
      const { productionJobsApi } = await import('../api/manufacturingApi');
      return productionJobsApi.finishStep(id, stepNumber, input);
    },
    onSuccess: invalidate,
  });
};

export const useSkipJobStep = () => {
  const invalidate = useInvalidateJobs();
  return useMutation({
    mutationFn: async ({ id, stepNumber }: { id: string; stepNumber: number }) => {
      const { productionJobsApi } = await import('../api/manufacturingApi');
      return productionJobsApi.skipStep(id, stepNumber);
    },
    onSuccess: invalidate,
  });
};

export const useReworkJobStep = () => {
  const invalidate = useInvalidateJobs();
  return useMutation({
    mutationFn: async ({ id, input }: { id: string; input: any }) => {
      const { productionJobsApi } = await import('../api/manufacturingApi');
      return productionJobsApi.rework(id, input);
    },
    onSuccess: invalidate,
  });
};

export const useTransitionJob = () => {
  const invalidate = useInvalidateJobs();
  return useMutation({
    mutationFn: async ({ id, action }: { id: string; action: 'hold' | 'resume' }) => {
      const { productionJobsApi } = await import('../api/manufacturingApi');
      return action === 'hold' ? productionJobsApi.putOnHold(id) : productionJobsApi.resume(id);
    },
    onSuccess: invalidate,
  });
};

export const useCancelJob = () => {
  const invalidate = useInvalidateJobs();
  return useMutation({
    mutationFn: async ({ id, input }: { id: string; input: any }) => {
      const { productionJobsApi } = await import('../api/manufacturingApi');
      return productionJobsApi.cancel(id, input);
    },
    onSuccess: invalidate,
  });
};

export const useCompleteJob = () => {
  const invalidate = useInvalidateJobs();
  return useMutation({
    mutationFn: async ({ id, input }: { id: string; input: any }) => {
      const { productionJobsApi } = await import('../api/manufacturingApi');
      return productionJobsApi.complete(id, input);
    },
    onSuccess: invalidate,
  });
};

export const useCreateWorkCenter = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateWorkCenterInput) => workCentersApi.create(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['work-centers'] }),
  });
};

export const useUpdateWorkCenter = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateWorkCenterInput) => workCentersApi.update(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['work-centers'] }),
  });
};

const useInvalidateOperators = () => {
  const qc = useQueryClient();
  return () => qc.invalidateQueries({ queryKey: ['work-center-operators'] });
};

export const useCreateOperator = () => {
  const invalidate = useInvalidateOperators();
  return useMutation({
    mutationFn: (input: CreateWorkCenterOperatorInput) => operatorsApi.create(input),
    onSuccess: invalidate,
  });
};

export const useUpdateOperator = () => {
  const invalidate = useInvalidateOperators();
  return useMutation({
    mutationFn: (input: UpdateWorkCenterOperatorInput) => operatorsApi.update(input),
    onSuccess: invalidate,
  });
};

export const useSetOperatorActive = () => {
  const invalidate = useInvalidateOperators();
  return useMutation({
    mutationFn: ({ id, active }: { id: string; active: boolean }) =>
      operatorsApi.setActive(id, active),
    onSuccess: invalidate,
  });
};
