import axios from 'axios';
import { logger } from '@/shared/lib/logger';
import { parseError, formatError } from './errorPipeline';
import { queueToast } from '@/shared/api/toastQueue';
import { reportClientError } from '@/shared/lib/clientErrorReporter';

const seen = new WeakSet<object>();

const dedupe = (raw: unknown): boolean => {
  if (raw === null || typeof raw !== 'object') return false;
  const obj = raw as object;
  if (seen.has(obj)) return true;
  seen.add(obj);
  return false;
};

const reportError = (source: 'unhandledrejection' | 'error', err: unknown): void => {
  if (axios.isCancel(err)) return;
  if (dedupe(err)) return;

  const parsed = parseError(err);
  if (parsed.isAborted) return;

  const description = formatError(parsed, { includeTrace: !!parsed.traceId });
  logger.error(`window.${source}`, err, {
    status: parsed.status,
    code: parsed.code,
    traceId: parsed.traceId,
  });

  if (parsed.status === 0 && !parsed.isNetworkError && parsed.message === '') return;

  reportClientError({
    message: parsed.message || description || `window.${source}`,
    severity: 'Error',
    component: `window.${source}`,
    stack: (err as { stack?: string })?.stack,
    context: { status: parsed.status, code: parsed.code, traceId: parsed.traceId },
  });

  const dedupeKey = `window:${parsed.status}:${parsed.code ?? parsed.message.slice(0, 80)}`;
  queueToast({ dedupeKey, description, variant: 'error' });
};

let installed = false;

export const installWindowErrorHandlers = (): void => {
  if (installed) return;
  installed = true;

  window.addEventListener('unhandledrejection', (event) => {
    reportError('unhandledrejection', event.reason);
  });

  window.addEventListener('error', (event) => {
    reportError('error', event.error ?? event.message);
  });
};
