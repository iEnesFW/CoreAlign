export interface ActivityLog {
  id: string;
  userId: string | null;
  method: string;
  path: string;
  statusCode: number;
  durationMs: number;
  ipAddress: string | null;
  userAgent: string | null;
  traceId: string | null;
  createdAtUtc: string;
}

export interface ActivityLogListParams {
  page: number;
  pageSize: number;
}
