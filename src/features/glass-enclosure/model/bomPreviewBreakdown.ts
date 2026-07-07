import type { CostBreakdown } from './costCalculator';
import type { BOMSummaryDto } from './engineering.types';

// Maps the backend BOM summary (the single source of truth: representative-profile selection, real
// NetAreaMm2 area, FX conversion, arc bend factor) onto the shape LiveCostPreview renders, so the
// on-screen price equals the real quote instead of the frontend's diverging local estimate.
export const mapBomSummaryToBreakdown = (summary: BOMSummaryDto): CostBreakdown => ({
  materials: summary.profileCost,
  glass: summary.glassCost,
  hardware: summary.hardwareCost,
  waste: summary.wasteCost,
  labor: summary.laborCost,
  scaffolding: summary.scaffoldingCost,
  crane: summary.craneCost,
  transport: summary.transportCost,
  totalBaseCost: summary.subtotal,
  margin: summary.marginAmount,
  taxBase: summary.subtotal + summary.marginAmount,
  taxAmount: summary.taxAmount,
  grandTotal: summary.grandTotal,
  currency: summary.currency,
  totalAreaM2: summary.totalAreaM2,
  totalPanels: summary.totalPanels,
  totalWeightKg: summary.totalWeightKg,
  lines: [],
});
