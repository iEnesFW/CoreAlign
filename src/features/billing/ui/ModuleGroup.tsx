import type { ModuleDto, ModulePricePlanDto, TenantModuleDto } from '../model/billing.types';
import { ModuleCard } from './ModuleCard';

interface Props {
  title: string;
  modules: ModuleDto[];
  activeByCode: Map<string, TenantModuleDto>;
  cartByModuleId: Map<string, string>;
  canPurchase: boolean;
  onAddToCart: (module: ModuleDto, plan: ModulePricePlanDto) => void;
}

export const ModuleGroup = ({
  title,
  modules,
  activeByCode,
  cartByModuleId,
  canPurchase,
  onAddToCart,
}: Props) => {
  if (modules.length === 0) return null;
  return (
    <section className="space-y-2">
      <h2 className="text-[11px] font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        {title}
      </h2>
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
        {modules.map((module) => (
          <ModuleCard
            key={module.id}
            module={module}
            activeSubscription={activeByCode.get(module.code.toLowerCase())}
            inCartPlanId={cartByModuleId.get(module.id) ?? null}
            canPurchase={canPurchase}
            onAddToCart={onAddToCart}
          />
        ))}
      </div>
    </section>
  );
};
