import { describe, expect, it } from 'vitest';
import { filterOptions, type SearchableOption } from './searchableSelectFilter';

const opts: SearchableOption[] = [
  { value: '1', label: 'Temperli 8 mm', keywords: '8' },
  { value: '2', label: 'Lamine 10 mm', keywords: '10 güvenlik' },
  { value: '3', label: 'Çift Cam 24 mm', keywords: '24 ısı' },
];

describe('filterOptions', () => {
  it('returns all options for an empty query', () => {
    expect(filterOptions(opts, '')).toHaveLength(3);
    expect(filterOptions(opts, '   ')).toHaveLength(3);
  });

  it('matches a case-insensitive substring of the label', () => {
    expect(filterOptions(opts, 'lam').map((o) => o.value)).toEqual(['2']);
    expect(filterOptions(opts, 'TeMpErLi').map((o) => o.value)).toEqual(['1']);
  });

  it('matches against keywords as well as the label', () => {
    expect(filterOptions(opts, 'güvenlik').map((o) => o.value)).toEqual(['2']);
    expect(filterOptions(opts, '24').map((o) => o.value)).toEqual(['3']);
  });

  it('requires every whitespace-separated term to match (AND)', () => {
    expect(filterOptions(opts, 'cam 24').map((o) => o.value)).toEqual(['3']);
    expect(filterOptions(opts, 'cam 99')).toHaveLength(0);
  });
});
