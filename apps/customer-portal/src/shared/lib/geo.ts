const TZ_TO_COUNTRY: Record<string, string> = {
  'Europe/Istanbul': 'tr',
  'Europe/London': 'gb',
  'Europe/Dublin': 'ie',
  'Europe/Berlin': 'de',
  'Europe/Paris': 'fr',
  'Europe/Madrid': 'es',
  'Europe/Rome': 'it',
  'Europe/Amsterdam': 'nl',
  'Europe/Brussels': 'be',
  'Europe/Vienna': 'at',
  'Europe/Zurich': 'ch',
  'Europe/Stockholm': 'se',
  'Europe/Oslo': 'no',
  'Europe/Copenhagen': 'dk',
  'Europe/Helsinki': 'fi',
  'America/New_York': 'us',
  'America/Chicago': 'us',
  'America/Denver': 'us',
  'America/Los_Angeles': 'us',
  'America/Toronto': 'ca',
  'America/Vancouver': 'ca',
  'Asia/Dubai': 'ae',
  'Asia/Riyadh': 'sa',
  'Asia/Singapore': 'sg',
  'Asia/Tokyo': 'jp',
  'Australia/Sydney': 'au',
};

export const detectTimezone = (): string => {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
  } catch {
    return 'UTC';
  }
};

export const countryCodeFromTimezone = (timezone: string): string | null =>
  TZ_TO_COUNTRY[timezone] ?? null;
