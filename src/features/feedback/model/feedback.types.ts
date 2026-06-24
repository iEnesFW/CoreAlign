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
