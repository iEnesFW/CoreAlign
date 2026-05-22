/**
 * Build and trigger a download of a UTF-8 CSV file in the browser.
 *
 * The output starts with a BOM so Excel detects the encoding correctly, and each
 * cell is escaped per RFC 4180 (commas, quotes and newlines wrapped in quotes,
 * embedded quotes doubled). Returns the row count exported.
 *
 * Centralizing here avoids the duplicate `csvEscape` + DOM-anchor dance that
 * was previously copy-pasted across list pages.
 */

type Primitive = string | number | boolean | null | undefined;

const escape = (value: Primitive): string => {
  if (value === null || value === undefined) return '';
  const s = typeof value === 'string' ? value : String(value);
  return /[",\n\r]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
};

const todayIso = (): string => new Date().toISOString().slice(0, 10);

const sanitizeFilename = (name: string): string => {
  const cleaned = name.replace(/[^a-z0-9-_]+/gi, '_').slice(0, 80);
  return cleaned.length === 0 ? 'export' : cleaned;
};

export interface CsvColumn<T> {
  /** Header text shown in row 1 of the CSV. */
  readonly header: string;
  /** Pull the cell value out of a row. */
  readonly value: (row: T) => Primitive;
}

export interface DownloadCsvOptions<T> {
  /** Logical name; date is appended automatically (e.g. `customers` → `customers_2026-05-15.csv`). */
  readonly filename: string;
  readonly columns: readonly CsvColumn<T>[];
  readonly rows: readonly T[];
}

// UTF-8 BOM (U+FEFF) — written as an escape sequence so the source file
// itself stays plain ASCII and the lint rule against irregular whitespace
// inside string literals is not triggered.
const UTF8_BOM = '﻿';

/**
 * Render <columns × rows> as CSV and trigger a download. Returns the row count.
 * Throws nothing — silently no-ops with row count 0 when there's no data.
 */
export const downloadCsv = <T>({ filename, columns, rows }: DownloadCsvOptions<T>): number => {
  if (rows.length === 0) return 0;

  const headerLine = columns.map((c) => escape(c.header)).join(',');
  const dataLines = rows.map((row) => columns.map((c) => escape(c.value(row))).join(','));
  const csv = [headerLine, ...dataLines].join('\r\n');

  const blob = new Blob([UTF8_BOM, csv], { type: 'text/csv;charset=utf-8' });
  const href = URL.createObjectURL(blob);
  try {
    const link = document.createElement('a');
    link.href = href;
    link.download = `${sanitizeFilename(filename)}_${todayIso()}.csv`;
    link.rel = 'noopener';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  } finally {
    // Defer revoke so Safari has a chance to start the download.
    setTimeout(() => URL.revokeObjectURL(href), 0);
  }

  return rows.length;
};
