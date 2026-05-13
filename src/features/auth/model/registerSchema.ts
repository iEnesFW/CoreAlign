import { z } from 'zod';

export const registerSchema = z
  .object({
    organizationName: z
      .string()
      .min(2, { message: 'Validation.OrganizationNameTooShort' })
      .max(150, { message: 'Validation.OrganizationNameTooLong' }),
    firstName: z.string().max(64).optional().or(z.literal('')),
    lastName: z.string().max(64).optional().or(z.literal('')),
    username: z
      .string()
      .min(3, { message: 'Validation.UsernameTooShort' })
      .max(64, { message: 'Validation.UsernameTooLong' })
      .regex(/^[a-zA-Z0-9._-]+$/, { message: 'Validation.UsernameInvalidChars' }),
    email: z
      .string()
      .min(1, { message: 'Validation.Required' })
      .email({ message: 'Validation.InvalidEmail' })
      .max(256, { message: 'Validation.EmailTooLong' }),
    password: z
      .string()
      .min(8, { message: 'Validation.PasswordTooShort' })
      .regex(/[A-Z]/, { message: 'Validation.PasswordNeedsUppercase' })
      .regex(/[a-z]/, { message: 'Validation.PasswordNeedsLowercase' })
      .regex(/[0-9]/, { message: 'Validation.PasswordNeedsDigit' })
      .regex(/[^a-zA-Z0-9]/, { message: 'Validation.PasswordNeedsSpecial' }),
    confirmPassword: z.string().min(1, { message: 'Validation.Required' }),
  })
  .refine((data) => data.password === data.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Validation.PasswordMismatch',
  });

export type RegisterFormValues = z.infer<typeof registerSchema>;
