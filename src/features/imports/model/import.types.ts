export type ImportEntityKind = 'customers' | 'products' | 'gl-accounts';

export interface ImportRowError {
  rowNumber: number;
  field: string;
  message: string;
}

export interface ImportRowPreview<T> {
  rowNumber: number;
  row: T;
  errors: ImportRowError[];
  isValid: boolean;
}

export interface ImportPreviewResult<T> {
  sessionId: string;
  entityKind: ImportEntityKind;
  headers: string[];
  rows: ImportRowPreview<T>[];
  totalRowCount: number;
  validRowCount: number;
  invalidRowCount: number;
}

export interface ImportCommitResult {
  sessionId: string;
  entityKind: ImportEntityKind;
  attemptedCount: number;
  committedCount: number;
  skippedCount: number;
  errors: ImportRowError[];
}
