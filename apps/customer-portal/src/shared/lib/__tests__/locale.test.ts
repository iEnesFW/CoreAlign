import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

describe('resolveFormatLocale', () => {
  beforeEach(() => {
    vi.resetModules();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.doUnmock('@/shared/lib/geo');
  });

  it('falls back to language only when region cannot be detected', async () => {
    vi.doMock('@/shared/lib/geo', () => ({
      detectTimezone: () => 'UTC',
      countryCodeFromTimezone: () => null,
    }));
    const { resolveFormatLocale } = await import('@/shared/lib/locale');
    expect(resolveFormatLocale('en')).toBe('en');
  });

  it('lower-cases language to two letters', async () => {
    vi.doMock('@/shared/lib/geo', () => ({
      detectTimezone: () => 'UTC',
      countryCodeFromTimezone: () => null,
    }));
    const { resolveFormatLocale } = await import('@/shared/lib/locale');
    expect(resolveFormatLocale('TR-tr')).toBe('tr');
  });

  it('appends detected region code uppercased', async () => {
    vi.doMock('@/shared/lib/geo', () => ({
      detectTimezone: () => 'Europe/Istanbul',
      countryCodeFromTimezone: () => 'tr',
    }));
    const { resolveFormatLocale } = await import('@/shared/lib/locale');
    expect(resolveFormatLocale('tr')).toBe('tr-TR');
  });

  it('defaults to en when language argument is undefined', async () => {
    vi.doMock('@/shared/lib/geo', () => ({
      detectTimezone: () => 'UTC',
      countryCodeFromTimezone: () => null,
    }));
    const { resolveFormatLocale } = await import('@/shared/lib/locale');
    expect(resolveFormatLocale(undefined)).toBe('en');
  });
});
