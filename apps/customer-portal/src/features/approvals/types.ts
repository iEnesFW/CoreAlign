import type { OrderDetail, OrderSummary } from '@/features/portal/types';

export type ApprovalOrderSummary = OrderSummary & {
  originPersona?: string;
  originDealerAccountId?: string | null;
  originDealerName?: string | null;
  dealerApprovalStatus?: 'PendingCustomerApproval' | 'Approved' | 'Rejected' | null;
};

export type ApprovalOrderDetail = OrderDetail & {
  originPersona?: string;
  originDealerAccountId?: string | null;
  originDealerName?: string | null;
  originDealerUserId?: string | null;
  dealerApprovalStatus?: 'PendingCustomerApproval' | 'Approved' | 'Rejected' | null;
  dealerApprovedByUserId?: string | null;
  dealerApprovedByName?: string | null;
  dealerApprovedAtUtc?: string | null;
  dealerRejectionReason?: string | null;
  createdAtUtc?: string;
};
