import { useCallback, useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, X } from 'lucide-react';
import { ConfirmContext, type ConfirmFn, type ConfirmOptions } from './useConfirm';

interface State {
  open: boolean;
  options: ConfirmOptions | null;
  resolve: ((value: boolean) => void) | null;
}

export const ConfirmDialogProvider = ({ children }: { children: React.ReactNode }) => {
  const { t } = useTranslation();
  const [state, setState] = useState<State>({ open: false, options: null, resolve: null });
  const confirmButtonRef = useRef<HTMLButtonElement>(null);

  const confirm: ConfirmFn = useCallback((options) => {
    return new Promise<boolean>((resolve) => {
      setState({ open: true, options, resolve });
    });
  }, []);

  const close = useCallback(
    (result: boolean) => {
      state.resolve?.(result);
      setState({ open: false, options: null, resolve: null });
    },
    [state],
  );

  useEffect(() => {
    if (!state.open) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') close(false);
    };
    document.addEventListener('keydown', handler);
    requestAnimationFrame(() => confirmButtonRef.current?.focus());
    return () => document.removeEventListener('keydown', handler);
  }, [state.open, close]);

  const tone = state.options?.tone ?? 'default';
  const confirmClass =
    tone === 'danger'
      ? 'bg-red-600 hover:bg-red-700 text-white'
      : 'bg-indigo-600 hover:bg-indigo-700 text-white';

  return (
    <ConfirmContext.Provider value={confirm}>
      {children}
      {state.open && state.options && (
        <div
          role="dialog"
          aria-modal="true"
          aria-labelledby="confirm-title"
          className="fixed inset-0 z-[60] flex items-center justify-center bg-black/40 p-4"
          onClick={() => close(false)}
        >
          <div
            className="relative w-full max-w-md rounded-lg border border-slate-200 bg-white shadow-2xl dark:border-slate-800 dark:bg-slate-900"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-start gap-3 p-5">
              {tone === 'danger' && (
                <AlertTriangle
                  className="mt-0.5 size-5 shrink-0 text-red-500 dark:text-red-400"
                  aria-hidden
                />
              )}
              <div className="min-w-0 flex-1">
                <h3
                  id="confirm-title"
                  className="text-base font-semibold text-slate-900 dark:text-slate-100"
                >
                  {state.options.title}
                </h3>
                <p className="mt-1 text-sm text-slate-600 dark:text-slate-300">
                  {state.options.message}
                </p>
              </div>
              <button
                type="button"
                onClick={() => close(false)}
                className="rounded p-1 text-slate-500 hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:hover:bg-slate-800"
                aria-label={t('common.cancel')}
              >
                <X size={16} />
              </button>
            </div>
            <div className="flex items-center justify-end gap-2 border-t border-slate-200 bg-slate-50 px-5 py-3 dark:border-slate-800 dark:bg-slate-800/50">
              <button
                type="button"
                onClick={() => close(false)}
                className="rounded-md border border-slate-200 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
              >
                {state.options.cancelLabel ?? t('common.cancel')}
              </button>
              <button
                ref={confirmButtonRef}
                type="button"
                onClick={() => close(true)}
                className={`rounded-md px-3 py-1.5 text-sm font-semibold focus:outline-none focus:ring-2 focus:ring-indigo-500 ${confirmClass}`}
              >
                {state.options.confirmLabel ?? t('common.confirm')}
              </button>
            </div>
          </div>
        </div>
      )}
    </ConfirmContext.Provider>
  );
};
