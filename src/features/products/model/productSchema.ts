import { z } from 'zod';

export const productSchema = z.object({
  sku: z
    .string()
    .min(1, { message: 'Validation.Required' })
    .max(64, { message: 'Validation.TooLong' }),
  name: z
    .string()
    .min(2, { message: 'Validation.NameTooShort' })
    .max(200, { message: 'Validation.NameTooLong' }),
  description: z.string().max(2000, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  unit: z
    .string()
    .min(1, { message: 'Validation.Required' })
    .max(20, { message: 'Validation.TooLong' }),
  price: z.number({ message: 'Validation.Required' }).min(0, { message: 'Validation.NonNegative' }),
  currency: z
    .string()
    .length(3, { message: 'Validation.CurrencyLength' })
    .regex(/^[A-Z]{3}$/, { message: 'Validation.CurrencyFormat' }),
  stockQuantity: z
    .number({ message: 'Validation.Required' })
    .min(0, { message: 'Validation.NonNegative' }),
  isActive: z.boolean(),
});

export type ProductFormValues = z.infer<typeof productSchema>;
