import type { AxiosError } from 'axios';
import { ApiError, isApiError } from './ApiError';

export interface ConcurrencyConflictDetails {
  currentVersion: string | null;
  attemptedVersion: string | null;
  conflictingFields: string[];
  message: string | null;
}

interface ConflictPayload {
  currentVersion?: unknown;
  attemptedVersion?: unknown;
  conflictingFields?: unknown;
  message?: unknown;
  errors?: unknown;
}

const CONFLICT_STATUS = 409;

const isAxiosLike = (err: unknown): err is AxiosError =>
  typeof err === 'object' && err !== null && 'isAxiosError' in err;

const readStatusCode = (err: unknown): number | null => {
  if (isApiError(err)) {
    return err.statusCode;
  }
  if (isAxiosLike(err) && typeof err.response?.status === 'number') {
    return err.response.status;
  }
  return null;
};

const readResponseBody = (err: unknown): ConflictPayload | null => {
  if (isAxiosLike(err) && err.response?.data && typeof err.response.data === 'object') {
    return err.response.data as ConflictPayload;
  }
  return null;
};

const asString = (value: unknown): string | null => {
  if (typeof value === 'string' && value.length > 0) {
    return value;
  }
  return null;
};

const asStringArray = (value: unknown): string[] => {
  if (!Array.isArray(value)) {
    return [];
  }
  return value.filter((entry): entry is string => typeof entry === 'string' && entry.length > 0);
};

export const isConcurrencyConflict = (error: unknown): boolean =>
  readStatusCode(error) === CONFLICT_STATUS;

export const parseConcurrencyConflict = (error: unknown): ConcurrencyConflictDetails | null => {
  if (!isConcurrencyConflict(error)) {
    return null;
  }

  const body = readResponseBody(error);
  const apiErrorMessage = error instanceof ApiError ? (error.errors[0] ?? error.message) : null;

  return {
    currentVersion: asString(body?.currentVersion),
    attemptedVersion: asString(body?.attemptedVersion),
    conflictingFields: asStringArray(body?.conflictingFields),
    message: asString(body?.message) ?? apiErrorMessage,
  };
};
