/**
 * Pickable values for the company profile's locale/financial defaults.
 *
 * WHY a curated list and not `Intl.supportedValuesOf('timeZone')`: that returns ~450 zones with no
 * ordering, which is worse than a free-text box for a form that in practice needs Türkiye plus the
 * handful of zones a Turkish company actually trades with. A value already stored on the tenant is
 * still rendered by the select (it keeps any unlisted value as its own option), so this list can
 * never lock an existing configuration out.
 */

export const LOCALE_OPTIONS = [
  { value: 'tr-TR', label: 'Türkçe (tr-TR)' },
  { value: 'en-US', label: 'English — US (en-US)' },
  { value: 'en-GB', label: 'English — UK (en-GB)' },
  { value: 'de-DE', label: 'Deutsch (de-DE)' },
  { value: 'fr-FR', label: 'Français (fr-FR)' },
  { value: 'ru-RU', label: 'Русский (ru-RU)' },
  { value: 'ar-SA', label: 'العربية (ar-SA)' },
];

export const TIME_ZONE_OPTIONS = [
  { value: 'Europe/Istanbul', label: 'Europe/Istanbul (UTC+3)' },
  { value: 'Europe/London', label: 'Europe/London' },
  { value: 'Europe/Berlin', label: 'Europe/Berlin' },
  { value: 'Europe/Paris', label: 'Europe/Paris' },
  { value: 'Europe/Amsterdam', label: 'Europe/Amsterdam' },
  { value: 'Europe/Moscow', label: 'Europe/Moscow' },
  { value: 'Asia/Dubai', label: 'Asia/Dubai' },
  { value: 'Asia/Riyadh', label: 'Asia/Riyadh' },
  { value: 'America/New_York', label: 'America/New_York' },
  { value: 'America/Chicago', label: 'America/Chicago' },
  { value: 'America/Los_Angeles', label: 'America/Los_Angeles' },
  { value: 'UTC', label: 'UTC' },
];

export const FISCAL_MONTHS = [
  { value: 1, labelKey: 'CompanyProfile.Month.1' },
  { value: 2, labelKey: 'CompanyProfile.Month.2' },
  { value: 3, labelKey: 'CompanyProfile.Month.3' },
  { value: 4, labelKey: 'CompanyProfile.Month.4' },
  { value: 5, labelKey: 'CompanyProfile.Month.5' },
  { value: 6, labelKey: 'CompanyProfile.Month.6' },
  { value: 7, labelKey: 'CompanyProfile.Month.7' },
  { value: 8, labelKey: 'CompanyProfile.Month.8' },
  { value: 9, labelKey: 'CompanyProfile.Month.9' },
  { value: 10, labelKey: 'CompanyProfile.Month.10' },
  { value: 11, labelKey: 'CompanyProfile.Month.11' },
  { value: 12, labelKey: 'CompanyProfile.Month.12' },
] as const;
