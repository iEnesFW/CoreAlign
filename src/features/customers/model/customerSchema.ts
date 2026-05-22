import { z } from 'zod';

const optionalString = (max: number) =>
  z.string().max(max, { message: 'Validation.TooLong' }).optional().or(z.literal(''));

export const customerSchema = z.object({
  name: z
    .string()
    .min(2, { message: 'Validation.NameTooShort' })
    .max(200, { message: 'Validation.NameTooLong' }),
  type: z.enum(['Individual', 'Business', 'Government']),
  code: optionalString(32),
  legalName: optionalString(200),
  tradeName: optionalString(200),
  email: z
    .string()
    .email({ message: 'Validation.InvalidEmail' })
    .max(256)
    .optional()
    .or(z.literal('')),
  phone: optionalString(30),
  nationalId: optionalString(32),
  taxNumber: optionalString(50),
  taxOffice: optionalString(100),
  website: optionalString(500),
  defaultCurrency: z.string().min(3).max(3),
  paymentTermsId: optionalString(64),
  priceListId: optionalString(64),
  customerGroupId: optionalString(64),
  // Kept as strings in the form (native number inputs emit strings); parsed to
  // numbers at submit. Avoids the RHF×zod coerce-resolver input-type conflict.
  creditLimit: optionalString(20),
  defaultDiscountPercent: optionalString(10),
  classification: optionalString(64),
  territory: optionalString(64),
  notes: optionalString(2000),
  isActive: z.boolean(),
});

export type CustomerFormValues = z.infer<typeof customerSchema>;
