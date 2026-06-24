import { apiClient, getLastCorrelationId } from '@/shared/api/apiClient';

type Severity = 'Error' | 'Warning' | 'Info';

interface ClientErrorReport {
  message: string;
  severity?: Severity;
  component?: string;
  stack?: string;
  context?: Record<string, unknown>;
}

let inFlight = false;
let lastSentAt = 0;

const safeJson = (value: Record<string, unknown>): string | undefined => {
  try {
    return JSON.stringify(value).slice(0, 16000);
  } catch {
    return undefined;
  }
};

export function reportClientError(report: ClientErrorReport): void {
  try {
    const now = Date.now();
    if (inFlight || now - lastSentAt < 1000) return;
    inFlight = true;
    lastSentAt = now;

    const body = {
      message: (report.message || '(no message)').slice(0, 4000),
      severity: report.severity ?? 'Error',
      page: window.location.pathname + window.location.hash,
      component: report.component?.slice(0, 256),
      stackTrace: report.stack?.slice(0, 16000),
      correlationId: getLastCorrelationId(),
      contextJson: report.context ? safeJson(report.context) : undefined,
    };

    void apiClient
      .post('/client-errors', body)
      .catch(() => undefined)
      .finally(() => {
        inFlight = false;
      });
  } catch {
    inFlight = false;
  }
}

let installed = false;

export function installGlobalErrorReporting(): void {
  if (typeof window === 'undefined' || installed) return;
  installed = true;

  window.addEventListener('error', (event) => {
    reportClientError({
      message: event.message || 'Unhandled window error',
      severity: 'Error',
      component: 'window.onerror',
      stack: event.error?.stack,
      context: { source: event.filename, line: event.lineno, column: event.colno },
    });
  });

  window.addEventListener('unhandledrejection', (event) => {
    const reason = event.reason;
    reportClientError({
      message: reason?.message ?? String(reason ?? 'Unhandled promise rejection'),
      severity: 'Error',
      component: 'unhandledrejection',
      stack: reason?.stack,
    });
  });
}
