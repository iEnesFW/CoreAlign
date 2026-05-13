import axios from 'axios';
import i18n from 'i18next';
import { isApiError } from '@/shared/api/ApiError';

export interface ParsedError {
  status: number;
  code?: string;
  message: string;
  traceId?: string;
  isNetworkError: boolean;
  isAborted: boolean;
}

export interface FormatOptions {
  codeMap?: Record<string, string>;
  fallbackKey?: string;
  includeTrace?: boolean;
}

const KEY_PATTERN = /^[A-Z][A-Za-z]+(\.[A-Z][A-Za-z]+)+$/;
const HTML_RESPONSE_PATTERN = /<!doctype html|<html|<body|<head/i;
const MAX_REASONABLE_LENGTH = 500;

const isJunk = (message: string): boolean => {
  if (!message) return true;
  if (message === '[object Object]') return true;
  if (message.length > MAX_REASONABLE_LENGTH) return true;
  if (HTML_RESPONSE_PATTERN.test(message)) return true;
  if (message.split('\n').length > 6) return true;
  return false;
};

export const parseError = (error: unknown): ParsedError => {
  if (axios.isCancel(error)) {
    return {
      status: 0,
      message: '',
      isNetworkError: false,
      isAborted: true,
    };
  }

  if (isApiError(error)) {
    return {
      status: error.statusCode,
      message: error.errors[0] ?? '',
      traceId: error.traceId,
      isNetworkError: false,
      isAborted: false,
    };
  }

  if (axios.isAxiosError(error)) {
    if (!error.response) {
      return {
        status: 0,
        message: error.message,
        isNetworkError: true,
        isAborted: false,
      };
    }
    const data = error.response.data as
      | { code?: string; errors?: string[]; error?: string; message?: string; traceId?: string }
      | undefined;
    return {
      status: error.response.status,
      code: data?.code,
      message: data?.errors?.[0] ?? data?.error ?? data?.message ?? error.message,
      traceId: data?.traceId,
      isNetworkError: false,
      isAborted: false,
    };
  }

  if (error instanceof Error) {
    return { status: 0, message: error.message, isNetworkError: false, isAborted: false };
  }

  return { status: 0, message: 'Unknown error', isNetworkError: false, isAborted: false };
};

const translate = (key: string, fallback?: string): string => {
  const out = i18n.t(key, { defaultValue: fallback ?? key }) as unknown as string;
  return typeof out === 'string' ? out : (fallback ?? key);
};

export const formatError = (parsed: ParsedError, options: FormatOptions = {}): string => {
  const { codeMap, fallbackKey, includeTrace = false } = options;

  if (parsed.isAborted) return '';
  if (parsed.isNetworkError) {
    return translate('error.NetworkError', 'Network error. Please try again.');
  }

  let description = '';

  if (parsed.code && codeMap?.[parsed.code]) {
    description = translate(codeMap[parsed.code], parsed.message);
  } else if (parsed.code) {
    description = translate(`error.${parsed.code}`, parsed.message);
  } else if (parsed.message && KEY_PATTERN.test(parsed.message)) {
    description = translate(parsed.message, parsed.message);
  } else if (parsed.message && !isJunk(parsed.message)) {
    description = parsed.message;
  } else if (fallbackKey) {
    description = translate(fallbackKey);
  } else {
    description = translate('error.DefaultError', 'Request failed.');
  }

  if (includeTrace && parsed.traceId) {
    description += ` (ref: ${parsed.traceId.slice(0, 8)})`;
  }
  return description;
};
