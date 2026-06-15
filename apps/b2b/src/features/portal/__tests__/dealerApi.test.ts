import { afterEach, describe, expect, it, vi } from 'vitest';
import { apiClient } from '@/shared/api/apiClient';
import { dealerApi } from '@/features/portal/api';

describe('dealerApi', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('getDashboard hits /dealer-portal/dashboard', async () => {
    const spy = vi.spyOn(apiClient, 'get').mockResolvedValue({ data: {} } as never);
    await dealerApi.getDashboard();
    expect(spy).toHaveBeenCalledWith('/dealer-portal/dashboard');
  });

  it('getAllowedCustomers hits /dealer-portal/customers', async () => {
    const spy = vi.spyOn(apiClient, 'get').mockResolvedValue({ data: [] } as never);
    await dealerApi.getAllowedCustomers();
    expect(spy).toHaveBeenCalledWith('/dealer-portal/customers');
  });

  it('getOrders forwards status and pagination', async () => {
    const spy = vi.spyOn(apiClient, 'get').mockResolvedValue({ data: {} } as never);
    await dealerApi.getOrders({ status: 'Submitted', page: 2, pageSize: 20 });
    expect(spy).toHaveBeenCalledWith(
      '/dealer-portal/orders',
      expect.objectContaining({ params: { status: 'Submitted', page: 2, pageSize: 20 } }),
    );
  });

  it('getOrderById builds the per-id route', async () => {
    const spy = vi.spyOn(apiClient, 'get').mockResolvedValue({ data: {} } as never);
    await dealerApi.getOrderById('abc-123');
    expect(spy).toHaveBeenCalledWith('/dealer-portal/orders/abc-123');
  });

  it('createOrder posts to /dealer-portal/orders', async () => {
    const spy = vi.spyOn(apiClient, 'post').mockResolvedValue({ data: {} } as never);
    await dealerApi.createOrder({ customerId: 'c-1', lines: [] });
    expect(spy).toHaveBeenCalledWith('/dealer-portal/orders', expect.any(Object));
  });

  it('cancelOrder posts to /cancel sub-route with reason payload', async () => {
    const spy = vi.spyOn(apiClient, 'post').mockResolvedValue({ data: {} } as never);
    await dealerApi.cancelOrder('o-1', 'duplicate');
    expect(spy).toHaveBeenCalledWith(
      '/dealer-portal/orders/o-1/cancel',
      expect.objectContaining({ reason: 'duplicate' }),
    );
  });

  it('getInvoices forwards filter parameters', async () => {
    const spy = vi.spyOn(apiClient, 'get').mockResolvedValue({ data: {} } as never);
    await dealerApi.getInvoices({ customerId: 'c-1', status: 'Issued' });
    expect(spy).toHaveBeenCalledWith(
      '/dealer-portal/invoices',
      expect.objectContaining({
        params: expect.objectContaining({ customerId: 'c-1', status: 'Issued' }),
      }),
    );
  });
});
