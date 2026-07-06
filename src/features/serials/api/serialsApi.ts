import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';
import type {
  RegisterSerialsInput,
  SerialWhereUsed,
  ShipSerialsInput,
} from '../model/serial.types';

const BASE = '/serial-units';

export const serialsApi = {
  whereUsed: (serialNumber: string) =>
    apiClient
      .get<ApiResponse<SerialWhereUsed[]>>(`${BASE}/where-used/${encodeURIComponent(serialNumber)}`)
      .then((r) => r.data),

  register: (input: RegisterSerialsInput) =>
    apiClient.post<ApiResponse<number>>(`${BASE}/register`, input).then((r) => r.data),

  ship: (input: ShipSerialsInput) =>
    apiClient.post<ApiResponse<number>>(`${BASE}/ship`, input).then((r) => r.data),
};
