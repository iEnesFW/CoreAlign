import { describe, expect, it, vi } from 'vitest';
import { z } from 'zod';
import { parseApiResponse } from './parseApiResponse';
import { ApiError } from './ApiError';

vi.mock('@/shared/lib/logger', () => ({
  logger: { error: vi.fn(), warn: vi.fn(), info: vi.fn(), debug: vi.fn() },
}));

const userSchema = z.object({ id: z.string(), name: z.string() });

describe('parseApiResponse', () => {
  it('passes a well-formed envelope through', () => {
    const body = {
      isSuccess: true,
      data: { id: 'u1', name: 'Ada' },
      errors: [],
      statusCode: 200,
      traceId: null,
    };
    const result = parseApiResponse(body, userSchema, 'test.endpoint');
    expect(result.isSuccess).toBe(true);
    expect(result.data).toEqual({ id: 'u1', name: 'Ada' });
  });

  it('accepts null data on isSuccess=false envelopes', () => {
    const body = {
      isSuccess: false,
      data: null,
      errors: ['Not found'],
      statusCode: 404,
    };
    const result = parseApiResponse(body, userSchema, 'test.endpoint');
    expect(result.isSuccess).toBe(false);
    expect(result.data).toBeNull();
    expect(result.errors).toEqual(['Not found']);
  });

  it('throws ApiError when data shape mismatches the schema', () => {
    const body = {
      isSuccess: true,
      data: { id: 1, name: 'wrong type' },
      errors: [],
      statusCode: 200,
    };
    expect(() => parseApiResponse(body, userSchema, 'test.endpoint')).toThrowError(ApiError);
  });

  it('throws ApiError when the envelope itself is malformed', () => {
    const body = { foo: 'bar' };
    expect(() => parseApiResponse(body, userSchema, 'test.endpoint')).toThrowError(ApiError);
  });
});
