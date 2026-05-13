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

export const registerErrorReporter = (impl: ErrorReporter | null): void => {
  reporter = impl;
};

const writeToConsole = (level: LogLevel, payload: LogPayload): void => {
  if (level === 'debug' || level === 'info') {
    if (isProduction) {
      return;
    }
  }

  const args: unknown[] = [`[${level.toUpperCase()}] ${payload.message}`];
  if (payload.context) args.push(payload.context);
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
      reporter.capture(level, payload);
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
