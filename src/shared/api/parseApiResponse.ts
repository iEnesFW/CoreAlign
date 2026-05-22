import { z, type ZodType } from 'zod';
import { ApiError } from './ApiError';
import { logger } from '@/shared/lib/logger';
import type { ApiResponse } from '@/shared/types/api';

const envelopeBase = z.object({
  isSuccess: z.boolean(),
  errors: z.array(z.string()).default([]),
  statusCode: z.number().int().default(200),
  traceId: z.string().nullish(),
});

export const apiEnvelopeSchema = <T>(dataSchema: ZodType<T>) =>
  envelopeBase.extend({
    data: dataSchema.nullable(),
  });

export const parseApiResponse = <T>(
  body: unknown,
  dataSchema: ZodType<T>,
  endpoint: string,
): ApiResponse<T> => {
  const schema = apiEnvelopeSchema(dataSchema);
  const result = schema.safeParse(body);
  if (result.success) {
    return result.data as ApiResponse<T>;
  }
  logger.error('Response shape validation failed', result.error, {
    endpoint,
    issues: result.error.issues.map((i) => ({ path: i.path.join('.'), code: i.code })),
  });
  throw new ApiError([`Server returned an unexpected response shape for ${endpoint}.`], 502);
};
