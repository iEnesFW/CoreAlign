import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type {
  AssignServiceTicketInput,
  CancelWarrantyContractInput,
  CreateServiceTicketInput,
  CreateWarrantyContractInput,
  ExtendWarrantyContractInput,
  MaintenanceSchedule,
  ResolveServiceTicketInput,
  ServiceTicket,
  ServiceTicketPriority,
  ServiceTicketStatus,
  ServiceTicketType,
  WarrantyContract,
  WarrantyContractStatus,
  WarrantyExpiryAlert,
} from '../model/warranty.types';

const WARRANTY_BASE = '/warranty-contracts';
const SERVICE_TICKET_BASE = '/service-tickets';
const MAINTENANCE_BASE = '/maintenance-schedules';
const INVALIDATION = [
  /\/warranty-contracts/i,
  /\/service-tickets/i,
  /\/maintenance-schedules/i,
] as const;

export interface WarrantyListParams {
  status?: WarrantyContractStatus;
  customerId?: string;
  orderId?: string;
}

export interface ServiceTicketListParams {
  status?: ServiceTicketStatus;
  type?: ServiceTicketType;
  priority?: ServiceTicketPriority;
  customerId?: string;
}

export const warrantyApi = {
  list: (params: WarrantyListParams) =>
    cachedGet<ApiResponse<WarrantyContract[]>>(apiClient, WARRANTY_BASE, { params }),

  getById: (id: string) =>
    cachedGet<ApiResponse<WarrantyContract>>(apiClient, `${WARRANTY_BASE}/${id}`),

  listExpiring: (withinDays = 30) =>
    cachedGet<ApiResponse<WarrantyExpiryAlert[]>>(apiClient, `${WARRANTY_BASE}/expiring`, {
      params: { withinDays },
    }),

  create: (input: CreateWarrantyContractInput) =>
    apiClient.post<ApiResponse<WarrantyContract>>(WARRANTY_BASE, input).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  extend: (input: ExtendWarrantyContractInput) =>
    apiClient
      .post<ApiResponse<WarrantyContract>>(`${WARRANTY_BASE}/${input.id}/extend`, input)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  cancel: (input: CancelWarrantyContractInput) =>
    apiClient
      .post<ApiResponse<WarrantyContract>>(`${WARRANTY_BASE}/${input.id}/cancel`, input)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),
};

export const serviceTicketApi = {
  list: (params: ServiceTicketListParams) =>
    cachedGet<ApiResponse<ServiceTicket[]>>(apiClient, SERVICE_TICKET_BASE, { params }),

  create: (input: CreateServiceTicketInput) =>
    apiClient.post<ApiResponse<ServiceTicket>>(SERVICE_TICKET_BASE, input).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  assign: (input: AssignServiceTicketInput) =>
    apiClient
      .post<ApiResponse<ServiceTicket>>(`${SERVICE_TICKET_BASE}/${input.id}/assign`, input)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  resolve: (input: ResolveServiceTicketInput) =>
    apiClient
      .post<ApiResponse<ServiceTicket>>(`${SERVICE_TICKET_BASE}/${input.id}/resolve`, input)
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  listMine: (customerId: string) =>
    cachedGet<ApiResponse<ServiceTicket[]>>(apiClient, `/customers/me/service-tickets`, {
      params: { customerId },
    }),
};

export const maintenanceScheduleApi = {
  listDue: (asOf?: string) =>
    cachedGet<ApiResponse<MaintenanceSchedule[]>>(apiClient, `${MAINTENANCE_BASE}/due`, {
      params: asOf ? { asOf } : undefined,
    }),
};
