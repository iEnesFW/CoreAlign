import { describe, it, expect, vi } from 'vitest';
import '@testing-library/jest-dom/vitest';
import { render, fireEvent, within } from '@testing-library/react';
import { I18nextProvider, initReactI18next } from 'react-i18next';
import { createInstance } from 'i18next';
import enTranslation from '@/app/i18n/locales/en.json';
import type { AppUser } from '@/features/users/model/user.types';
import type { DunningSetting, DunningType } from '@/features/dunning/model/dunning.types';
import { DunningTypeRow } from '../DunningTypeRow';

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

const users: AppUser[] = [
  {
    id: 'u1',
    username: 'ali',
    email: 'ali@example.com',
    firstName: 'Ali',
    lastName: 'Veli',
    isActive: true,
    isEmailConfirmed: true,
    roleIds: [],
    roles: [],
    lastLoginAtUtc: null,
    createdAtUtc: '2026-01-01T00:00:00Z',
  },
];

const RECIPIENT_LABEL = 'Ali Veli · ali@example.com';

const setting = (type: DunningType, over: Partial<DunningSetting> = {}): DunningSetting => ({
  type,
  isEnabled: true,
  sendInApp: true,
  sendEmail: false,
  recipientUserIds: [],
  ...over,
});

describe('DunningTypeRow', () => {
  it('renders a unique id for every checkbox across sibling rows', () => {
    const { container } = renderWithI18n(
      <>
        <DunningTypeRow
          setting={setting('InvoiceDueReminder')}
          users={users}
          onSave={vi.fn()}
          isSaving={false}
        />
        <DunningTypeRow
          setting={setting('QuoteExpiringReminder')}
          users={users}
          onSave={vi.fn()}
          isSaving={false}
        />
      </>,
    );

    const ids = Array.from(
      container.querySelectorAll<HTMLInputElement>('input[type="checkbox"]'),
    ).map((el) => el.id);

    expect(ids.length).toBeGreaterThan(0);
    expect(ids.every((id) => id.length > 0)).toBe(true);
    expect(new Set(ids).size).toBe(ids.length);
  });

  it('toggles only its own recipient when the label is clicked', () => {
    const { container } = renderWithI18n(
      <>
        <DunningTypeRow
          setting={setting('InvoiceDueReminder')}
          users={users}
          onSave={vi.fn()}
          isSaving={false}
        />
        <DunningTypeRow
          setting={setting('QuoteExpiringReminder')}
          users={users}
          onSave={vi.fn()}
          isSaving={false}
        />
      </>,
    );

    const sections = Array.from(container.querySelectorAll('section'));
    expect(sections).toHaveLength(2);
    const firstRow = within(sections[0]);
    const secondRow = within(sections[1]);

    fireEvent.click(secondRow.getByText(RECIPIENT_LABEL));

    expect(secondRow.getByLabelText(RECIPIENT_LABEL)).toBeChecked();
    expect(firstRow.getByLabelText(RECIPIENT_LABEL)).not.toBeChecked();
  });

  it('reflects a refreshed server value instead of keeping a stale draft', () => {
    const view = renderWithI18n(
      <DunningTypeRow
        setting={setting('InvoiceDueReminder')}
        users={users}
        onSave={vi.fn()}
        isSaving={false}
      />,
    );

    const emailBox = () =>
      view.container.querySelectorAll<HTMLInputElement>('input[type="checkbox"]')[2];

    expect(emailBox()).not.toBeChecked();

    view.rerender(
      <I18nextProvider i18n={i18n}>
        <DunningTypeRow
          setting={setting('InvoiceDueReminder', { sendEmail: true })}
          users={users}
          onSave={vi.fn()}
          isSaving={false}
        />
      </I18nextProvider>,
    );

    expect(emailBox()).toBeChecked();
  });
});
