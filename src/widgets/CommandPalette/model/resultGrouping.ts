import {
  GROUP_ORDER,
  type PaletteGroup,
  type PaletteKind,
  type PaletteResult,
} from './paletteTypes';

export const buildGroups = (
  byKind: Partial<Record<PaletteKind, PaletteResult[]>>,
  capPerKind = 5,
): PaletteGroup[] =>
  GROUP_ORDER.map((kind) => ({ kind, results: (byKind[kind] ?? []).slice(0, capPerKind) })).filter(
    (g) => g.results.length > 0,
  );

export const flattenGroups = (groups: PaletteGroup[]): PaletteResult[] =>
  groups.flatMap((g) => g.results);
