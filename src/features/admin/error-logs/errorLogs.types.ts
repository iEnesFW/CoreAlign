export type ErrorSeverity = 'Error' | 'Warning' | 'Info';
export type ErrorSourceKind = 'Backend' | 'Frontend';

export interface ErrorLogListItem {
  id: string;
  correlationId: string;
  occurredAtUtc: string;
  source: ErrorSourceKind;
  severity: ErrorSeverity;
  statusCode: number | null;
  httpMethod: string | null;
  path: string | null;
  clientPage: string | null;
  exceptionType: string | null;
  message: string;
  tenantId: string | null;
  userId: string | null;
  userName: string | null;
  isResolved: boolean;
}

export interface ErrorLogDetail extends ErrorLogListItem {
  traceId: string | null;
  stackTrace: string | null;
  clientComponent: string | null;
  userAgent: string | null;
  contextJson: string | null;
  resolutionNotes: string | null;
  resolvedAtUtc: string | null;
}

export interface ErrorLogPage {
  items: ErrorLogListItem[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ErrorLogFilters {
  severity?: ErrorSeverity;
  source?: ErrorSourceKind;
  statusCode?: number;
  correlationId?: string;
  path?: string;
  onlyUnresolved?: boolean;
  fromUtc?: string;
  toUtc?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}
