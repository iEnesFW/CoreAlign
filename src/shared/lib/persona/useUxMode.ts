import { usePersonaStore } from './personaStore';
import type { UxComplexityMode } from './personaStore';

export const useUxMode = (): UxComplexityMode => usePersonaStore((s) => s.mode);

export const useIsProMode = (): boolean => usePersonaStore((s) => s.mode === 'Pro');

export const useIsSimpleMode = (): boolean => usePersonaStore((s) => s.mode === 'Simple');
