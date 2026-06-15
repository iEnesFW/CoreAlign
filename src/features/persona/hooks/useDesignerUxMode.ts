import { useScreenOverride, useScreenUxMode } from './useScreenUxMode';
import type { UxComplexityMode } from '../model/personaStore';

export const DESIGNER_SCREEN_KEY = 'GlassEnclosure.Designer';

export const useDesignerUxMode = (): UxComplexityMode => useScreenUxMode(DESIGNER_SCREEN_KEY);

export const useDesignerScreenOverride = (): UxComplexityMode | null =>
  useScreenOverride(DESIGNER_SCREEN_KEY);
