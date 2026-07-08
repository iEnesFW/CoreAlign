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
