import { describe, expect, it } from 'vitest';
import type { Customer } from './customer.types';
import { buildCustomerUpdateInput } from './customerUpdateMerge';

const customer = (id: string, name: string): Customer => ({
  id,
  code: 'CUS-1',
  type: 'Business',
  name,
  legalName: 'Legal AŞ',
  tradeName: null,
  nationalId: null,
  taxNumber: '1234567890',
  taxOffice: 'Kadıköy',
  email: 'a@b.co',
  phone: '+905551112233',
  website: null,
  defaultCurrency: 'TRY',
  paymentTermsId: null,
  priceListId: null,
  customerGroupId: null,
  salesRepUserId: null,
  creditLimit: 50000,
  currentBalance: 1234.56,
  overdueAmount: 100,
  defaultDiscountPercent: 5,
  classification: 'A',
  channel: null,
  territory: null,
  languageCode: 'tr',
  parentCustomerId: null,
  status: 'Active',
  blockReason: null,
  notes: 'not',
  isActive: true,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: '2026-06-01T00:00:00Z',
});

describe('buildCustomerUpdateInput', () => {
  it('overrides only the name and preserves money/status/type fields', () => {
    const input = buildCustomerUpdateInput(customer('c1', 'Eski Ad'), { name: 'Yeni Ad' });

    expect(input.name).toBe('Yeni Ad');
    expect(input.creditLimit).toBe(50000);
    expect(input.defaultDiscountPercent).toBe(5);
    expect(input.status).toBe('Active');
    expect(input.type).toBe('Business');
    expect(input.defaultCurrency).toBe('TRY');
    expect(input.taxNumber).toBe('1234567890');
  });

  it('does not leak Customer-only fields into the payload', () => {
    const input = buildCustomerUpdateInput(customer('c1', 'Ad'));
    const keys = Object.keys(input);

    expect(keys).not.toContain('code');
    expect(keys).not.toContain('currentBalance');
    expect(keys).not.toContain('overdueAmount');
    expect(keys).not.toContain('blockReason');
    expect(keys).not.toContain('isActive');
    expect(keys).not.toContain('createdAtUtc');
    expect(keys).not.toContain('updatedAtUtc');
  });

  it('returns an unchanged snapshot with empty overrides', () => {
    const c = customer('c1', 'Ad');
    const input = buildCustomerUpdateInput(c);

    expect(input.id).toBe('c1');
    expect(input.name).toBe('Ad');
    expect(input.legalName).toBe('Legal AŞ');
    expect(input.notes).toBe('not');
  });
});
