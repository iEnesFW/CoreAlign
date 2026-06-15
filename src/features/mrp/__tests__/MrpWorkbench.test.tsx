import { describe, it, expect, vi } from 'vitest';
import '@testing-library/jest-dom/vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { I18nextProvider } from 'react-i18next';
import { createInstance } from 'i18next';
import { initReactI18next } from 'react-i18next';
import enTranslation from '@/app/i18n/locales/en.json';
import { KpiStrip } from '../ui/KpiStrip';
import { MrpPlanningGrid } from '../ui/MrpPlanningGrid';
import { ActionMessageQueue } from '../ui/ActionMessageQueue';
import { PeggingDrawer } from '../ui/PeggingDrawer';
import type {
  MrpActionMessage,
  MrpItemPlan,
  MrpPegging,
  MrpPlanResult,
} from '../model/mrp-planning.types';

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

const baseItem = (over: Partial<MrpItemPlan> = {}): MrpItemPlan => ({
  productId: 'p1',
  sku: 'SKU-1',
  name: 'Widget',
  lowLevelCode: 0,
  onHand: 10,
  reserved: 0,
  safetyStock: 20,
  reorderPoint: 25,
  policy: 'MinMax',
  procurementType: 'Buy',
  abcClass: 'Unclassified',
  preferredSupplierId: null,
  leadTimeDays: 5,
  buckets: [
    {
      startUtc: '2026-06-12T00:00:00Z',
      grossRequirements: 30,
      scheduledReceipts: 0,
      projectedOnHand: 5,
      netRequirements: 15,
      plannedReceipts: 0,
      plannedReleases: 0,
    },
  ],
  plannedOrders: [],
  productionOrders: [],
  actions: [],
  pegs: [],
  ...over,
});

describe('KpiStrip', () => {
  it('renders the four planning KPIs with accessible groups', () => {
    const plan: MrpPlanResult = {
      planRunId: null,
      status: 'Preview',
      asOfUtc: '2026-06-12T00:00:00Z',
      bucketKind: 'Day',
      horizonDays: 60,
      productsEvaluated: 12,
      plannedOrderCount: 4,
      actionMessageCount: 7,
      makeOrderCount: 1,
      buyOrderCount: 3,
      stockoutRiskCount: 3,
      projectedStockoutCount: 1,
      excessSupplyCount: 0,
      onOrderCount: 5,
      items: [],
    };
    renderWithI18n(<KpiStrip plan={plan} />);
    const stockout = screen.getByRole('group', { name: 'Stockout risk' });
    expect(within(stockout).getByTestId('stat-value')).toHaveTextContent('3');
    const exceptions = screen.getByRole('group', { name: 'Open exceptions' });
    expect(within(exceptions).getByTestId('stat-value')).toHaveTextContent('7');
  });
});

describe('MrpPlanningGrid', () => {
  it('renders bucket rows and flags projected-on-hand below safety stock', () => {
    renderWithI18n(<MrpPlanningGrid items={[baseItem()]} locale="en" onSelectItem={() => {}} />);
    expect(screen.getByText('SKU-1')).toBeInTheDocument();
    expect(screen.getByText('Gross requirements')).toBeInTheDocument();
    const projCell = screen.getByTestId('proj-on-hand-cell');
    expect(projCell).toHaveAttribute('data-below-safety', 'true');
  });

  it('shows an empty state with no items', () => {
    renderWithI18n(<MrpPlanningGrid items={[]} locale="en" onSelectItem={() => {}} />);
    expect(screen.getByText('Run a preview to see the time-phased plan.')).toBeInTheDocument();
  });

  it('emits one tbody per item directly under the table (no nested tbody)', () => {
    const { container } = renderWithI18n(
      <MrpPlanningGrid
        items={[baseItem(), baseItem({ productId: 'p2', sku: 'SKU-2' })]}
        locale="en"
        onSelectItem={() => {}}
      />,
    );
    const table = container.querySelector('table');
    expect(table).not.toBeNull();
    const bodies = table!.querySelectorAll('tbody');
    expect(bodies).toHaveLength(2);
    bodies.forEach((body) => {
      expect(body.parentElement?.tagName).toBe('TABLE');
      expect(body.querySelector('tbody')).toBeNull();
    });
  });
});

const releaseMessage = (over: Partial<MrpActionMessage> = {}): MrpActionMessage => ({
  id: 'm1',
  planRunId: 'r1',
  productId: 'p1',
  productSku: 'SKU-1',
  productName: 'Widget',
  actionType: 'Release',
  severity: 'Critical',
  quantity: 15,
  currentDateUtc: null,
  suggestedDateUtc: '2026-06-12T00:00:00Z',
  relatedPurchaseOrderId: null,
  relatedPlannedOrderId: 'po1',
  daysUntilStockOut: 2,
  isDismissed: false,
  dismissedAtUtc: null,
  message: 'Release a new planned order now.',
  ...over,
});

