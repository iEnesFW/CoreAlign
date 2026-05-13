export type PaymentDirection =
  | 'CustomerReceipt'
  | 'CustomerRefund'
  | 'SupplierPayment'
  | 'SupplierRefundReceived';

export type PaymentStatus =
  | 'Draft'
  | 'Confirmed'
  | 'PartiallyApplied'
  | 'FullyApplied'
  | 'Refunded'
  | 'Void';

export type PaymentMethod =
  | 'Cash'
  | 'BankTransfer'
  | 'CreditCard'
  | 'DebitCard'
  | 'Check'
  | 'PromissoryNote'
  | 'OnlineGateway'
  | 'Other';

export type LedgerEntryType = 'Debit' | 'Credit';

export type LedgerSourceType =
  | 'OpeningBalance'
  | 'Invoice'
  | 'InvoiceVoid'
  | 'CreditNote'
  | 'Payment'
  | 'PaymentReversal'
  | 'Adjustment'
  | 'Refund';

export interface PaymentApplicationItem {
  id: string;
  paymentId: string;
  invoiceId: string;
  invoiceNumber: string;
  appliedAmount: number;
  appliedAtUtc: string;
}

export interface Payment {
  id: string;
  paymentNumber: string;
  direction: PaymentDirection;
  status: PaymentStatus;
  customerId: string;
  customerName: string;
  paymentDate: string;
  postingDate: string;
  method: PaymentMethod;
  currency: string;
  exchangeRate: number;
  amount: number;
  appliedAmount: number;
  unappliedAmount: number;
  bankAccountInfo: string | null;
  referenceNumber: string | null;
  checkNumber: string | null;
  checkDueDate: string | null;
  confirmedAtUtc: string | null;
  voidedAtUtc: string | null;
  voidReason: string | null;
  notes: string | null;
  applications: PaymentApplicationItem[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface PaymentSummary {
  id: string;
  paymentNumber: string;
  direction: PaymentDirection;
  status: PaymentStatus;
  customerId: string;
  customerName: string;
  paymentDate: string;
  method: PaymentMethod;
  amount: number;
  unappliedAmount: number;
  currency: string;
}

export interface CustomerLedgerEntry {
  id: string;
  customerId: string;
  occurredAtUtc: string;
  postingDate: string;
  entryType: LedgerEntryType;
  amount: number;
  currency: string;
  amountInBase: number;
  sourceType: LedgerSourceType;
  sourceDocumentId: string | null;
  sourceDocumentNumber: string | null;
  runningBalanceAfter: number;
  description: string | null;
}

export interface AgingBucket {
  bucket: string;
  amount: number;
  invoiceCount: number;
}

export interface CustomerAging {
  customerId: string;
  currency: string;
  current: number;
  days1To30: number;
  days31To60: number;
  days61To90: number;
  daysOver90: number;
  totalOutstanding: number;
  buckets: AgingBucket[];
}

export interface ApplyPaymentLine {
  invoiceId: string;
  appliedAmount: number;
}

export interface CreatePaymentInput {
  customerId: string;
  paymentDate: string;
  method: PaymentMethod;
  amount: number;
  currency?: string;
  direction?: PaymentDirection;
  exchangeRate?: number;
  bankAccountInfo?: string | null;
  referenceNumber?: string | null;
  checkNumber?: string | null;
  checkDueDate?: string | null;
  notes?: string | null;
  autoConfirm?: boolean;
  applications?: ApplyPaymentLine[];
}

export interface ApplyPaymentInput {
  id: string;
  applications: ApplyPaymentLine[];
}
