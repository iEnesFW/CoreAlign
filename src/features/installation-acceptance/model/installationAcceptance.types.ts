export type InstallationAcceptanceStatus =
  | 'Draft'
  | 'InProgress'
  | 'SignedByCustomer'
  | 'Accepted'
  | 'Rejected';

export type InstallationChecklistResult = 'NotEvaluated' | 'Pass' | 'Fail' | 'NotApplicable';

export type PunchListSeverity = 'Minor' | 'Moderate' | 'Critical';

export type PunchListItemStatus = 'Open' | 'InProgress' | 'Resolved' | 'Deferred';

export interface InstallationAcceptance {
  id: string;
  workOrderId: string;
  projectId: string;
  customerId: string;
  status: InstallationAcceptanceStatus;
  startedAtUtc: string;
  completedAtUtc: string | null;
  inspectorUserId: string;
  customerSignatureFileId: string | null;
  customerSignatureCapturedAtUtc: string | null;
  customerName: string | null;
  checklistJson: string;
  photoFileIds: string;
  notesMd: string | null;
  rejectionReason: string | null;
}

export interface PunchListItem {
  id: string;
  acceptanceId: string;
  description: string;
  severity: PunchListSeverity;
  status: PunchListItemStatus;
  assignedToUserId: string | null;
  resolvedAtUtc: string | null;
  resolutionNotes: string | null;
}

export interface AcceptanceFullDetails {
  acceptance: InstallationAcceptance;
  punchList: PunchListItem[];
}

export interface ChecklistItemEntry {
  key: string;
  label?: string;
  result: InstallationChecklistResult;
  notes: string | null;
}

export interface ChecklistCategoryEntry {
  category: string;
  items: ChecklistItemEntry[];
}

export interface StartAcceptanceInput {
  workOrderId: string;
  inspectorUserId: string;
}

export interface UpdateChecklistItemInput {
  acceptanceId: string;
  category: string;
  itemKey: string;
  result: InstallationChecklistResult;
  notes?: string | null;
}

export interface UploadPhotoInput {
  acceptanceId: string;
  fileId: string;
}

export interface CaptureSignatureInput {
  acceptanceId: string;
  fileId: string;
  customerName: string;
}

export interface AcceptInstallationInput {
  acceptanceId: string;
  idempotencyKey: string;
}

export interface RejectInstallationInput {
  acceptanceId: string;
  reason: string;
}

export interface AddPunchListItemInput {
  acceptanceId: string;
  description: string;
  severity: PunchListSeverity;
}

export interface ResolvePunchListItemInput {
  punchItemId: string;
  resolutionNotes?: string | null;
}
