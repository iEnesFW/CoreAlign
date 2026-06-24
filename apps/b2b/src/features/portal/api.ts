import { apiClient } from '@/shared/api/apiClient';
import type {
  CommissionEntry,
  CommissionStatus,
  CommissionSummary,
  CreditSnapshot,
  DealerAllowedCustomer,
  DealerPortalDashboard,
  DealerProfile,
  InvoiceDetail,
  InvoiceSummary,
  OrderDetail,
  OrderSummary,
  PagedResult,
  ProductSummary,
} from './types';

const BASE = '/dealer-portal';

export interface NewOrderLine {
  productId: string;
  quantity: number;
  unitPrice?: number;
  lineNotes?: string;
}

export interface CreateDealerOrderInput {
  customerId: string;
  lines: NewOrderLine[];
  notes?: string;
  customerNotes?: string;
  currency?: string;
  shippingAddressId?: string | null;
  billingAddressId?: string | null;
}

export interface UpdateDealerProfileInput {
  firstName?: string | null;
  lastName?: string | null;
  phoneNumber?: string | null;
  avatarUrl?: string | null;
}

export interface ChangeDealerPasswordInput {
  currentPassword: string;
  newPassword: string;
}

export const dealerApi = {
  getDashboard: async (): Promise<DealerPortalDashboard> => {
    const { data } = await apiClient.get<DealerPortalDashboard>(`${BASE}/dashboard`);
    return data;
  },
  getAllowedCustomers: async (): Promise<DealerAllowedCustomer[]> => {
    const { data } = await apiClient.get<DealerAllowedCustomer[]>(`${BASE}/customers`);
    return data;
  },
  getOrders: async (params: {
    status?: string;
    approvalStatus?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PagedResult<OrderSummary>> => {
    const { data } = await apiClient.get<PagedResult<OrderSummary>>(`${BASE}/orders`, { params });
    return data;
  },
  getOrderById: async (id: string): Promise<OrderDetail> => {
    const { data } = await apiClient.get<OrderDetail>(`${BASE}/orders/${id}`);
    return data;
  },
  createOrder: async (input: CreateDealerOrderInput): Promise<OrderDetail> => {
    const { data } = await apiClient.post<OrderDetail>(`${BASE}/orders`, input);
    return data;
  },
  cancelOrder: async (id: string, reason: string | undefined): Promise<OrderDetail> => {
    const { data } = await apiClient.post<OrderDetail>(`${BASE}/orders/${id}/cancel`, { reason });
    return data;
  },
  getCatalogProducts: async (params: {
    search?: string;
    customerId?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PagedResult<ProductSummary>> => {
    const { data } = await apiClient.get<PagedResult<ProductSummary>>(`${BASE}/catalog/products`, {
      params,
    });
    return data;
  },
  getInvoices: async (params: {
    customerId?: string;
    status?: string;
    fromUtc?: string;
    toUtc?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PagedResult<InvoiceSummary>> => {
    const { data } = await apiClient.get<PagedResult<InvoiceSummary>>(`${BASE}/invoices`, {
      params,
    });
    return data;
  },
  getInvoiceById: async (id: string): Promise<InvoiceDetail> => {
    const { data } = await apiClient.get<InvoiceDetail>(`${BASE}/invoices/${id}`);
    return data;
  },
  getCommissions: async (params: {
    status?: CommissionStatus;
    fromUtc?: string;
    toUtc?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PagedResult<CommissionEntry>> => {
    const { data } = await apiClient.get<PagedResult<CommissionEntry>>(`${BASE}/commissions`, {
      params,
    });
    return data;
  },
  getCommissionSummary: async (): Promise<CommissionSummary> => {
    const { data } = await apiClient.get<CommissionSummary>(`${BASE}/commissions/summary`);
    return data;
  },
  getProfile: async (): Promise<DealerProfile> => {
    const { data } = await apiClient.get<DealerProfile>(`${BASE}/profile`);
    return data;
  },
  getCustomerCredit: async (customerId: string): Promise<CreditSnapshot> => {
    const { data } = await apiClient.get<CreditSnapshot>(`${BASE}/customers/${customerId}/credit`);
    return data;
  },
  updateProfile: async (input: UpdateDealerProfileInput): Promise<void> => {
    await apiClient.put('/auth/profile', input);
  },
  changePassword: async (input: ChangeDealerPasswordInput): Promise<void> => {
    await apiClient.post('/auth/change-password', input);
  },
};
