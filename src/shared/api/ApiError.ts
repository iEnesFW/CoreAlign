export class ApiError extends Error {
  readonly errors: readonly string[];
  readonly statusCode: number;
  readonly traceId?: string;

  constructor(errors: readonly string[], statusCode: number, traceId?: string) {
    super(errors[0] ?? 'Request failed.');
    this.name = 'ApiError';
    this.errors = errors;
    this.statusCode = statusCode;
    this.traceId = traceId;
  }
}

export const isApiError = (err: unknown): err is ApiError => err instanceof ApiError;
