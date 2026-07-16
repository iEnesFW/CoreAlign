import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type {
  AssignUserWarehousesInput,
  ConsumeGlassPlateInput,
  ConsumeGlassPlateResult,
  CreateStorageLocationInput,
  GlassPlate,
  GlassPlateConsumption,
  GlassPlateListParams,
  GlassScrapResult,
  LowStockPlate,
  MoveGlassPlateInput,
  ReceiveGlassPlatesInput,
  ReceiveGlassPlatesResult,
  ScrapGlassPlateInput,
  SetGlassPlateTrackingInput,
  StorageLocation,
  UpdateStorageLocationInput,
  UsablePlatesParams,
} from '../model/glassPlate.types';

const PLATES = '/glass-plates';
const LOCATIONS = '/storage-locations';
const ACCESS = '/user-warehouse-access';

const PLATE_INVALIDATION = [/\/glass-plates/i] as const;
const LOCATION_INVALIDATION = [/\/storage-locations/i] as const;
const PRODUCT_PLATE_INVALIDATION = [/\/glass-plates/i, /\/products/i] as const;

export const glassPlatesApi = {
  list: (params: GlassPlateListParams) =>
    cachedGet<ApiResponse<GlassPlate[]>>(apiClient, PLATES, { params }),

  usable: (params: UsablePlatesParams) =>
    cachedGet<ApiResponse<GlassPlate[]>>(apiClient, `${PLATES}/usable`, { params }),

  lowStock: () => cachedGet<ApiResponse<LowStockPlate[]>>(apiClient, `${PLATES}/low-stock`),

  whereUsed: (id: string) =>
    cachedGet<ApiResponse<GlassPlateConsumption[]>>(apiClient, `${PLATES}/${id}/where-used`),

  receive: (input: ReceiveGlassPlatesInput) =>
    apiClient.post<ApiResponse<ReceiveGlassPlatesResult>>(`${PLATES}/receive`, input).then((r) => {
      invalidateHttpCache(PLATE_INVALIDATION);
      return r.data;
    }),

  consume: (input: ConsumeGlassPlateInput) =>
    apiClient.post<ApiResponse<ConsumeGlassPlateResult>>(`${PLATES}/consume`, input).then((r) => {
      invalidateHttpCache(PLATE_INVALIDATION);
      return r.data;
    }),

  scrap: (input: ScrapGlassPlateInput) =>
    apiClient.post<ApiResponse<GlassScrapResult>>(`${PLATES}/scrap`, input).then((r) => {
      invalidateHttpCache(PLATE_INVALIDATION);
      return r.data;
    }),

  move: (id: string, input: MoveGlassPlateInput) =>
    apiClient.post<ApiResponse<GlassPlate>>(`${PLATES}/${id}/move`, input).then((r) => {
      invalidateHttpCache(PLATE_INVALIDATION);
      return r.data;
    }),

  setTracking: (input: SetGlassPlateTrackingInput) =>
    apiClient.post<ApiResponse<string>>(`${PLATES}/definitions`, input).then((r) => {
      invalidateHttpCache(PRODUCT_PLATE_INVALIDATION);
      return r.data;
    }),
};

export const storageLocationsApi = {
  list: (warehouseId?: string) =>
    cachedGet<ApiResponse<StorageLocation[]>>(apiClient, LOCATIONS, { params: { warehouseId } }),

  create: (input: CreateStorageLocationInput) =>
    apiClient.post<ApiResponse<StorageLocation>>(LOCATIONS, input).then((r) => {
      invalidateHttpCache(LOCATION_INVALIDATION);
      return r.data;
    }),

  update: (input: UpdateStorageLocationInput) =>
    apiClient.put<ApiResponse<StorageLocation>>(`${LOCATIONS}/${input.id}`, input).then((r) => {
      invalidateHttpCache(LOCATION_INVALIDATION);
      return r.data;
    }),
};

export const userWarehouseAccessApi = {
  get: (userId: string) => cachedGet<ApiResponse<string[]>>(apiClient, `${ACCESS}/${userId}`),

  assign: (input: AssignUserWarehousesInput) =>
    apiClient.post<ApiResponse<string[]>>(ACCESS, input).then((r) => r.data),
};
