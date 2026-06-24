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

const globalContext: Record<string, unknown> = {};

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

const consoleMethod = (name: 'log'): ((...args: unknown[]) => void) =>
  (console as unknown as Record<string, (...args: unknown[]) => void>)[name];

const writeDebug = (args: unknown[]): void => {
  consoleMethod('log')(...args);
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
      writeDebug(args);
  }
};

const emit = (level: LogLevel, payload: LogPayload): void => {
  writeToConsole(level, payload);
  if (reporter && (level === 'warn' || level === 'error')) {
    try {
      reporter.capture(level, { ...payload, context: enrichContext(payload.context) });
    } catch {
      void 0;
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
