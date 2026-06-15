import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Package, ShoppingBag } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { EmptyState } from '@/shared/ui/EmptyState/EmptyState';
import { TableSkeleton } from '@/shared/ui/Skeleton/Skeleton';
import { CartDrawer } from '@/features/billing/ui/CartDrawer';
import { ModuleGroup } from '@/features/billing/ui/ModuleGroup';
import { ActiveModulesBanner } from '@/features/billing/ui/ActiveModulesBanner';
import { useActiveModulesQuery, useModulesCatalogQuery } from '@/features/billing/hooks/useBilling';
import { useIsTenantAdmin } from '@/features/billing/hooks/useIsTenantAdmin';
import type {
  CartLine,
  ModuleDto,
  ModulePricePlanDto,
  TenantModuleDto,
} from '@/features/billing/model/billing.types';

const UNCATEGORIZED_KEY = '__uncategorized__';

const groupByCategory = (modules: ModuleDto[]) => {
  const groups = new Map<string, ModuleDto[]>();
  for (const m of modules) {
    const key = m.category && m.category.trim() ? m.category : UNCATEGORIZED_KEY;
    const arr = groups.get(key) ?? [];
    arr.push(m);
    groups.set(key, arr);
  }
  for (const arr of groups.values()) {
    arr.sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name));
  }
  return groups;
};

export const ModulesCatalogPage = () => {
  const { t } = useTranslation();
  const isAdmin = useIsTenantAdmin();
  const catalogQuery = useModulesCatalogQuery();
  const activeQuery = useActiveModulesQuery();

  const [cart, setCart] = useState<CartLine[]>([]);
  const [drawerOpen, setDrawerOpen] = useState(false);

  const modules = useMemo(() => catalogQuery.data?.data ?? [], [catalogQuery.data]);
  const activeModules = useMemo(() => activeQuery.data?.data ?? [], [activeQuery.data]);

  const activeByCode = useMemo(() => {
    const map = new Map<string, TenantModuleDto>();
    for (const m of activeModules) map.set(m.code.toLowerCase(), m);
    return map;
  }, [activeModules]);

  const cartByModuleId = useMemo(() => {
    const map = new Map<string, string>();
    for (const line of cart) map.set(line.module.id, line.plan.id);
    return map;
  }, [cart]);

  const grouped = useMemo(() => groupByCategory(modules), [modules]);

  const handleAddToCart = (module: ModuleDto, plan: ModulePricePlanDto) => {
    setCart((prev) => {
      const filtered = prev.filter((l) => l.module.id !== module.id);
      return [...filtered, { module, plan }];
    });
    setDrawerOpen(true);
  };

  const handleRemove = (moduleId: string) =>
    setCart((prev) => prev.filter((l) => l.module.id !== moduleId));

  const handleClear = () => setCart([]);

  const isLoading = catalogQuery.isPending || activeQuery.isPending;
  const hasError = catalogQuery.isError;

  return (
    <div className="space-y-4 p-4">
      <PageHeader
        icon={<Package size={20} />}
        eyebrow={t('billing.eyebrow')}
        title={t('billing.modules.title')}
        subtitle={t('billing.modules.subtitle')}
        tone="indigo"
        actions={
          <button
            type="button"
            onClick={() => setDrawerOpen(true)}
            className="inline-flex items-center gap-1.5 rounded-lg bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white shadow-sm hover:bg-indigo-700"
          >
            <ShoppingBag size={13} />
            {t('billing.cart.openButton', { count: cart.length })}
          </button>
        }
      />

      {!isLoading && activeModules.length >= 0 && <ActiveModulesBanner modules={activeModules} />}

      {hasError && (
        <QueryError
          onRetry={() => catalogQuery.refetch()}
          isRetrying={catalogQuery.isFetching}
          title={t('billing.errors.catalogTitle')}
          description={t('billing.errors.catalogDescription')}
        />
      )}

      {isLoading && !hasError && <TableSkeleton rows={3} columns={4} />}

      {!isLoading && !hasError && modules.length === 0 && (
        <EmptyState
          icon={<Package size={22} />}
          title={t('billing.modules.emptyTitle')}
          description={t('billing.modules.emptyDescription')}
        />
      )}

      {!isLoading && !hasError && modules.length > 0 && (
        <div className="space-y-5">
          {Array.from(grouped.entries())
            .sort(([a], [b]) => {
              if (a === UNCATEGORIZED_KEY) return 1;
              if (b === UNCATEGORIZED_KEY) return -1;
              return a.localeCompare(b);
            })
            .map(([category, list]) => (
              <ModuleGroup
                key={category}
                title={
                  category === UNCATEGORIZED_KEY ? t('billing.modules.uncategorized') : category
                }
                modules={list}
                activeByCode={activeByCode}
                cartByModuleId={cartByModuleId}
                canPurchase={isAdmin}
                onAddToCart={handleAddToCart}
              />
            ))}
        </div>
      )}

      <CartDrawer
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        items={cart}
        canPurchase={isAdmin}
        onRemove={handleRemove}
        onClear={handleClear}
      />
    </div>
  );
};

export default ModulesCatalogPage;
