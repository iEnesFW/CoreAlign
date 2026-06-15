import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, CreditCard, Loader2, Lock } from 'lucide-react';
import { toast } from 'sonner';
import { z } from 'zod';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { cn } from '@/shared/lib/cn';
import { useAuthStore } from '@/features/auth/model/authStore';
import { useCreateSubscriptionOrder, usePaymentGatewaysQuery } from '../hooks/useBilling';
import type {
  CartLine,
  PaymentGatewayDescriptor,
  SubscriptionBillingInfoInput,
} from '../model/billing.types';

interface Props {
  items: CartLine[];
  onBack: () => void;
  onCompleted: () => void;
}

// Messages are i18n keys; the form translates them via t() when rendering an
// error. Keeping the schema at module scope (no t() bound in) sidesteps zod's
// excessive-depth type inference under the strict project tsconfig.
const billingSchema = z.object({
  name: z.string().trim().min(1, 'billing.billingInfo.errors.nameRequired').max(100),
  surname: z.string().trim().min(1, 'billing.billingInfo.errors.surnameRequired').max(100),
  email: z.string().trim().email('billing.billingInfo.errors.emailInvalid').max(256),
  gsmNumber: z
    .string()
    .trim()
    .regex(/^\+?[0-9]{10,15}$/, 'billing.billingInfo.errors.gsmInvalid'),
  identityNumber: z
    .string()
    .trim()
    .regex(/^[A-Za-z0-9]{5,32}$/, 'billing.billingInfo.errors.identityInvalid'),
  address: z.string().trim().min(1, 'billing.billingInfo.errors.addressRequired').max(500),
  city: z.string().trim().min(1, 'billing.billingInfo.errors.cityRequired').max(100),
  country: z.string().trim().min(2, 'billing.billingInfo.errors.countryInvalid').max(100),
  zipCode: z.string().trim().min(1, 'billing.billingInfo.errors.zipRequired').max(32),
});

type BillingFormState = SubscriptionBillingInfoInput;

const EMPTY_FORM: BillingFormState = {
  name: '',
  surname: '',
  email: '',
  gsmNumber: '',
  identityNumber: '',
  address: '',
  city: '',
  country: 'Turkey',
  zipCode: '',
};

