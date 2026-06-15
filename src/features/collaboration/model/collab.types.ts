export type CollabEntityType = 'Order' | 'VendorBill' | 'Shipment' | 'SubscriptionOrder';

export interface Comment {
  id: string;
  entityType: CollabEntityType;
  entityId: string;
  authorUserId: string;
  authorName: string;
  body: string;
  parentCommentId: string | null;
  createdAtUtc: string;
  editedAtUtc: string | null;
}

export interface CreateCommentInput {
  entityType: CollabEntityType;
  entityId: string;
  body: string;
  parentCommentId?: string | null;
}

export interface EditCommentInput {
  id: string;
  body: string;
}

export type NotificationType = 'CommentPosted' | string;

export interface Notification {
  id: string;
  type: NotificationType;
  entityType: CollabEntityType;
  entityId: string;
  title: string;
  body: string;
  actorUserId: string | null;
  actorName: string | null;
  isRead: boolean;
  createdAtUtc: string;
}
