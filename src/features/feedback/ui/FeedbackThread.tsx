import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Lock, Paperclip, Send } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { Textarea } from '@/shared/ui/Textarea/Textarea';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { feedbackThreadApi } from '../api/feedbackApi';
import {
  useAddFeedbackComment,
  useFeedbackAttachmentsQuery,
  useFeedbackCommentsQuery,
} from '../hooks/useFeedback';

interface FeedbackThreadProps {
  ticketId: string;
  canWriteInternal: boolean;
}

const formatMoment = (iso: string, locale: string) =>
  new Intl.DateTimeFormat(locale, {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(iso));

export const FeedbackThread = ({ ticketId, canWriteInternal }: FeedbackThreadProps) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const commentsQuery = useFeedbackCommentsQuery(ticketId);
  const attachmentsQuery = useFeedbackAttachmentsQuery(ticketId);
  const addComment = useAddFeedbackComment();

  const [body, setBody] = useState('');
  const [isInternal, setIsInternal] = useState(false);

  const comments = commentsQuery.data?.data ?? [];
  const attachments = attachmentsQuery.data?.data ?? [];

  const submit = async () => {
    const trimmed = body.trim();
    if (!trimmed) return;
    try {
      await addComment.mutateAsync({ ticketId, body: trimmed, isInternal });
      setBody('');
      setIsInternal(false);
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div className="space-y-3 border-t border-slate-200 pt-3 dark:border-slate-800">
      {attachments.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {attachments.map((a) => (
            <a
              key={a.id}
              href={feedbackThreadApi.attachmentUrl(ticketId, a.id)}
              target="_blank"
              rel="noreferrer"
              className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-2 py-1 text-[11px] text-slate-700 hover:border-primary-300 hover:text-primary-700 dark:border-white/10 dark:bg-slate-900 dark:text-slate-200"
            >
              <Paperclip size={12} className="shrink-0" />
              <span className="max-w-[160px] truncate">{a.fileName}</span>
            </a>
          ))}
        </div>
      )}

      <div className="space-y-2">
        <div className="text-[11px] font-semibold text-slate-500 dark:text-slate-400">
          {t('feedback.comments.title', { defaultValue: 'Konuşma' })}
        </div>
        {comments.length === 0 ? (
          <p className="text-xs text-slate-400 dark:text-slate-500">
            {t('feedback.comments.empty', { defaultValue: 'Henüz yorum yok.' })}
          </p>
        ) : (
          <ul className="space-y-2">
            {comments.map((c) => (
              <li
                key={c.id}
                className={`rounded-lg border px-2.5 py-2 text-xs ${
                  c.isInternal
                    ? 'border-amber-200 bg-amber-50 dark:border-amber-500/30 dark:bg-amber-500/10'
                    : 'border-slate-200 bg-white dark:border-white/10 dark:bg-slate-900'
                }`}
              >
                <div className="mb-1 flex items-center gap-2 text-[10px] text-slate-500 dark:text-slate-400">
                  <span className="font-semibold text-slate-700 dark:text-slate-200">
                    {c.authorName ??
                      t('feedback.comments.unknownAuthor', { defaultValue: 'Bilinmeyen' })}
                  </span>
                  <span>{formatMoment(c.createdAtUtc, locale)}</span>
                  {c.isInternal && (
                    <span className="inline-flex items-center gap-1 rounded bg-amber-100 px-1 py-0.5 font-semibold text-amber-800 dark:bg-amber-500/20 dark:text-amber-200">
                      <Lock size={9} />
                      {t('feedback.comments.internal', { defaultValue: 'Dahili' })}
                    </span>
                  )}
                </div>
                <p className="whitespace-pre-wrap text-slate-700 dark:text-slate-200">{c.body}</p>
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="space-y-2">
        <Textarea
          value={body}
          onChange={(e) => setBody(e.target.value)}
          rows={2}
          placeholder={t('feedback.comments.placeholder', { defaultValue: 'Bir yorum yazın…' })}
        />
        <div className="flex flex-wrap items-center justify-between gap-2">
          {canWriteInternal ? (
            <label className="inline-flex items-center gap-1.5 text-[11px] text-slate-600 dark:text-slate-300">
              <input
                type="checkbox"
                checked={isInternal}
                onChange={(e) => setIsInternal(e.target.checked)}
                className="h-3.5 w-3.5 rounded border-slate-300 text-primary-600 focus:ring-primary-500"
              />
              {t('feedback.comments.internalHint', {
                defaultValue: 'Dahili not (yalnız platform yöneticisi görür)',
              })}
            </label>
          ) : (
            <span />
          )}
          <Button
            type="button"
            size="sm"
            onClick={submit}
            disabled={addComment.isPending || body.trim().length === 0}
          >
            <Send size={13} />
            {t('feedback.comments.send', { defaultValue: 'Gönder' })}
          </Button>
        </div>
      </div>
    </div>
  );
};
