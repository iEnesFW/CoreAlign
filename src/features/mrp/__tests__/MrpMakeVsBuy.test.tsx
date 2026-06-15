import { describe, it, expect, vi } from 'vitest';
import '@testing-library/jest-dom/vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { I18nextProvider, initReactI18next } from 'react-i18next';
import { createInstance } from 'i18next';
import enTranslation from '@/app/i18n/locales/en.json';
import { MrpPlanningGrid } from '../ui/MrpPlanningGrid';
import { PeggingDrawer } from '../ui/PeggingDrawer';
import { ChangeImpactView } from '../ui/ChangeImpactView';
import { ProcurementBadge } from '../ui/ProcurementBadge';
import { AbcBadge } from '../ui/AbcBadge';
import type {
  ChangeImpactResult,
  MrpItemPlan,
  MrpPlannedOrder,
  MrpProductionOrderDraft,
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

const plannedOrder = (over: Partial<MrpPlannedOrder> = {}): MrpPlannedOrder => ({
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
  isReleased: false,
  convertedRequisitionId: null,
  productionOrderId: null,
  ...over,
});

const productionOrder = (over: Partial<MrpProductionOrderDraft> = {}): MrpProductionOrderDraft => ({
  id: 'wo1',
  productId: 'p1',
  lowLevelCode: 0,
  quantity: 15,
  dueDateUtc: '2026-06-20T00:00:00Z',
  releaseDateUtc: '2026-06-15T00:00:00Z',
  estimatedUnitCost: 10,
  sourcePolicy: 'MinMax',
  peggingParentProductId: null,
  peggingSourceOrderLineId: null,
  status: 'Planned',
  ...over,
});

const item = (over: Partial<MrpItemPlan> = {}): MrpItemPlan => ({
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
  buckets: [],
  plannedOrders: [],
  productionOrders: [],
  actions: [],
  pegs: [],
  ...over,
});

describe('ProcurementBadge', () => {
  it('renders a Make badge with the make label and data attribute', () => {
    renderWithI18n(<ProcurementBadge type="Make" />);
    const badge = screen.getByTestId('procurement-badge');
    expect(badge).toHaveAttribute('data-procurement-type', 'Make');
    expect(badge).toHaveTextContent('Make');
  });

  it('renders a Buy badge with the buy label', () => {
    renderWithI18n(<ProcurementBadge type="Buy" />);
    expect(screen.getByTestId('procurement-badge')).toHaveTextContent('Buy');
  });
});

describe('AbcBadge', () => {
  it('renders the class label and data attribute for A/B/C', () => {
    (['A', 'B', 'C'] as const).forEach((cls) => {
      const { unmount } = renderWithI18n(<AbcBadge abcClass={cls} />);
      const badge = screen.getByTestId('abc-badge');
      expect(badge).toHaveAttribute('data-abc-class', cls);
      expect(badge).toHaveTextContent(cls);
      unmount();
    });
  });

  it('renders nothing for Unclassified', () => {
    renderWithI18n(<AbcBadge abcClass="Unclassified" />);
    expect(screen.queryByTestId('abc-badge')).not.toBeInTheDocument();
  });
});

describe('MrpPlanningGrid procurement badges', () => {
  it('tags each item row with its procurement type', () => {
    renderWithI18n(
      <MrpPlanningGrid
        items={[
          item({ productId: 'm1', sku: 'MAKE-1', procurementType: 'Make' }),
          item({ productId: 'b1', sku: 'BUY-1', procurementType: 'Buy' }),
        ]}
        locale="en"
        onSelectItem={() => {}}
      />,
    );
    const badges = screen.getAllByTestId('procurement-badge');
    const types = badges.map((b) => b.getAttribute('data-procurement-type'));
    expect(types).toContain('Make');
    expect(types).toContain('Buy');
  });
});

describe('PeggingDrawer make-vs-buy split', () => {
  const mixed = () =>
    item({
      plannedOrders: [plannedOrder({ id: 'buy1', procurementType: 'Buy' })],
      productionOrders: [productionOrder({ id: 'make1' })],
    });

  it('renders separate Make and Buy planned-order groups', () => {
    renderWithI18n(
      <PeggingDrawer
        item={mixed()}
        pegging={[]}
        planRunId="r1"
        locale="en"
        onClose={() => {}}
        onFirm={() => {}}
        onRelease={() => {}}
        onComplete={() => {}}
      />,
    );
    expect(screen.getByTestId('planned-order-group-Make')).toBeInTheDocument();
    expect(screen.getByTestId('planned-order-group-Buy')).toBeInTheDocument();
  });

  it('labels the Make release action as create-production-order and Buy as convert', () => {
    renderWithI18n(
      <PeggingDrawer
        item={mixed()}
        pegging={[]}
        planRunId="r1"
        locale="en"
        onClose={() => {}}
        onFirm={() => {}}
        onRelease={() => {}}
        onComplete={() => {}}
      />,
    );
    expect(screen.getByRole('button', { name: 'Create production order' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Convert to PO' })).toBeInTheDocument();
  });

  it('routes a Make firm/release through the production-order id', async () => {
    const onFirm = vi.fn();
    const onRelease = vi.fn();
    renderWithI18n(
      <PeggingDrawer
        item={item({ productionOrders: [productionOrder({ id: 'make1', status: 'Planned' })] })}
        pegging={[]}
        planRunId="r1"
        locale="en"
        onClose={() => {}}
        onFirm={onFirm}
        onRelease={onRelease}
        onComplete={() => {}}
      />,
    );
    await userEvent.click(screen.getByRole('button', { name: 'Firm' }));
    expect(onFirm).toHaveBeenCalledWith(
      expect.objectContaining({ procurementType: 'Make', productionOrderId: 'make1' }),
    );
    await userEvent.click(screen.getByRole('button', { name: 'Create production order' }));
    expect(onRelease).toHaveBeenCalledWith(
      expect.objectContaining({ procurementType: 'Make', productionOrderId: 'make1' }),
    );
  });

  it('routes a Buy firm/release through the planned-order id', async () => {
    const onFirm = vi.fn();
    const onRelease = vi.fn();
    renderWithI18n(
      <PeggingDrawer
        item={item({ plannedOrders: [plannedOrder({ id: 'buy1', procurementType: 'Buy' })] })}
        pegging={[]}
        planRunId="r1"
        locale="en"
        onClose={() => {}}
        onFirm={onFirm}
        onRelease={onRelease}
        onComplete={() => {}}
      />,
    );
    await userEvent.click(screen.getByRole('button', { name: 'Firm' }));
    expect(onFirm).toHaveBeenCalledWith(
      expect.objectContaining({ procurementType: 'Buy', plannedOrderId: 'buy1' }),
    );
    await userEvent.click(screen.getByRole('button', { name: 'Convert to PO' }));
    expect(onRelease).toHaveBeenCalledWith(
      expect.objectContaining({ procurementType: 'Buy', plannedOrderId: 'buy1' }),
    );
  });

  it('shows the production-order-created status when a Make order is released', () => {
    renderWithI18n(
      <PeggingDrawer
        item={item({
          productionOrders: [productionOrder({ id: 'make1', status: 'Released' })],
        })}
        pegging={[]}
        planRunId="r1"
        locale="en"
        onClose={() => {}}
        onFirm={() => {}}
        onRelease={() => {}}
        onComplete={() => {}}
      />,
    );
    expect(screen.getByText('Production order created')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Create production order' })).toBeDisabled();
  });

  it('offers Complete on a Released make order and routes it through the production-order id', async () => {
    const onComplete = vi.fn();
    renderWithI18n(
      <PeggingDrawer
        item={item({ productionOrders: [productionOrder({ id: 'make1', status: 'Released' })] })}
        pegging={[]}
        planRunId="r1"
        locale="en"
        onClose={() => {}}
        onFirm={() => {}}
        onRelease={() => {}}
        onComplete={onComplete}
      />,
    );
    await userEvent.click(screen.getByRole('button', { name: 'Complete' }));
    expect(onComplete).toHaveBeenCalledWith({ productionOrderId: 'make1' });
  });

  it('shows a Completed badge and no Complete button for a Closed make order', () => {
    renderWithI18n(
      <PeggingDrawer
        item={item({ productionOrders: [productionOrder({ id: 'make1', status: 'Closed' })] })}
        pegging={[]}
        planRunId="r1"
        locale="en"
        onClose={() => {}}
        onFirm={() => {}}
        onRelease={() => {}}
        onComplete={() => {}}
      />,
    );
    expect(screen.getByText('Completed')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Complete' })).not.toBeInTheDocument();
  });

  it('does not offer Complete on a Buy released order', () => {
    renderWithI18n(
      <PeggingDrawer
        item={item({
          plannedOrders: [plannedOrder({ id: 'buy1', procurementType: 'Buy', isReleased: true })],
        })}
        pegging={[]}
        planRunId="r1"
        locale="en"
        onClose={() => {}}
        onFirm={() => {}}
        onRelease={() => {}}
        onComplete={() => {}}
      />,
    );
    expect(screen.queryByRole('button', { name: 'Complete' })).not.toBeInTheDocument();
  });

  it('keeps an uncommitted Make draft (no id) locked', () => {
    renderWithI18n(
      <PeggingDrawer
        item={item({ productionOrders: [productionOrder({ id: null, status: null })] })}
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
    expect(screen.getByRole('button', { name: 'Create production order' })).toBeDisabled();
  });
});

describe('ChangeImpactView', () => {
  const buildResult = (over: Partial<ChangeImpactResult> = {}): ChangeImpactResult => ({
    planRunId: 'r1',
    sourceOrderLineId: 'l1',
    downstreamSupply: [
      {
        productId: 'fg1',
        lowLevelCode: 0,
        sinkKind: 'ProductionOrder',
        quantity: 5,
        dueDateUtc: '2026-06-20T00:00:00Z',
        releaseDateUtc: '2026-06-15T00:00:00Z',
        directParentProductId: null,
      },
      {
        productId: 'comp1',
        lowLevelCode: 1,
        sinkKind: 'PurchaseRequisition',
        quantity: 10,
        dueDateUtc: '2026-06-18T00:00:00Z',
        releaseDateUtc: '2026-06-13T00:00:00Z',
        directParentProductId: 'fg1',
      },
    ],
    ...over,
  });

  const productInfo = {
    fg1: { sku: 'FG-1', name: 'Finished good' },
    comp1: { sku: 'COMP-1', name: 'Component' },
  };

  it('maps each supply order sink kind to a make/buy procurement badge', () => {
    renderWithI18n(
      <ChangeImpactView
        result={buildResult()}
        locale="en"
        sourceLabel="SO-100"
        productInfo={productInfo}
      />,
    );
    const view = screen.getByTestId('change-impact-view');
    expect(within(view).getByText(/SO-100/)).toBeInTheDocument();
    expect(within(view).getByText('FG-1')).toBeInTheDocument();
    expect(within(view).getByText('COMP-1')).toBeInTheDocument();
    const rows = screen.getAllByTestId('change-impact-row');
    expect(rows).toHaveLength(2);
    expect(rows[0]).toHaveAttribute('data-procurement-type', 'Make');
    expect(rows[1]).toHaveAttribute('data-procurement-type', 'Buy');
  });

  it('falls back to the product id when no product info is supplied', () => {
    renderWithI18n(<ChangeImpactView result={buildResult()} locale="en" />);
    const view = screen.getByTestId('change-impact-view');
    expect(within(view).getByText('fg1')).toBeInTheDocument();
  });

  it('shows an empty state when nothing is pegged to the source', () => {
    renderWithI18n(<ChangeImpactView result={buildResult({ downstreamSupply: [] })} locale="en" />);
    expect(screen.getByText('No planned orders depend on this demand source.')).toBeInTheDocument();
  });

  it('prompts the planner to select a source when no result is loaded', () => {
    renderWithI18n(<ChangeImpactView result={null} locale="en" />);
    expect(screen.getByText('Select a demand source and run the analysis.')).toBeInTheDocument();
  });
});
