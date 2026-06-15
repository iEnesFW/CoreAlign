import type { ScenePanelState } from './project.types';

export const createPanelFromTemplate = (
  template: ScenePanelState | undefined,
  fallbackGlassTypeId: string,
): Omit<ScenePanelState, 'panelIndex'> => ({
  id: crypto.randomUUID(),
  widthMm: template?.widthMm ?? 600,
  openingType: template?.openingType ?? 'Fixed',
  glassTypeId: template?.glassTypeId ?? fallbackGlassTypeId,
  hasHandle: false,
  hasLock: false,
  hasBrushSeal: false,
  hardware: [],
});
