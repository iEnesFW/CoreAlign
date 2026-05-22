import { z } from 'zod';

const optionalId = z.string().optional().or(z.literal(''));
const optionalNumeric = z.string().optional().or(z.literal(''));
const optionalString = (max: number) =>
  z.string().max(max, { message: 'Validation.TooLong' }).optional().or(z.literal(''));

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
  shortDescription: optionalString(500),
  barcode: optionalString(64),
  mpn: optionalString(64),
  brandId: optionalId,
  categoryId: optionalId,
  status: z.enum(['Active', 'New', 'Discontinued', 'EndOfLife']),
  unit: z
    .string()
    .min(1, { message: 'Validation.Required' })
    .max(20, { message: 'Validation.TooLong' }),
  baseUomId: optionalId,
  salesUomId: optionalId,
  purchaseUomId: optionalId,
  price: z.number({ message: 'Validation.Required' }).min(0, { message: 'Validation.NonNegative' }),
  listPrice: optionalNumeric,
  minSellingPrice: optionalNumeric,
  standardCost: optionalNumeric,
  currency: z
    .string()
    .length(3, { message: 'Validation.CurrencyLength' })
    .regex(/^[A-Z]{3}$/, { message: 'Validation.CurrencyFormat' }),
  taxRateId: optionalId,
  isPriceTaxInclusive: z.boolean(),
  stockQuantity: z
    .number({ message: 'Validation.Required' })
    .min(0, { message: 'Validation.NonNegative' }),
  isStockTracked: z.boolean(),
  isLotTracked: z.boolean(),
  isSerialTracked: z.boolean(),
  minStock: optionalNumeric,
  maxStock: optionalNumeric,
  reorderPoint: optionalNumeric,
  safetyStock: optionalNumeric,
  leadTimeDays: optionalNumeric,
  weightKg: optionalNumeric,
  widthCm: optionalNumeric,
  heightCm: optionalNumeric,
  depthCm: optionalNumeric,
  volumeM3: optionalNumeric,
  launchDate: z.string().optional().or(z.literal('')),
  endOfLifeDate: z.string().optional().or(z.literal('')),
  isActive: z.boolean(),
});

export type ProductFormValues = z.infer<typeof productSchema>;
