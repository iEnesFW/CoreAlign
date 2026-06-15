import { useMemo } from 'react';
import { useActiveModulesQuery } from './useBilling';
import type { TenantModuleDto } from '../model/billing.types';

interface UseActiveModulesResult {
  modules: TenantModuleDto[];
  isActive: (code: string) => boolean;
  isLoading: boolean;
}

export const useActiveModules = (): UseActiveModulesResult => {
  const query = useActiveModulesQuery();
  const modules = useMemo(() => query.data?.data ?? [], [query.data]);

  const activeCodes = useMemo(() => {
    const set = new Set<string>();
    for (const m of modules) {
      if (m.isCurrentlyActive) set.add(m.code.toLowerCase());
    }
    return set;
  }, [modules]);

  const isActive = useMemo(
    () => (code: string) => activeCodes.has(code.toLowerCase()),
    [activeCodes],
  );

  return {
    modules,
    isActive,
    isLoading: query.isPending,
  };
};
