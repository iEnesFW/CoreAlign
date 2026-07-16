import type { HardwareCategoryKind } from './glassEnclosure.types';
import type { PanelHardwareInput, SceneHardwareItem, SceneHardwareKind } from './project.types';

const KIND_TO_CATEGORY: Record<SceneHardwareKind, HardwareCategoryKind> = {
  Handle: 'Handle',
  PullHandle: 'Handle',
  Lock: 'Lock',
  Hinge: 'Hinge',
  Roller: 'Roller',
  Stopper: 'Bumper',
  CornerJoint: 'CornerPost',
  GasketStrip: 'Gasket',
  DripProfile: 'DripCap',
  Vent: 'Other',
  Louver: 'Other',
  Bracket: 'WallBracket',
  Accessory: 'Other',
};

// Map a render kind to the catalog category so "quick add by kind" can auto-link a real catalog
// item — otherwise the placed hardware has no hardwareItemId and never reaches the BOM/quote.
export const sceneHardwareKindToCategory = (kind: SceneHardwareKind): HardwareCategoryKind =>
  KIND_TO_CATEGORY[kind] ?? 'Other';

export const aggregatePanelHardware = (items: SceneHardwareItem[]): PanelHardwareInput[] => {
  const totals = new Map<string, number>();
  for (const item of items) {
    const id = item.hardwareItemId;
    if (!id) continue;
    const quantity = item.quantity ?? 1;
    if (quantity <= 0) continue;
    totals.set(id, (totals.get(id) ?? 0) + quantity);
  }
  return [...totals].map(([hardwareItemId, quantity]) => ({ hardwareItemId, quantity }));
};

interface CatalogHardwareRef {
  id: string;
  category: HardwareCategoryKind;
}

interface PanelHardwareSource {
  hardware: SceneHardwareItem[];
  hasHandle?: boolean;
  hasLock?: boolean;
  hasBrushSeal?: boolean;
}

type PanelHardwareBoolKey = 'hasHandle' | 'hasLock' | 'hasBrushSeal';

// WHY: map each fitting bool straight to its catalog CATEGORY — a brush seal is category 'Brush',
// distinct from a gasket strip's 'Gasket'; routing hasBrushSeal through the GasketStrip kind collapsed
// both into the Gasket bucket, so a panel with a gasket strip silently dropped its brush seal.
const BOOL_HARDWARE_CATEGORIES: ReadonlyArray<
  readonly [PanelHardwareBoolKey, HardwareCategoryKind]
> = [
  ['hasHandle', 'Handle'],
  ['hasLock', 'Lock'],
  ['hasBrushSeal', 'Brush'],
];

// WHY: panel hardware reaches the BOM from two models — explicit SceneHardwareItem objects AND
// the quick has* fitting bools (render-only, never catalog-linked → never quoted). Fold each true
// bool into a quoted piece via its category's first catalog item, skipping it when an explicit
// object of that same category is already quoted (a handle placed both ways is one handle, not two).
export const combinePanelHardware = (
  panel: PanelHardwareSource,
  catalog: readonly CatalogHardwareRef[],
): PanelHardwareInput[] => {
  const categoryById = new Map(catalog.map((h) => [h.id, h.category]));
  const totals = new Map<string, number>();
  for (const item of panel.hardware) {
    const id = item.hardwareItemId;
    if (!id) continue;
    const quantity = item.quantity ?? 1;
    if (quantity <= 0) continue;
    totals.set(id, (totals.get(id) ?? 0) + quantity);
  }
  const quotedCategories = new Set<HardwareCategoryKind>();
  for (const id of totals.keys()) {
    const category = categoryById.get(id);
    if (category) quotedCategories.add(category);
  }
  for (const [flag, category] of BOOL_HARDWARE_CATEGORIES) {
    if (!panel[flag]) continue;
    if (quotedCategories.has(category)) continue;
    const match = catalog.find((h) => h.category === category);
    if (!match) continue;
    totals.set(match.id, 1);
  }
  return [...totals].map(([hardwareItemId, quantity]) => ({ hardwareItemId, quantity }));
};
