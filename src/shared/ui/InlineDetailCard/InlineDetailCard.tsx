import { PanelRightOpen, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';

export interface InlineDetailCardProps {
  title: string;
  subtitle?: string;
  onOpenPanel?: () => void;
  openPanelLabel?: string;
  onClose: () => void;
  children: React.ReactNode;
}

export const InlineDetailCard = ({
  title,
  subtitle,
  onOpenPanel,
  openPanelLabel,
  onClose,
  children,
}: InlineDetailCardProps) => {
  const { t } = useTranslation();
  const resolvedOpenPanelLabel =
    openPanelLabel ?? t('Common.AllDetails', { defaultValue: 'Tüm detaylar' });

  return (
    <section
      className="animate-[fadeIn_120ms_ease-out] overflow-hidden rounded-xl border border-primary-200/70 bg-white shadow-sm dark:border-primary-500/30 dark:bg-slate-900"
      aria-label={title}
    >
      <header className="flex items-center justify-between gap-2 border-b border-slate-200 bg-slate-50/70 px-4 py-2 dark:border-slate-800 dark:bg-slate-800/40">
        <div className="min-w-0">
          <h3 className="truncate text-sm font-semibold text-slate-900 dark:text-slate-100">
            {title}
          </h3>
          {subtitle && (
            <p className="truncate text-[11px] text-slate-500 dark:text-slate-400">{subtitle}</p>
          )}
        </div>
        <div className="flex shrink-0 items-center gap-1">
          {onOpenPanel && (
            <button
              type="button"
              onClick={onOpenPanel}
              className="inline-flex items-center gap-1 rounded border border-slate-200 bg-white px-2 py-1 text-[11px] font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              <PanelRightOpen size={12} />
              {resolvedOpenPanelLabel}
            </button>
          )}
          <button
            type="button"
            onClick={onClose}
            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:text-slate-500 dark:hover:bg-slate-800 dark:hover:text-slate-200"
            aria-label={t('Common.Close', { defaultValue: 'Kapat' })}
          >
            <X size={15} />
          </button>
        </div>
      </header>
      <div className="max-h-[42vh] overflow-y-auto p-4">{children}</div>
    </section>
  );
};
