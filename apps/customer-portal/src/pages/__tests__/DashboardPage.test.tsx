import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { DashboardPage } from '@/pages/DashboardPage';
import i18n from '@/app/i18n';
import type * as PortalHooks from '@/features/portal/hooks';
import type { CustomerPortalDashboard } from '@/features/portal/types';

const dashboardMock = vi.fn();
const approvalsCountMock = vi.fn();

vi.mock('@/features/portal/hooks', async () => {
  const actual = await vi.importActual<typeof PortalHooks>('@/features/portal/hooks');
  return {
    ...actual,
    useDashboard: () => dashboardMock(),
  };
});

vi.mock('@/features/approvals/hooks', () => ({
  useApprovalsPendingCount: () => approvalsCountMock(),
}));

beforeAll(async () => {
  await i18n.changeLanguage('en');
});

const renderDashboard = () => {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
};

const baseDashboard: CustomerPortalDashboard = {
  customerId: 'c-1',
  customerName: 'Acme',
  totalActiveOrders: 12,
  totalOpenInvoices: 4,
  openInvoiceTotalAmount: 25000,
  openInvoiceCurrency: 'TRY',
  totalActiveDealers: 3,
  invoicedLast30DaysAmount: 50000,
  invoicedLast30DaysCurrency: 'TRY',
  recentOrders: [
    {
      id: 'o-1',
      orderNumber: 'ORD-001',
      customerId: 'c-1',
      customerName: 'Acme',
      orderDate: '2025-01-10T00:00:00Z',
      status: 'Submitted',
      currency: 'TRY',
      total: 1500,
    },
  ],
  recentInvoices: [
    {
      id: 'i-1',
      invoiceNumber: 'INV-001',
      customerName: 'Acme',
      issueDate: '2025-01-10T00:00:00Z',
      dueDate: '2025-02-10T00:00:00Z',
      status: 'Issued',
      currency: 'TRY',
      total: 5000,
      amountPaid: 0,
      amountDue: 5000,
      isOverdue: false,
    },
  ],
};

beforeEach(() => {
  dashboardMock.mockReset();
  approvalsCountMock.mockReset();
});

describe('DashboardPage', () => {
  it('shows spinner when isLoading', () => {
    dashboardMock.mockReturnValue({ data: undefined, isLoading: true });
    approvalsCountMock.mockReturnValue({ data: 0 });
    renderDashboard();
    expect(screen.getByText(/loading|yükle/i)).toBeInTheDocument();
  });

  it('renders KPIs from dashboard data', async () => {
    dashboardMock.mockReturnValue({ data: baseDashboard, isLoading: false });
    approvalsCountMock.mockReturnValue({ data: 0 });
    renderDashboard();
    await waitFor(() => {
      const openOrdersCard = screen.getByRole('group', { name: /open orders/i });
      expect(within(openOrdersCard).getByTestId('stat-value')).toHaveTextContent('12');
      const dealersCard = screen.getByRole('group', { name: /active dealers/i });
      expect(within(dealersCard).getByTestId('stat-value')).toHaveTextContent('3');
    });
  });

  it('renders recent orders list with order number link', async () => {
    dashboardMock.mockReturnValue({ data: baseDashboard, isLoading: false });
    approvalsCountMock.mockReturnValue({ data: 0 });
    renderDashboard();
    await waitFor(() => {
      const links = screen.getAllByText('ORD-001');
      expect(links.length).toBeGreaterThan(0);
    });
  });

  it('renders recent invoices section', async () => {
    dashboardMock.mockReturnValue({ data: baseDashboard, isLoading: false });
    approvalsCountMock.mockReturnValue({ data: 0 });
    renderDashboard();
    await waitFor(() => expect(screen.getByText('INV-001')).toBeInTheDocument());
  });

  it('renders the empty state when recentOrders is empty', async () => {
    dashboardMock.mockReturnValue({
      data: { ...baseDashboard, recentOrders: [], recentInvoices: [] },
      isLoading: false,
    });
    approvalsCountMock.mockReturnValue({ data: 0 });
    renderDashboard();
    await waitFor(() => {
      const empties = screen.getAllByText(/nothing here|nothing yet|henüz/i);
      expect(empties.length).toBeGreaterThan(0);
    });
  });

  it('renders pending approvals count when greater than zero', async () => {
    dashboardMock.mockReturnValue({ data: baseDashboard, isLoading: false });
    approvalsCountMock.mockReturnValue({ data: 7 });
    renderDashboard();
    await waitFor(() =>
      expect(screen.getByTestId('pending-approvals-value')).toHaveTextContent('7'),
    );
  });
});
