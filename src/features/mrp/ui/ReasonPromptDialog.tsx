import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { X } from 'lucide-react';

interface Props {
  title: string;
  confirmLabel: string;
  confirmTone?: 'rose' | 'slate';
  isSubmitting?: boolean;
  onConfirm: (reason: string | null) => void;
  onCancel: () => void;
}

export const ReasonPromptDialog = ({
  title,
  confirmLabel,
  confirmTone = 'rose',
  isSubmitting = false,
  onConfirm,
  onCancel,
}: Props) => {
  const { t } = useTranslation();
  const [reason, setReason] = useState<string>('');

  const confirmClass =
    confirmTone === 'rose' ? 'bg-rose-600 hover:bg-rose-500' : 'bg-slate-600 hover:bg-slate-500';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
      <div
        role="dialog"
        aria-label={title}
        className="w-full max-w-md rounded-lg border border-slate-200 bg-white p-4 shadow-xl dark:border-slate-700 dark:bg-slate-900"
      >
        <header className="mb-3 flex items-center justify-between">
          <h2 className="text-sm font-semibold text-slate-800 dark:text-slate-100">{title}</h2>
          <button
            type="button"
            onClick={onCancel}
            aria-label={t('Common.Close') ?? 'Close'}
            className="rounded p-1 text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-700"
          >
            <X className="h-4 w-4" />
          </button>
        </header>
        <label className="flex flex-col gap-1">
          <span className="text-xs font-medium text-slate-600 dark:text-slate-300">
            {t('Mrp.Requisition.ReasonPrompt')}
          </span>
          <textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            rows={3}
            className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
          />
        </label>
        <div className="mt-3 flex justify-end gap-2">
          <button
            type="button"
            onClick={onCancel}
            className="rounded-md border border-slate-300 px-3 py-2 text-sm text-slate-700 hover:bg-slate-100 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-700"
          >
            {t('Common.Cancel')}
          </button>
          <button
            type="button"
            disabled={isSubmitting}
            onClick={() => onConfirm(reason.trim() ? reason.trim() : null)}
            className={`rounded-md px-3 py-2 text-sm font-medium text-white disabled:cursor-not-allowed disabled:opacity-60 ${confirmClass}`}
          >
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
};
