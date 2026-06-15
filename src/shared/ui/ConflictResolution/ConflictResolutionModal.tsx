import { useTranslation } from 'react-i18next';
import { AlertTriangle } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { cn } from '@/shared/lib/cn';

export interface ConflictResolutionModalProps {
  open: boolean;
  conflictMessage?: string | null;
  currentVersion?: string | null;
  attemptedVersion?: string | null;
  conflictingFields?: string[];
  canOverwrite?: boolean;
  onReload: () => void;
  onForceOverwrite: () => void;
  onCancel: () => void;
}

export const ConflictResolutionModal = ({
  open,
  conflictMessage,
  currentVersion,
  attemptedVersion,
  conflictingFields = [],
  canOverwrite = false,
  onReload,
  onForceOverwrite,
  onCancel,
}: ConflictResolutionModalProps) => {
  const { t } = useTranslation();

  const description = conflictMessage ?? t('Conflict.Description');

  const footer = (
    <>
      <button
        type="button"
        onClick={onCancel}
        className="rounded-md border border-slate-200 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
      >
        {t('Common.Cancel')}
      </button>
      <button
        type="button"
        onClick={onForceOverwrite}
        disabled={!canOverwrite}
        title={canOverwrite ? undefined : t('Conflict.OverwriteWarning')}
        className={cn(
          'rounded-md px-3 py-1.5 text-sm font-semibold transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-red-500',
          canOverwrite
            ? 'bg-red-600 text-white hover:bg-red-700'
            : 'cursor-not-allowed bg-red-200 text-red-700/70 dark:bg-red-900/40 dark:text-red-300/60',
        )}
      >
        {t('Conflict.OverwriteButton')}
      </button>
      <button
        type="button"
        onClick={onReload}
        autoFocus
        className="rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-semibold text-white transition-colors hover:bg-indigo-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500"
      >
        {t('Conflict.ReloadButton')}
      </button>
    </>
  );

  return (
    <Modal
      open={open}
      onClose={onCancel}
      size="md"
      closeOnBackdrop={false}
      title={t('Conflict.Title')}
      icon={<AlertTriangle size={18} aria-hidden />}
      footer={footer}
    >
      <div className="space-y-4">
        <p className="text-sm leading-relaxed text-slate-700 dark:text-slate-200">{description}</p>

        {conflictingFields.length > 0 && (
          <section
            aria-labelledby="conflict-fields-heading"
            className="rounded-lg border border-amber-200 bg-amber-50 p-3 dark:border-amber-900/50 dark:bg-amber-950/30"
          >
            <h3
              id="conflict-fields-heading"
              className="text-xs font-semibold uppercase tracking-wide text-amber-800 dark:text-amber-300"
            >
              {t('Conflict.ConflictingFields')}
            </h3>
            <ul className="mt-2 flex flex-wrap gap-1.5">
              {conflictingFields.map((field) => (
                <li
                  key={field}
                  className="rounded-md bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-900 dark:bg-amber-900/50 dark:text-amber-100"
                >
                  {field}
                </li>
              ))}
            </ul>
          </section>
        )}

        <p
          role="alert"
          className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700 dark:border-red-900/50 dark:bg-red-950/30 dark:text-red-300"
        >
          {t('Conflict.OverwriteWarning')}
        </p>

        {(currentVersion || attemptedVersion) && (
          <dl className="grid grid-cols-1 gap-1 text-[11px] text-slate-500 sm:grid-cols-2 dark:text-slate-400">
            {currentVersion && (
              <div className="flex flex-col">
                <dt className="font-medium uppercase tracking-wide">
                  {t('Conflict.CurrentVersion')}
                </dt>
                <dd className="break-all font-mono">{currentVersion}</dd>
              </div>
            )}
            {attemptedVersion && (
              <div className="flex flex-col">
                <dt className="font-medium uppercase tracking-wide">
                  {t('Conflict.AttemptedVersion')}
                </dt>
                <dd className="break-all font-mono">{attemptedVersion}</dd>
              </div>
            )}
          </dl>
        )}
      </div>
    </Modal>
  );
};
