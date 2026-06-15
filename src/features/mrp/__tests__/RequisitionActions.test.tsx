import { describe, it, expect, vi, beforeEach } from 'vitest';
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { I18nextProvider, initReactI18next } from 'react-i18next';
import { createInstance } from 'i18next';
import enTranslation from '@/app/i18n/locales/en.json';
import { ReasonPromptDialog } from '../ui/ReasonPromptDialog';
import { ConvertRequisitionDialog } from '../ui/ConvertRequisitionDialog';

const vendorsQueryMock = vi.fn();
vi.mock('@/features/vendors/hooks/useVendorQueries', () => ({
  useVendorsQuery: () => vendorsQueryMock(),
}));

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

beforeEach(() => {
  vendorsQueryMock.mockReturnValue({
    data: {
      data: { items: [{ id: 'v1', name: 'Acme Supplies' }], total: 1, page: 1, pageSize: 100 },
    },
  });
});

describe('ReasonPromptDialog (MRP-BUG-7)', () => {
  it('collects an optional reason and confirms with it', async () => {
    const user = userEvent.setup();
    const onConfirm = vi.fn();
    renderWithI18n(
      <ReasonPromptDialog
        title="Reject REQ-1"
        confirmLabel="Reject"
        onConfirm={onConfirm}
        onCancel={() => {}}
      />,
    );
    await user.type(screen.getByRole('textbox'), 'Wrong supplier');
    await user.click(screen.getByRole('button', { name: 'Reject' }));
    expect(onConfirm).toHaveBeenCalledWith('Wrong supplier');
  });

  it('confirms with null when the reason is blank', async () => {
    const user = userEvent.setup();
    const onConfirm = vi.fn();
    renderWithI18n(
      <ReasonPromptDialog
        title="Cancel REQ-1"
        confirmLabel="Confirm cancel"
        onConfirm={onConfirm}
        onCancel={() => {}}
      />,
    );
    await user.click(screen.getByRole('button', { name: 'Confirm cancel' }));
    expect(onConfirm).toHaveBeenCalledWith(null);
  });
});

describe('ConvertRequisitionDialog (MRP-BUG-3)', () => {
  it('disables convert until a vendor is chosen, then submits the convert input', async () => {
    const user = userEvent.setup();
    const onConfirm = vi.fn();
    renderWithI18n(
      <ConvertRequisitionDialog
        requisitionId="req1"
        requisitionNumber="REQ-1"
        onConfirm={onConfirm}
        onCancel={() => {}}
      />,
    );
    const convertButton = screen.getByRole('button', { name: 'Convert to PO' });
    expect(convertButton).toBeDisabled();

    await user.selectOptions(screen.getByRole('combobox', { name: 'Vendor' }), 'v1');
    expect(convertButton).toBeEnabled();
    await user.click(convertButton);

    expect(onConfirm).toHaveBeenCalledTimes(1);
    expect(onConfirm.mock.calls[0][0]).toMatchObject({
      id: 'req1',
      vendorId: 'v1',
      currency: 'TRY',
    });
  });
});
