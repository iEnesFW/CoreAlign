import { describe, it, expect, vi } from 'vitest';
import '@testing-library/jest-dom/vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { I18nextProvider, initReactI18next } from 'react-i18next';
import { createInstance } from 'i18next';
import enTranslation from '@/app/i18n/locales/en.json';
import { TransferSuggestionsView } from '../ui/TransferSuggestionsView';
import type { MrpTransferSuggestionsResult } from '../model/mrp-planning.types';

const i18n = createInstance();
i18n.use(initReactI18next).init({
  lng: 'en',
  fallbackLng: 'en',
  ns: ['translation'],
  defaultNS: 'translation',
  resources: { en: { translation: enTranslation } },
  interpolation: { escapeValue: false },
});

const renderWithI18n = (ui: React.ReactElement) =>
  render(<I18nextProvider i18n={i18n}>{ui}</I18nextProvider>);

const result = (
  over: Partial<MrpTransferSuggestionsResult> = {},
): MrpTransferSuggestionsResult => ({
  productsEvaluated: 3,
  transferCount: 1,
  externalReplenishmentCount: 1,
  transfers: [
    {
      productId: 'p1',
      productSku: 'SKU-1',
      productName: 'Widget',
      fromWarehouseId: 'w1',
      fromWarehouseCode: 'WH-A',
      fromWarehouseName: 'Warehouse A',
      toWarehouseId: 'w2',
      toWarehouseCode: 'WH-B',
      toWarehouseName: 'Warehouse B',
      quantity: 12,
    },
  ],
  netPositions: [
    {
      productId: 'p1',
      productSku: 'SKU-1',
      productName: 'Widget',
      warehouseId: 'w1',
      warehouseCode: 'WH-A',
      warehouseName: 'Warehouse A',
      available: 20,
      demand: 8,
      net: 12,
    },
    {
      productId: 'p1',
      productSku: 'SKU-1',
      productName: 'Widget',
      warehouseId: 'w2',
      warehouseCode: 'WH-B',
      warehouseName: 'Warehouse B',
      available: 0,
      demand: 12,
      net: -12,
    },
  ],
  externalReplenishment: [
    {
      productId: 'p2',
      productSku: 'SKU-2',
      productName: 'Gadget',
      warehouseId: 'w2',
      warehouseCode: 'WH-B',
      warehouseName: 'Warehouse B',
      quantity: 5,
    },
  ],
  ...over,
});

describe('TransferSuggestionsView', () => {
  it('renders a suggestion row grouped by product with from/to codes and quantity', () => {
    renderWithI18n(<TransferSuggestionsView result={result()} locale="en" />);
    const view = screen.getByTestId('transfer-suggestions-view');

    const group = within(view).getByTestId('transfer-product-group');
    expect(group).toHaveAttribute('data-product-id', 'p1');
    expect(within(group).getByText('Widget')).toBeInTheDocument();

    const row = within(group).getByTestId('transfer-suggestion-row');
    expect(within(row).getByText('WH-A')).toBeInTheDocument();
    expect(within(row).getByText('WH-B')).toBeInTheDocument();
    expect(row).toHaveTextContent('12');
    expect(within(row).getByText(/Transfer 12 SKU-1 from Warehouse A to Warehouse B/)).toHaveClass(
      'sr-only',
    );
  });

  it('renders external replenishment and per-warehouse net positions', () => {
    renderWithI18n(<TransferSuggestionsView result={result()} locale="en" />);
    expect(screen.getByTestId('external-replenishment')).toBeInTheDocument();
    expect(screen.getAllByTestId('net-position-row')).toHaveLength(2);
  });

  it('shows an empty state when there are no transfer suggestions', () => {
    renderWithI18n(
      <TransferSuggestionsView
        result={result({ transfers: [], transferCount: 0, externalReplenishment: [] })}
        locale="en"
      />,
    );
    expect(
      screen.getByText('No transfers needed — every warehouse can cover its own demand.'),
    ).toBeInTheDocument();
  });

  it('shows an empty state when no result has loaded yet', () => {
    renderWithI18n(<TransferSuggestionsView result={null} locale="en" />);
    expect(screen.getByTestId('transfer-suggestions-empty')).toBeInTheDocument();
  });

  it('renders no execute button when onExecute is not provided', () => {
    renderWithI18n(<TransferSuggestionsView result={result()} locale="en" />);
    expect(screen.queryByTestId('transfer-execute-button')).not.toBeInTheDocument();
  });

  it('calls onExecute with the suggestion ids and quantity when Execute is clicked', async () => {
    const user = userEvent.setup();
    const onExecute = vi.fn();
    renderWithI18n(<TransferSuggestionsView result={result()} locale="en" onExecute={onExecute} />);

    await user.click(screen.getByTestId('transfer-execute-button'));

    expect(onExecute).toHaveBeenCalledTimes(1);
    expect(onExecute).toHaveBeenCalledWith(
      expect.objectContaining({
        productId: 'p1',
        fromWarehouseId: 'w1',
        toWarehouseId: 'w2',
        quantity: 12,
      }),
    );
  });

  it('disables every execute button while a transfer is executing', () => {
    renderWithI18n(
      <TransferSuggestionsView
        result={result()}
        locale="en"
        onExecute={vi.fn()}
        isExecuting
        executingKey="p1:w1:w2"
      />,
    );
    expect(screen.getByTestId('transfer-execute-button')).toBeDisabled();
    expect(screen.getByText('Executing…')).toBeInTheDocument();
  });
});
