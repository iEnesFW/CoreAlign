import { describe, expect, it } from 'vitest';
import { mapBomSummaryToBreakdown } from './bomPreviewBreakdown';
import type { BOMSummaryDto } from './engineering.types';

const summary = (over: Partial<BOMSummaryDto> = {}): BOMSummaryDto => ({
  totalAreaM2: 4,
  totalPanels: 2,
  totalWeightKg: 50,
  profileCost: 200,
  glassCost: 300,
  hardwareCost: 40,
  laborCost: 60,
  wasteCost: 10,
  transportCost: 15,
  scaffoldingCost: 0,
  craneCost: 0,
  subtotal: 625,
  marginAmount: 125,
  taxAmount: 150,
  grandTotal: 900,
  currency: 'TRY',
  lines: [],
  ...over,
});

describe('mapBomSummaryToBreakdown', () => {
  it('maps the backend summary onto the LiveCostPreview breakdown fields', () => {
    const b = mapBomSummaryToBreakdown(summary());
    expect(b.materials).toBe(200); // profileCost
    expect(b.glass).toBe(300);
    expect(b.hardware).toBe(40);
    expect(b.waste).toBe(10);
    expect(b.labor).toBe(60);
    expect(b.transport).toBe(15);
    expect(b.totalBaseCost).toBe(625); // subtotal
    expect(b.margin).toBe(125);
    expect(b.taxAmount).toBe(150);
    expect(b.grandTotal).toBe(900);
    expect(b.currency).toBe('TRY');
  });

  it('derives taxBase as subtotal + margin (what tax is applied to)', () => {
    expect(mapBomSummaryToBreakdown(summary()).taxBase).toBe(750);
  });

  it('carries area/panels/weight through', () => {
    const b = mapBomSummaryToBreakdown(
      summary({ totalAreaM2: 12, totalPanels: 5, totalWeightKg: 88 }),
    );
    expect(b.totalAreaM2).toBe(12);
    expect(b.totalPanels).toBe(5);
    expect(b.totalWeightKg).toBe(88);
  });
});
