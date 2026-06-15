import enLocale from '@/app/locales/en.json';

export const ALWAYS_VISIBLE_LANGUAGES = ['tr', 'en'] as const;
export const GATED_LANGUAGES = ['ar', 'de', 'ru'] as const;

export type AlwaysVisibleLanguage = (typeof ALWAYS_VISIBLE_LANGUAGES)[number];
export type GatedLanguage = (typeof GATED_LANGUAGES)[number];
export type SupportedLanguage = AlwaysVisibleLanguage | GatedLanguage;

export const PARITY_THRESHOLD = 0.8;

const enKeyCount = countKeys(enLocale as unknown as Record<string, unknown>);

function countKeys(value: unknown, prefix = ''): number {
  if (value === null || value === undefined) return prefix ? 1 : 0;
  if (typeof value !== 'object' || Array.isArray(value)) return prefix ? 1 : 0;
  let total = 0;
  for (const [k, v] of Object.entries(value as Record<string, unknown>)) {
    const nextKey = prefix ? `${prefix}.${k}` : k;
    if (typeof v === 'object' && v !== null && !Array.isArray(v)) {
      total += countKeys(v, nextKey);
    } else {
      total += 1;
    }
  }
  return total;
}

export interface LocaleParityInfo {
  code: SupportedLanguage;
  parity: number;
  keyCount: number;
  visible: boolean;
}

export const evaluateLocaleParity = (
  locales: Partial<Record<GatedLanguage, Record<string, unknown> | undefined>>,
): LocaleParityInfo[] => {
  const result: LocaleParityInfo[] = ALWAYS_VISIBLE_LANGUAGES.map((code) => ({
    code,
    parity: 1,
    keyCount: enKeyCount,
    visible: true,
  }));
  for (const code of GATED_LANGUAGES) {
    const data = locales[code];
    if (!data) {
      result.push({ code, parity: 0, keyCount: 0, visible: false });
      continue;
    }
    const keyCount = countKeys(data);
    const parity = enKeyCount === 0 ? 0 : keyCount / enKeyCount;
    result.push({
      code,
      parity,
      keyCount,
      visible: parity >= PARITY_THRESHOLD,
    });
  }
  return result;
};

export const visibleLanguages = (
  locales: Partial<Record<GatedLanguage, Record<string, unknown> | undefined>> = {},
): SupportedLanguage[] =>
  evaluateLocaleParity(locales)
    .filter((entry) => entry.visible)
    .map((entry) => entry.code);

const gatedLocaleModules = import.meta.glob<Record<string, unknown>>(
  '@/app/locales/{ar,de,ru}.json',
  { eager: true, import: 'default' },
);

export const loadGatedLocales = (): Partial<
  Record<GatedLanguage, Record<string, unknown> | undefined>
> => {
  const out: Partial<Record<GatedLanguage, Record<string, unknown> | undefined>> = {};
  for (const [path, value] of Object.entries(gatedLocaleModules)) {
    const match = /\/locales\/(ar|de|ru)\.json$/.exec(path);
    if (!match) continue;
    const code = match[1] as GatedLanguage;
    out[code] = value;
  }
  return out;
};
