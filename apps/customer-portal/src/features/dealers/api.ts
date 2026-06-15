import { apiClient } from '@/shared/api/apiClient';
import type { DealerAccount, DealerUser } from '@/features/portal/types';

export interface CreateDealerAccountRequest {
  code: string;
  name: string;
  primaryCustomerId?: string;
  legalName?: string;
  taxNumber?: string;
  email?: string;
  phone?: string;
  address?: string;
  notes?: string;
}

export interface InviteDealerUserRequest {
  dealerAccountId: string;
  email: string;
  firstName?: string;
  lastName?: string;
  role?: 'DealerOwner' | 'DealerStaff';
}

export interface UpdateDealerAccountRequest {
  name: string;
  legalName?: string | null;
  taxNumber?: string | null;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
  notes?: string | null;
}

export const dealerApi = {
  createDealer: async (payload: CreateDealerAccountRequest): Promise<DealerAccount> => {
    const { data } = await apiClient.post<DealerAccount>('/dealer-accounts', payload);
    return data;
  },
  updateDealer: async (id: string, payload: UpdateDealerAccountRequest): Promise<DealerAccount> => {
    const { data } = await apiClient.put<DealerAccount>(`/dealer-accounts/${id}`, payload);
    return data;
  },
  inviteDealerUser: async (payload: InviteDealerUserRequest): Promise<DealerUser> => {
    const { data } = await apiClient.post<DealerUser>('/dealer-users', payload);
    return data;
  },
  listDealerUsers: async (dealerAccountId: string): Promise<DealerUser[]> => {
    const { data } = await apiClient.get<DealerUser[]>('/dealer-users', {
      params: { dealerAccountId },
    });
    return data;
  },
  updateDealerUserStatus: async (
    id: string,
    status: 'Active' | 'Suspended' | 'Archived',
    reason?: string,
  ): Promise<DealerUser> => {
    const { data } = await apiClient.put<DealerUser>(`/dealer-users/${id}/status`, {
      status,
      reason,
    });
    return data;
  },
  unlinkDealer: async (linkId: string, reason?: string): Promise<void> => {
    await apiClient.delete(`/dealer-customer-links/${linkId}`, {
      params: reason ? { reason } : undefined,
    });
  },
  listLinks: async (dealerAccountId?: string, customerId?: string) => {
    const { data } = await apiClient.get('/dealer-customer-links', {
      params: { dealerAccountId, customerId },
    });
    return data as Array<{
      id: string;
      dealerAccountId: string;
      dealerAccountName: string;
      customerId: string;
      customerName: string;
      status: 'Active' | 'Suspended' | 'Archived';
    }>;
  },
  getDealerVisibility: async (linkId: string): Promise<DealerProductVisibility> => {
    const { data } = await apiClient.get<DealerProductVisibility>(
      `/customer-portal/dealer-links/${linkId}/product-visibility`,
    );
    return data;
  },
  setDealerVisibility: async (
    linkId: string,
    payload: SetDealerProductVisibilityRequest,
  ): Promise<DealerProductVisibility> => {
    const { data } = await apiClient.put<DealerProductVisibility>(
      `/customer-portal/dealer-links/${linkId}/product-visibility`,
      payload,
    );
    return data;
  },
  listCatalogProducts: async (params: {
    search?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PagedResult<CatalogProductSummary>> => {
    const { data } = await apiClient.get<PagedResult<CatalogProductSummary>>(
      '/customer-portal/catalog/products',
      { params },
    );
    return data;
  },
};

export type DealerProductVisibilityMode = 'All' | 'Whitelist';

export interface DealerProductVisibility {
  linkId: string;
  mode: DealerProductVisibilityMode;
  visibleProductIds: string[];
}

export interface SetDealerProductVisibilityRequest {
  mode: DealerProductVisibilityMode;
  productIds: string[];
}

export interface CatalogProductSummary {
  id: string;
  sku: string;
  name: string;
  price?: number;
  currency?: string;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}
