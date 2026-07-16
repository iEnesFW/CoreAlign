import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { glassPlatesApi, storageLocationsApi, userWarehouseAccessApi } from '../api/glassPlatesApi';
import type {
  AssignUserWarehousesInput,
  ConsumeGlassPlateInput,
  CreateStorageLocationInput,
  GlassPlateListParams,
  MoveGlassPlateInput,
  ReceiveGlassPlatesInput,
  ScrapGlassPlateInput,
  SetGlassPlateTrackingInput,
  UpdateStorageLocationInput,
  UsablePlatesParams,
} from '../model/glassPlate.types';

export const glassPlateKeys = {
  all: ['glass-plates'] as const,
  list: (params: GlassPlateListParams) => [...glassPlateKeys.all, 'list', params] as const,
  usable: (params: UsablePlatesParams) => [...glassPlateKeys.all, 'usable', params] as const,
  lowStock: () => [...glassPlateKeys.all, 'low-stock'] as const,
  whereUsed: (id: string | null) => [...glassPlateKeys.all, 'where-used', id] as const,
  locations: (warehouseId?: string) => ['storage-locations', warehouseId ?? 'all'] as const,
  access: (userId: string | null) => ['user-warehouse-access', userId] as const,
};

export const useGlassPlatesQuery = (params: GlassPlateListParams) =>
  useQuery({
    queryKey: glassPlateKeys.list(params),
    queryFn: async () => (await glassPlatesApi.list(params)).data ?? [],
    staleTime: 30_000,
  });

export const useUsablePlatesQuery = (params: UsablePlatesParams | null) =>
  useQuery({
    queryKey: glassPlateKeys.usable(params ?? ({} as UsablePlatesParams)),
    queryFn: async () => (params ? ((await glassPlatesApi.usable(params)).data ?? []) : []),
    enabled: !!params && !!params.productId && params.widthMm > 0 && params.heightMm > 0,
  });

export const useLowStockPlatesQuery = () =>
  useQuery({
    queryKey: glassPlateKeys.lowStock(),
    queryFn: async () => (await glassPlatesApi.lowStock()).data ?? [],
    staleTime: 60_000,
  });

export const useGlassPlateWhereUsedQuery = (id: string | null) =>
  useQuery({
    queryKey: glassPlateKeys.whereUsed(id),
    queryFn: async () => (id ? ((await glassPlatesApi.whereUsed(id)).data ?? []) : []),
    enabled: !!id,
  });

export const useStorageLocationsQuery = (warehouseId?: string) =>
  useQuery({
    queryKey: glassPlateKeys.locations(warehouseId),
    queryFn: async () => (await storageLocationsApi.list(warehouseId)).data ?? [],
    staleTime: 60_000,
  });

export const useUserWarehouseAccessQuery = (userId: string | null) =>
  useQuery({
    queryKey: glassPlateKeys.access(userId),
    queryFn: async () => (userId ? ((await userWarehouseAccessApi.get(userId)).data ?? []) : []),
    enabled: !!userId,
  });

const useInvalidatePlates = () => {
  const qc = useQueryClient();
  return () => qc.invalidateQueries({ queryKey: glassPlateKeys.all });
};

export const useReceiveGlassPlates = () => {
  const invalidate = useInvalidatePlates();
  return useMutation({
    mutationFn: (input: ReceiveGlassPlatesInput) => glassPlatesApi.receive(input),
    onSuccess: invalidate,
  });
};

export const useConsumeGlassPlate = () => {
  const invalidate = useInvalidatePlates();
  return useMutation({
    mutationFn: (input: ConsumeGlassPlateInput) => glassPlatesApi.consume(input),
    onSuccess: invalidate,
  });
};

export const useScrapGlassPlate = () => {
  const invalidate = useInvalidatePlates();
  return useMutation({
    mutationFn: (input: ScrapGlassPlateInput) => glassPlatesApi.scrap(input),
    onSuccess: invalidate,
  });
};

export const useMoveGlassPlate = () => {
  const invalidate = useInvalidatePlates();
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: MoveGlassPlateInput }) =>
      glassPlatesApi.move(id, input),
    onSuccess: invalidate,
  });
};

export const useSetGlassPlateTracking = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: SetGlassPlateTrackingInput) => glassPlatesApi.setTracking(input),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: glassPlateKeys.all });
      qc.invalidateQueries({ queryKey: ['products'] });
    },
  });
};

export const useCreateStorageLocation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateStorageLocationInput) => storageLocationsApi.create(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['storage-locations'] }),
  });
};

export const useUpdateStorageLocation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateStorageLocationInput) => storageLocationsApi.update(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['storage-locations'] }),
  });
};

export const useAssignUserWarehouses = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: AssignUserWarehousesInput) => userWarehouseAccessApi.assign(input),
    onSuccess: (_result, input) =>
      qc.invalidateQueries({ queryKey: glassPlateKeys.access(input.userId) }),
  });
};
