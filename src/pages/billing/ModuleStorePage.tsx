import { LayoutGrid, PackageOpen } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import {
  useActiveModulesQuery,
  useCreateSubscriptionOrder,
  useModulesCatalogQuery,
  usePaymentGatewaysQuery,
} from '@/features/billing/hooks/useBilling';
import { useCartStore } from '@/features/billing/model/cartStore';
import type { BillingCycle } from '@/features/billing/model/moduleStore';
import {
  buildGroups,
  buildLines,
  cartCurrency,
  cartTotal,
  hasMixedCurrency,
} from '@/features/billing/model/moduleStore';
import type { SubscriptionBillingInfoInput } from '@/features/billing/model/billing.types';
import { EMPTY_BILLING_INFO, validateBillingInfo } from '@/features/billing/model/billingInfo';
import { BillingInfoForm } from '@/features/billing/ui/BillingInfoForm';
import { ModuleStoreGrid } from '@/features/billing/ui/ModuleStoreGrid';
import { OrderSummaryPanel } from '@/features/billing/ui/OrderSummaryPanel';
import { useIsTenantAdmin } from '@/shared/lib/auth/useIsTenantAdmin';
import { toastApiError } from '@/shared/lib/mutationToast';
import { newOperationId } from '@/shared/lib/operationId';
import { Button } from '@/shared/ui/Button/Button';
import { EmptyState } from '@/shared/ui/EmptyState/EmptyState';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { Stepper } from '@/shared/ui/Stepper/Stepper';

type Step = 'select' | 'payment';

