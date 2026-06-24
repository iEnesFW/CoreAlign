import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { CornerDownRight, Trash2 } from 'lucide-react';
import type { Comment } from '../model/collab.types';
import { CommentForm } from './CommentForm';
import { useRelativeTime } from './useRelativeTime';

interface Props {
  comment: Comment;
  replies: Comment[];
  currentUserId: string | null;
  busyDeleting?: boolean;
  canReply: boolean;
  onReply: (body: string, parentCommentId: string) => Promise<void> | void;
  onDelete: (id: string) => void;
}

export const CommentItem = ({
  comment,
  replies,
  currentUserId,
  busyDeleting,
  canReply,
  onReply,
  onDelete,
}: Props) => {
  const { t } = useTranslation();
  const relative = useRelativeTime();
  const [replying, setReplying] = useState(false);

  const isMine = currentUserId !== null && comment.authorUserId === currentUserId;

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-2.5 dark:border-slate-800 dark:bg-slate-900">
      <CommentHeader
        authorName={comment.authorName}
        createdAt={comment.createdAtUtc}
        editedAt={comment.editedAtUtc}
        relative={relative}
      />
      <p className="mt-1 whitespace-pre-wrap text-xs text-slate-800 dark:text-slate-200">
        {comment.body}
      </p>

      <div className="mt-1.5 flex items-center gap-1.5 text-[10px]">
        {canReply && (
          <button
            type="button"
            onClick={() => setReplying((v) => !v)}
            className="rounded px-1.5 py-0.5 font-medium text-primary-600 hover:bg-primary-50 dark:text-primary-300 dark:hover:bg-primary-500/10"
          >
            {replying ? t('common.cancel') : t('collab.comments.reply')}
          </button>
        )}
        {isMine && (
          <button
            type="button"
            onClick={() => onDelete(comment.id)}
            disabled={busyDeleting}
            className="inline-flex items-center gap-0.5 rounded px-1.5 py-0.5 font-medium text-danger-600 hover:bg-danger-50 disabled:opacity-50 dark:text-danger-300 dark:hover:bg-danger-500/10"
          >
            <Trash2 size={10} />
            {t('common.delete')}
          </button>
        )}
      </div>

      {replies.length > 0 && (
        <ul className="mt-2 space-y-1.5 border-l-2 border-slate-100 pl-3 dark:border-slate-800">
          {replies.map((r) => (
            <li key={r.id}>
              <ReplyItem
                comment={r}
                currentUserId={currentUserId}
                busyDeleting={busyDeleting}
                onDelete={onDelete}
              />
            </li>
          ))}
        </ul>
      )}

      {replying && (
        <div className="mt-2">
          <CommentForm
            autoFocus
            placeholder={t('collab.comments.replyPlaceholder')}
            submitLabel={t('collab.comments.reply')}
            onSubmit={async (body) => {
              await onReply(body, comment.id);
              setReplying(false);
            }}
            onCancel={() => setReplying(false)}
          />
        </div>
      )}
    </div>
  );
};

interface ReplyItemProps {
  comment: Comment;
  currentUserId: string | null;
  busyDeleting?: boolean;
  onDelete: (id: string) => void;
}

const ReplyItem = ({ comment, currentUserId, busyDeleting, onDelete }: ReplyItemProps) => {
  const { t } = useTranslation();
  const relative = useRelativeTime();
  const isMine = currentUserId !== null && comment.authorUserId === currentUserId;
  return (
    <div className="flex gap-1.5 rounded border border-slate-100 bg-slate-50/60 px-2 py-1.5 dark:border-slate-800 dark:bg-slate-800/30">
      <CornerDownRight size={12} className="mt-0.5 shrink-0 text-slate-400" />
      <div className="min-w-0 flex-1">
        <CommentHeader
          authorName={comment.authorName}
          createdAt={comment.createdAtUtc}
          editedAt={comment.editedAtUtc}
          relative={relative}
          compact
        />
        <p className="mt-0.5 whitespace-pre-wrap text-[11px] text-slate-700 dark:text-slate-200">
          {comment.body}
        </p>
        {isMine && (
          <button
            type="button"
            onClick={() => onDelete(comment.id)}
            disabled={busyDeleting}
            className="mt-1 inline-flex items-center gap-0.5 rounded px-1 py-0.5 text-[10px] font-medium text-danger-600 hover:bg-danger-50 disabled:opacity-50 dark:text-danger-300 dark:hover:bg-danger-500/10"
          >
            <Trash2 size={9} />
            {t('common.delete')}
          </button>
        )}
      </div>
    </div>
  );
};

interface HeaderProps {
  authorName: string;
  createdAt: string;
  editedAt: string | null;
  relative: (iso: string) => string;
  compact?: boolean;
}

const CommentHeader = ({ authorName, createdAt, editedAt, relative, compact }: HeaderProps) => {
  const { t } = useTranslation();
  return (
    <div className={`flex items-center gap-1.5 ${compact ? 'text-[10px]' : 'text-[11px]'}`}>
      <span className="font-semibold text-slate-800 dark:text-slate-100">{authorName || '—'}</span>
      <span className="text-slate-400">·</span>
      <span className="text-slate-500 dark:text-slate-400">{relative(createdAt)}</span>
      {editedAt && (
        <>
          <span className="text-slate-400">·</span>
          <span className="italic text-slate-400">{t('collab.comments.edited')}</span>
        </>
      )}
    </div>
  );
};
