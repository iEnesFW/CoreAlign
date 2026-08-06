/**
 * Input masks + identity checksums for Turkish business fields.
 *
 * WHY the checksums live here and not only on the server: a mistyped VKN/TCKN is accepted by every
 * length-only rule in the stack and only surfaces when the e-invoice integrator rejects the
 * document — days later, on a real customer. These are the official algorithms, so the form can
 * refuse the typo at the keystroke that made it.
 */

const digitsOnly = (value: string): string => value.replace(/\D+/g, '');

export const maskDigits = (value: string, maxLength: number): string =>
  digitsOnly(value).slice(0, maxLength);

/** VKN — 10 digits. */
export const maskTaxNumber = (value: string): string => maskDigits(value, 10);

/** TCKN — 11 digits. */
export const maskNationalId = (value: string): string => maskDigits(value, 11);

/** MERSIS — 16 digits. */
export const maskMersisNumber = (value: string): string => maskDigits(value, 16);

/**
 * Phone — keeps a leading `+` (international) and groups the rest for readability.
 * A Turkish national number renders as `0532 123 45 67`; anything else stays grouped in 3s/4s.
 */
export const maskPhone = (value: string): string => {
  const plus = value.trimStart().startsWith('+');
  const digits = digitsOnly(value).slice(0, plus ? 15 : 11);
  if (digits.length === 0) return plus ? '+' : '';
  if (plus) {
    const groups = digits.match(/.{1,3}/g) ?? [];
    return `+${groups.join(' ')}`;
  }
  const parts = [digits.slice(0, 4), digits.slice(4, 7), digits.slice(7, 9), digits.slice(9, 11)];
  return parts.filter((part) => part.length > 0).join(' ');
};

/** IBAN — uppercase, grouped in 4s, capped at the 34-char ISO 13616 maximum. */
export const maskIban = (value: string): string => {
  const raw = value
    .replace(/[^0-9a-zA-Z]+/g, '')
    .toUpperCase()
    .slice(0, 34);
  const groups = raw.match(/.{1,4}/g) ?? [];
  return groups.join(' ');
};

/**
 * TCKN checksum (official rule): 11 digits, first ≠ 0;
 * d10 = ((Σ odd positions)·7 − (Σ even positions)) mod 10, d11 = (Σ first ten) mod 10.
 */
export const isValidNationalId = (value: string): boolean => {
  const d = digitsOnly(value);
  if (d.length !== 11 || d[0] === '0') return false;
  const n = [...d].map(Number);
  const odd = n[0] + n[2] + n[4] + n[6] + n[8];
  const even = n[1] + n[3] + n[5] + n[7];
  const tenth = (odd * 7 - even) % 10;
  if ((tenth + 10) % 10 !== n[9]) return false;
  const sumFirstTen = n.slice(0, 10).reduce((a, b) => a + b, 0);
  return sumFirstTen % 10 === n[10];
};

/**
 * VKN checksum (Gelir İdaresi rule): 10 digits; for each of the first nine, tmp = (digit + 10 − i)
 * mod 10, and when tmp ≠ 0 it contributes (tmp · 2^(9−i)) mod 9 — with the special case that a
 * non-zero power result of 0 counts as 9. The last digit is (10 − sum mod 10) mod 10.
 */
export const isValidTaxNumber = (value: string): boolean => {
  const d = digitsOnly(value);
  if (d.length !== 10) return false;
  let sum = 0;
  for (let i = 0; i < 9; i += 1) {
    const tmp = (Number(d[i]) + 10 - i) % 10;
    if (tmp === 0) continue;
    const powered = (tmp * 2 ** (9 - i)) % 9;
    sum += powered === 0 ? 9 : powered;
  }
  return (10 - (sum % 10)) % 10 === Number(d[9]);
};

/** IBAN mod-97 (ISO 13616): rearrange, letters → numbers, remainder must be 1. */
export const isValidIban = (value: string): boolean => {
  const raw = value.replace(/\s+/g, '').toUpperCase();
  if (!/^[A-Z]{2}\d{2}[0-9A-Z]{10,30}$/.test(raw)) return false;
  const rearranged = raw.slice(4) + raw.slice(0, 4);
  let remainder = 0;
  for (const char of rearranged) {
    const code = char >= 'A' && char <= 'Z' ? String(char.charCodeAt(0) - 55) : char;
    for (const digit of code) remainder = (remainder * 10 + Number(digit)) % 97;
  }
  return remainder === 1;
};
