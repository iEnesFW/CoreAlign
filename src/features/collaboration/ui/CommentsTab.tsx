import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { MessageSquare } from 'lucide-react';
import { useAuthStore } from '@/features/auth/model/authStore';
import { toastApiError } from '@/shared/lib/mutationToast';
import type { CollabEntityType, Comment } from '../model/collab.types';
import { useComments, useCreateComment, useDeleteComment } from '../hooks/useCollab';
import { CommentForm } from './CommentForm';
import { CommentItem } from './CommentItem';

interface Props {
  entityType: CollabEntityType;
  entityId: string;
}

interface Thread {
  top: Comment;
  replies: Comment[];
}

const groupThreads = (comments: Comment[]): Thread[] => {
  const topLevel = comments.filter((c) => !c.parentCommentId);
  const repliesByParent = new Map<string, Comment[]>();
  for (const c of comments) {
    if (!c.parentCommentId) continue;
    const arr = repliesByParent.get(c.parentCommentId) ?? [];
    arr.push(c);
    repliesByParent.set(c.parentCommentId, arr);
  }
  return topLevel.map((top) => ({
    top,
    replies: (repliesByParent.get(top.id) ?? []).sort(
      (a, b) => Date.parse(a.createdAtUtc) - Date.parse(b.createdAtUtc),
    ),
  }));
};

export const CommentsTab = ({ entityType, entityId }: Props) => {
  const { t } = useTranslation();
  const currentUserId = useAuthStore((s) => s.user?.id ?? null);

  const commentsQuery = useComments(entityType, entityId);
  const createMutation = useCreateComment(entityType, entityId);
  const deleteMutation = useDeleteComment(entityType, entityId);

  const items = commentsQuery.data?.data ?? [];
  const threads = groupThreads(items);

  const handleCreate = async (body: string, parentCommentId?: string) => {
    try {
      const response = await createMutation.mutateAsync({
        body,
        parentCommentId: parentCommentId ?? null,
      });
      if (!response.isSuccess) {
        toast.error(response.errors?.[0] ?? t('auth.common.unexpectedError'));
      }
    } catch (err) {
      toastApiError(err, t('auth.common.unexpectedError'));
    }
  };

  const handleDelete = (id: string) => {
    deleteMutation.mutate(id, {
      onSuccess: (response) => {
        if (response.isSuccess) {
          toast.success(t('collab.comments.deleted'));
        } else {
          toast.error(response.errors?.[0] ?? t('auth.common.unexpectedError'));
        }
      },
      onError: (err) => toastApiError(err, t('auth.common.unexpectedError')),
    });
  };

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="inline-flex items-center gap-1.5 text-sm font-semibold text-slate-700 dark:text-slate-200">
          <MessageSquare size={14} />
          {t('collab.comments.title')}
        </h3>
        <span className="text-[11px] text-slate-500 dark:text-slate-400">
          {t('collab.comments.count', { count: items.length })}
        </span>
      </div>

      {commentsQuery.isPending ? (
        <div className="px-3 py-6 text-center text-sm text-slate-500">{t('common.loading')}</div>
      ) : threads.length === 0 ? (
        <div className="rounded border border-dashed border-slate-300 p-6 text-center text-xs text-slate-500 dark:border-slate-700">
          {t('collab.comments.empty')}
        </div>
      ) : (
        <ul className="space-y-2">
          {threads.map((thread) => (
            <li key={thread.top.id}>
              <CommentItem
                comment={thread.top}
                replies={thread.replies}
                currentUserId={currentUserId}
                busyDeleting={deleteMutation.isPending}
                canReply
                onReply={(body, parentCommentId) => handleCreate(body, parentCommentId)}
                onDelete={handleDelete}
              />
            </li>
          ))}
        </ul>
      )}

      <CommentForm onSubmit={(body) => handleCreate(body)} disabled={createMutation.isPending} />
    </div>
  );
};
