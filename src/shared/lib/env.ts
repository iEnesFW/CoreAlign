import { z } from 'zod';

const envSchema = z.object({
  VITE_API_URL: z.string().default(''),
  VITE_RECAPTCHA_SITE_KEY: z.string().default(''),
});

const parsed = envSchema.safeParse(import.meta.env);

if (!parsed.success) {
  const flat = parsed.error.flatten();
  throw new Error(
    `Invalid environment configuration:\n${JSON.stringify(flat.fieldErrors, null, 2)}`,
  );
}

export const env = parsed.data;
