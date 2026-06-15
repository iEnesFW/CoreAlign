import { describe, expect, it } from 'vitest';
import { formatCurrency, formatDate, formatDateTime, formatNumber } from '@/shared/lib/format';

describe('formatNumber', () => {
  it('formats integers with default 2 fraction digits', () => {
    expect(formatNumber(1500, 'en-US')).toBe('1,500.00');
  });

  it('respects fractionDigits override', () => {
    expect(formatNumber(0.5, 'en-US', 0)).toBe('1');
  });

  it('formats according to tr-TR thousands separator', () => {
    const result = formatNumber(1234567, 'tr-TR', 0);
    expect(result).toMatch(/1[.\s]234[.\s]567/);
  });
});

describe('formatCurrency', () => {
  it('renders TRY symbol or code', () => {
    const result = formatCurrency(100, 'en-US', 'TRY');
    expect(result).toMatch(/TRY|₺/);
  });

  it('falls back to TRY for invalid currency code', () => {
    const result = formatCurrency(50, 'en-US', 'XX');
    expect(result).toMatch(/TRY|₺/);
  });

  it('uses USD when supplied', () => {
    const result = formatCurrency(99.5, 'en-US', 'USD');
    expect(result).toMatch(/\$|USD/);
  });

  it('respects fractionDigits = 0', () => {
    const result = formatCurrency(2000, 'en-US', 'TRY', 0);
    expect(result).not.toMatch(/\.00/);
  });
});

describe('formatDate', () => {
  it('returns em-dash for null', () => {
    expect(formatDate(null, 'en-US')).toBe('—');
  });

  it('returns em-dash for undefined', () => {
    expect(formatDate(undefined, 'en-US')).toBe('—');
  });

  it('formats an ISO date string', () => {
    const out = formatDate('2025-01-15T00:00:00Z', 'en-US');
    expect(out).toMatch(/Jan|2025|15/);
  });
});

describe('formatDateTime', () => {
  it('returns em-dash for empty', () => {
    expect(formatDateTime('', 'en-US')).toBe('—');
  });

  it('renders both date and time fragments', () => {
    const out = formatDateTime('2025-06-15T12:30:00Z', 'en-US');
    expect(out.length).toBeGreaterThan(8);
  });
});
