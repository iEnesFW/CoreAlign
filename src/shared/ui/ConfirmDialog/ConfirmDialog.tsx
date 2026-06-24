import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, X } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { ConfirmContext, type ConfirmFn, type ConfirmOptions } from './useConfirm';

interface State {
  open: boolean;
  options: ConfirmOptions | null;
  resolve: ((value: boolean) => void) | null;
}

export const ConfirmDialogProvider = ({ children }: { children: React.ReactNode }) => {
  const { t } = useTranslation();
  const [state, setState] = useState<State>({ open: false, options: null, resolve: null });

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
    return () => document.removeEventListener('keydown', handler);
  }, [state.open, close]);

  const tone = state.options?.tone ?? 'default';
  const isDanger = tone === 'danger';

  return (
    <ConfirmContext.Provider value={confirm}>
      {children}
      {state.open && state.options && (
        <div
          role="dialog"
          aria-modal="true"
          aria-labelledby="confirm-title"
          className="animate-fade-in fixed inset-0 z-[60] flex items-center justify-center bg-slate-900/40 p-4 backdrop-blur-sm"
          onClick={() => close(false)}
        >
          <div
            className="animate-zoom-in relative w-full max-w-md overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-2xl ring-1 ring-black/5 dark:border-white/10 dark:bg-slate-950 dark:ring-white/5"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-start gap-3 p-5">
              <span
                className={`grid h-9 w-9 shrink-0 place-items-center rounded-xl bg-gradient-to-br text-white shadow-md ${
                  isDanger
                    ? 'from-danger-500 to-danger-600 shadow-danger-500/25'
                    : 'from-primary-500 to-primary-600 shadow-primary-500/25'
                }`}
                aria-hidden
              >
                <AlertTriangle className="size-4" />
              </span>
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
                className="rounded-md p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:text-slate-500 dark:hover:bg-white/5 dark:hover:text-slate-200"
                aria-label={t('common.cancel')}
              >
                <X size={16} />
              </button>
            </div>
            <div className="flex items-center justify-end gap-2 border-t border-slate-200/80 bg-slate-50/50 px-5 py-3 dark:border-white/5 dark:bg-slate-900/40">
              <Button variant="ghost" size="sm" onClick={() => close(false)}>
                {state.options.cancelLabel ?? t('common.cancel')}
              </Button>
              <Button
                autoFocus
                size="sm"
                variant={isDanger ? 'danger' : 'primary'}
                onClick={() => close(true)}
              >
                {state.options.confirmLabel ?? t('common.confirm')}
              </Button>
            </div>
          </div>
        </div>
      )}
    </ConfirmContext.Provider>
  );
};
