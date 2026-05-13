import { describe, expect, it, vi } from 'vitest';
import { AxiosError } from 'axios';
import { toast } from 'sonner';
import { toastApiError, toastApiSuccess } from './mutationToast';

describe('toastApiError', () => {
  it('uses API response error message when present', () => {
    const apiResponse = {
      isSuccess: false,
      data: null,
      errors: ['Validation failed'],
      statusCode: 400,
    };
    const error = new AxiosError('Request failed', '400');
    error.response = { data: apiResponse } as never;

    toastApiError(error, 'fallback');

    expect(toast.error).toHaveBeenCalledWith('Validation failed');
  });

  it('falls back to provided message when no API errors', () => {
    const error = new AxiosError('Network down');

    toastApiError(error, 'Server unreachable');

    expect(toast.error).toHaveBeenCalledWith(expect.any(String));
  });

  it('handles plain Error instances', () => {
    const error = new Error('Plain error');
    toastApiError(error, 'fallback');
    expect(toast.error).toHaveBeenCalledWith('Plain error');
  });

  it('uses fallback for unknown error shapes', () => {
    toastApiError({ weird: 'shape' }, 'fallback');
    expect(toast.error).toHaveBeenCalledWith('fallback');
  });
});

describe('toastApiSuccess', () => {
  it('passes message through to toast.success', () => {
    vi.mocked(toast.success).mockClear();
    toastApiSuccess('Saved successfully');
    expect(toast.success).toHaveBeenCalledWith('Saved successfully');
  });
});
