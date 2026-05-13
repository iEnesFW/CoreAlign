import axios from 'axios';
import { toast } from 'sonner';
import i18n from 'i18next';
import { isApiError } from '@/shared/api/ApiError';
import type { ApiResponse } from '@/shared/types/api';

const KEY_PATTERN = /^[A-Z][A-Za-z]+(\.[A-Z][A-Za-z]+)+$/;

const translateIfKey = (message: string): string => {
  if (!KEY_PATTERN.test(message)) return message;
  const translated = i18n.t(message, { defaultValue: message }) as unknown as string;
  return typeof translated === 'string' ? translated : message;
};

export const toastApiError = (error: unknown, fallback?: string): void => {
  const resolvedFallback =
    fallback ?? (i18n.t('common.error', { defaultValue: 'Request failed.' }) as string);

  if (isApiError(error)) {
    const raw = error.errors[0];
    toast.error(raw ? translateIfKey(raw) : resolvedFallback);
    return;
  }

  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ApiResponse<unknown> | undefined;
    const raw = data?.errors?.[0] ?? error.message ?? resolvedFallback;
    toast.error(translateIfKey(raw));
    return;
  }

  if (error instanceof Error) {
    toast.error(translateIfKey(error.message || resolvedFallback));
    return;
  }

  toast.error(resolvedFallback);
};

export const toastApiSuccess = (message: string): void => {
  toast.success(message);
};
