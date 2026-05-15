export type CustomerType = 'Individual' | 'Business' | 'Government';
export type CustomerStatus = 'Active' | 'Blocked' | 'Archived';

export interface Customer {
  id: string;
  code: string | null;
  type: CustomerType;
  name: string;
  legalName: string | null;
  tradeName: string | null;
  nationalId: string | null;
  taxNumber: string | null;
  taxOffice: string | null;
  email: string | null;
  phone: string | null;
  website: string | null;
  defaultCurrency: string;
  paymentTermsId: string | null;
  priceListId: string | null;
  customerGroupId: string | null;
  salesRepUserId: string | null;
  creditLimit: number;
  currentBalance: number;
  overdueAmount: number;
  defaultDiscountPercent: number;
  classification: string | null;
  channel: string | null;
  territory: string | null;
  languageCode: string | null;
  parentCustomerId: string | null;
  status: CustomerStatus;
  blockReason: string | null;
  notes: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateCustomerInput {
  name: string;
  type?: CustomerType;
  code?: string | null;
  legalName?: string | null;
  tradeName?: string | null;
  nationalId?: string | null;
  taxNumber?: string | null;
  taxOffice?: string | null;
  email?: string | null;
  phone?: string | null;
  website?: string | null;
  defaultCurrency?: string;
  paymentTermsId?: string | null;
  priceListId?: string | null;
  customerGroupId?: string | null;
  salesRepUserId?: string | null;
  creditLimit?: number;
  defaultDiscountPercent?: number;
  classification?: string | null;
  channel?: string | null;
  territory?: string | null;
  languageCode?: string | null;
  parentCustomerId?: string | null;
  notes?: string | null;
}

export interface UpdateCustomerInput {
  id: string;
  name: string;
  type: CustomerType;
  legalName?: string | null;
  tradeName?: string | null;
  nationalId?: string | null;
  taxNumber?: string | null;
  taxOffice?: string | null;
  email?: string | null;
  phone?: string | null;
  website?: string | null;
  defaultCurrency: string;
  paymentTermsId?: string | null;
  priceListId?: string | null;
  customerGroupId?: string | null;
  salesRepUserId?: string | null;
  creditLimit: number;
  defaultDiscountPercent: number;
  classification?: string | null;
  channel?: string | null;
  territory?: string | null;
  languageCode?: string | null;
  parentCustomerId?: string | null;
  notes?: string | null;
  status: CustomerStatus;
}

export interface CustomerListParams {
  page: number;
  pageSize: number;
  search?: string;
  isActive?: boolean;
}

export interface CustomerSummary {
  customerId: string;
  orderCount: number;
  totalOrderAmount: number;
  invoiceCount: number;
  totalInvoiced: number;
  totalPaid: number;
  outstanding: number;
  currency: string;
}

export interface CustomerActivityItem {
  occurredAtUtc: string;
  kind: 'Order' | 'Invoice' | 'Payment' | string;
  sourceId: string;
  sourceNumber: string | null;
  status: string | null;
  amount: number;
  currency: string;
  description: string | null;
}

export interface CustomerOverview {
  customerId: string;
  groupName: string | null;
  salesRepName: string | null;
  priceListName: string | null;
  paymentTermsName: string | null;
  paymentTermsNetDays: number | null;
  primaryBillingAddress: CustomerAddress | null;
  primaryShippingAddress: CustomerAddress | null;
  primaryContact: CustomerContact | null;
  lastOrderAtUtc: string | null;
  lastInvoiceAtUtc: string | null;
  lastPaymentAtUtc: string | null;
  currentBalance: number;
  outstanding: number;
  creditLimit: number;
  creditAvailable: number;
  creditUsedPercent: number;
  isOverCreditLimit: boolean;
  recentActivity: CustomerActivityItem[];
}

export type CustomerTransactionType = 'InvoiceIssued' | 'Payment' | 'Refund' | 'Adjustment';

export interface CustomerTransaction {
  id: string;
  customerId: string;
  occurredAtUtc: string;
  type: CustomerTransactionType;
  amount: number;
  currency: string;
  invoiceId: string | null;
  orderId: string | null;
  reference: string | null;
  notes: string | null;
}

export interface CustomerAddress {
  id: string;
  customerId: string;
  label: string;
  line1: string;
  line2: string | null;
  city: string | null;
  state: string | null;
  postalCode: string | null;
  country: string | null;
  isPrimary: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CustomerAddressInput {
  customerId: string;
  label: string;
  line1: string;
  line2?: string | null;
  city?: string | null;
  state?: string | null;
  postalCode?: string | null;
  country?: string | null;
  isPrimary: boolean;
}

export interface UpdateCustomerAddressInput extends CustomerAddressInput {
  id: string;
}

export interface CustomerContact {
  id: string;
  customerId: string;
  name: string;
  role: string | null;
  email: string | null;
  phone: string | null;
  notes: string | null;
  isPrimary: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CustomerContactInput {
  customerId: string;
  name: string;
  role?: string | null;
  email?: string | null;
  phone?: string | null;
  notes?: string | null;
  isPrimary: boolean;
}

export interface UpdateCustomerContactInput extends CustomerContactInput {
  id: string;
}
