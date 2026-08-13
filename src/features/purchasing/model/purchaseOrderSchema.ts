import { z } from 'zod';

const optionalId = z.string().optional().or(z.literal(''));
const optionalNumeric = z.string().optional().or(z.literal(''));

export const purchaseOrderLineSchema = z.object({
  productId: z.string().min(1, { message: 'Validation.Required' }),
  quantity: z.number({ message: 'Validation.Required' }).gt(0, { message: 'Validation.Positive' }),
  unitCost: z
    .number({ message: 'Validation.Required' })
    .min(0, { message: 'Validation.NonNegative' }),
  taxRatePercent: optionalNumeric,
  lineNotes: z.string().max(500, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
});

export const purchaseOrderSchema = z.object({
  vendorId: z.string().min(1, { message: 'Validation.Required' }),
  orderDate: z.string().min(1, { message: 'Validation.Required' }),
  expectedDate: z.string().optional().or(z.literal('')),
  currency: z
    .string()
    .length(3, { message: 'Validation.CurrencyLength' })
    .regex(/^[A-Z]{3}$/, { message: 'Validation.CurrencyFormat' }),
  exchangeRate: optionalNumeric,
  warehouseId: optionalId,
  notes: z.string().max(2000, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  lines: z.array(purchaseOrderLineSchema).min(1, { message: 'Validation.AtLeastOneLine' }),
});

export type PurchaseOrderFormValues = z.infer<typeof purchaseOrderSchema>;
export type PurchaseOrderLineFormValues = z.infer<typeof purchaseOrderLineSchema>;
