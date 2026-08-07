import { CreditCard, ShieldCheck } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { Input } from '@/shared/ui/Input/Input';
import { Label } from '@/shared/ui/Label/Label';

import type {
  PaymentGatewayDescriptor,
  SubscriptionBillingInfoInput,
} from '../model/billing.types';

interface Props {
  value: SubscriptionBillingInfoInput;
  errors: Partial<Record<keyof SubscriptionBillingInfoInput, string>>;
  gateways: PaymentGatewayDescriptor[];
  gatewayName: string | null;
  disabled: boolean;
  onChange: (patch: Partial<SubscriptionBillingInfoInput>) => void;
  onGatewayChange: (name: string) => void;
}

export const BillingInfoForm = ({
  value,
  errors,
  gateways,
  gatewayName,
  disabled,
  onChange,
  onGatewayChange,
}: Props) => {
  const { t } = useTranslation();

  const field = (
    key: keyof SubscriptionBillingInfoInput,
    labelKey: string,
    type: string = 'text',
  ) => (
    <div>
      <Label htmlFor={`billing-${key}`}>{t(labelKey)}</Label>
      <Input
        id={`billing-${key}`}
        type={type}
        value={value[key] ?? ''}
        disabled={disabled}
        onChange={(e) => onChange({ [key]: e.target.value })}
        aria-invalid={errors[key] ? true : undefined}
      />
      {errors[key] && (
        <p className="mt-1 text-xs text-danger-600 dark:text-danger-400">
          {t(errors[key] as string)}
        </p>
      )}
    </div>
  );

  return (
    <div className="space-y-5">
      <section className="rounded-xl border border-slate-200/70 bg-white p-4 dark:border-white/10 dark:bg-slate-900">
        <h3 className="mb-3 text-sm font-semibold text-slate-900 dark:text-slate-100">
          {t('billing.store.buyerSection')}
        </h3>
        <div className="grid gap-3 sm:grid-cols-2">
          {field('name', 'billing.billingInfo.name')}
          {field('surname', 'billing.billingInfo.surname')}
          {field('email', 'billing.billingInfo.email', 'email')}
          {field('gsmNumber', 'billing.billingInfo.gsm', 'tel')}
          {field('identityNumber', 'billing.billingInfo.identity')}
          {field('zipCode', 'billing.billingInfo.zip')}
          <div className="sm:col-span-2">{field('address', 'billing.billingInfo.address')}</div>
          {field('city', 'billing.billingInfo.city')}
          {field('country', 'billing.billingInfo.country')}
        </div>
      </section>

      <section className="rounded-xl border border-slate-200/70 bg-white p-4 dark:border-white/10 dark:bg-slate-900">
        <h3 className="mb-3 text-sm font-semibold text-slate-900 dark:text-slate-100">
          {t('billing.store.paymentSection')}
        </h3>

        {gateways.length <= 1 ? (
          <div className="flex items-center gap-2 rounded-lg border border-slate-200 px-3 py-2.5 text-sm text-slate-700 dark:border-white/10 dark:text-slate-200">
            <CreditCard size={16} className="text-slate-400" aria-hidden="true" />
            {gateways[0]?.displayLabel ?? t('billing.store.noGateway')}
          </div>
        ) : (
          <fieldset className="space-y-2" disabled={disabled}>
            <legend className="sr-only">{t('billing.store.paymentSection')}</legend>
            {gateways.map((gw) => (
              <label
                key={gw.name}
                className="flex cursor-pointer items-center gap-2 rounded-lg border border-slate-200 px-3 py-2.5 text-sm text-slate-700 hover:border-primary-300 dark:border-white/10 dark:text-slate-200"
              >
                <input
                  type="radio"
                  name="gateway"
                  value={gw.name}
                  checked={gatewayName === gw.name}
                  onChange={() => onGatewayChange(gw.name)}
                  className="accent-primary-600"
                />
                <CreditCard size={16} className="text-slate-400" aria-hidden="true" />
                {gw.displayLabel}
              </label>
            ))}
          </fieldset>
        )}

        <p className="mt-3 flex items-start gap-2 rounded-lg bg-success-50 px-3 py-2.5 text-xs text-success-800 dark:bg-success-500/10 dark:text-success-300">
          <ShieldCheck size={14} className="mt-0.5 shrink-0" aria-hidden="true" />
          <span>{t('billing.store.cardNoticeLong')}</span>
        </p>
      </section>
    </div>
  );
};
