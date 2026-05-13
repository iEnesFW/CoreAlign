import { z } from 'zod';

export const loginSchema = z.object({
  email: z
    .string()
    .min(1, { message: 'Validation.Required' })
    .email({ message: 'Validation.InvalidEmail' }),
  password: z.string().min(1, { message: 'Validation.Required' }),
  rememberMe: z.boolean().optional(),
});

export type LoginFormValues = z.infer<typeof loginSchema>;
