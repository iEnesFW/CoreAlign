export type FeedbackType = 'Bug' | 'Feature' | 'Improvement' | 'Question' | 'Other';
export type FeedbackPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type FeedbackStatus = 'Open' | 'InProgress' | 'Resolved' | 'Closed' | 'Rejected';

export interface FeedbackTicket {
  id: string;
  type: FeedbackType;
  title: string;
  description: string;
  priority: FeedbackPriority;
  status: FeedbackStatus;
  module: string | null;
  stepsToReproduce: string | null;
  pageUrl: string | null;
  createdByName: string | null;
  adminResponse: string | null;
  attachmentFileName: string | null;
  createdAtUtc: string;
  resolvedAtUtc: string | null;
  createdByUserId: string | null;
  statusChangeCount: number;
  allowedNextStatuses: FeedbackStatus[];
}

export interface CreateFeedbackInput {
  type: FeedbackType;
  title: string;
  description: string;
  priority: FeedbackPriority;
  module?: string | null;
  stepsToReproduce?: string | null;
  pageUrl?: string | null;
}

export interface UpdateFeedbackStatusInput {
  id: string;
  status: FeedbackStatus;
  adminResponse?: string | null;
}

export interface FeedbackComment {
  id: string;
  ticketId: string;
  authorUserId: string | null;
  authorName: string | null;
  body: string;
  isInternal: boolean;
  createdAtUtc: string;
}

export interface FeedbackAttachment {
  id: string;
  ticketId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  createdAtUtc: string;
}

export interface AddFeedbackCommentInput {
  ticketId: string;
  body: string;
  isInternal?: boolean;
}

export const FEEDBACK_ATTACHMENT_MAX = 5;
