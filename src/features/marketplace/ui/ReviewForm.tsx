import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { useRateTemplateMutation } from '../hooks/useMarketplace';
import { TemplateRatingStars } from './TemplateRatingStars';

interface ReviewFormProps {
  templateId: string;
  onSubmitted?: () => void;
}

export const ReviewForm = ({ templateId, onSubmitted }: ReviewFormProps) => {
  const { t } = useTranslation();
  const mutation = useRateTemplateMutation();
  const [rating, setRating] = useState(0);
  const [comment, setComment] = useState('');

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (rating < 1) {
      toast.error(t('Marketplace.Reviews.SelectRating', 'Please select a rating'));
      return;
    }
    try {
      await mutation.mutateAsync({
        templateId,
        ratingStars: rating,
        commentMd: comment.trim() ? comment.trim() : null,
      });
      toast.success(t('Marketplace.Reviews.Submitted', 'Review submitted'));
      setRating(0);
      setComment('');
      onSubmitted?.();
    } catch {
      toast.error(t('Marketplace.Reviews.SubmitFailed', 'Failed to submit review'));
    }
  };

  return (
    <form
      onSubmit={handleSubmit}
      className="space-y-3 rounded-md border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900"
    >
      <h4 className="text-sm font-semibold text-slate-800 dark:text-slate-100">
        {t('Marketplace.Reviews.LeaveReview', 'Leave a review')}
      </h4>
      <TemplateRatingStars rating={rating} interactive onSelect={setRating} size={20} />
      <textarea
        value={comment}
        onChange={(event) => setComment(event.target.value)}
        rows={3}
        maxLength={4000}
        placeholder={t('Marketplace.Reviews.CommentPlaceholder', 'Share your experience...')}
        className="w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm focus:border-success-500 focus:outline-none dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
      />
      <div className="flex justify-end">
        <button
          type="submit"
          disabled={mutation.isPending}
          className="rounded-md bg-success-600 px-4 py-1.5 text-sm font-semibold text-white hover:bg-success-700 disabled:opacity-50"
        >
          {mutation.isPending
            ? t('Marketplace.Reviews.Submitting', 'Submitting...')
            : t('Marketplace.Reviews.Submit', 'Submit review')}
        </button>
      </div>
    </form>
  );
};
