import { developedLengthMm } from './arcGeometry';
import type {
  ColorOptionDto,
  GlassEnclosureSettingsDto,
  GlassTypeDto,
  HardwareItemDto,
  HardwareKitDto,
  ProfileSystemDto,
} from './glassEnclosure.types';
import type { ProfileItemDto } from './glassEnclosure.types';
import type { SceneRunState } from './project.types';

export interface CostLine {
  kind:
    | 'ProfileCut'
    | 'GlassPiece'
    | 'HardwarePiece'
    | 'Labor'
    | 'Transport'
    | 'Installation'
    | 'Other';
  description: string;
  quantity: number;
  unit: string;
  unitCost: number;
  lineCost: number;
  source?: string;
}

export interface CostBreakdown {
  materials: number;
  glass: number;
  hardware: number;
  waste: number;
  labor: number;
  scaffolding: number;
  crane: number;
  transport: number;
  totalBaseCost: number;
  margin: number;
  taxBase: number;
  taxAmount: number;
  grandTotal: number;
  currency: string;
  totalAreaM2: number;
  totalPanels: number;
  totalWeightKg: number;
  lines: CostLine[];
}

export interface CostCalculatorInput {
  scene: { runs: SceneRunState[] };
  catalog: {
    profileSystems: ProfileSystemDto[];
    glassTypes: GlassTypeDto[];
    colors: ColorOptionDto[];
    hardwareItems: HardwareItemDto[];
    hardwareKits: HardwareKitDto[];
  };
  settings?: GlassEnclosureSettingsDto | null;
  floorNumber?: number | null;
  taxRatePercent?: number;
  discountPercent?: number;
}

const DEFAULT_PROFILE_KG_PER_METER = 1.1;
const DEFAULT_PROFILE_PRICE_PER_KG = 200;

const safeNumber = (value: number | undefined | null): number =>
  value === undefined || value === null || Number.isNaN(value) ? 0 : value;

const findProfileItemsForSystem = (system: ProfileSystemDto | undefined): ProfileItemDto[] =>
  system?.items ?? [];

const meanProfileMetadata = (items: ProfileItemDto[]) => {
  if (items.length === 0) {
    return {
      weightKgPerMeter: DEFAULT_PROFILE_KG_PER_METER,
      pricePerKg: DEFAULT_PROFILE_PRICE_PER_KG,
    };
  }
  const totalWeight = items.reduce((acc, i) => acc + i.weightKgPerMeter, 0);
  const totalPrice = items.reduce((acc, i) => acc + i.pricePerKg, 0);
  return {
    weightKgPerMeter: totalWeight / items.length,
    pricePerKg: totalPrice / items.length,
  };
};

