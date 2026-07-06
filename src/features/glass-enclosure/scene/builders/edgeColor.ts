import { Color, SRGBColorSpace } from 'three';

const derivedCache = new Map<string, string>();

// Outline color derived from the painted body: the fixed light slate edge constants read as
// white corner lines on dark paints. Unpainted bodies (and textured ones, which render a white
// base + map) keep the design-system fallback untouched.
export const edgeColorFor = (bodyHex: string | null | undefined, fallback: string): string => {
  if (!bodyHex) return fallback;
  const cached = derivedCache.get(bodyHex);
  if (cached) return cached;
  const body = new Color(bodyHex);
  // WHY: read/write HSL in sRGB (three's default working space is LINEAR — a linear-space
  // threshold would fire the near-black branch for ordinary mid-dark paints and mis-scale the
  // offsets, inverting the darken direction the constants are calibrated for).
  const hsl = { h: 0, s: 0, l: 0 };
  body.getHSL(hsl, SRGBColorSpace);
  const lightness = hsl.l < 0.12 ? hsl.l + 0.16 : hsl.l - 0.18;
  const derived = new Color().setHSL(
    hsl.h,
    hsl.s,
    Math.min(1, Math.max(0, lightness)),
    SRGBColorSpace,
  );
  const hex = `#${derived.getHexString(SRGBColorSpace)}`;
  derivedCache.set(bodyHex, hex);
  return hex;
};
