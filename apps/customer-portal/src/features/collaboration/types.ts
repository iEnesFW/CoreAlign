export interface CommentDto {
  id: string;
  entityType: string;
  entityId: string;
  authorUserId: string;
  authorName: string;
  body: string;
  parentCommentId: string | null;
  createdAtUtc: string;
  editedAtUtc: string | null;
}
