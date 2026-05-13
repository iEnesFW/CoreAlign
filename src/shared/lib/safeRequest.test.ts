import { beforeEach, describe, expect, it, vi } from 'vitest';
import { toast } from 'sonner';
import {
  safeBatchRequest,
  safeBatchRequestSettled,
  safeRequest,
  safeRequestWithNotify,
} from './safeRequest';
import { ApiError } from '@/shared/api/ApiError';
import { resetToastQueue } from '@/shared/api/toastQueue';

beforeEach(() => {
  resetToastQueue();
  vi.mocked(toast.success).mockClear();
  vi.mocked(toast.error).mockClear();
});

describe('safeRequest', () => {
  it('returns [data, null] when the promise resolves', async () => {
    const [data, error] = await safeRequest(Promise.resolve({ id: 'abc' }));
    expect(data).toEqual({ id: 'abc' });
    expect(error).toBeNull();
  });

  it('returns [null, error] when the promise rejects with ApiError', async () => {
    const apiError = new ApiError(['Something broke'], 400, 'trace-1');
    const [data, error] = await safeRequest(Promise.reject(apiError));
    expect(data).toBeNull();
    expect(error).toBe(apiError);
  });

  it('captures generic thrown errors', async () => {
    const thrown = new Error('Network down');
    const [data, error] = await safeRequest(Promise.reject(thrown));
    expect(data).toBeNull();
    expect(error?.message).toBe('Network down');
  });
});

describe('safeRequestWithNotify', () => {
  it('shows success toast when configured and request resolves', async () => {
    await safeRequestWithNotify(Promise.resolve(true), {
      successMessage: 'Saved.',
      showSuccessNotification: true,
    });
    expect(toast.success).toHaveBeenCalledWith('Saved.', expect.anything());
  });

  it('does not show a success toast by default', async () => {
    await safeRequestWithNotify(Promise.resolve(true), { successMessage: 'Saved.' });
    expect(toast.success).not.toHaveBeenCalled();
  });

  it('shows error toast with translated message on ApiError', async () => {
    const apiError = new ApiError(['Validation failed'], 400);
    await safeRequestWithNotify(Promise.reject(apiError));
    expect(toast.error).toHaveBeenCalled();
  });

  it('skips error toast when resolveError returns null', async () => {
    const apiError = new ApiError(['silenced'], 400);
    await safeRequestWithNotify(Promise.reject(apiError), { resolveError: () => null });
    expect(toast.error).not.toHaveBeenCalled();
  });
});

describe('safeBatchRequest', () => {
  it('returns tuple of resolved values when all succeed', async () => {
    const [data, error] = await safeBatchRequest([Promise.resolve(1), Promise.resolve('two')]);
    expect(error).toBeNull();
    expect(data).toEqual([1, 'two']);
  });

  it('returns the rejection on first failure', async () => {
    const boom = new Error('boom');
    const [data, error] = await safeBatchRequest([Promise.resolve(1), Promise.reject(boom)]);
    expect(data).toBeNull();
    expect(error).toBe(boom);
  });
});

describe('safeBatchRequestSettled', () => {
  it('returns per-item results with allOk=false when any fails', async () => {
    const boom = new Error('boom');
    const { results, allOk, firstError } = await safeBatchRequestSettled([
      Promise.resolve('ok'),
      Promise.reject(boom),
    ]);
    expect(allOk).toBe(false);
    expect(firstError).toBe(boom);
    expect(results[0]).toMatchObject({ ok: true, data: 'ok' });
    expect(results[1]).toMatchObject({ ok: false, error: boom });
  });
});
