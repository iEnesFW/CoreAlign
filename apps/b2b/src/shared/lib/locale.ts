import { countryCodeFromTimezone, detectTimezone } from './geo';

let cachedRegion: string | null | undefined;

const detectRegion = (): string | null => {
  if (cachedRegion === undefined) {
    cachedRegion = countryCodeFromTimezone(detectTimezone());
  }
  return cachedRegion;
};

export const resolveFormatLocale = (language: string | undefined): string => {
  const lang = (language || 'en').slice(0, 2).toLowerCase();
  const region = detectRegion();
  return region ? `${lang}-${region.toUpperCase()}` : lang;
};
