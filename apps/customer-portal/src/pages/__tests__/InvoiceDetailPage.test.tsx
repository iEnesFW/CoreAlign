import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { InvoiceDetailPage } from '@/pages/InvoiceDetailPage';
import i18n from '@/app/i18n';
import type * as PortalHooks from '@/features/portal/hooks';
import type { InvoiceDetail } from '@/features/portal/types';

const invoiceMock = vi.fn();
const payInvoiceMock = vi.fn();
const pdfDownloadMock = vi.fn();

vi.mock('@/features/portal/hooks', async () => {
  const actual = await vi.importActual<typeof PortalHooks>('@/features/portal/hooks');
  return { ...actual, usePortalInvoice: (id: string | undefined) => invoiceMock(id) };
});

vi.mock('@/features/portal/profileHooks', () => ({
  usePayInvoice: () => ({ mutateAsync: payInvoiceMock, isPending: false }),
}));

vi.mock('@/shared/lib/usePdfDownload', () => ({
  usePdfDownload: () => ({ download: pdfDownloadMock, isLoading: false }),
}));

beforeAll(async () => {
  await i18n.changeLanguage('en');
});

const baseInvoice: InvoiceDetail = {
  id: 'inv-1',
  invoiceNumber: 'INV-100',
  customerName: 'Acme',
  issueDate: '2025-01-10T00:00:00Z',
  dueDate: '2025-02-10T00:00:00Z',
  status: 'Issued',
  currency: 'TRY',
  total: 1000,
  amountPaid: 0,
  amountDue: 1000,
  isOverdue: false,
  subtotal: 800,
  taxTotal: 200,
  shippingCost: 0,
  lines: [
    {
      id: 'l-1',
      lineNumber: 1,
      productSku: 'SKU-A',
      productName: 'Product A',
      quantity: 2,
      unitPrice: 400,
      lineDiscountAmount: 0,
      taxAmount: 200,
      lineNetAmount: 800,
      lineTotal: 800,
    },
  ],
};

const renderPage = (id = 'inv-1') => {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[`/invoices/${id}`]}>
        <Routes>
          <Route path="/invoices/:id" element={<InvoiceDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
};

beforeEach(() => {
  invoiceMock.mockReset();
  payInvoiceMock.mockReset();
  pdfDownloadMock.mockReset();
});

describe('InvoiceDetailPage', () => {
  it('shows loading state', () => {
    invoiceMock.mockReturnValue({ data: undefined, isLoading: true, isError: false });
    renderPage();
    expect(screen.getByText(/loading|yükle/i)).toBeInTheDocument();
  });

  it('shows no-data state on error', () => {
    invoiceMock.mockReturnValue({ data: undefined, isLoading: false, isError: true });
    renderPage();
    expect(screen.getByText(/no data|kayıt yok|veri yok/i)).toBeInTheDocument();
  });

  it('renders invoice number and customer', async () => {
    invoiceMock.mockReturnValue({ data: baseInvoice, isLoading: false, isError: false });
    renderPage();
    await waitFor(() => expect(screen.getByText('INV-100')).toBeInTheDocument());
    expect(screen.getByText(/Acme/)).toBeInTheDocument();
  });

  it('renders Pay button when status is Issued and amountDue > 0', async () => {
    invoiceMock.mockReturnValue({ data: baseInvoice, isLoading: false, isError: false });
    renderPage();
    await waitFor(() =>
      expect(screen.getByRole('button', { name: /pay now|öde/i })).toBeInTheDocument(),
    );
  });

  it('hides Pay button when amountDue is zero', async () => {
    invoiceMock.mockReturnValue({
      data: { ...baseInvoice, amountDue: 0, amountPaid: 1000, status: 'Paid' as const },
      isLoading: false,
      isError: false,
    });
    renderPage();
    await waitFor(() => expect(screen.getByText('INV-100')).toBeInTheDocument());
    expect(screen.queryByRole('button', { name: /pay now|öde/i })).not.toBeInTheDocument();
  });

  it('hides Pay button on Void status', async () => {
    invoiceMock.mockReturnValue({
      data: { ...baseInvoice, status: 'Void' as const, amountDue: 0 },
      isLoading: false,
      isError: false,
    });
    renderPage();
    await waitFor(() => expect(screen.getByText('INV-100')).toBeInTheDocument());
    expect(screen.queryByRole('button', { name: /pay now|öde/i })).not.toBeInTheDocument();
  });

  it('always renders the PDF download button', async () => {
    invoiceMock.mockReturnValue({ data: baseInvoice, isLoading: false, isError: false });
    renderPage();
    await waitFor(() =>
      expect(screen.getByRole('button', { name: /download pdf|pdf indir/i })).toBeInTheDocument(),
    );
  });

  it('renders invoice line rows', async () => {
    invoiceMock.mockReturnValue({ data: baseInvoice, isLoading: false, isError: false });
    renderPage();
    await waitFor(() => expect(screen.getByText('Product A')).toBeInTheDocument());
    expect(screen.getByText('SKU-A')).toBeInTheDocument();
  });
});
