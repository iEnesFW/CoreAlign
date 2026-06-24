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
  readonly header: string;
  readonly value: (row: T) => Primitive;
}

export interface DownloadCsvOptions<T> {
  readonly filename: string;
  readonly columns: readonly CsvColumn<T>[];
  readonly rows: readonly T[];
}

const UTF8_BOM = '﻿';

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
    setTimeout(() => URL.revokeObjectURL(href), 0);
  }

  return rows.length;
};
