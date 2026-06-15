import { describe, it, expect } from 'vitest';
import '@testing-library/jest-dom/vitest';
import { render, screen, within } from '@testing-library/react';
import { I18nextProvider, initReactI18next } from 'react-i18next';
import { createInstance } from 'i18next';
import enTranslation from '@/app/i18n/locales/en.json';
import { CapacityLoadView } from '../ui/CapacityLoadView';
import type { MrpCapacityLoadResult } from '../model/mrp-planning.types';

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

const result = (over: Partial<MrpCapacityLoadResult> = {}): MrpCapacityLoadResult => ({
  asOfUtc: '2026-06-15T00:00:00Z',
  bucketKind: 'Day',
  horizonDays: 60,
  bucketStarts: ['2026-06-15T00:00:00Z', '2026-06-16T00:00:00Z'],
  workCenters: [
    {
      workCenterId: 'wc1',
      code: 'WC-CNC',
      name: 'CNC Machining',
      dailyCapacityMinutes: 480,
      buckets: [
        {
          startUtc: '2026-06-15T00:00:00Z',
          loadMinutes: 200,
          capacityMinutes: 480,
          isOverloaded: false,
        },
        {
          startUtc: '2026-06-16T00:00:00Z',
          loadMinutes: 720,
          capacityMinutes: 480,
          isOverloaded: true,
        },
      ],
    },
  ],
  unroutedProductionOrderCount: 2,
  ...over,
});

describe('CapacityLoadView', () => {
  it('renders a work center with an overloaded bucket highlighted red', () => {
    renderWithI18n(<CapacityLoadView result={result()} locale="en" />);
    const view = screen.getByTestId('capacity-load-view');

    const card = within(view).getByTestId('capacity-work-center');
    expect(card).toHaveAttribute('data-work-center-id', 'wc1');
    expect(card).toHaveAttribute('data-overloaded', 'true');
    expect(within(card).getByText('WC-CNC')).toBeInTheDocument();
    expect(within(card).getByText('CNC Machining')).toBeInTheDocument();

    const rows = within(card).getAllByTestId('capacity-bucket-row');
    expect(rows).toHaveLength(2);

    const overloadedRow = rows.find((r) => r.getAttribute('data-overloaded') === 'true');
    expect(overloadedRow).toBeDefined();
    const bar = within(overloadedRow as HTMLElement).getByTestId('capacity-bucket-bar');
    expect(bar.className).toMatch(/bg-rose-500/);

    const withinRow = rows.find((r) => r.getAttribute('data-overloaded') === 'false');
    const okBar = within(withinRow as HTMLElement).getByTestId('capacity-bucket-bar');
    expect(okBar.className).toMatch(/bg-emerald-500/);
  });

  it('shows the unrouted production-order note when present', () => {
    renderWithI18n(<CapacityLoadView result={result()} locale="en" />);
    const note = screen.getByTestId('capacity-unrouted-note');
    expect(note).toHaveTextContent('2 production order(s) have no work center routing');
  });

  it('shows an empty state when there are no work centers', () => {
    renderWithI18n(<CapacityLoadView result={result({ workCenters: [] })} locale="en" />);
    expect(screen.getByTestId('capacity-load-empty')).toBeInTheDocument();
  });

  it('shows an empty state when no result has loaded yet', () => {
    renderWithI18n(<CapacityLoadView result={null} locale="en" />);
    expect(screen.getByTestId('capacity-load-empty')).toBeInTheDocument();
  });
});
