import { z } from 'zod';

export const customerAddressSchema = z.object({
  label: z
    .string()
    .min(1, { message: 'Validation.Required' })
    .max(64, { message: 'Validation.TooLong' }),
  line1: z
    .string()
    .min(1, { message: 'Validation.Required' })
    .max(200, { message: 'Validation.TooLong' }),
  line2: z.string().max(200, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  city: z.string().max(100, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  state: z.string().max(100, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  postalCode: z.string().max(32, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  country: z.string().max(100, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  isPrimary: z.boolean(),
});

export type CustomerAddressFormValues = z.infer<typeof customerAddressSchema>;

export const emptyCustomerAddressForm: CustomerAddressFormValues = {
  label: '',
  line1: '',
  line2: '',
  city: '',
  state: '',
  postalCode: '',
  country: '',
  isPrimary: false,
};
