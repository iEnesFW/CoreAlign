import type { ReactNode } from 'react';

export interface SearchableOption {
  value: string;
  label: string;
  keywords?: string;
  render?: ReactNode;
}

export const filterOptions = (options: SearchableOption[], query: string): SearchableOption[] => {
  const q = query.trim().toLowerCase();
  if (!q) return options;
  const terms = q.split(/\s+/);
  return options.filter((o) => {
    const hay = `${o.label} ${o.keywords ?? ''}`.toLowerCase();
    return terms.every((term) => hay.includes(term));
  });
};
