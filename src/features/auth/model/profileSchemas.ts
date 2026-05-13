import { z } from 'zod';

export const profileSchema = z.object({
  firstName: z.string().max(64, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  lastName: z.string().max(64, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  phoneNumber: z.string().max(20, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  avatarUrl: z.string().max(500, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
});

export type ProfileFormValues = z.infer<typeof profileSchema>;

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, { message: 'Validation.Required' }),
    newPassword: z
      .string()
      .min(8, { message: 'Validation.PasswordTooShort' })
      .regex(/[A-Z]/, { message: 'Validation.PasswordNeedsUppercase' })
      .regex(/[a-z]/, { message: 'Validation.PasswordNeedsLowercase' })
      .regex(/[0-9]/, { message: 'Validation.PasswordNeedsDigit' })
      .regex(/[^a-zA-Z0-9]/, { message: 'Validation.PasswordNeedsSpecial' }),
    confirmPassword: z.string().min(1, { message: 'Validation.Required' }),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Validation.PasswordMismatch',
  })
  .refine((data) => data.newPassword !== data.currentPassword, {
    path: ['newPassword'],
    message: 'Validation.PasswordMustDiffer',
  });

export type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>;
