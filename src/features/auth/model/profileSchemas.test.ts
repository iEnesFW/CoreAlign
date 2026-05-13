import { describe, expect, it } from 'vitest';
import { changePasswordSchema, profileSchema } from './profileSchemas';

describe('profileSchema', () => {
  it('accepts empty optional fields', () => {
    const result = profileSchema.safeParse({
      firstName: '',
      lastName: '',
      phoneNumber: '',
      avatarUrl: '',
    });
    expect(result.success).toBe(true);
  });

  it('accepts normal values', () => {
    const result = profileSchema.safeParse({
      firstName: 'John',
      lastName: 'Doe',
      phoneNumber: '+1 555 0100',
      avatarUrl: 'https://cdn.example/me.png',
    });
    expect(result.success).toBe(true);
  });

  it('rejects firstName longer than 64 chars', () => {
    const result = profileSchema.safeParse({
      firstName: 'A'.repeat(65),
      lastName: '',
      phoneNumber: '',
      avatarUrl: '',
    });
    expect(result.success).toBe(false);
  });
});

const validChange = {
  currentPassword: 'OldStrong1!',
  newPassword: 'NewStrong1!',
  confirmPassword: 'NewStrong1!',
};

describe('changePasswordSchema', () => {
  it('accepts strong new password', () => {
    expect(changePasswordSchema.safeParse(validChange).success).toBe(true);
  });

  it('rejects mismatched confirm', () => {
    const result = changePasswordSchema.safeParse({
      ...validChange,
      confirmPassword: 'Different1!',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.message === 'Validation.PasswordMismatch')).toBe(
        true,
      );
    }
  });

  it('rejects new password equal to current', () => {
    const result = changePasswordSchema.safeParse({
      currentPassword: 'Same123!',
      newPassword: 'Same123!',
      confirmPassword: 'Same123!',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.message === 'Validation.PasswordMustDiffer')).toBe(
        true,
      );
    }
  });

  it('rejects weak new password (no special char)', () => {
    const result = changePasswordSchema.safeParse({
      currentPassword: 'OldStrong1!',
      newPassword: 'NoSpecial1',
      confirmPassword: 'NoSpecial1',
    });
    expect(result.success).toBe(false);
  });
});
