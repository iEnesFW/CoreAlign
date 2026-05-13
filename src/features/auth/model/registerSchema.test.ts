import { describe, expect, it } from 'vitest';
import { registerSchema } from './registerSchema';

const validInput = {
  organizationName: 'Acme Inc.',
  firstName: '',
  lastName: '',
  username: 'acmeuser',
  email: 'user@acme.com',
  password: 'StrongPass1!',
  confirmPassword: 'StrongPass1!',
};

describe('registerSchema', () => {
  it('accepts valid registration', () => {
    expect(registerSchema.safeParse(validInput).success).toBe(true);
  });

  it('rejects mismatched passwords', () => {
    const result = registerSchema.safeParse({ ...validInput, confirmPassword: 'Different1!' });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.message === 'Validation.PasswordMismatch')).toBe(
        true,
      );
    }
  });

  it('rejects weak password missing uppercase', () => {
    const result = registerSchema.safeParse({
      ...validInput,
      password: 'weakpass1!',
      confirmPassword: 'weakpass1!',
    });
    expect(result.success).toBe(false);
  });

  it('rejects short organization name', () => {
    const result = registerSchema.safeParse({ ...validInput, organizationName: 'A' });
    expect(result.success).toBe(false);
  });

  it('rejects username with invalid chars', () => {
    const result = registerSchema.safeParse({ ...validInput, username: 'invalid user!' });
    expect(result.success).toBe(false);
  });
});
