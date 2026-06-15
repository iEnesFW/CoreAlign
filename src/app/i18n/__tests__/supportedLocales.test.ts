import { describe, it, expect } from 'vitest';
import {
  ALL_LOCALES,
  SUPPORTED_LOCALES,
  SUPPORTED_LOCALE_CODES,
  getLocaleParity,
  isRtlLocale,
  resolveLocale,
} from '../supportedLocales';

describe('supportedLocales', () => {
  it('always includes English and Turkish', () => {
    expect(SUPPORTED_LOCALE_CODES).toContain('en');
    expect(SUPPORTED_LOCALE_CODES).toContain('tr');
  });

  it('only gates locales that ship in the catalog', () => {
    for (const code of SUPPORTED_LOCALE_CODES) {
      expect(ALL_LOCALES.some((l) => l.code === code)).toBe(true);
    }
  });

  it('reports parity ratios between 0 and 1', () => {
    for (const locale of ALL_LOCALES) {
      const parity = getLocaleParity(locale.code);
      expect(parity).toBeGreaterThanOrEqual(0);
      expect(parity).toBeLessThanOrEqual(1);
    }
  });

  it('marks Arabic as RTL', () => {
    expect(isRtlLocale('ar')).toBe(true);
    expect(isRtlLocale('ar-SA')).toBe(true);
    expect(isRtlLocale('en')).toBe(false);
    expect(isRtlLocale('tr')).toBe(false);
  });

  it('resolves unknown locales to English', () => {
    expect(resolveLocale('xx')).toBe('en');
    expect(resolveLocale(null)).toBe('en');
    expect(resolveLocale('')).toBe('en');
  });

  it('keeps supported locale strings unchanged after normalisation', () => {
    for (const code of SUPPORTED_LOCALE_CODES) {
      expect(resolveLocale(code)).toBe(code);
    }
  });

  it('exposes the Arabic descriptor as rtl', () => {
    const arabic = SUPPORTED_LOCALES.find((l) => l.code === 'ar');
    if (arabic) {
      expect(arabic.dir).toBe('rtl');
    }
  });
});
