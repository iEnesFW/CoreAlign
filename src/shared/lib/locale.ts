import { countryCodeFromTimezone, detectTimezone } from './geo';

// Region is derived once from the IANA timezone and never changes within a
// session, so it is safe to memoize. `undefined` = not yet computed,
// `null` = computed but the timezone is unmapped.
let cachedRegion: string | null | undefined;

const detectRegion = (): string | null => {
  if (cachedRegion === undefined) {
    cachedRegion = countryCodeFromTimezone(detectTimezone());
  }
  return cachedRegion;
};

/**
 * BCP-47 locale used for *formatting* (dates, numbers, currency). It pins the
 * UI language's words to the user's regional conventions: an English UI in
 * Turkey formats "20.05.2026" / "1.234,56" rather than US "5/20/2026" /
 * "1,234.56", while month/word labels stay in the chosen language.
 */
export const resolveFormatLocale = (language: string | undefined): string => {
  const lang = (language || 'en').slice(0, 2).toLowerCase();
  const region = detectRegion();
  return region ? `${lang}-${region.toUpperCase()}` : lang;
};

/** ISO 3166-1 alpha-2 region detected from the timezone, or null. */
export const detectRegionCode = (): string | null => detectRegion();