export const CheckoutPanel = ({ items, onBack, onCompleted }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const gatewaysQuery = usePaymentGatewaysQuery();
  const createOrder = useCreateSubscriptionOrder();

  const currency = items[0]?.plan.currency ?? 'USD';
  const total = useMemo(() => items.reduce((sum, line) => sum + line.plan.price, 0), [items]);

  const gateways: PaymentGatewayDescriptor[] = gatewaysQuery.data?.data ?? [];
  // Derived default: prefer the registry-marked default, else the first; user
  // selection overrides. Avoids a setState-in-effect "sync prop to state" cycle.
  const defaultGatewayName = gateways.find((g) => g.isDefault)?.name ?? gateways[0]?.name ?? null;
  const [userSelectedGateway, setUserSelectedGateway] = useState<string | null>(null);
  const selectedGateway = userSelectedGateway ?? defaultGatewayName;
  const setSelectedGateway = setUserSelectedGateway;

  const activeGateway = gateways.find((g) => g.name === selectedGateway) ?? null;
  const requiresBillingInfo = activeGateway?.requiresBillingInfo ?? false;

  const [billing, setBilling] = useState<BillingFormState>(() => ({
    ...EMPTY_FORM,
    name: user?.firstName ?? '',
    surname: user?.lastName ?? '',
    email: user?.email ?? '',
  }));
  const [errors, setErrors] = useState<Partial<Record<keyof BillingFormState, string>>>({});

  const updateField = (key: keyof BillingFormState, value: string) => {
    setBilling((prev) => ({ ...prev, [key]: value }));
    if (errors[key]) {
      setErrors((prev) => {
        const next = { ...prev };
        delete next[key];
        return next;
      });
    }
  };

  const submit = () => {
    if (!selectedGateway) {
      toast.error(t('billing.gateway.pickerRequired'));
      return;
    }

    let billingPayload: SubscriptionBillingInfoInput | null = null;
    if (requiresBillingInfo) {
      const parsed = billingSchema.safeParse(billing);
      if (!parsed.success) {
        const fieldErrors: Partial<Record<keyof BillingFormState, string>> = {};
        for (const issue of parsed.error.issues) {
          const path = issue.path[0];
          if (typeof path === 'string') {
            fieldErrors[path as keyof BillingFormState] = t(issue.message as never);
          }
        }
        setErrors(fieldErrors);
        toast.error(t('billing.billingInfo.errors.formInvalid'));
        return;
      }
      billingPayload = parsed.data;
    }

    createOrder.mutate(
      {
        items: items.map((l) => ({ moduleId: l.module.id, planId: l.plan.id })),
        gatewayName: selectedGateway,
        billingInfo: billingPayload,
      },
      {
        onSuccess: (response) => {
          const result = response.data;
          if (!result) {
            toast.error(t('billing.toast.failed'));
            return;
          }
          toast.success(t('billing.toast.created'));
          onCompleted();
          if (result.redirectUrl) {
            const url = result.redirectUrl;
            if (/^https?:\/\//i.test(url)) {
              window.location.href = url;
            } else {
              navigate(url);
            }
            return;
          }
          navigate(`/dashboard/billing/orders/${result.order.id}`);
        },
        onError: (err) => toastApiError(err, t('billing.toast.failed')),
      },
    );
  };

  return (
    <div className="flex h-full flex-col">
      <header className="flex items-center gap-2 border-b border-slate-200/80 bg-slate-50/50 px-4 py-3 dark:border-slate-800/80 dark:bg-slate-900/40">
        <button
          type="button"
          onClick={onBack}
          className="rounded-md p-1.5 text-slate-500 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800 dark:hover:text-slate-200"
          aria-label={t('common.back', { defaultValue: 'Back' })}
        >
          <ArrowLeft size={14} />
        </button>
        <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
          {t('billing.checkout.title')}
        </h2>
      </header>

      <div className="flex-1 overflow-y-auto p-4">
        <section className="mb-5">
          <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
            {t('billing.gateway.title')}
          </h3>
          {gatewaysQuery.isLoading ? (
            <div className="flex items-center gap-2 text-xs text-slate-500">
              <Loader2 size={12} className="animate-spin" />{' '}
              {t('common.loading', { defaultValue: 'Loading...' })}
            </div>
          ) : gateways.length === 0 ? (
            <p className="rounded-md bg-amber-50 px-2 py-2 text-[11px] text-amber-700 dark:bg-amber-500/10 dark:text-amber-300">
              {t('billing.gateway.empty')}
            </p>
          ) : (
            <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
              {gateways.map((g) => (
                <button
                  type="button"
                  key={g.name}
                  onClick={() => setSelectedGateway(g.name)}
                  className={cn(
                    'flex items-center gap-2 rounded-lg border p-3 text-left text-xs transition-colors',
                    selectedGateway === g.name
                      ? 'border-indigo-500 bg-indigo-50 dark:border-indigo-400 dark:bg-indigo-500/10'
                      : 'border-slate-200 bg-white hover:bg-slate-50 dark:border-slate-800 dark:bg-slate-950 dark:hover:bg-slate-900/40',
                  )}
                >
                  <CreditCard size={14} className="text-indigo-500" />
                  <div className="min-w-0">
                    <p className="truncate font-semibold text-slate-900 dark:text-slate-100">
                      {g.name === 'mock'
                        ? t('billing.gateway.mockLabel')
                        : g.name === 'iyzico'
                          ? t('billing.gateway.iyzicoLabel')
                          : g.displayLabel}
                    </p>
                    <p className="truncate text-[10px] text-slate-500 dark:text-slate-400">
                      {g.requiresBillingInfo
                        ? t('billing.gateway.requiresInfo')
                        : t('billing.gateway.devOnly')}
                    </p>
                  </div>
                </button>
              ))}
            </div>
          )}
        </section>

        {requiresBillingInfo && (
          <section>
            <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
              {t('billing.billingInfo.title')}
            </h3>
            <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
              <Field
                label={t('billing.billingInfo.name')}
                value={billing.name}
                error={errors.name}
                onChange={(v) => updateField('name', v)}
              />
              <Field
                label={t('billing.billingInfo.surname')}
                value={billing.surname}
                error={errors.surname}
                onChange={(v) => updateField('surname', v)}
              />
              <Field
                label={t('billing.billingInfo.email')}
                value={billing.email}
                error={errors.email}
                onChange={(v) => updateField('email', v)}
                type="email"
                className="sm:col-span-2"
              />
              <Field
                label={t('billing.billingInfo.gsm')}
                value={billing.gsmNumber}
                error={errors.gsmNumber}
                onChange={(v) => updateField('gsmNumber', v)}
                placeholder="+90..."
              />
              <Field
                label={t('billing.billingInfo.identity')}
                value={billing.identityNumber}
                error={errors.identityNumber}
                onChange={(v) => updateField('identityNumber', v)}
              />
              <Field
                label={t('billing.billingInfo.address')}
                value={billing.address}
                error={errors.address}
                onChange={(v) => updateField('address', v)}
                className="sm:col-span-2"
                multiline
              />
              <Field
                label={t('billing.billingInfo.city')}
                value={billing.city}
                error={errors.city}
                onChange={(v) => updateField('city', v)}
              />
              <Field
                label={t('billing.billingInfo.zip')}
                value={billing.zipCode}
                error={errors.zipCode}
                onChange={(v) => updateField('zipCode', v)}
              />
              <Field
                label={t('billing.billingInfo.country')}
                value={billing.country}
                error={errors.country}
                onChange={(v) => updateField('country', v)}
                className="sm:col-span-2"
              />
            </div>
            <p className="mt-2 flex items-center gap-1 text-[10px] text-slate-500 dark:text-slate-400">
              <Lock size={10} /> {t('billing.billingInfo.privacy')}
            </p>
          </section>
        )}
      </div>

      <footer className="border-t border-slate-200/80 bg-slate-50/40 px-4 py-3 dark:border-slate-800/80 dark:bg-slate-900/40">
        <div className="mb-2 flex items-center justify-between text-sm">
          <span className="font-medium text-slate-600 dark:text-slate-300">
            {t('billing.cart.total')}
          </span>
          <span className="text-base font-bold tabular-nums text-slate-900 dark:text-slate-100">
            {formatCurrency(total, locale, currency)}
          </span>
        </div>
        <button
          type="button"
          onClick={submit}
          disabled={
            !selectedGateway ||
            items.length === 0 ||
            createOrder.isPending ||
            gatewaysQuery.isLoading
          }
          className="inline-flex w-full items-center justify-center gap-1.5 rounded-lg bg-indigo-600 px-3 py-2 text-xs font-semibold text-white transition-colors hover:bg-indigo-700 disabled:cursor-not-allowed disabled:bg-slate-300 disabled:text-slate-500 dark:disabled:bg-slate-700"
        >
          {createOrder.isPending ? (
            <Loader2 size={13} className="animate-spin" />
          ) : (
            <CreditCard size={13} />
          )}
          {t('billing.checkout.payNow')}
        </button>
      </footer>
    </div>
  );
};

