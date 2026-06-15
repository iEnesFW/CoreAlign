import { apiClient } from '@/shared/api/apiClient';
import type {
  CatalogProduct,
  CreditSnapshot,
  CustomerPortalDashboard,
  DealerAccount,
  InvoiceDetail,
  InvoiceSummary,
  OrderDetail,
  OrderSummary,
  PagedResult,
  PortalAddress,
  PortalAddressInput,
} from './types';

const BASE = '/customer-portal';

export interface DirectOrderLineInput {
  productId: string;
  quantity: number;
  lineNotes?: string;
}

export interface CreateCustomerDirectOrderInput {
  lines: DirectOrderLineInput[];
  notes?: string;
  customerNotes?: string;
  shippingAddressId?: string | null;
  billingAddressId?: string | null;
}

export const portalApi = {
  getDashboard: async (): Promise<CustomerPortalDashboard> => {
    const { data } = await apiClient.get<CustomerPortalDashboard>(`${BASE}/dashboard`);
    return data;
  },
  getOrders: async (params: { status?: string; page?: number; pageSize?: number }) => {
    const { data } = await apiClient.get<PagedResult<OrderSummary>>(`${BASE}/orders`, { params });
    return data;
  },
  getOrderById: async (id: string): Promise<OrderDetail> => {
    const { data } = await apiClient.get<OrderDetail>(`${BASE}/orders/${id}`);
    return data;
  },
  createDirectOrder: async (input: CreateCustomerDirectOrderInput): Promise<string> => {
    const { data } = await apiClient.post<string>(`${BASE}/orders`, input);
    return data;
  },
  getCatalogProducts: async (params: {
    search?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PagedResult<CatalogProduct>> => {
    const { data } = await apiClient.get<PagedResult<CatalogProduct>>(`${BASE}/catalog/products`, {
      params,
    });
    return data;
  },
  getInvoices: async (params: { status?: string; page?: number; pageSize?: number }) => {
    const { data } = await apiClient.get<PagedResult<InvoiceSummary>>(`${BASE}/invoices`, {
      params,
    });
    return data;
  },
  getInvoiceById: async (id: string): Promise<InvoiceDetail> => {
    const { data } = await apiClient.get<InvoiceDetail>(`${BASE}/invoices/${id}`);
    return data;
  },
  getDealers: async (): Promise<DealerAccount[]> => {
    const { data } = await apiClient.get<DealerAccount[]>(`${BASE}/dealers`);
    return data;
  },
  getCredit: async (): Promise<CreditSnapshot> => {
    const { data } = await apiClient.get<CreditSnapshot>(`${BASE}/credit`);
    return data;
  },
  listAddresses: async (): Promise<PortalAddress[]> => {
    const { data } = await apiClient.get<PortalAddress[]>(`${BASE}/addresses`);
    return data;
  },
  createAddress: async (input: PortalAddressInput): Promise<PortalAddress> => {
    const { data } = await apiClient.post<PortalAddress>(`${BASE}/addresses`, input);
    return data;
  },
  updateAddress: async (id: string, input: PortalAddressInput): Promise<PortalAddress> => {
    const { data } = await apiClient.put<PortalAddress>(`${BASE}/addresses/${id}`, input);
    return data;
  },
  deleteAddress: async (id: string): Promise<boolean> => {
    const { data } = await apiClient.delete<boolean>(`${BASE}/addresses/${id}`);
    return data;
  },
};
