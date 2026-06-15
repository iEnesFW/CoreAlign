import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { MessageSquare, Send } from 'lucide-react';
import { Button } from '@/shared/ui/Button';
import { Spinner } from '@/shared/ui/Spinner';
import { formatDateTime } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useOrderComments, usePostOrderComment } from './hooks';

interface CommentsTabProps {
  orderId: string;
}

export const CommentsTab = ({ orderId }: CommentsTabProps) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const [draft, setDraft] = useState('');

  const commentsQuery = useOrderComments(orderId);
  const postMutation = usePostOrderComment(orderId);

  const submit = () => {
    const trimmed = draft.trim();
    if (!trimmed) {
      toast.error(t('comments.empty'));
      return;
    }
    postMutation.mutate(trimmed, {
      onSuccess: () => {
        setDraft('');
        toast.success(t('comments.posted'));
      },
      onError: (caught: unknown) => {
        const err = caught as { normalizedMessage?: string; message?: string };
        toast.error(err.normalizedMessage ?? err.message ?? t('common.errorGeneric'));
      },
    });
  };

  const items = commentsQuery.data ?? [];
  const posting = postMutation.isPending;
  const loading = commentsQuery.isPending;

  return (
    <div className="flex flex-col gap-3">
      <header className="flex items-center justify-between">
        <h3 className="inline-flex items-center gap-2 text-sm font-semibold text-slate-800 dark:text-slate-100">
          <MessageSquare size={16} />
          {t('comments.title')}
        </h3>
        <span className="text-xs text-slate-500 dark:text-slate-400">
          {t('comments.count', { count: items.length })}
        </span>
      </header>

      <div className="max-h-96 overflow-y-auto rounded-xl border border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-900">
        {loading ? (
          <div className="flex items-center justify-center gap-2 px-3 py-6 text-sm text-slate-500">
            <Spinner /> {t('common.loading')}
          </div>
        ) : items.length === 0 ? (
          <p className="px-4 py-6 text-center text-xs text-slate-500 dark:text-slate-400">
            {t('comments.emptyList')}
          </p>
        ) : (
          <ul className="divide-y divide-slate-100 dark:divide-slate-800">
            {items.map((comment) => (
              <li key={comment.id} className="px-4 py-3">
                <div className="flex items-baseline justify-between gap-3">
                  <span className="text-xs font-semibold text-slate-800 dark:text-slate-100">
                    {comment.authorName || t('comments.unknownAuthor')}
                  </span>
                  <span className="text-[11px] text-slate-500 dark:text-slate-400">
                    {formatDateTime(comment.createdAtUtc, locale)}
                  </span>
                </div>
                <p className="mt-1 whitespace-pre-wrap break-words text-sm text-slate-700 dark:text-slate-200">
                  {comment.body}
                </p>
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
        <textarea
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          placeholder={t('comments.placeholder')}
          rows={2}
          maxLength={4000}
          disabled={posting}
          className="min-h-[48px] flex-1 rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-800 placeholder:text-slate-400 focus:border-sky-400 focus:outline-none focus:ring-1 focus:ring-sky-400 disabled:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 dark:placeholder:text-slate-500"
        />
        <Button onClick={submit} disabled={posting || !draft.trim()} size="sm">
          {posting ? <Spinner size={14} className="text-white" /> : <Send size={14} />}
          {t('comments.send')}
        </Button>
      </div>
    </div>
  );
};
