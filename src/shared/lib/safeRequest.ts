import axios from 'axios';
import i18n from 'i18next';
import { ApiError, isApiError } from '@/shared/api/ApiError';
import { queueToast } from '@/shared/api/toastQueue';
import { logger } from './logger';

export type SafeResult<T> = [T, null] | [null, Error];

export interface BatchSettledItem<T> {
  ok: boolean;
  data?: T;
  error?: Error;
}

export interface BatchSettledResult<T extends readonly unknown[]> {
  results: { [K in keyof T]: BatchSettledItem<T[K]> };
  allOk: boolean;
  firstError: Error | null;
}

interface NotifyOptions {
  successMessage?: string;
  errorMessage?: string;
  showSuccessNotification?: boolean;
  resolveError?: (error: unknown) => string | null;
}

const KEY_PATTERN = /^[A-Z][A-Za-z]+(\.[A-Z][A-Za-z]+)+$/;

const translateIfKey = (message: string): string => {
  if (!KEY_PATTERN.test(message)) return message;
  const translated = i18n.t(message, { defaultValue: message }) as unknown as string;
  return typeof translated === 'string' ? translated : message;
};

export const resolveErrorMessage = (error: unknown): string | null => {
  if (axios.isCancel(error)) return null;

  if (isApiError(error)) {
    if (error.errors.length === 0) return null;
    return translateIfKey(error.errors[0]);
  }

  if (axios.isAxiosError(error)) {
    if (!error.response) {
      return i18n.t('error.NetworkError', { defaultValue: 'Network error.' }) as string;
    }
    const data = error.response.data as { errors?: string[] } | undefined;
    const first = data?.errors?.[0] ?? error.message;
    return translateIfKey(first);
  }

  if (error instanceof Error) return translateIfKey(error.message);
  return null;
};

export async function safeRequest<T>(promise: Promise<T>): Promise<SafeResult<T>> {
  try {
    const data = await promise;
    return [data, null];
  } catch (error) {
    if (axios.isCancel(error)) {
      return [null, error as Error];
    }
    if (!isApiError(error)) {
      logger.error('safeRequest caught non-ApiError', error);
    }
    return [null, error as Error];
  }
}

export async function safeRequestWithNotify<T>(
  promise: Promise<T>,
  options: NotifyOptions = {},
): Promise<SafeResult<T>> {
  const {
    successMessage,
    errorMessage,
    showSuccessNotification = false,
    resolveError = resolveErrorMessage,
  } = options;

  const [data, error] = await safeRequest(promise);

  if (error) {
    if (axios.isCancel(error)) return [null, error];
    const resolved = resolveError(error);
    if (resolved === null) return [null, error];
    const description =
      resolved ||
      errorMessage ||
      (i18n.t('error.DefaultError', { defaultValue: 'Request failed.' }) as string);
    const status = isApiError(error) ? error.statusCode : 0;
    queueToast({
      dedupeKey: `${status}:${description.slice(0, 80)}`,
      description,
      variant: 'error',
    });
    return [null, error];
  }

  if (showSuccessNotification && successMessage) {
    queueToast({
      dedupeKey: `success:${successMessage.slice(0, 80)}`,
      description: successMessage,
      variant: 'success',
    });
  }
  return [data, null];
}

export async function safeBatchRequest<T extends readonly unknown[]>(promises: {
  [K in keyof T]: Promise<T[K]>;
}): Promise<SafeResult<T>> {
  try {
    const results = (await Promise.all(promises)) as unknown as T;
    return [results, null];
  } catch (error) {
    return [null, error as Error];
  }
}

export async function safeBatchRequestSettled<T extends readonly unknown[]>(promises: {
  [K in keyof T]: Promise<T[K]>;
}): Promise<BatchSettledResult<T>> {
  const settled = await Promise.allSettled(promises);
  const results = settled.map((s) =>
    s.status === 'fulfilled'
      ? { ok: true, data: s.value }
      : { ok: false, error: s.reason as Error },
  ) as { [K in keyof T]: BatchSettledItem<T[K]> };
  const firstError = settled.find((s) => s.status === 'rejected') as { reason: Error } | undefined;
  return {
    results,
    allOk: settled.every((s) => s.status === 'fulfilled'),
    firstError: firstError ? firstError.reason : null,
  };
}

export { ApiError };
