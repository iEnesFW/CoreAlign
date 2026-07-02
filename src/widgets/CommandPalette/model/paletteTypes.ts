export type PaletteKind = 'customer' | 'order' | 'invoice' | 'quote' | 'product';

export interface PaletteResult {
  id: string;
  kind: PaletteKind;
  label: string;
  sublabel?: string;
  to: string;
}

export interface PaletteGroup {
  kind: PaletteKind;
  results: PaletteResult[];
}

export const GROUP_ORDER: PaletteKind[] = ['customer', 'order', 'invoice', 'quote', 'product'];
