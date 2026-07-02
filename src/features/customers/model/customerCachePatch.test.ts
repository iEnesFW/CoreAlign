import { describe, expect, it } from 'vitest';
import type { Customer } from './customer.types';
import { buildCustomerUpdateInput } from './customerUpdateMerge';
import { patchCustomerInPaged, type PagedCustomersResponse } from './customerCachePatch';

const customer = (id: string, name: string): Customer => ({
  id,
  code: null,
  type: 'Individual',
  name,
  legalName: null,
  tradeName: null,
  nationalId: null,
  taxNumber: null,
  taxOffice: null,
  email: null,
  phone: null,
  website: null,
  defaultCurrency: 'TRY',
  paymentTermsId: null,
  priceListId: null,
  customerGroupId: null,
  salesRepUserId: null,
  creditLimit: 0,
  currentBalance: 750.25,
  overdueAmount: 50,
  defaultDiscountPercent: 0,
  classification: null,
  channel: null,
  territory: null,
  languageCode: null,
  parentCustomerId: null,
  status: 'Active',
  blockReason: null,
  notes: null,
  isActive: true,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: '2026-06-01T00:00:00Z',
});

const page = (...customers: Customer[]): PagedCustomersResponse => ({
  isSuccess: true,
  data: { items: customers, total: customers.length, page: 1, pageSize: 10 },
  errors: [],
  statusCode: 200,
});

describe('patchCustomerInPaged', () => {
  it('patches only the matching row and preserves Customer-only fields', () => {
    const old = page(customer('a', 'Eski'), customer('b', 'Diğer'));
    const input = buildCustomerUpdateInput(old.data!.items[0], { name: 'Yeni' });

    const patched = patchCustomerInPaged(old, input);

    expect(patched).not.toBe(old);
    expect(patched!.data!.items[0].name).toBe('Yeni');
    expect(patched!.data!.items[0].currentBalance).toBe(750.25);
    expect(patched!.data!.items[0].overdueAmount).toBe(50);
    expect(patched!.data!.items[1]).toBe(old.data!.items[1]);
  });

  it('returns the same reference when the page does not contain the customer', () => {
    const old = page(customer('x', 'Başka'));
    const input = buildCustomerUpdateInput(customer('zzz', 'Yok'), { name: 'Yeni' });

    expect(patchCustomerInPaged(old, input)).toBe(old);
  });

  it('returns undefined/null-data caches untouched', () => {
    const input = buildCustomerUpdateInput(customer('a', 'Ad'), { name: 'Yeni' });
    expect(patchCustomerInPaged(undefined, input)).toBeUndefined();

    const nullData: PagedCustomersResponse = {
      isSuccess: false,
      data: null,
      errors: ['x'],
      statusCode: 500,
    };
    expect(patchCustomerInPaged(nullData, input)).toBe(nullData);
  });
});
