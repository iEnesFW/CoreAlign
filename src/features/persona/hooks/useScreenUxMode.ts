import { useUxMode } from './useUxMode';
import { usePersonaStore } from '../model/personaStore';
import type { UxComplexityMode } from '../model/personaStore';

export const useScreenUxMode = (screenKey: string): UxComplexityMode => {
  const baseMode = useUxMode();
  const override = usePersonaStore((s) => s.perScreenOverrides[screenKey]);
  return override ?? baseMode;
};

export const useScreenOverride = (screenKey: string): UxComplexityMode | null => {
  return usePersonaStore((s) => s.perScreenOverrides[screenKey] ?? null);
};
