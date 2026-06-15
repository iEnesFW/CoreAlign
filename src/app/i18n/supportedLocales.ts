import enAdmin from './locales/en.json';
import trAdmin from './locales/tr.json';
import arAdmin from './locales/ar.json';
import deAdmin from './locales/de.json';
import ruAdmin from './locales/ru.json';

const COMPLETENESS_THRESHOLD = 0.8;

export interface LocaleDescriptor {
  code: string;
  label: string;
  nativeLabel: string;
  dir: 'ltr' | 'rtl';
}

export const ALL_LOCALES: readonly LocaleDescriptor[] = Object.freeze([
  { code: 'en', label: 'English', nativeLabel: 'English', dir: 'ltr' },
  { code: 'tr', label: 'Turkish', nativeLabel: 'Türkçe', dir: 'ltr' },
  { code: 'ar', label: 'Arabic', nativeLabel: 'العربية', dir: 'rtl' },
  { code: 'de', label: 'German', nativeLabel: 'Deutsch', dir: 'ltr' },
  { code: 'ru', label: 'Russian', nativeLabel: 'Русский', dir: 'ltr' },
]);

const RTL_CODES = new Set(['ar', 'fa', 'he', 'ur']);

export const isRtlLocale = (code: string): boolean => RTL_CODES.has(code.slice(0, 2).toLowerCase());

const flattenKeys = (value: unknown, prefix = ''): string[] => {
  if (value === null || value === undefined) return prefix ? [prefix] : [];
  if (typeof value !== 'object' || Array.isArray(value)) {
    return prefix ? [prefix] : [];
  }
  const out: string[] = [];
  for (const [k, v] of Object.entries(value as Record<string, unknown>)) {
    const next = prefix ? `${prefix}.${k}` : k;
    if (typeof v === 'object' && v !== null && !Array.isArray(v)) {
      out.push(...flattenKeys(v, next));
    } else {
      out.push(next);
    }
  }
  return out;
};

const parityRatio = (baseKeys: ReadonlySet<string>, candidate: unknown): number => {
  if (baseKeys.size === 0) return 0;
  const candidateKeys = new Set(flattenKeys(candidate));
  let hits = 0;
  for (const key of baseKeys) {
    if (candidateKeys.has(key)) hits++;
  }
  return hits / baseKeys.size;
};

const enKeys = new Set(flattenKeys(enAdmin));

const PARITY_BY_CODE: Record<string, number> = {
  en: 1,
  tr: parityRatio(enKeys, trAdmin),
  ar: parityRatio(enKeys, arAdmin),
  de: parityRatio(enKeys, deAdmin),
  ru: parityRatio(enKeys, ruAdmin),
};

export const getLocaleParity = (code: string): number => PARITY_BY_CODE[code] ?? 0;

export const SUPPORTED_LOCALES: readonly LocaleDescriptor[] = Object.freeze(
  ALL_LOCALES.filter((l) => getLocaleParity(l.code) >= COMPLETENESS_THRESHOLD),
);

export const SUPPORTED_LOCALE_CODES: readonly string[] = Object.freeze(
  SUPPORTED_LOCALES.map((l) => l.code),
);

export const resolveLocale = (candidate: string | null | undefined): string => {
  if (!candidate) return 'en';
  const base = candidate.slice(0, 2).toLowerCase();
  return SUPPORTED_LOCALE_CODES.includes(base) ? base : 'en';
};
