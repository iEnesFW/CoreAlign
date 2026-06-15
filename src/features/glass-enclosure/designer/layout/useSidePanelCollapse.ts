import { useState } from 'react';
import { usePersistedState } from '@/shared/hooks/usePersistedState';

export interface SidePanelCollapse {
  collapsed: boolean;
  setCollapsed: (next: boolean) => void;
}

export const useSidePanelCollapse = (
  storageKey: string,
  defaultCollapsed: boolean,
): SidePanelCollapse => {
  const [preferred, setPreferred] = usePersistedState<boolean>(storageKey, false);
  const [override, setOverride] = useState<boolean | null>(null);
  const [lastDefaultCollapsed, setLastDefaultCollapsed] = useState(defaultCollapsed);

  if (lastDefaultCollapsed !== defaultCollapsed) {
    setLastDefaultCollapsed(defaultCollapsed);
    setOverride(null);
  }

  const collapsed = override !== null ? override : defaultCollapsed || preferred;

  const setCollapsed = (next: boolean) => {
    if (defaultCollapsed) {
      setOverride(next);
      return;
    }
    setOverride(null);
    setPreferred(next);
  };

  return { collapsed, setCollapsed };
};
