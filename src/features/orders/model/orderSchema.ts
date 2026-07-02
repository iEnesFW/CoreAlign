import { z } from 'zod';

const optionalId = z.string().optional().or(z.literal(''));
const optionalNumeric = z.string().optional().or(z.literal(''));

export const orderLineSchema = z.object({
  productId: z.string().min(1, { message: 'Validation.Required' }),
  quantity: z.number({ message: 'Validation.Required' }).gt(0, { message: 'Validation.Positive' }),
  unitPrice: z
    .number({ message: 'Validation.Required' })
    .min(0, { message: 'Validation.NonNegative' }),
  uomId: optionalId,
  uomCode: z.string().optional().or(z.literal('')),
  lineDiscountPercent: optionalNumeric,
  taxRateId: optionalId,
  taxRatePercent: optionalNumeric,
  withholdingRatePercent: optionalNumeric,
  warehouseId: optionalId,
  lineNotes: z.string().max(500, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
});

export const orderSchema = z.object({
  orderNumber: z.string().max(64, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
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
  type: z.enum(['Standard', 'Blanket', 'Return', 'Sample', 'Internal']),
  source: z.enum(['Manual', 'Web', 'Api', 'Edi', 'Marketplace', 'Phone', 'InStore']),
  currency: z
    .string()
    .length(3, { message: 'Validation.CurrencyLength' })
    .regex(/^[A-Z]{3}$/, { message: 'Validation.CurrencyFormat' }),
  exchangeRate: optionalNumeric,
  requestedDeliveryDate: z.string().optional().or(z.literal('')),
  promisedDeliveryDate: z.string().optional().or(z.literal('')),
  paymentTermsId: optionalId,
  priceListId: optionalId,
  billingAddressId: optionalId,
  shippingAddressId: optionalId,
  headerDiscountPercent: optionalNumeric,
  shippingCost: optionalNumeric,
  channel: z.string().max(64, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  internalNotes: z
    .string()
    .max(2000, { message: 'Validation.TooLong' })
    .optional()
    .or(z.literal('')),
  customerNotes: z
    .string()
    .max(2000, { message: 'Validation.TooLong' })
    .optional()
    .or(z.literal('')),
  notes: z.string().max(2000, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  lines: z.array(orderLineSchema).min(1, { message: 'Validation.AtLeastOneLine' }),
});

export type OrderFormValues = z.infer<typeof orderSchema>;
export type OrderLineFormValues = z.infer<typeof orderLineSchema>;
