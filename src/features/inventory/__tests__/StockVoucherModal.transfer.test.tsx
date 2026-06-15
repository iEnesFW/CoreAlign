import { describe, it, expect, vi, beforeEach } from 'vitest';
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { I18nextProvider, initReactI18next } from 'react-i18next';
import { createInstance } from 'i18next';
import enTranslation from '@/app/i18n/locales/en.json';

const transferMutate = vi.fn(() => Promise.resolve({ isSuccess: true }));

vi.mock('../hooks/useInventoryQueries', () => ({
  useReceiveStock: () => ({ mutateAsync: vi.fn() }),
  useIssueStock: () => ({ mutateAsync: vi.fn() }),
  useAdjustStock: () => ({ mutateAsync: vi.fn() }),
  useTransferStock: () => ({ mutateAsync: transferMutate }),
  useStockItemsQuery: () => ({ data: { data: { items: [] } } }),
}));

vi.mock('@/features/products/hooks/useProductQueries', () => ({
  useProductsQuery: () => ({
    data: {
      data: {
        items: [
          { id: 'p1', sku: 'SKU-1', name: 'Widget', barcode: null, stockQuantity: 5, unit: 'pcs' },
        ],
      },
    },
  }),
}));

vi.mock('@/features/master-data/hooks/useMasterData', () => ({
  useWarehousesQuery: () => ({
    data: {
      data: [
        { id: 'w1', code: 'WH-A', name: 'Warehouse A' },
        { id: 'w2', code: 'WH-B', name: 'Warehouse B' },
      ],
    },
  }),
}));

import { StockVoucherModal } from '../ui/StockVoucherModal';

const i18n = createInstance();
i18n.use(initReactI18next).init({
  lng: 'en',
  fallbackLng: 'en',
  ns: ['translation'],
  defaultNS: 'translation',
  resources: { en: { translation: enTranslation } },
  interpolation: { escapeValue: false },
});

const renderModal = (onClose = vi.fn()) =>
  render(
    <I18nextProvider i18n={i18n}>
      <StockVoucherModal type="transfer" onClose={onClose} />
    </I18nextProvider>,
  );

describe('StockVoucherModal (transfer)', () => {
  beforeEach(() => {
    transferMutate.mockClear();
  });

  it('renders a from warehouse, a to warehouse and a quantity field', () => {
    renderModal();
    expect(screen.getByRole('combobox', { name: 'From warehouse' })).toBeInTheDocument();
    expect(screen.getByRole('combobox', { name: 'To warehouse' })).toBeInTheDocument();
    expect(screen.getByText('Stock transfer voucher')).toBeInTheDocument();
  });

  it('submits a transfer with product, from/to warehouses and quantity', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderModal(onClose);

    await user.selectOptions(screen.getByRole('combobox', { name: 'From warehouse' }), 'w1');
    await user.selectOptions(screen.getByRole('combobox', { name: 'To warehouse' }), 'w2');

    const productInput = screen.getByPlaceholderText('Select product');
    await user.click(productInput);
    await user.click(await screen.findByText('Widget'));

    const qty = screen.getByRole('spinbutton');
    await user.clear(qty);
    await user.type(qty, '7');

    await user.click(screen.getByRole('button', { name: 'Post voucher' }));

    expect(transferMutate).toHaveBeenCalledTimes(1);
    expect(transferMutate).toHaveBeenCalledWith(
      expect.objectContaining({
        productId: 'p1',
        fromWarehouseId: 'w1',
        toWarehouseId: 'w2',
        quantity: 7,
      }),
    );
  });
});
