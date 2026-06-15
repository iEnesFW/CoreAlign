export type WarrantyCoverageType =
  | 'ManufacturerDefect'
  | 'Installation'
  | 'FullService'
  | 'Limited';

export type WarrantyContractStatus = 'Active' | 'Expired' | 'Cancelled' | 'Suspended';

export type MaintenanceScheduleType = 'PreventiveAnnual' | 'SemiAnnual' | 'Quarterly' | 'Custom';

export type ServiceTicketType =
  | 'PreventiveMaintenance'
  | 'WarrantyClaim'
  | 'OutOfWarrantyRepair'
  | 'Inspection';

export type ServiceTicketStatus = 'Open' | 'Assigned' | 'InProgress' | 'Resolved' | 'Cancelled';

export type ServiceTicketPriority = 'Low' | 'Normal' | 'High' | 'Urgent';

export interface WarrantyContract {
  id: string;
  orderId: string;
  invoiceId: string | null;
  customerId: string;
  productId: string | null;
  workOrderId: string | null;
  number: string;
  coverageType: WarrantyCoverageType;
  startDate: string;
  endDate: string;
  warrantyMonths: number;
  status: WarrantyContractStatus;
  termsJson: string;
  notes: string | null;
  cancellationReason: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface MaintenanceSchedule {
  id: string;
  warrantyContractId: string;
  type: MaintenanceScheduleType;
  nextDueDate: string;
  lastCompletedAtUtc: string | null;
  recurrencePattern: string;
  isActive: boolean;
  notes: string | null;
}

export interface ServiceTicket {
  id: string;
  warrantyContractId: string | null;
  customerId: string;
  workOrderId: string | null;
  type: ServiceTicketType;
  status: ServiceTicketStatus;
  priority: ServiceTicketPriority;
  title: string;
  descriptionMd: string;
  reportedAtUtc: string;
  assignedToUserId: string | null;
  resolvedAtUtc: string | null;
  resolutionNotesMd: string | null;
  isUnderWarranty: boolean;
  chargeableAmount: number | null;
}

export interface WarrantyExpiryAlert {
  warrantyContractId: string;
  customerId: string;
  number: string;
  endDate: string;
  daysRemaining: number;
}

export interface CreateWarrantyContractInput {
  orderId: string;
  customerId: string;
  coverageType: WarrantyCoverageType;
  warrantyMonths: number;
  termsJson: string;
  productId?: string | null;
  workOrderId?: string | null;
  invoiceId?: string | null;
  notes?: string | null;
}

export interface ExtendWarrantyContractInput {
  id: string;
  monthsAdded: number;
  reason?: string | null;
}

export interface CancelWarrantyContractInput {
  id: string;
  reason: string;
}

export interface CreateServiceTicketInput {
  customerId: string;
  type: ServiceTicketType;
  priority: ServiceTicketPriority;
  title: string;
  descriptionMd: string;
  warrantyContractId?: string | null;
}

export interface AssignServiceTicketInput {
  id: string;
  userId: string;
}

export interface ResolveServiceTicketInput {
  id: string;
  resolutionNotesMd: string;
  workOrderId?: string | null;
  chargeableAmount?: number | null;
}
