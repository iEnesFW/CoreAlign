import { z } from 'zod';

const optionalId = z.string().optional().or(z.literal(''));
const optionalNumeric = z.string().optional().or(z.literal(''));

export const standaloneInvoiceLineSchema = z.object({
  productSku: z.string().min(1, { message: 'Validation.Required' }),
  productName: z.string().min(1, { message: 'Validation.Required' }),
  description: z.string().max(500, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  quantity: z.number({ message: 'Validation.Required' }).gt(0, { message: 'Validation.Positive' }),
  unitPrice: z
    .number({ message: 'Validation.Required' })
    .min(0, { message: 'Validation.NonNegative' }),
  lineDiscountPercent: optionalNumeric,
  taxRatePercent: optionalNumeric,
  withholdingTaxCodeId: optionalId,
});

export const standaloneInvoiceSchema = z.object({
  customerId: z.string().min(1, { message: 'Validation.Required' }),
  issueDate: z.string().min(1, { message: 'Validation.Required' }),
  dueDays: z
    .number({ message: 'Validation.Required' })
    .int({ message: 'Validation.Integer' })
    .min(0, { message: 'Validation.NonNegative' })
    .max(365, { message: 'Validation.TooLong' }),
  currency: z
    .string()
    .length(3, { message: 'Validation.CurrencyLength' })
    .regex(/^[A-Z]{3}$/, { message: 'Validation.CurrencyFormat' }),
  headerDiscountPercent: optionalNumeric,
  shippingCost: optionalNumeric,
  vatExemptionCodeId: optionalId,
  vatExemptionReason: z
    .string()
    .max(500, { message: 'Validation.TooLong' })
    .optional()
    .or(z.literal('')),
  publicNotes: z.string().max(2000, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  internalNotes: z
    .string()
    .max(2000, { message: 'Validation.TooLong' })
    .optional()
    .or(z.literal('')),
  lines: z.array(standaloneInvoiceLineSchema).min(1, { message: 'Validation.AtLeastOneLine' }),
});

export type StandaloneInvoiceFormValues = z.infer<typeof standaloneInvoiceSchema>;
export type StandaloneInvoiceLineFormValues = z.infer<typeof standaloneInvoiceLineSchema>;
