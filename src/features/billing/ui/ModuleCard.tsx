import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { CheckCircle2, Lock, Plus, Sparkles } from 'lucide-react';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { cn } from '@/shared/lib/cn';
import type { ModuleDto, ModulePricePlanDto, TenantModuleDto } from '../model/billing.types';
import { ModuleIcon } from './ModuleIcon';

interface Props {
  module: ModuleDto;
  activeSubscription?: TenantModuleDto;
  inCartPlanId?: string | null;
  canPurchase: boolean;
  onAddToCart: (module: ModuleDto, plan: ModulePricePlanDto) => void;
}

const MS_PER_DAY = 1000 * 60 * 60 * 24;

const computeDaysLeft = (endUtc: string | null | undefined): number | null => {
  if (!endUtc) return null;
  const end = new Date(endUtc).getTime();
  if (Number.isNaN(end)) return null;
  const diff = end - Date.now();
  return Math.max(0, Math.ceil(diff / MS_PER_DAY));
};

export const ModuleCard = ({
  module,
  activeSubscription,
  inCartPlanId,
  canPurchase,
  onAddToCart,
}: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();

  const visiblePlans = useMemo(
    () => module.plans.filter((p) => p.isActive).sort((a, b) => a.sortOrder - b.sortOrder),
    [module.plans],
  );

  const initialPlanId = inCartPlanId ?? visiblePlans[0]?.id ?? null;
  const [selectedPlanId, setSelectedPlanId] = useState<string | null>(initialPlanId);
  const selectedPlan = visiblePlans.find((p) => p.id === selectedPlanId) ?? visiblePlans[0] ?? null;

  const daysLeft = computeDaysLeft(activeSubscription?.endUtc);
  const isCurrentlyActive = !!activeSubscription?.isCurrentlyActive;
  const isCore = module.isCore;
  const isRenewing = isCurrentlyActive && !isCore;

  const handleAdd = () => {
    if (!selectedPlan || !canPurchase || isCore) return;
    onAddToCart(module, selectedPlan);
  };

  const addDisabled = isCore || !canPurchase || !selectedPlan;
  const isInCart = !!inCartPlanId && selectedPlan?.id === inCartPlanId;

  return (
    <div className="flex h-full flex-col rounded-xl border border-slate-200/70 bg-white p-3 shadow-sm transition-shadow hover:shadow-md dark:border-slate-800/70 dark:bg-slate-900">
      <div className="flex items-start gap-3">
        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 text-white shadow-md shadow-indigo-500/20">
          <ModuleIcon iconKey={module.iconKey} size={18} />
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex items-start justify-between gap-2">
            <h3 className="truncate text-sm font-semibold text-slate-900 dark:text-slate-100">
              {module.name}
            </h3>
            {isCore && (
              <span className="inline-flex shrink-0 items-center gap-1 rounded-full bg-emerald-50 px-1.5 py-0.5 text-[10px] font-semibold text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-300">
                <Sparkles size={10} />
                {t('billing.modules.coreLabel')}
              </span>
            )}
          </div>
          {module.category && (
            <p className="mt-0.5 text-[10px] font-medium uppercase tracking-wider text-slate-400 dark:text-slate-500">
              {module.category}
            </p>
          )}
        </div>
      </div>

      {module.description && (
        <p className="mt-2 line-clamp-3 text-xs text-slate-600 dark:text-slate-400">
          {module.description}
        </p>
      )}

      <div className="mt-3 rounded-lg border border-slate-100 bg-slate-50/60 px-2.5 py-1.5 text-[11px] dark:border-slate-800 dark:bg-slate-800/40">
        {isCore ? (
          <span className="inline-flex items-center gap-1 font-medium text-emerald-700 dark:text-emerald-300">
            <CheckCircle2 size={12} />
            {t('billing.modules.coreAlwaysOn')}
          </span>
        ) : isCurrentlyActive ? (
          <span className="inline-flex flex-wrap items-center gap-1 font-medium text-emerald-700 dark:text-emerald-300">
            <CheckCircle2 size={12} />
            <span>
              {t('billing.modules.activeUntil', {
                date: activeSubscription?.endUtc
                  ? formatDate(activeSubscription.endUtc, locale)
                  : '—',
              })}
            </span>
            {daysLeft !== null && (
              <span className="text-slate-500 dark:text-slate-400">
                · {t('billing.modules.daysLeft', { count: daysLeft })}
              </span>
            )}
          </span>
        ) : (
          <span className="text-slate-500 dark:text-slate-400">
            {t('billing.modules.notSubscribed')}
          </span>
        )}
      </div>

      {!isCore && visiblePlans.length > 0 && (
        <div className="mt-3 flex flex-wrap gap-1.5">
          {visiblePlans.map((plan) => {
            const isSelected = selectedPlanId === plan.id;
            return (
              <button
                key={plan.id}
                type="button"
                onClick={() => setSelectedPlanId(plan.id)}
                className={cn(
                  'flex flex-col items-start gap-0.5 rounded-lg border px-2.5 py-1.5 text-left text-[11px] transition-all',
                  isSelected
                    ? 'border-indigo-400 bg-indigo-50 text-indigo-700 ring-1 ring-indigo-300 dark:border-indigo-500/60 dark:bg-indigo-500/10 dark:text-indigo-200'
                    : 'border-slate-200 bg-white text-slate-600 hover:border-indigo-300 hover:bg-indigo-50/40 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300 dark:hover:border-indigo-500/40',
                )}
              >
                <span className="font-semibold">{plan.displayLabel}</span>
                <span className="text-[10px] tabular-nums">
                  {formatCurrency(plan.price, locale, plan.currency)}
                </span>
                <span className="text-[10px] text-slate-400 dark:text-slate-500">
                  {t('billing.modules.durationDays', { count: plan.durationDays })}
                </span>
              </button>
            );
          })}
        </div>
      )}

      {isRenewing && selectedPlan && (
        <p className="mt-2 rounded-md bg-amber-50 px-2 py-1 text-[11px] text-amber-700 dark:bg-amber-500/10 dark:text-amber-300">
          {t('billing.modules.renewalHint', {
            duration: t('billing.modules.durationDays', { count: selectedPlan.durationDays }),
          })}
        </p>
      )}

      <div className="mt-auto pt-3">
        {isCore ? (
          <button
            type="button"
            disabled
            className="inline-flex w-full items-center justify-center gap-1.5 rounded-lg border border-dashed border-slate-300 px-3 py-1.5 text-xs font-medium text-slate-400 dark:border-slate-700 dark:text-slate-500"
          >
            <Lock size={12} />
            {t('billing.modules.coreNoPurchase')}
          </button>
        ) : (
          <button
            type="button"
            onClick={handleAdd}
            disabled={addDisabled}
            title={!canPurchase ? t('billing.cart.adminOnly') : undefined}
            className={cn(
              'inline-flex w-full items-center justify-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-semibold transition-colors',
              isInCart
                ? 'bg-emerald-600 text-white hover:bg-emerald-700 disabled:bg-emerald-600/60'
                : 'bg-indigo-600 text-white hover:bg-indigo-700 disabled:cursor-not-allowed disabled:bg-slate-300 disabled:text-slate-500 dark:disabled:bg-slate-700',
            )}
          >
            {isInCart ? (
              <>
                <CheckCircle2 size={12} />
                {t('billing.modules.inCart')}
              </>
            ) : (
              <>
                <Plus size={12} />
                {isCurrentlyActive ? t('billing.modules.renew') : t('billing.modules.addToCart')}
              </>
            )}
          </button>
        )}
      </div>
    </div>
  );
};
