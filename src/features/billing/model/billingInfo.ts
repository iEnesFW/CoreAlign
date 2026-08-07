import { z } from 'zod';

import type { SubscriptionBillingInfoInput } from './billing.types';

// WHY: iyzico's required buyer fields — deliberately no card data; the card is entered on the gateway's own page.
export const billingInfoSchema = z.object({
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

export const EMPTY_BILLING_INFO: SubscriptionBillingInfoInput = {
  name: '',
  surname: '',
  email: '',
  gsmNumber: '',
  identityNumber: '',
  address: '',
  city: '',
  country: 'Türkiye',
  zipCode: '',
};

export const validateBillingInfo = (
  value: SubscriptionBillingInfoInput,
): Partial<Record<keyof SubscriptionBillingInfoInput, string>> => {
  const parsed = billingInfoSchema.safeParse(value);
  if (parsed.success) return {};
  const errors: Partial<Record<keyof SubscriptionBillingInfoInput, string>> = {};
  for (const issue of parsed.error.issues) {
    const key = issue.path[0] as keyof SubscriptionBillingInfoInput;
    if (!errors[key]) errors[key] = issue.message;
  }
  return errors;
};