describe('ActionMessageQueue', () => {
  it('multi-selects releasable rows and calls release with the planned-order ids', async () => {
    const user = userEvent.setup();
    const onRelease = vi.fn();
    renderWithI18n(
      <ActionMessageQueue
        messages={[releaseMessage(), releaseMessage({ id: 'm2', relatedPlannedOrderId: 'po2' })]}
        locale="en"
        onReleaseSelected={onRelease}
        onDismiss={() => {}}
        onOpenInGrid={() => {}}
      />,
    );
    await user.click(screen.getByRole('checkbox', { name: 'Select all' }));
    await user.click(screen.getByRole('button', { name: 'Release selected' }));
    expect(onRelease).toHaveBeenCalledTimes(1);
    expect(onRelease.mock.calls[0][0].sort()).toEqual(['po1', 'po2']);
  });

  it('calls dismiss for a single row', async () => {
    const user = userEvent.setup();
    const onDismiss = vi.fn();
    renderWithI18n(
      <ActionMessageQueue
        messages={[releaseMessage()]}
        locale="en"
        onReleaseSelected={() => {}}
        onDismiss={onDismiss}
        onOpenInGrid={() => {}}
      />,
    );
    await user.click(screen.getByRole('button', { name: 'Dismiss' }));
    expect(onDismiss).toHaveBeenCalledWith('m1');
  });

  it('hides release controls until the plan is committed (canRelease=false)', () => {
    renderWithI18n(
      <ActionMessageQueue
        messages={[releaseMessage()]}
        locale="en"
        canRelease={false}
        onReleaseSelected={() => {}}
        onDismiss={() => {}}
        onOpenInGrid={() => {}}
      />,
    );
    expect(screen.getByRole('checkbox', { name: 'Select all' })).toBeDisabled();
    expect(screen.queryByRole('button', { name: 'Release' })).not.toBeInTheDocument();
    expect(screen.queryByRole('checkbox', { name: 'Select SKU-1' })).not.toBeInTheDocument();
  });

  it('treats select-all as complete when two messages share one planned order', async () => {
    const user = userEvent.setup();
    renderWithI18n(
      <ActionMessageQueue
        messages={[
          releaseMessage({ id: 'm1', relatedPlannedOrderId: 'po1' }),
          releaseMessage({ id: 'm2', actionType: 'Expedite', relatedPlannedOrderId: 'po1' }),
        ]}
        locale="en"
        onReleaseSelected={() => {}}
        onDismiss={() => {}}
        onOpenInGrid={() => {}}
      />,
    );
    const selectAll = screen.getByRole('checkbox', { name: 'Select all' });
    await user.click(selectAll);
    expect(selectAll).toBeChecked();
  });
});

describe('PeggingDrawer', () => {
  it('shows the pegged source orders', () => {
    const pegging: MrpPegging[] = [
      {
        componentProductId: 'p1',
        requirementQuantity: 12,
        dueDateUtc: '2026-06-20T00:00:00Z',
        sourceKind: 'SalesOrder',
        sourceParentProductId: null,
        sourceParentProductName: null,
        sourceOrderLineId: 'l1',
        sourceOrderNumber: 'SO-100',
      },
    ];
    renderWithI18n(
      <PeggingDrawer
        item={baseItem()}
        pegging={pegging}
        planRunId="r1"
        locale="en"
        onClose={() => {}}
        onFirm={() => {}}
        onRelease={() => {}}
        onComplete={() => {}}
      />,
    );
    const list = screen.getByTestId('pegging-list');
    expect(within(list).getByText(/SO-100/)).toBeInTheDocument();
  });

  const plannedOrderItem = () =>
    baseItem({
      plannedOrders: [
        {
          id: 'po1',
          planRunId: 'r1',
          productId: 'p1',
          productSku: 'SKU-1',
          productName: 'Widget',
          lowLevelCode: 0,
          quantity: 15,
          dueDateUtc: '2026-06-20T00:00:00Z',
          releaseDateUtc: '2026-06-15T00:00:00Z',
          preferredSupplierId: null,
          estimatedUnitCost: 10,
          sourcePolicy: 'MinMax',
          procurementType: 'Buy',
          isFirmed: false,
          convertedRequisitionId: null,
          productionOrderId: null,
        },
      ],
    });

  it('disables firm and release on a preview (no committed plan run)', () => {
    renderWithI18n(
      <PeggingDrawer
        item={plannedOrderItem()}
        pegging={[]}
        planRunId={null}
        locale="en"
        onClose={() => {}}
        onFirm={() => {}}
        onRelease={() => {}}
        onComplete={() => {}}
      />,
    );
    expect(screen.getByRole('button', { name: 'Firm' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Convert to PO' })).toBeDisabled();
  });

  it('enables firm and release once the plan is committed', () => {
    renderWithI18n(
      <PeggingDrawer
        item={plannedOrderItem()}
        pegging={[]}
        planRunId="r1"
        locale="en"
        onClose={() => {}}
        onFirm={() => {}}
        onRelease={() => {}}
        onComplete={() => {}}
      />,
    );
    expect(screen.getByRole('button', { name: 'Firm' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Convert to PO' })).toBeEnabled();
  });
});
