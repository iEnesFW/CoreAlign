export const clamp01 = (x: number) => Math.max(0, Math.min(1, x));

export const smooth = (x: number) => {
  const c = clamp01(x);
  return c * c * (3 - 2 * c);
};

export const lerp = (a: number, b: number, t: number) => a + (b - a) * t;

export const seg = (p: number, start: number, len: number) => smooth((p - start) / len);
