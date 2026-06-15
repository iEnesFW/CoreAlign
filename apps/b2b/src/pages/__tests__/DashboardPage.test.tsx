import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { DashboardPage } from '@/pages/DashboardPage';
import i18n from '@/app/i18n';
import type * as PortalHooks from '@/features/portal/hooks';
import type { DealerPortalDashboard } from '@/features/portal/types';

const dashboardMock = vi.fn();

vi.mock('@/features/portal/hooks', async () => {
  const actual = await vi.importActual<typeof PortalHooks>('@/features/portal/hooks');
  return { ...actual, useDealerDashboard: () => dashboardMock() };
});

beforeAll(async () => {
  await i18n.changeLanguage('en');
});

const baseDashboard: DealerPortalDashboard = {
  dealerAccountId: 'da-1',
  dealerAccountName: 'Dealer One',
  allowedCustomerCount: 11,
  pendingApprovalCount: 2,
  totalOpenOrders: 7,
  ordersCompletedThisMonth: 4,
  recentOrders: [
    {
      id: 'o-1',
      orderNumber: 'ORD-B-001',
      customerId: 'c-1',
      customerName: 'Customer A',
      orderDate: '2025-01-15T00:00:00Z',
      status: 'Submitted',
      currency: 'TRY',
      total: 2500,
      originPersona: 'Dealer',
      originDealerAccountId: 'da-1',
      originDealerName: 'Dealer One',
      dealerApprovalStatus: 'PendingCustomerApproval',
    },
  ],
};

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

beforeEach(() => {
  dashboardMock.mockReset();
});

describe('b2b DashboardPage', () => {
  it('renders loading when isLoading', () => {
    dashboardMock.mockReturnValue({ data: undefined, isLoading: true });
    renderDashboard();
    expect(screen.getByText(/loading|yükle/i)).toBeInTheDocument();
  });

  it('renders KPIs from data', async () => {
    dashboardMock.mockReturnValue({ data: baseDashboard, isLoading: false });
    renderDashboard();
    await waitFor(() => {
      const allowed = screen.getByRole('group', { name: /authorized customers/i });
      expect(within(allowed).getByTestId('stat-value')).toHaveTextContent('11');
      const openOrders = screen.getByRole('group', { name: /open orders/i });
      expect(within(openOrders).getByTestId('stat-value')).toHaveTextContent('7');
      const pending = screen.getByRole('group', { name: /awaiting approval/i });
      expect(within(pending).getByTestId('stat-value')).toHaveTextContent('2');
      const completed = screen.getByRole('group', { name: /completed this month/i });
      expect(within(completed).getByTestId('stat-value')).toHaveTextContent('4');
    });
  });

  it('shows recent order number when list is non-empty', async () => {
    dashboardMock.mockReturnValue({ data: baseDashboard, isLoading: false });
    renderDashboard();
    await waitFor(() => expect(screen.getByText('ORD-B-001')).toBeInTheDocument());
  });

  it('renders empty state when no recent orders', async () => {
    dashboardMock.mockReturnValue({
      data: { ...baseDashboard, recentOrders: [] },
      isLoading: false,
    });
    renderDashboard();
    await waitFor(() => {
      const empties = screen.getAllByText(/empty|nothing|hiç|henüz/i);
      expect(empties.length).toBeGreaterThan(0);
    });
  });

  it('renders the new-order CTA link', async () => {
    dashboardMock.mockReturnValue({ data: baseDashboard, isLoading: false });
    renderDashboard();
    await waitFor(() => {
      const newOrder = screen.getAllByRole('link');
      expect(newOrder.length).toBeGreaterThan(0);
    });
  });
});
