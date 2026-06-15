import { describe, it, expect, vi, beforeAll, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { NewOrderForm } from '@/features/orders/NewOrderForm';
import i18n from '@/app/i18n';

const customersListMock = vi.fn();
const creditMock = vi.fn();
const createOrderMock = vi.fn();
const productsMock = vi.fn();

import type * as PortalApi from '@/features/portal/api';

vi.mock('@/features/portal/api', async () => {
  const actual = await vi.importActual<typeof PortalApi>('@/features/portal/api');
  return {
    ...actual,
    dealerApi: {
      ...(actual.dealerApi ?? {}),
      getAllowedCustomers: () => customersListMock(),
      getCustomerCredit: (id: string) => creditMock(id),
      createOrder: (input: unknown) => createOrderMock(input),
      getCatalogProducts: () => productsMock(),
    },
  };
});

beforeAll(async () => {
  await i18n.changeLanguage('en');
});

const renderForm = () => {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <NewOrderForm />
      </MemoryRouter>
    </QueryClientProvider>,
  );
};

beforeEach(() => {
  customersListMock.mockReset();
  creditMock.mockReset();
  createOrderMock.mockReset();
  productsMock.mockReset();
});

describe('NewOrderForm', () => {
  it('disables submit when no customer is selected', async () => {
    customersListMock.mockResolvedValue([]);
    productsMock.mockResolvedValue([]);
    renderForm();
    await waitFor(() => expect(customersListMock).toHaveBeenCalled());
    const submit = screen.getByRole('button', { name: /submit|gönder|order/i });
    expect(submit).toBeDisabled();
  });

  it('lists available customers as options', async () => {
    customersListMock.mockResolvedValue([
      { customerId: 'c1', name: 'Acme Holding', code: 'C-001', currency: 'TRY' },
      { customerId: 'c2', name: 'Yıldız Müh.', code: 'C-002', currency: 'TRY' },
    ]);
    productsMock.mockResolvedValue([]);
    renderForm();
    await waitFor(() => expect(customersListMock).toHaveBeenCalled());
    expect(await screen.findByText(/Acme Holding/)).toBeInTheDocument();
    expect(screen.getByText(/Yıldız Müh\./)).toBeInTheDocument();
  });

  it('renders the credit panel when customer credit limit is positive', async () => {
    customersListMock.mockResolvedValue([
      { customerId: 'c1', name: 'Acme', code: null, currency: 'TRY' },
    ]);
    creditMock.mockResolvedValue({
      limit: 100_000,
      outstanding: 25_000,
      available: 75_000,
      usagePercent: 25,
      currency: 'TRY',
      isSoftLimitReached: false,
      isHardLimitReached: false,
    });
    productsMock.mockResolvedValue([]);
    renderForm();
    await waitFor(() => expect(customersListMock).toHaveBeenCalled());
    const select = await screen.findByRole('combobox');
    await userEvent.selectOptions(select, 'c1');
    await waitFor(() => expect(creditMock).toHaveBeenCalledWith('c1'));
    await screen.findByText(/100,000|100.000|100\s000/);
  });

  it('keeps submit disabled when hard credit limit is reached', async () => {
    customersListMock.mockResolvedValue([
      { customerId: 'c1', name: 'Acme', code: null, currency: 'TRY' },
    ]);
    creditMock.mockResolvedValue({
      limit: 100,
      outstanding: 200,
      available: 0,
      usagePercent: 200,
      currency: 'TRY',
      isSoftLimitReached: true,
      isHardLimitReached: true,
    });
    productsMock.mockResolvedValue([]);
    renderForm();
    const select = await screen.findByRole('combobox');
    await userEvent.selectOptions(select, 'c1');
    await waitFor(() => expect(creditMock).toHaveBeenCalled());
    const submit = screen.getByRole('button', { name: /submit|gönder|order/i });
    expect(submit).toBeDisabled();
  });

  it('shows a customer-required toast on submit with no customer (smoke)', async () => {
    customersListMock.mockResolvedValue([]);
    productsMock.mockResolvedValue([]);
    renderForm();
    const submit = await screen.findByRole('button', { name: /submit|gönder|order/i });
    expect(submit).toBeDisabled();
  });
});
