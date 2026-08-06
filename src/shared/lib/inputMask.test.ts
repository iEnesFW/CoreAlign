import { describe, expect, it } from 'vitest';
import {
  isValidIban,
  isValidNationalId,
  isValidTaxNumber,
  maskIban,
  maskMersisNumber,
  maskNationalId,
  maskPhone,
  maskTaxNumber,
} from './inputMask';

describe('masks keep the field to what the field can hold', () => {
  it('strips everything but digits and caps the length', () => {
    expect(maskTaxNumber('12a3-45 67.890123')).toBe('1234567890');
    expect(maskNationalId('abc10000000146xyz')).toBe('10000000146');
    expect(maskMersisNumber('0-123-456-789-012-3456')).toBe('0123456789012345');
  });

  it('groups a national phone number and keeps an international prefix', () => {
    expect(maskPhone('05321234567')).toBe('0532 123 45 67');
    expect(maskPhone('0532')).toBe('0532');
    expect(maskPhone('+905321234567')).toBe('+905 321 234 567');
    expect(maskPhone('')).toBe('');
  });

  it('groups an IBAN in fours and uppercases it', () => {
    expect(maskIban('tr330006100519786457841326')).toBe('TR33 0006 1005 1978 6457 8413 26');
  });
});

describe('identity checksums refuse a typo the length rule accepts', () => {
  it('accepts a valid TCKN and refuses a one-digit typo', () => {
    expect(isValidNationalId('10000000146')).toBe(true);
    expect(isValidNationalId('10000000147')).toBe(false);
  });

  it('refuses a TCKN of the wrong length or starting with zero', () => {
    expect(isValidNationalId('1000000014')).toBe(false);
    expect(isValidNationalId('01000000146')).toBe(false);
  });

  it('accepts a valid VKN and refuses a wrong check digit', () => {
    // Check digits derived from the GİB rule itself, not guessed.
    expect(isValidTaxNumber('1234567899')).toBe(true);
    expect(isValidTaxNumber('4444444443')).toBe(true);
    expect(isValidTaxNumber('1234567890')).toBe(false);
    expect(isValidTaxNumber('123456789')).toBe(false);
  });

  it('accepts a real IBAN and refuses a mutated check digit', () => {
    expect(isValidIban('TR33 0006 1005 1978 6457 8413 26')).toBe(true);
    expect(isValidIban('TR34 0006 1005 1978 6457 8413 26')).toBe(false);
    expect(isValidIban('TR33')).toBe(false);
  });
});
