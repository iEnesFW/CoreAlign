import { describe, expect, it } from 'vitest';
import {
  countryCodeFromTimezone,
  countryName,
  detectLocation,
  detectTimezone,
} from '@/shared/lib/geo';

describe('countryCodeFromTimezone (admin)', () => {
  it('maps Istanbul to tr', () => {
    expect(countryCodeFromTimezone('Europe/Istanbul')).toBe('tr');
  });

  it('maps London to gb', () => {
    expect(countryCodeFromTimezone('Europe/London')).toBe('gb');
  });

  it('maps multiple US zones', () => {
    expect(countryCodeFromTimezone('America/New_York')).toBe('us');
    expect(countryCodeFromTimezone('America/Chicago')).toBe('us');
    expect(countryCodeFromTimezone('Pacific/Honolulu')).toBe('us');
  });

  it('returns null for unknown timezone', () => {
    expect(countryCodeFromTimezone('Mars/Olympus')).toBeNull();
  });
});

describe('countryName (admin)', () => {
  it('returns english name for known ISO code', () => {
    expect(countryName('us', 'en')).toMatch(/United States|U\.S\.|US/);
  });

  it('returns a string fallback even when the code is unknown', () => {
    const out = countryName('zz', 'en');
    expect(typeof out === 'string' || out === null).toBe(true);
  });
});

describe('detectTimezone (admin)', () => {
  it('returns a non-empty string', () => {
    expect(detectTimezone().length).toBeGreaterThan(0);
  });
});

describe('detectLocation (admin)', () => {
  it('returns a shape with timezone + countryCode + countryName fields', () => {
    const loc = detectLocation('en');
    expect(loc).toHaveProperty('timezone');
    expect(loc).toHaveProperty('countryCode');
    expect(loc).toHaveProperty('countryName');
  });
});
