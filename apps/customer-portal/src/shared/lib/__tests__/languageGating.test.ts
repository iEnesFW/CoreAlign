import { describe, expect, it } from 'vitest';
import {
  evaluateLocaleParity,
  PARITY_THRESHOLD,
  visibleLanguages,
  loadGatedLocales,
} from '@/shared/lib/languageGating';
import en from '@/app/locales/en.json';

const countKeys = (value: unknown, prefix = ''): number => {
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
};

const enKeyCount = countKeys(en as unknown as Record<string, unknown>);

const buildLocale = (parity: number): Record<string, unknown> => {
  const target = Math.floor(enKeyCount * parity);
  const flat: Record<string, string> = {};
  for (let i = 0; i < target; i += 1) flat[`k${i}`] = 'v';
  return flat;
};

describe('languageGating', () => {
  it('keeps tr and en always visible', () => {
    const result = visibleLanguages({});
    expect(result).toContain('tr');
    expect(result).toContain('en');
  });

  it('hides gated locales that are absent', () => {
    const result = visibleLanguages({});
    expect(result).not.toContain('de');
    expect(result).not.toContain('ar');
    expect(result).not.toContain('ru');
  });

  it('hides gated locales below the parity threshold', () => {
    const halfParity = buildLocale(PARITY_THRESHOLD / 2);
    const result = visibleLanguages({ de: halfParity });
    expect(result).not.toContain('de');
  });

  it('reveals gated locales at or above the parity threshold', () => {
    const fullParity = buildLocale(1);
    const result = visibleLanguages({ ru: fullParity });
    expect(result).toContain('ru');
  });

  it('reports parity ratio for evaluation results', () => {
    const partial = buildLocale(0.5);
    const evaluation = evaluateLocaleParity({ ar: partial });
    const ar = evaluation.find((e) => e.code === 'ar');
    expect(ar).toBeDefined();
    expect(ar?.visible).toBe(false);
    expect(ar!.parity).toBeGreaterThan(0);
    expect(ar!.parity).toBeLessThan(PARITY_THRESHOLD);
  });

  it('loadGatedLocales returns an object discoverable at build time', () => {
    const loaded = loadGatedLocales();
    expect(loaded).toBeTypeOf('object');
  });
});
