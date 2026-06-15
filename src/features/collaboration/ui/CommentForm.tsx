import { useState, type KeyboardEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { Send } from 'lucide-react';

interface Props {
  placeholder?: string;
  submitLabel?: string;
  initialValue?: string;
  disabled?: boolean;
  autoFocus?: boolean;
  onSubmit: (body: string) => Promise<void> | void;
  onCancel?: () => void;
}

/**
 * Small comment composer used both for top-level posts and for replies. Keeps
 * its own draft state; Ctrl/Cmd+Enter submits, Esc cancels (when onCancel set).
 */
export const CommentForm = ({
  placeholder,
  submitLabel,
  initialValue = '',
  disabled = false,
  autoFocus = false,
  onSubmit,
  onCancel,
}: Props) => {
  const { t } = useTranslation();
  const [value, setValue] = useState(initialValue);
  const [busy, setBusy] = useState(false);

  const trimmed = value.trim();
  const canSubmit = trimmed.length > 0 && !disabled && !busy;

  const submit = async () => {
    if (!canSubmit) return;
    setBusy(true);
    try {
      await onSubmit(trimmed);
      setValue('');
    } finally {
      setBusy(false);
    }
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if ((e.metaKey || e.ctrlKey) && e.key === 'Enter') {
      e.preventDefault();
      void submit();
    } else if (e.key === 'Escape' && onCancel) {
      e.preventDefault();
      onCancel();
    }
  };

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-2 dark:border-slate-700 dark:bg-slate-900/50">
      <textarea
        value={value}
        onChange={(e) => setValue(e.target.value)}
        onKeyDown={handleKeyDown}
        placeholder={placeholder ?? t('collab.comments.placeholder')}
        autoFocus={autoFocus}
        disabled={disabled || busy}
        rows={3}
        className="block w-full resize-y rounded border border-transparent bg-slate-50 px-2 py-1.5 text-xs text-slate-800 placeholder:text-slate-400 focus:border-indigo-400 focus:outline-none focus:ring-1 focus:ring-indigo-300 dark:bg-slate-800/60 dark:text-slate-100 dark:placeholder:text-slate-500"
      />
      <div className="mt-1.5 flex items-center justify-between gap-2">
        <span className="text-[10px] text-slate-400 dark:text-slate-500">
          {t('collab.comments.shortcutHint')}
        </span>
        <div className="flex items-center gap-1">
          {onCancel && (
            <button
              type="button"
              onClick={onCancel}
              disabled={busy}
              className="rounded px-2 py-1 text-[11px] font-medium text-slate-600 hover:bg-slate-100 disabled:opacity-50 dark:text-slate-300 dark:hover:bg-slate-800"
            >
              {t('common.cancel')}
            </button>
          )}
          <button
            type="button"
            onClick={() => void submit()}
            disabled={!canSubmit}
            className="inline-flex items-center gap-1 rounded bg-indigo-600 px-2.5 py-1 text-[11px] font-semibold text-white shadow-sm transition hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Send size={11} />
            {submitLabel ?? t('collab.comments.submit')}
          </button>
        </div>
      </div>
    </div>
  );
};