export function calculateCost(input: CostCalculatorInput): CostBreakdown {
  const { scene, catalog, settings } = input;
  const currency = settings?.defaultCurrency ?? 'TRY';
  const wastePercent = safeNumber(settings?.defaultWastePercent) / 100;
  const laborPerM2 = safeNumber(settings?.laborCostPerM2);
  const marginPercent = safeNumber(settings?.defaultMarginPercent) / 100;
  const transportPerKg = safeNumber(settings?.transportRatePerKg);
  const transportPerKm = safeNumber(settings?.transportRatePerKm);
  const scaffoldingRate = safeNumber(settings?.scaffoldingRatePerM2);
  const scaffoldingFromFloor = settings?.scaffoldingRequiredFromFloor ?? 5;
  const craneRate = safeNumber(settings?.craneRatePerMeter);
  const craneFromFloor = settings?.craneRequiredFromFloor ?? 10;
  const taxRate = safeNumber(input.taxRatePercent) / 100;
  const discountPercent = safeNumber(input.discountPercent) / 100;

  const systemById = new Map(catalog.profileSystems.map((s) => [s.id, s]));
  const glassById = new Map(catalog.glassTypes.map((g) => [g.id, g]));
  const colorById = new Map(catalog.colors.map((c) => [c.id, c]));
  const hardwareById = new Map(catalog.hardwareItems.map((h) => [h.id, h]));
  const hardwareKitsBySystem = new Map<string, HardwareKitDto[]>();
  for (const kit of catalog.hardwareKits) {
    const list = hardwareKitsBySystem.get(kit.systemId) ?? [];
    list.push(kit);
    hardwareKitsBySystem.set(kit.systemId, list);
  }

  const lines: CostLine[] = [];
  let materials = 0;
  let glassCost = 0;
  let hardwareCost = 0;
  let totalAreaM2 = 0;
  let totalPanels = 0;
  let totalWeightKg = 0;

  for (const run of scene.runs) {
    const system = systemById.get(run.profileSystemId);
    const profileItems = findProfileItemsForSystem(system);
    const { weightKgPerMeter, pricePerKg } = meanProfileMetadata(profileItems);
    // WHY developed and not lengthMm: on an ARC run lengthMm is the CHORD, but the top/bottom
    // rails are BENT — they run radius·sweep. Quoting the chord under-bills every curved run
    // (an R2000/90° run is short by ~11 %) while the panel widths already carry the developed
    // span, so the same run was priced with two different lengths.
    const developedMm = developedLengthMm(run.lengthMm, run.geomArcRadiusMm, run.geomArcSweepDeg);
    const railMeters = developedMm / 1000;
    const heightMeters = run.heightMm / 1000;
    const panelCount = Math.max(1, run.panels.length);
    const color = run.colorId ? colorById.get(run.colorId) : undefined;
    const priceModifier = 1 + safeNumber(color?.priceModifierPercent) / 100;

    const profileLines: { role: string; meters: number }[] = [
      { role: 'Top', meters: railMeters },
      { role: 'Bottom', meters: railMeters },
      { role: 'SideJamb-A', meters: heightMeters },
      { role: 'SideJamb-B', meters: heightMeters },
      { role: 'Sash', meters: 2 * heightMeters * panelCount },
      { role: 'Mullion', meters: heightMeters * Math.max(0, panelCount - 1) },
    ];

    let runProfileWeightKg = 0;
    let runProfileCost = 0;
    for (const segment of profileLines) {
      const weight = segment.meters * weightKgPerMeter;
      const unitCost = pricePerKg * priceModifier * weightKgPerMeter;
      const lineCost = segment.meters * unitCost;
      runProfileWeightKg += weight;
      runProfileCost += lineCost;
      lines.push({
        kind: 'ProfileCut',
        description: `${run.label} · ${segment.role}`,
        quantity: segment.meters,
        unit: 'm',
        unitCost,
        lineCost,
        source: run.id,
      });
    }
    materials += runProfileCost;
    totalWeightKg += runProfileWeightKg;

    for (const panel of run.panels) {
      const glass = glassById.get(panel.glassTypeId);
      const panelAreaM2 = (panel.widthMm / 1000) * heightMeters;
      totalAreaM2 += panelAreaM2;
      totalPanels += 1;
      if (glass) {
        const panelCost = panelAreaM2 * glass.pricePerM2;
        glassCost += panelCost;
        totalWeightKg += panelAreaM2 * glass.weightKgPerM2;
        lines.push({
          kind: 'GlassPiece',
          description: `${run.label} · ${t(`panel-${panel.panelIndex + 1}`)} · ${glass.name}`,
          quantity: panelAreaM2,
          unit: 'm²',
          unitCost: glass.pricePerM2,
          lineCost: panelCost,
          source: panel.id,
        });
      }
    }

    const kits = hardwareKitsBySystem.get(run.profileSystemId) ?? [];
    for (const kit of kits) {
      for (const kitItem of kit.items) {
        const hardware = hardwareById.get(kitItem.hardwareItemId);
        if (!hardware) continue;
        const variables = {
          panel_count: panelCount,
          run_length_mm: Math.round(developedMm),
          run_height_mm: run.heightMm,
          opening_count_folding: run.panels.filter((p) => p.openingType === 'Folding').length,
          opening_count_sliding: run.panels.filter(
            (p) => p.openingType === 'SlidingLeft' || p.openingType === 'SlidingRight',
          ).length,
          opening_count_hinged: run.panels.filter((p) => p.openingType === 'Hinged').length,
          glass_thickness_mm: run.panels[0]?.glassTypeId
            ? (glassById.get(run.panels[0].glassTypeId)?.thicknessMm ?? 0)
            : 0,
        };
        const qty = safeEvalFormula(kitItem.quantityFormula, variables);
        if (qty <= 0) continue;
        const lineCost = qty * hardware.unitPrice;
        hardwareCost += lineCost;
        lines.push({
          kind: 'HardwarePiece',
          description: `${run.label} · ${kit.name} · ${hardware.name}`,
          quantity: qty,
          unit: hardware.unit,
          unitCost: hardware.unitPrice,
          lineCost,
          source: run.id,
        });
      }
    }
  }

  const waste = (materials + glassCost) * wastePercent;
  const labor = totalAreaM2 * laborPerM2;
  const floor = input.floorNumber ?? 0;
  const scaffolding = floor >= scaffoldingFromFloor ? totalAreaM2 * scaffoldingRate : 0;
  const crane = floor >= craneFromFloor ? floor * 3 * craneRate : 0;
  const transport = totalWeightKg * transportPerKg + transportPerKm;

  if (waste > 0)
    lines.push({
      kind: 'Other',
      description: 'Waste allowance',
      quantity: 1,
      unit: '%',
      unitCost: waste,
      lineCost: waste,
    });
  if (labor > 0)
    lines.push({
      kind: 'Labor',
      description: 'Workshop labor',
      quantity: totalAreaM2,
      unit: 'm²',
      unitCost: laborPerM2,
      lineCost: labor,
    });
  if (scaffolding > 0)
    lines.push({
      kind: 'Installation',
      description: 'Scaffolding',
      quantity: totalAreaM2,
      unit: 'm²',
      unitCost: scaffoldingRate,
      lineCost: scaffolding,
    });
  if (crane > 0)
    lines.push({
      kind: 'Installation',
      description: 'Crane',
      quantity: floor * 3,
      unit: 'm',
      unitCost: craneRate,
      lineCost: crane,
    });
  if (transport > 0)
    lines.push({
      kind: 'Transport',
      description: 'Transport',
      quantity: 1,
      unit: 'trip',
      unitCost: transport,
      lineCost: transport,
    });

  const totalBaseCost =
    materials + glassCost + hardwareCost + waste + labor + scaffolding + crane + transport;
  const margin = totalBaseCost * marginPercent;
  const afterMargin = totalBaseCost + margin;
  const discount = afterMargin * discountPercent;
  const taxBase = afterMargin - discount;
  const taxAmount = taxBase * taxRate;
  const grandTotal = taxBase + taxAmount;

  return {
    materials,
    glass: glassCost,
    hardware: hardwareCost,
    waste,
    labor,
    scaffolding,
    crane,
    transport,
    totalBaseCost,
    margin,
    taxBase,
    taxAmount,
    grandTotal,
    currency,
    totalAreaM2,
    totalPanels,
    totalWeightKg,
    lines,
  };
}

const FORMULA_TOKENS = /^[\d\s+\-*/().,a-zA-Z_]+$/;

function safeEvalFormula(formula: string, variables: Record<string, number>): number {
  if (!formula || !FORMULA_TOKENS.test(formula)) return 0;
  try {
    const symbols = Object.keys(variables);
    const values = symbols.map((s) => variables[s]);
    const helpers = {
      ceil: Math.ceil,
      floor: Math.floor,
      round: Math.round,
      max: Math.max,
      min: Math.min,
      abs: Math.abs,
    };
    const helperNames = Object.keys(helpers);
    const helperValues = helperNames.map((h) => helpers[h as keyof typeof helpers]);
    const factory = new Function(
      ...symbols,
      ...helperNames,
      `"use strict"; return Math.max(0, Math.ceil(${formula}));`,
    );
    const result = factory(...values, ...helperValues);
    return Number.isFinite(result) ? result : 0;
  } catch {
    return 0;
  }
}

function t(value: string): string {
  return value;
}
