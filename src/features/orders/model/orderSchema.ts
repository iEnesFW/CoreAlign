import { z } from 'zod';

export const orderLineSchema = z.object({
  productId: z.string().min(1, { message: 'Validation.Required' }),
  quantity: z.number({ message: 'Validation.Required' }).gt(0, { message: 'Validation.Positive' }),
  unitPrice: z
    .number({ message: 'Validation.Required' })
    .min(0, { message: 'Validation.NonNegative' }),
});

export const orderSchema = z.object({
  orderNumber: z
    .string()
    .min(1, { message: 'Validation.Required' })
    .max(64, { message: 'Validation.TooLong' }),
  customerId: z.string().min(1, { message: 'Validation.Required' }),
  orderDate: z.string().min(1, { message: 'Validation.Required' }),
  status: z.enum([
    'Draft',
    'Submitted',
    'Approved',
    'Allocated',
    'Picking',
    'Packed',
    'PartiallyShipped',
    'Shipped',
    'Delivered',
    'Closed',
    'Cancelled',
    'Returned',
    'Confirmed',
  ]),
  currency: z
    .string()
    .length(3, { message: 'Validation.CurrencyLength' })
    .regex(/^[A-Z]{3}$/, { message: 'Validation.CurrencyFormat' }),
  notes: z.string().max(2000, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  lines: z.array(orderLineSchema).min(1, { message: 'Validation.AtLeastOneLine' }),
});

export type OrderFormValues = z.infer<typeof orderSchema>;
