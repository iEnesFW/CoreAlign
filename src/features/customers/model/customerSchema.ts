import { z } from 'zod';

const optionalString = (max: number) =>
  z.string().max(max, { message: 'Validation.TooLong' }).optional().or(z.literal(''));

export const customerSchema = z.object({
  name: z
    .string()
    .min(2, { message: 'Validation.NameTooShort' })
    .max(200, { message: 'Validation.NameTooLong' }),
  email: z
    .string()
    .email({ message: 'Validation.InvalidEmail' })
    .max(256)
    .optional()
    .or(z.literal('')),
  phone: optionalString(30),
  taxNumber: optionalString(50),
  notes: optionalString(2000),
  isActive: z.boolean(),
});

export type CustomerFormValues = z.infer<typeof customerSchema>;
