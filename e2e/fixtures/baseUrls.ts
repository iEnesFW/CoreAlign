import type { RoleName } from './roles';

const env = process.env;

export const adminBaseUrl = env.E2E_ADMIN_URL ?? 'http://localhost:5273';
export const customerBaseUrl = env.E2E_CUSTOMER_URL ?? 'http://localhost:5274';
export const b2bBaseUrl = env.E2E_B2B_URL ?? 'http://localhost:5275';

export const baseUrlForRole: Record<RoleName, string> = {
  admin: adminBaseUrl,
  'customer-portal': customerBaseUrl,
  b2b: b2bBaseUrl,
};
