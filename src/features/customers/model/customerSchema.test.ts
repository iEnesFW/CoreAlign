import { describe, expect, it } from 'vitest';
import { customerSchema } from './customerSchema';

const minimal = {
  name: 'Acme',
  type: 'Business' as const,
  defaultCurrency: 'TRY',
  email: '',
  phone: '',
  taxNumber: '',
  notes: '',
  isActive: true,
};

describe('customerSchema', () => {
  it('accepts minimal valid customer', () => {
    expect(customerSchema.safeParse(minimal).success).toBe(true);
  });

  it('accepts valid email', () => {
    const result = customerSchema.safeParse({ ...minimal, email: 'billing@acme.com' });
    expect(result.success).toBe(true);
  });

  it('rejects invalid email format when provided', () => {
    const result = customerSchema.safeParse({ ...minimal, email: 'not-an-email' });
    expect(result.success).toBe(false);
  });

  it('rejects name shorter than 2 chars', () => {
    const result = customerSchema.safeParse({ ...minimal, name: 'A' });
    expect(result.success).toBe(false);
  });

  it('rejects name longer than 200 chars', () => {
    const result = customerSchema.safeParse({ ...minimal, name: 'A'.repeat(201) });
    expect(result.success).toBe(false);
  });
});
