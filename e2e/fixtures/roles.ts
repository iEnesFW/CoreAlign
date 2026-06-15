import { adminUser, customerUser, dealerUser, type UserCredentials } from './credentials';

export type RoleName = 'admin' | 'customer-portal' | 'b2b';

export interface RoleProfile {
  name: RoleName;
  credentials: UserCredentials;
  storageStatePath: string;
  postLoginPath: string;
  postLoginUrlPattern: RegExp;
}

const storageStateFor = (role: RoleName) => `e2e/.auth/${role}.json`;

export const roleProfiles: Record<RoleName, RoleProfile> = {
  admin: {
    name: 'admin',
    credentials: adminUser,
    storageStatePath: storageStateFor('admin'),
    postLoginPath: '/dashboard',
    postLoginUrlPattern: /\/dashboard/,
  },
  'customer-portal': {
    name: 'customer-portal',
    credentials: customerUser,
    storageStatePath: storageStateFor('customer-portal'),
    postLoginPath: '/',
    postLoginUrlPattern: /\/(invoices|dashboard|portal|customer-portal)/,
  },
  b2b: {
    name: 'b2b',
    credentials: dealerUser,
    storageStatePath: storageStateFor('b2b'),
    postLoginPath: '/',
    postLoginUrlPattern: /\/(customers|orders|dashboard)/,
  },
};
