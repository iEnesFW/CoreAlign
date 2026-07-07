import type { PanelHardwareInput, SceneHardwareItem } from './project.types';

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
