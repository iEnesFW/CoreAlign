import { describe, expect, it } from 'vitest';
import { countryCodeFromTimezone, detectTimezone } from '@/shared/lib/geo';

describe('countryCodeFromTimezone', () => {
  it('maps Europe/Istanbul to tr', () => {
    expect(countryCodeFromTimezone('Europe/Istanbul')).toBe('tr');
  });

  it('maps multiple America zones to us', () => {
    expect(countryCodeFromTimezone('America/New_York')).toBe('us');
    expect(countryCodeFromTimezone('America/Los_Angeles')).toBe('us');
  });

  it('returns null for unknown zone', () => {
    expect(countryCodeFromTimezone('Mars/Olympus')).toBeNull();
  });

  it('returns null for empty string', () => {
    expect(countryCodeFromTimezone('')).toBeNull();
  });
});

describe('detectTimezone', () => {
  it('returns a non-empty timezone string', () => {
    const tz = detectTimezone();
    expect(typeof tz).toBe('string');
    expect(tz.length).toBeGreaterThan(0);
  });
});
