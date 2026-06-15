export interface PersonalProfileDto {
  id: string;
  username: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  phoneNumber: string | null;
  createdAtUtc: string;
  lastLoginAtUtc: string | null;
  roles: string[];
}

export interface PersonalOrderDto {
  id: string;
  orderNumber: string;
  orderDate: string;
  status: string;
  total: number;
}

export interface PersonalActivityDto {
  atUtc: string;
  method: string;
  path: string;
  statusCode: number;
  ipAddress: string | null;
}

export interface PersonalMembershipDto {
  id: string;
  organizationId: string;
  organizationName: string;
  role: string;
  status: string;
  joinedAtUtc: string;
  acceptedAtUtc: string | null;
}

export interface PersonalDataExportDto {
  profile: PersonalProfileDto;
  customerMemberships: PersonalMembershipDto[];
  dealerMemberships: PersonalMembershipDto[];
  orders: PersonalOrderDto[];
  recentActivity: PersonalActivityDto[];
  exportedAtUtc: string;
}

export interface ErasureResultDto {
  userId: string;
  anonymizedAtUtc: string;
  notice: string;
}

export type DataSubjectRequestType =
  | 'Export'
  | 'Erasure'
  | 'Access'
  | 'Rectification'
  | 'Portability'
  | 'Restriction'
  | 'Objection';

export type DataSubjectRequestStatus = 'Submitted' | 'InProgress' | 'Completed' | 'Rejected';

export type RetentionActionOnExpiry = 'Anonymize' | 'Archive' | 'Delete';

export type LegalBasisOverride =
  | 'None'
  | 'Consent'
  | 'Contract'
  | 'LegalObligation'
  | 'VitalInterest'
  | 'PublicTask'
  | 'LegitimateInterest';

export interface DataSubjectRequestDto {
  id: string;
  tenantId: string;
  type: DataSubjectRequestType;
  status: DataSubjectRequestStatus;
  requesterUserId: string | null;
  requesterCustomerId: string | null;
  submittedAtUtc: string;
  completedAtUtc: string | null;
  rejectionReason: string | null;
  dataExportFileId: string | null;
  legalBasisOverride: LegalBasisOverride;
  notes: string | null;
}

export interface SubmitDataSubjectRequestBody {
  type: DataSubjectRequestType;
  requesterUserId?: string | null;
  requesterCustomerId?: string | null;
  requesterEmail?: string | null;
  notes?: string | null;
}

export type ProcessAction = 'Access' | 'Erasure' | 'Portability' | 'Rectification' | 'Reject';

export interface RectificationCorrectionsBody {
  firstName?: string | null;
  lastName?: string | null;
  phoneNumber?: string | null;
  email?: string | null;
}

export interface ProcessDataSubjectRequestBody {
  action: ProcessAction;
  keepFinancialTrail?: boolean;
  rejectionReason?: string | null;
  corrections?: RectificationCorrectionsBody | null;
}

export interface RetentionPolicyDto {
  id: string;
  tenantId: string;
  entityType: string;
  retentionDays: number;
  actionOnExpiry: RetentionActionOnExpiry;
  keepFinancialTrail: boolean;
  isEnabled: boolean;
  lastRunAtUtc: string | null;
  lastRunAffectedCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface UpsertRetentionPolicyBody {
  entityType: string;
  retentionDays: number;
  actionOnExpiry: RetentionActionOnExpiry;
  keepFinancialTrail?: boolean;
  isEnabled?: boolean;
}

export interface PagedRequestList {
  items: DataSubjectRequestDto[];
  total: number;
  page: number;
  pageSize: number;
}
