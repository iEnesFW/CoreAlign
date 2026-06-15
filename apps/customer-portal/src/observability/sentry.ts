import * as Sentry from '@sentry/react';
import type { ComponentType } from 'react';

const SENSITIVE_HEADERS = [
  'authorization',
  'cookie',
  'set-cookie',
  'x-api-key',
  'x-auth-token',
  'proxy-authorization',
];

const SENSITIVE_QUERY_KEYS = [
  'token',
  'access_token',
  'refresh_token',
  'code',
  'password',
  'api_key',
  'apikey',
];

const SENSITIVE_PAYLOAD_KEYS = [
  'password',
  'newpassword',
  'currentpassword',
  'passwordconfirmation',
  'confirmpassword',
  'iban',
  'taxnumber',
  'nationalid',
  'tcno',
  'tckn',
  'ssn',
  'creditcard',
  'cardnumber',
  'cvv',
  'cvc',
  'secretkey',
  'clientsecret',
];

const REDACTED = '[REDACTED]';

function normalizeKey(key: string): string {
  return key.replace(/[_-]/g, '').toLowerCase();
}

function isSensitivePayloadKey(key: string): boolean {
  const normalized = normalizeKey(key);
  return SENSITIVE_PAYLOAD_KEYS.some((s) => normalized.includes(s));
}

function scrubObject(value: unknown): unknown {
  if (value === null || value === undefined) return value;
  if (Array.isArray(value)) {
    return value.map(scrubObject);
  }
  if (typeof value === 'object') {
    const result: Record<string, unknown> = {};
    for (const [key, val] of Object.entries(value as Record<string, unknown>)) {
      result[key] = isSensitivePayloadKey(key) ? REDACTED : scrubObject(val);
    }
    return result;
  }
  return value;
}

function scrubQueryString(query: string): string {
  return query
    .split('&')
    .map((pair) => {
      const [k, v] = pair.split('=');
      if (!k) return pair;
      return SENSITIVE_QUERY_KEYS.includes(k.toLowerCase())
        ? `${k}=${REDACTED}`
        : `${k}${v !== undefined ? `=${v}` : ''}`;
    })
    .join('&');
}

function scrubHeaders(
  headers: Record<string, string> | undefined,
): Record<string, string> | undefined {
  if (!headers) return headers;
  const result: Record<string, string> = {};
  for (const [key, val] of Object.entries(headers)) {
    result[key] = SENSITIVE_HEADERS.includes(key.toLowerCase()) ? REDACTED : val;
  }
  return result;
}

export function initSentry(): void {
  const dsn = import.meta.env.VITE_SENTRY_DSN as string | undefined;
  if (!dsn) return;

  Sentry.init({
    dsn,
    environment: (import.meta.env.MODE as string | undefined) ?? 'production',
    release: (import.meta.env.VITE_RELEASE_SHA as string | undefined) ?? undefined,
    sendDefaultPii: false,
    tracesSampleRate: Number(import.meta.env.VITE_SENTRY_TRACES_SAMPLE_RATE ?? 0.1),
    integrations: [
      Sentry.browserTracingIntegration(),
      Sentry.replayIntegration({ maskAllText: true, blockAllMedia: true }),
    ],
    replaysSessionSampleRate: 0,
    replaysOnErrorSampleRate: 0.1,
    beforeSend(event) {
      if (event.request) {
        if (event.request.cookies) {
          (event.request as { cookies?: unknown }).cookies = REDACTED;
        }
        event.request.headers = scrubHeaders(
          event.request.headers as Record<string, string> | undefined,
        );
        if (typeof event.request.query_string === 'string') {
          event.request.query_string = scrubQueryString(event.request.query_string);
        }
        if (event.request.data && typeof event.request.data === 'object') {
          event.request.data = scrubObject(event.request.data);
        }
      }
      if (event.user) {
        event.user.ip_address = undefined;
        event.user.email = undefined;
        event.user.username = undefined;
      }
      return event;
    },
  });
}

export function withSentryProfiler<P extends object>(
  Component: ComponentType<P>,
): ComponentType<P> {
  return Sentry.withProfiler(Component);
}

export const SentryErrorBoundary = Sentry.ErrorBoundary;
