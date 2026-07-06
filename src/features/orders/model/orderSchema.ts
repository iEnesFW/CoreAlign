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
  withholdingTaxCodeId: optionalId,
  warehouseId: optionalId,
  lineNotes: z.string().max(500, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  // Optional glass cut size — when width/height/pieces are all set the line quantity is derived
  // as the total m² (server-authoritative; mirrored client-side for the preview).
  widthMm: optionalNumeric,
  heightMm: optionalNumeric,
  pieces: optionalNumeric,
});

// Square millimetres per unit for a square unit of measure, mirroring the server GlassLineMath.
// Returns null for non-area units (mt, kg, adet …) so those keep a single plain quantity input.
export const areaUnitDivisor = (unitCode?: string | null): number | null => {
  const code = unitCode?.trim().toLowerCase().replace('²', '2');
  switch (code) {
    case 'm2':
    case 'sqm':
    case 'metrekare':
      return 1_000_000;
    case 'dm2':
      return 10_000;
    case 'cm2':
      return 100;
    case 'mm2':
      return 1;
    default:
      return null;
  }
};

export const isAreaUnit = (unitCode?: string | null): boolean => areaUnitDivisor(unitCode) !== null;

// Total area in the unit's own square measure from a cut size (width × height mm) × pieces.
export const glassLineArea = (
  unitCode: string | null | undefined,
  widthMm?: string | number | null,
  heightMm?: string | number | null,
  pieces?: string | number | null,
): number | null => {
  const divisor = areaUnitDivisor(unitCode);
  const w = Number(widthMm);
  const h = Number(heightMm);
  const p = Number(pieces);
  if (divisor === null || !(w > 0) || !(h > 0) || !(p > 0)) return null;
  return Math.round((p * w * h * 10000) / divisor) / 10000;
};

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
