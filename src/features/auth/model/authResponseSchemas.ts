import { z } from 'zod';

export const userProfileSchema = z.object({
  id: z.string(),
  tenantId: z.string(),
  tenantName: z.string(),
  tenantSlug: z.string(),
  username: z.string(),
  email: z.string(),
  firstName: z
    .string()
    .nullish()
    .transform((v) => v ?? null),
  lastName: z
    .string()
    .nullish()
    .transform((v) => v ?? null),
  avatarUrl: z
    .string()
    .nullish()
    .transform((v) => v ?? null),
  roles: z.array(z.string()),
});

export const authResponseSchema = z.object({
  accessToken: z.string().min(1),
  expiresAt: z.string(),
  user: userProfileSchema,
});

export const sessionInfoSchema = z.object({
  id: z.string(),
  deviceInfo: z.string().nullable(),
  ipAddress: z.string().nullable(),
  createdAtUtc: z.string(),
  lastActivityAtUtc: z.string(),
  isCurrent: z.boolean(),
});

export const loginHistoryEntrySchema = z.object({
  ipAddress: z.string().nullable(),
  userAgent: z.string().nullable(),
  deviceFingerprint: z.string().nullable(),
  loginResult: z.string(),
  failureReason: z.string().nullable(),
  attemptedAtUtc: z.string(),
});

export const sessionInfoListSchema = z.array(sessionInfoSchema);
export const loginHistoryListSchema = z.array(loginHistoryEntrySchema);
export const booleanResultSchema = z.boolean();
