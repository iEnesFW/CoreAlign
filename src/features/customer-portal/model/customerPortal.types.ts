import type {
  ServiceTicket,
  ServiceTicketPriority,
  ServiceTicketType,
  WarrantyContract,
} from '@/features/warranty/model/warranty.types';
import type { Invoice, InvoiceSummary } from '@/features/invoices/model/invoice.types';
import type { Payment, PaymentSummary } from '@/features/payments/model/payment.types';

export type GlassProjectStatus =
  | 'Draft'
  | 'InDesign'
  | 'AwaitingApproval'
  | 'Approved'
  | 'InProduction'
  | 'Shipped'
  | 'Installed'
  | 'Closed'
  | 'Cancelled';

export interface MyGlassProjectSummary {
  id: string;
  code: string;
  projectName: string;
  customerId: string;
  customerName: string | null;
  status: GlassProjectStatus;
  grandTotal: number;
  currency: string;
  totalPanels: number;
  totalAreaM2: number;
  updatedAtUtc: string;
}

export interface MyProjectInstallationStatus {
  id: string;
  code: string;
  projectName: string;
  status: GlassProjectStatus;
  siteCity: string | null;
  siteDistrict: string | null;
  validUntilDate: string | null;
  updatedAtUtc: string;
}

export interface PortalBillingInfoInput {
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

export interface InitiatePaymentInput {
  invoiceId: string;
  billingInfo?: PortalBillingInfoInput | null;
  gatewayName?: string | null;
}

export interface InitiatePaymentResult {
  paymentSessionId: string;
  gatewayName: string;
  intentId: string;
  redirectUrl: string | null;
  amount: number;
  currency: string;
  invoiceNumber: string;
}

export interface CreateMyServiceTicketInput {
  type: ServiceTicketType;
  priority: ServiceTicketPriority;
  title: string;
  descriptionMd: string;
  warrantyContractId?: string | null;
}

export type MyWarrantyContract = WarrantyContract;
export type MyServiceTicket = ServiceTicket;
export type MyInvoiceSummary = InvoiceSummary;
export type MyInvoice = Invoice;
export type MyPaymentSummary = PaymentSummary;
export type MyPayment = Payment;
