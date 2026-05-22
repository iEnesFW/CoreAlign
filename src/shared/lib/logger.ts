type LogLevel = 'debug' | 'info' | 'warn' | 'error';

interface LogPayload {
  message: string;
  context?: Record<string, unknown>;
  error?: unknown;
}

export interface ErrorReporter {
  capture(level: LogLevel, payload: LogPayload): void;
}

const isProduction = import.meta.env.PROD;
let reporter: ErrorReporter | null = null;

// Global structured fields attached to every log record. Used by the auth
// store to inject userId/tenantId once authenticated, and by the apiClient
// to stash the most recent correlation id for cross-stack debugging.
const globalContext: Record<string, unknown> = {};

/**
 * Merge or remove fields from the global structured-log context.
 * Pass `null`/`undefined` for a value to drop that key.
 */
export const setLoggerContext = (fields: Record<string, unknown>): void => {
  for (const [k, v] of Object.entries(fields)) {
    if (v === undefined || v === null) {
      delete globalContext[k];
    } else {
      globalContext[k] = v;
    }
  }
};

export const clearLoggerContext = (keys?: readonly string[]): void => {
  if (!keys) {
    for (const k of Object.keys(globalContext)) delete globalContext[k];
    return;
  }
  for (const k of keys) delete globalContext[k];
};

export const registerErrorReporter = (impl: ErrorReporter | null): void => {
  reporter = impl;
};

const enrichContext = (context?: Record<string, unknown>): Record<string, unknown> | undefined => {
  const hasGlobal = Object.keys(globalContext).length > 0;
  if (!context && !hasGlobal) return undefined;
  return { ...globalContext, ...(context ?? {}) };
};

const writeToConsole = (level: LogLevel, payload: LogPayload): void => {
  if (level === 'debug' || level === 'info') {
    if (isProduction) {
      return;
    }
  }

  const enriched = enrichContext(payload.context);
  const args: unknown[] = [`[${level.toUpperCase()}] ${payload.message}`];
  if (enriched) args.push(enriched);
  if (payload.error) args.push(payload.error);

  switch (level) {
    case 'warn':
      console.warn(...args);
      break;
    case 'error':
      console.error(...args);
      break;
    default:
      // eslint-disable-next-line no-console
      console.log(...args);
  }
};

const emit = (level: LogLevel, payload: LogPayload): void => {
  writeToConsole(level, payload);
  if (reporter && (level === 'warn' || level === 'error')) {
    try {
      reporter.capture(level, { ...payload, context: enrichContext(payload.context) });
    } catch {
      /* never let a reporter failure break the app */
    }
  }
};

export const logger = {
  debug: (message: string, context?: Record<string, unknown>) =>
    emit('debug', { message, context }),
  info: (message: string, context?: Record<string, unknown>) => emit('info', { message, context }),
  warn: (message: string, context?: Record<string, unknown>) => emit('warn', { message, context }),
  error: (message: string, error?: unknown, context?: Record<string, unknown>) =>
    emit('error', { message, error, context }),
};
