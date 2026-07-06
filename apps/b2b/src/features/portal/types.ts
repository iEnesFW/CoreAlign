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

export type DealerApprovalStatus = 'PendingCustomerApproval' | 'Approved' | 'Rejected' | null;

export type InvoiceStatus =
  | 'Draft'
  | 'Issued'
  | 'Sent'
  | 'PartiallyPaid'
  | 'Paid'
  | 'Overdue'
  | 'Void'
  | 'Cancelled';

export type CommissionStatus = 'Accrued' | 'Paid' | 'Cancelled';

export type DealerMembershipRole = 'DealerOwner' | 'DealerStaff';

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface DealerPortalDashboard {
  dealerAccountId: string;
  dealerAccountName: string;
  allowedCustomerCount: number;
  pendingApprovalCount: number;
  totalOpenOrders: number;
  ordersCompletedThisMonth: number;
  recentOrders: OrderSummary[];
}

export interface DealerAllowedCustomer {
  customerId: string;
  code: string | null;
  name: string;
  taxNumber: string | null;
  currency: string;
  defaultPriceListId: string | null;
  defaultPriceListName: string | null;
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
  originPersona: string;
  originDealerAccountId: string | null;
  originDealerName: string | null;
  dealerApprovalStatus: DealerApprovalStatus;
}

export interface OrderLine {
  id: string;
  lineNumber: number;
  productId: string;
  productSku: string;
  productName: string;
  uomCode: string | null;
  quantity: number;
  unitPrice: number;
  lineDiscountAmount: number;
  taxAmount: number;
  lineNetAmount: number;
  lineTotal: number;
  lineNotes: string | null;
}

export interface OrderDetail extends OrderSummary {
  subtotal: number;
  taxTotal: number;
  shippingCost: number;
  customerNotes: string | null;
  internalNotes: string | null;
  notes: string | null;
  lines: OrderLine[];
  dealerApprovedByUserId: string | null;
  dealerApprovedByName: string | null;
  dealerApprovedAtUtc: string | null;
  dealerRejectionReason: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface ProductSummary {
  id: string;
  sku: string;
  name: string;
  shortDescription: string | null;
  unit: string;
  price: number;
  currency: string;
  minOrderQuantity?: number | null;
  stockQuantity?: number | null;
  isStockTracked?: boolean;
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
  customerId: string;
  lines: InvoiceLine[];
}

export interface CommissionEntry {
  id: string;
  orderId: string;
  shipmentId: string | null;
  customerId: string;
  currency: string;
  orderTotal: number;
  commissionPercent: number;
  commissionAmount: number;
  status: CommissionStatus;
  accruedAtUtc: string;
  paidOutAtUtc: string | null;
  notes: string | null;
}

export interface CommissionSummary {
  ytdAccrued: number;
  ytdPaid: number;
  thisMonthAccrued: number;
  thisMonthPaid: number;
  lifetimeAccrued: number;
  lifetimePaid: number;
  currency: string;
}

export interface DealerProfile {
  userId: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  phoneNumber: string | null;
  tenantName: string;
  dealerAccountId: string;
  dealerName: string;
  dealerCode: string;
  membershipRole: DealerMembershipRole;
  lastLoginAtUtc: string | null;
}
