import { describe, expect, it } from 'vitest';
import axios from 'axios';
import { formatError, parseError } from './errorPipeline';
import { ApiError } from '@/shared/api/ApiError';

describe('parseError', () => {
  it('marks cancellations as aborted', () => {
    const parsed = parseError(new axios.Cancel('user cancelled'));
    expect(parsed.isAborted).toBe(true);
    expect(parsed.status).toBe(0);
  });

  it('extracts ApiError fields', () => {
    const err = new ApiError(['Validation.Required'], 400, 'trace-1');
    const parsed = parseError(err);
    expect(parsed.status).toBe(400);
    expect(parsed.message).toBe('Validation.Required');
    expect(parsed.traceId).toBe('trace-1');
  });

  it('flags network errors when axios has no response', () => {
    const err = Object.assign(new Error('Network Error'), {
      isAxiosError: true,
      config: {},
      toJSON: () => ({}),
      response: undefined,
    });
    const parsed = parseError(err);
    expect(parsed.isNetworkError).toBe(true);
    expect(parsed.status).toBe(0);
  });

  it('falls back to "Unknown error" for unknown values', () => {
    const parsed = parseError('unexpected');
    expect(parsed.message).toBe('Unknown error');
    expect(parsed.status).toBe(0);
  });
});

describe('formatError', () => {
  it('returns empty string for aborted requests', () => {
    const description = formatError({
      status: 0,
      message: '',
      isAborted: true,
      isNetworkError: false,
    });
    expect(description).toBe('');
  });

  it('uses raw message when not a translation key', () => {
    const description = formatError({
      status: 400,
      message: 'Something specific went wrong.',
      isAborted: false,
      isNetworkError: false,
    });
    expect(description).toBe('Something specific went wrong.');
  });

  it('drops HTML/junk payloads in favour of default key', () => {
    const description = formatError({
      status: 500,
      message: '<!doctype html><html><body>Bad Gateway</body></html>',
      isAborted: false,
      isNetworkError: false,
    });
    expect(description).not.toContain('<html');
  });

  it('appends short trace id when includeTrace is set', () => {
    const description = formatError(
      {
        status: 500,
        message: 'boom',
        traceId: 'abcdef1234567890',
        isAborted: false,
        isNetworkError: false,
      },
      { includeTrace: true },
    );
    expect(description).toMatch(/ref: abcdef12\)?$/);
  });
});
