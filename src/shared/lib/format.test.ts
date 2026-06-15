import { describe, expect, it } from 'vitest';
import { formatCurrency, formatDate, formatDateTime, formatNumber } from '@/shared/lib/format';

describe('formatNumber (admin)', () => {
  it('formats integers with default two fraction digits', () => {
    expect(formatNumber(1234, 'en-US')).toBe('1,234.00');
  });

  it('respects override fractionDigits=4', () => {
    expect(formatNumber(1.23456, 'en-US', 4)).toBe('1.2346');
  });

  it('formats with tr-TR thousand and decimal separator', () => {
    const out = formatNumber(1234567.89, 'tr-TR', 2);
    expect(out).toMatch(/1[.\s]234[.\s]567,89/);
  });
});

describe('formatCurrency (admin)', () => {
  it('renders the TRY symbol or code', () => {
    const out = formatCurrency(99.5, 'en-US', 'TRY');
    expect(out).toMatch(/TRY|₺/);
  });

  it('renders USD currency style', () => {
    const out = formatCurrency(120, 'en-US', 'USD');
    expect(out).toMatch(/\$|USD/);
  });

  it('appends code when an unknown currency is passed', () => {
    const out = formatCurrency(50, 'en-US', 'XYZ');
    expect(out).toContain('XYZ');
  });

  it('drops fractional digits when 0 is requested', () => {
    const out = formatCurrency(1000, 'en-US', 'TRY', 0);
    expect(out).not.toMatch(/\.00/);
  });
});

describe('formatDate (admin)', () => {
  it('returns em-dash for null', () => {
    expect(formatDate(null, 'en-US')).toBe('—');
  });

  it('formats a Date object', () => {
    const out = formatDate(new Date('2025-04-01T00:00:00Z'), 'en-US');
    expect(out).toMatch(/Apr|2025|1/);
  });

  it('formats an ISO string', () => {
    const out = formatDate('2025-04-01T00:00:00Z', 'en-US');
    expect(out).toMatch(/Apr|2025|1/);
  });
});

describe('formatDateTime (admin)', () => {
  it('returns em-dash for undefined', () => {
    expect(formatDateTime(undefined, 'en-US')).toBe('—');
  });

  it('renders both date and time portions', () => {
    const out = formatDateTime('2025-04-01T13:45:00Z', 'en-US');
    expect(out.length).toBeGreaterThan(8);
  });
});
