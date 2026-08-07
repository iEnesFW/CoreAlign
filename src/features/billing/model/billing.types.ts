export type SubscriptionOrderStatus =
  | 'Draft'
  | 'PendingPayment'
  | 'Paid'
  | 'Failed'
  | 'Cancelled'
  | 'Expired';

export type TenantModuleSource = 'Trial' | 'Paid' | 'Granted' | 'Comp';

export type PaymentAttemptStatus = 'Initiated' | 'Succeeded' | 'Failed' | 'Cancelled' | 'Refunded';

export type MockApproveAction = 'approve' | 'cancel' | 'fail';

export interface ModulePricePlanDto {
  id: string;
  moduleId: string;
  code: string;
  displayLabel: string;
  durationDays: number;
  price: number;
  currency: string;
  isActive: boolean;
  sortOrder: number;
}

export interface ModuleDto {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  category?: string | null;
  iconKey?: string | null;
  sortOrder: number;
  isActive: boolean;
  isCore: boolean;
  plans: ModulePricePlanDto[];
}

export interface TenantModuleDto {
  id: string;
  moduleId: string;
  code: string;
  name: string;
  startUtc: string;
  endUtc: string | null;
  isCurrentlyActive: boolean;
  source: TenantModuleSource;
  notes?: string | null;
}

export interface SubscriptionOrderItemDto {
  id: string;
  moduleId: string;
  planId: string;
  moduleCode: string;
  moduleName: string;
  planLabel: string;
  durationDays: number;
  unitPrice: number;
  currency: string;
}

export interface PaymentAttemptDto {
  id: string;
  gatewayName: string;
  intentId?: string | null;
  status: PaymentAttemptStatus;
  amount: number;
  currency: string;
  attemptedAtUtc: string;
  completedAtUtc?: string | null;
  failureReason?: string | null;
}

export interface SubscriptionOrderBillingDto {
  buyerName?: string | null;
  buyerSurname?: string | null;
  buyerEmail?: string | null;
  buyerGsmNumber?: string | null;
  buyerIdentityNumberMasked?: string | null;
  billingAddress?: string | null;
  billingCity?: string | null;
  billingCountry?: string | null;
  billingZipCode?: string | null;
}

export interface SubscriptionOrderDto {
  id: string;
  orderNumber: string;
  status: SubscriptionOrderStatus;
  totalAmount: number;
  currency: string;
  createdByUserId: string;
  gatewayName?: string | null;
  gatewayIntentId?: string | null;
  paymentReference?: string | null;
  paidAtUtc?: string | null;
  completedAtUtc?: string | null;
  createdAtUtc: string;
  billingInfo?: SubscriptionOrderBillingDto | null;
  items: SubscriptionOrderItemDto[];
  attempts: PaymentAttemptDto[];
}

export interface SubscriptionOrderCreationResult {
  order: SubscriptionOrderDto;
  gatewayName: string;
  intentId?: string | null;
  redirectUrl?: string | null;
}

export interface SubscriptionBillingInfoInput {
  name: string;
  surname: string;
  email: string;
  gsmNumber: string;
  identityNumber: string;
  address: string;
  city: string;
  country: string;
  zipCode: string;
}

export interface CreateSubscriptionOrderInput {
  items: Array<{ moduleId: string; planId: string }>;
  gatewayName?: string | null;
  billingInfo?: SubscriptionBillingInfoInput | null;
  operationId?: string;
}

export interface PaymentGatewayDescriptor {
  name: string;
  displayLabel: string;
  requiresBillingInfo: boolean;
  isDefault: boolean;
}

export interface MockApproveInput {
  orderId: string;
  action: MockApproveAction;
  reference?: string;
  reason?: string;
}

export interface CartLine {
  module: ModuleDto;
  plan: ModulePricePlanDto;
}
