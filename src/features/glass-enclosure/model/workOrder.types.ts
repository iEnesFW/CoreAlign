export type WorkOrderRevisionStatus =
  | 'SilentSnapshot'
  | 'PendingApproval'
  | 'Approved'
  | 'Rejected'
  | 'Blocked';

export interface WorkOrderRevisionDto {
  id: string;
  workOrderId: string;
  revisionNumber: number;
  deltaPercent: number;
  status: WorkOrderRevisionStatus;
  reason: string;
  rejectionReason: string | null;
  overrideReason: string | null;
  previousSnapshotJson: string | null;
  newSnapshotJson: string;
  deltaJson: string | null;
  createdByUserId: string;
  approvedByUserId: string | null;
  createdAtUtc: string;
  approvedAtUtc: string | null;
}

export interface ApproveWorkOrderRevisionInput {
  overrideReason?: string;
}

export interface RejectWorkOrderRevisionInput {
  reason: string;
}
