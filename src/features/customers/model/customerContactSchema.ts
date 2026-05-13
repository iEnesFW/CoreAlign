import { z } from 'zod';

export const customerContactSchema = z.object({
  name: z
    .string()
    .min(2, { message: 'Validation.NameTooShort' })
    .max(150, { message: 'Validation.NameTooLong' }),
  role: z.string().max(100, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  email: z
    .string()
    .email({ message: 'Validation.InvalidEmail' })
    .max(200, { message: 'Validation.EmailTooLong' })
    .optional()
    .or(z.literal('')),
  phone: z.string().max(50, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  notes: z.string().max(500, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  isPrimary: z.boolean(),
});

export type CustomerContactFormValues = z.infer<typeof customerContactSchema>;

export const emptyCustomerContactForm: CustomerContactFormValues = {
  name: '',
  role: '',
  email: '',
  phone: '',
  notes: '',
  isPrimary: false,
};
