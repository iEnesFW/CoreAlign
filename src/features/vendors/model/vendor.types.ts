export type VendorType = 'Individual' | 'Business';
export type VendorStatus = 'Active' | 'Blocked' | 'Archived' | 'PendingApproval';
export type LedgerEntryType = 'Debit' | 'Credit';

export interface Vendor {
  id: string;
  code?: string | null;
  type: VendorType;
  name: string;
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
  paymentTermsName?: string | null;
  buyerUserId?: string | null;
  classification?: string | null;
  territory?: string | null;
  languageCode?: string | null;
  parentVendorId?: string | null;
  status: VendorStatus;
  blockReason?: string | null;
  notes?: string | null;
  rating?: number | null;
  currentBalance: number;
  overdueAmount: number;
  totalPayable: number;
  approvedAtUtc?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface VendorSummary {
  id: string;
  code?: string | null;
  name: string;
  legalName?: string | null;
  taxNumber?: string | null;
  email?: string | null;
  phone?: string | null;
  type: VendorType;
  status: VendorStatus;
  defaultCurrency: string;
  currentBalance: number;
  overdueAmount: number;
}

export interface VendorAddress {
  id: string;
  vendorId: string;
  label: string;
  line1: string;
  line2?: string | null;
  city?: string | null;
  state?: string | null;
  postalCode?: string | null;
  country?: string | null;
  isPrimary: boolean;
}

export interface VendorContact {
  id: string;
  vendorId: string;
  name: string;
  role?: string | null;
  email?: string | null;
  phone?: string | null;
  notes?: string | null;
  isPrimary: boolean;
}

export interface VendorBankAccount {
  id: string;
  vendorId: string;
  bankName: string;
  branchName?: string | null;
  accountHolder: string;
  iban: string;
  swift?: string | null;
  currency: string;
  accountNumber?: string | null;
  isPrimary: boolean;
  notes?: string | null;
}

export interface VendorLedgerEntry {
  id: string;
  vendorId: string;
  occurredAtUtc: string;
  postingDate: string;
  entryType: LedgerEntryType;
  amount: number;
  currency: string;
  exchangeRate: number;
  amountInBase: number;
  sourceType: string;
  sourceDocumentId?: string | null;
  sourceDocumentNumber?: string | null;
  runningBalanceAfter: number;
  description?: string | null;
}

export interface CreateVendorRequest {
  name: string;
  type?: VendorType;
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
  buyerUserId?: string | null;
  classification?: string | null;
  territory?: string | null;
  languageCode?: string | null;
  parentVendorId?: string | null;
  notes?: string | null;
}

export interface VendorListParams {
  search?: string;
  status?: VendorStatus;
  page?: number;
  pageSize?: number;
}
