export type OrderStatus =
  | 'Draft'
  | 'Submitted'
  | 'Approved'
  | 'Allocated'
  | 'Picking'
  | 'Packed'
  | 'PartiallyShipped'
  | 'Shipped'
  | 'Delivered'
  | 'Closed'
  | 'Returned'
  | 'Cancelled'
  | 'Confirmed';

export type InvoiceStatus =
  | 'Draft'
  | 'Issued'
  | 'Sent'
  | 'PartiallyPaid'
  | 'Paid'
  | 'Overdue'
  | 'Void'
  | 'Cancelled';

export type DealerAccountStatus = 'Active' | 'Suspended' | 'Archived';
export type MembershipStatus = 'Active' | 'Suspended' | 'Archived';

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface OrderSummary {
  id: string;
  orderNumber: string;
  customerId: string;
  customerName: string;
  orderDate: string;
  status: OrderStatus;
  currency: string;
  total: number;
}

export interface OrderLine {
  id: string;
  lineNumber: number;
  productSku: string;
  productName: string;
  uomCode: string | null;
  quantity: number;
  unitPrice: number;
  lineDiscountAmount: number;
  taxAmount: number;
  lineNetAmount: number;
  lineTotal: number;
}

export interface OrderDetail extends OrderSummary {
  subtotal: number;
  taxTotal: number;
  shippingCost: number;
  customerNotes?: string | null;
  lines: OrderLine[];
}

export interface InvoiceSummary {
  id: string;
  invoiceNumber: string;
  customerName: string;
  issueDate: string;
  dueDate: string;
  status: InvoiceStatus;
  currency: string;
  total: number;
  amountPaid: number;
  amountDue: number;
  isOverdue: boolean;
}

export interface InvoiceLine {
  id: string;
  lineNumber: number;
  productSku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineDiscountAmount: number;
  taxAmount: number;
  lineNetAmount: number;
  lineTotal: number;
}

export interface InvoiceDetail extends InvoiceSummary {
  subtotal: number;
  taxTotal: number;
  shippingCost: number;
  publicNotes?: string | null;
  lines: InvoiceLine[];
}

export interface CustomerPortalDashboard {
  customerId: string;
  customerName: string;
  totalActiveOrders: number;
  totalOpenInvoices: number;
  openInvoiceTotalAmount: number;
  openInvoiceCurrency: string;
  totalActiveDealers: number;
  invoicedLast30DaysAmount: number;
  invoicedLast30DaysCurrency: string;
  recentOrders: OrderSummary[];
  recentInvoices: InvoiceSummary[];
}

export interface CatalogProduct {
  id: string;
  sku: string;
  name: string;
  shortDescription: string | null;
  unit: string;
  price: number;
  currency: string;
  minOrderQuantity?: number | null;
}

export interface PortalAddress {
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

export interface PortalAddressInput {
  label: string;
  line1: string;
  line2?: string | null;
  city?: string | null;
  state?: string | null;
  postalCode?: string | null;
  country?: string | null;
  isPrimary: boolean;
}

export interface CreditSnapshot {
  customerId: string;
  currency: string;
  limit: number;
  outstanding: number;
  available: number;
  usagePercent: number;
  isSoftLimitReached: boolean;
  isHardLimitReached: boolean;
}

export interface DealerAccount {
  id: string;
  code: string;
  name: string;
  legalName: string | null;
  taxNumber: string | null;
  email: string | null;
  phone: string | null;
  address: string | null;
  notes: string | null;
  status: DealerAccountStatus;
  createdByUserId: string | null;
  suspensionReason: string | null;
  createdAtUtc: string;
}

export interface DealerUser {
  id: string;
  dealerAccountId: string;
  dealerAccountName: string;
  userId: string;
  userEmail: string;
  userFirstName: string | null;
  userLastName: string | null;
  membershipRole: 'DealerOwner' | 'DealerStaff';
  status: MembershipStatus;
  invitedByUserId: string | null;
  invitedAtUtc: string;
  acceptedAtUtc: string | null;
  lastLoginAtUtc: string | null;
  suspensionReason: string | null;
  createdAtUtc: string;
}

export interface DealerCustomerLink {
  id: string;
  dealerAccountId: string;
  dealerAccountName: string;
  customerId: string;
  customerName: string;
  status: MembershipStatus;
  assignedByUserId: string | null;
  assignedAtUtc: string;
  revokedAtUtc: string | null;
  notes: string | null;
}
