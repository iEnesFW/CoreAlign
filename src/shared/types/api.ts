export interface ApiResponse<T> {
  isSuccess: boolean;
  data: T | null;
  errors: string[];
  statusCode: number;
  traceId?: string | null;
}

export interface ApiError {
  status: number;
  errors: string[];
  traceId?: string | null;
  isNetworkError: boolean;
  isAborted: boolean;
}

export type SafeResult<T> = [T, null] | [null, ApiError];

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages?: number;
}
