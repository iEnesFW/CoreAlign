import type { FormEventHandler, HTMLAttributes, KeyboardEventHandler, ReactNode } from 'react';
import { X } from 'lucide-react';

interface Props {
  presentation: 'modal' | 'page';
  title: string;
  closeAriaLabel: string;
  onRequestClose: () => void;
  backdropProps?: HTMLAttributes<HTMLDivElement>;
  stepNavigation?: ReactNode;
  renderPageHeader?: (stepNavigation: ReactNode) => ReactNode;
  onSubmit: FormEventHandler<HTMLFormElement>;
  onKeyDown?: KeyboardEventHandler<HTMLFormElement>;
  footer: ReactNode;
  overlay?: ReactNode;
  children: ReactNode;
}

export const DocumentFormLayout = ({
  presentation,
  title,
  closeAriaLabel,
  onRequestClose,
  backdropProps,
  stepNavigation,
  renderPageHeader,
  onSubmit,
  onKeyDown,
  footer,
  overlay,
  children,
}: Props) => {
  const isPage = presentation === 'page';

  const card = (
    <div
      className={
        isPage
          ? 'flex min-h-0 w-full flex-1 flex-col overflow-hidden rounded-[32px] border border-white/20 bg-white/95 shadow-xl backdrop-blur-2xl dark:border-slate-700/50 dark:bg-slate-900/90'
          : 'flex max-h-[92vh] min-h-0 w-full max-w-4xl flex-col overflow-hidden rounded-[32px] border border-white/20 bg-white/95 shadow-2xl backdrop-blur-2xl dark:border-slate-700/50 dark:bg-slate-900/90'
      }
      onClick={isPage ? undefined : (e) => e.stopPropagation()}
      role={isPage ? undefined : 'dialog'}
      aria-modal={isPage ? undefined : true}
    >
      {isPage ? null : (
        <div className="sticky top-0 z-20 flex items-center justify-between border-b border-slate-200/50 bg-slate-50/50 dark:bg-slate-900/50 backdrop-blur-md px-6 py-4 dark:border-slate-800/50">
          <h2 className="text-lg font-bold tracking-tight text-slate-800 dark:text-slate-100">
            {title}
          </h2>
          <button
            type="button"
            onClick={onRequestClose}
            className="rounded-full p-2 text-slate-400 transition-colors hover:bg-slate-200/50 hover:text-slate-700 dark:hover:bg-slate-800 dark:hover:text-slate-200"
            aria-label={closeAriaLabel}
          >
            <X size={20} />
          </button>
        </div>
      )}

      {stepNavigation && (!isPage || !renderPageHeader) && (
        <div className="shrink-0 border-b border-slate-200/50 bg-slate-50/50 px-4 py-2 dark:border-slate-800/50 dark:bg-slate-900/50">
          <div className="flex justify-center">{stepNavigation}</div>
        </div>
      )}

      <form
        onSubmit={onSubmit}
        onKeyDown={onKeyDown}
        noValidate
        className="flex min-h-0 flex-1 flex-col"
      >
        <div className="min-h-0 flex-1 overflow-y-auto px-5 py-4">{children}</div>

        <div className="flex shrink-0 items-center justify-between gap-2 border-t border-slate-200 bg-white px-5 py-3 dark:border-slate-800 dark:bg-slate-900">
          {footer}
        </div>
      </form>
    </div>
  );

  if (isPage) {
    return (
      <div className="flex h-full min-h-0 w-full flex-col gap-4">
        {renderPageHeader?.(stepNavigation)}
        {card}
        {overlay}
      </div>
    );
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      {...backdropProps}
      role="presentation"
    >
      {card}
      {overlay}
    </div>
  );
};