export const ModuleStorePage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const canPurchase = useIsTenantAdmin();

  const catalogQuery = useModulesCatalogQuery();
  const activeQuery = useActiveModulesQuery();
  const gatewaysQuery = usePaymentGatewaysQuery();
  const createOrder = useCreateSubscriptionOrder();

  const cycle = useCartStore((s) => s.cycle);
  const entries = useCartStore((s) => s.entries);
  const hydrate = useCartStore((s) => s.hydrate);
  const setCycle = useCartStore((s) => s.setCycle);
  const toggle = useCartStore((s) => s.toggle);
  const removeLine = useCartStore((s) => s.remove);
  const clearCart = useCartStore((s) => s.clear);

  const [step, setStep] = useState<Step>('select');
  const [billingInfo, setBillingInfo] = useState<SubscriptionBillingInfoInput>(EMPTY_BILLING_INFO);
  const [errors, setErrors] = useState<Partial<Record<keyof SubscriptionBillingInfoInput, string>>>(
    {},
  );
  const [gatewayName, setGatewayName] = useState<string | null>(null);

  useEffect(() => {
    hydrate();
  }, [hydrate]);

  // Derive inside useMemo and depend on the raw query object: `data?.data ?? []` produces a new
  // array identity on every render and trips exhaustive-deps at --max-warnings=0.
  const modules = useMemo(() => catalogQuery.data?.data ?? [], [catalogQuery.data]);
  const activeModules = useMemo(() => activeQuery.data?.data ?? [], [activeQuery.data]);
  const gateways = useMemo(() => gatewaysQuery.data?.data ?? [], [gatewaysQuery.data]);

  const groups = useMemo(
    () => buildGroups(modules, activeModules, cycle, entries),
    [modules, activeModules, cycle, entries],
  );
  const lines = useMemo(
    () => buildLines(modules, activeModules, entries),
    [modules, activeModules, entries],
  );

  const total = cartTotal(lines);
  const currency = cartCurrency(lines);
  const mixedCurrency = hasMixedCurrency(lines);

  const effectiveGateway =
    gatewayName ?? gateways.find((g) => g.isDefault)?.name ?? gateways[0]?.name ?? null;

  const isLoading = catalogQuery.isLoading || activeQuery.isLoading;
  const sellableCount = groups.flatMap((g) => g.modules).filter((m) => !m.module.isCore).length;

  const handleSubmit = () => {
    const validation = validateBillingInfo(billingInfo);
    setErrors(validation);
    if (Object.keys(validation).length > 0) return;

    createOrder.mutate(
      {
        items: lines.map((l) => ({ moduleId: l.moduleId, planId: l.planId })),
        gatewayName: effectiveGateway,
        billingInfo,
        // A fresh id per click: a retry of THIS submit replays the same order instead of
        // burning a second order number and opening a second payment intent.
        operationId: newOperationId(),
      },
      {
        onSuccess: (response) => {
          const result = response.data;
          if (!result) return;
          clearCart();
          // The card is entered on the gateway's own page — CoreAlign never sees it.
          if (result.redirectUrl) {
            if (result.redirectUrl.startsWith('/')) navigate(result.redirectUrl);
            else window.location.assign(result.redirectUrl);
            return;
          }
          navigate(`/dashboard/billing/orders/${result.order.id}`);
        },
        onError: (error) => toastApiError(error),
      },
    );
  };

  return (
    <div className="ca-page-bg min-h-full p-4 sm:p-6" data-testid="module-store-page">
      <PageHeader
        icon={<LayoutGrid size={18} />}
        title={t('billing.store.title')}
        subtitle={t('billing.store.subtitle')}
        actions={
          <Button variant="ghost" size="sm" onClick={() => navigate('/dashboard/billing')}>
            {t('billing.store.backToOverview')}
          </Button>
        }
      />

      <div className="mt-5 grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_360px]">
        <section className="min-w-0 space-y-5">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <Stepper
              steps={[
                { id: 'select', label: t('billing.store.stepSelect') },
                { id: 'payment', label: t('billing.store.stepPayment') },
              ]}
              current={step}
              onStepClick={(id) => setStep(id as Step)}
            />

            {step === 'select' && (
              <div
                className="inline-flex rounded-lg border border-slate-200 p-0.5 dark:border-white/10"
                role="group"
                aria-label={t('billing.store.cycle')}
              >
                {(['monthly', 'yearly'] as BillingCycle[]).map((option) => (
                  <button
                    key={option}
                    type="button"
                    data-testid="cycle-option"
                    aria-pressed={cycle === option}
                    onClick={() => setCycle(option)}
                    className={
                      cycle === option
                        ? 'rounded-md bg-primary-600 px-3 py-1 text-xs font-medium text-white'
                        : 'rounded-md px-3 py-1 text-xs font-medium text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800'
                    }
                  >
                    {t(`billing.store.cycle_${option}`)}
                  </button>
                ))}
              </div>
            )}
          </div>

          {step === 'select' ? (
            isLoading ? (
              <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
                {Array.from({ length: 6 }).map((_, i) => (
                  <div key={i} className="ca-skeleton h-28 rounded-xl" />
                ))}
              </div>
            ) : sellableCount === 0 ? (
              <EmptyState
                icon={<PackageOpen size={22} />}
                title={t('billing.store.emptyCatalog')}
                description={t('billing.store.emptyCatalogHint')}
              />
            ) : (
              <ModuleStoreGrid groups={groups} canPurchase={canPurchase} onToggle={toggle} />
            )
          ) : (
            <BillingInfoForm
              value={billingInfo}
              errors={errors}
              gateways={gateways}
              gatewayName={effectiveGateway}
              disabled={createOrder.isPending}
              onChange={(patch) => setBillingInfo((prev) => ({ ...prev, ...patch }))}
              onGatewayChange={setGatewayName}
            />
          )}
        </section>

        <aside className="lg:sticky lg:top-20">
          <OrderSummaryPanel
            step={step}
            lines={lines}
            currency={currency}
            total={total}
            canPurchase={canPurchase}
            isSubmitting={createOrder.isPending}
            mixedCurrency={mixedCurrency}
            onRemove={removeLine}
            onClear={clearCart}
            onNext={() => setStep('payment')}
            onBack={() => setStep('select')}
            onSubmit={handleSubmit}
          />
        </aside>
      </div>
    </div>
  );
};

export default ModuleStorePage;