interface FieldProps {
  label: string;
  value: string;
  onChange: (v: string) => void;
  error?: string;
  className?: string;
  type?: string;
  placeholder?: string;
  multiline?: boolean;
}

const Field = ({
  label,
  value,
  onChange,
  error,
  className,
  type = 'text',
  placeholder,
  multiline,
}: FieldProps) => (
  <label className={cn('flex flex-col gap-1', className)}>
    <span className="text-[10px] font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
      {label}
    </span>
    {multiline ? (
      <textarea
        rows={2}
        value={value}
        placeholder={placeholder}
        onChange={(e) => onChange(e.target.value)}
        className={cn(
          'rounded-md border bg-white px-2 py-1.5 text-xs text-slate-900 outline-none transition-colors placeholder:text-slate-400 focus:border-indigo-500 dark:bg-slate-900 dark:text-slate-100',
          error
            ? 'border-rose-400 dark:border-rose-500/60'
            : 'border-slate-200 dark:border-slate-800',
        )}
      />
    ) : (
      <input
        type={type}
        value={value}
        placeholder={placeholder}
        onChange={(e) => onChange(e.target.value)}
        className={cn(
          'rounded-md border bg-white px-2 py-1.5 text-xs text-slate-900 outline-none transition-colors placeholder:text-slate-400 focus:border-indigo-500 dark:bg-slate-900 dark:text-slate-100',
          error
            ? 'border-rose-400 dark:border-rose-500/60'
            : 'border-slate-200 dark:border-slate-800',
        )}
      />
    )}
    {error && <span className="text-[10px] text-rose-600 dark:text-rose-400">{error}</span>}
  </label>
);
