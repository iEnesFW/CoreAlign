import { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { X } from 'lucide-react';
import { cn } from '@/shared/lib/cn';

type Size = 'sm' | 'md' | 'lg' | 'xl' | '2xl';

const sizeMap: Record<Size, string> = {
  sm: 'max-w-sm',
  md: 'max-w-md',
  lg: 'max-w-lg',
  xl: 'max-w-2xl',
  '2xl': 'max-w-4xl',
};

interface Props {
  open: boolean;
  title?: string;
  subtitle?: string;
  icon?: React.ReactNode;
  onClose: () => void;
  children: React.ReactNode;
  footer?: React.ReactNode;
  size?: Size;
  closeOnBackdrop?: boolean;
  className?: string;
  bodyClassName?: string;
}

export const Modal = ({
  open,
  title,
  subtitle,
  icon,
  onClose,
  children,
  footer,
  size = 'md',
  closeOnBackdrop = true,
  className,
  bodyClassName,
}: Props) => {
  const { t } = useTranslation();
  const closeRef = useRef<HTMLButtonElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
      if (e.key !== 'Tab') return;
      const root = containerRef.current;
      if (!root) return;
      const focusable = root.querySelectorAll<HTMLElement>(
        'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])',
      );
      if (focusable.length === 0) return;
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (e.shiftKey && document.activeElement === first) {
        e.preventDefault();
        last.focus();
      } else if (!e.shiftKey && document.activeElement === last) {
        e.preventDefault();
        first.focus();
      }
    };
    document.addEventListener('keydown', handler);
    requestAnimationFrame(() => closeRef.current?.focus());
    return () => document.removeEventListener('keydown', handler);
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center sm:items-center sm:p-4">
      <div
        className="absolute inset-0 bg-slate-900/40 backdrop-blur-sm animate-fade-in"
        onClick={closeOnBackdrop ? onClose : undefined}
        aria-hidden
      />
      <div
        ref={containerRef}
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className={cn(
          'relative flex max-h-[92vh] w-full flex-col overflow-hidden rounded-t-[32px] border border-white/20 bg-white/95 backdrop-blur-2xl shadow-2xl ring-1 ring-black/5 sm:rounded-3xl dark:border-slate-700/50 dark:bg-slate-900/90 dark:ring-white/5',
          'animate-in zoom-in-95 duration-300 ease-out',
          sizeMap[size],
          className,
        )}
      >
        {(title || icon) && (
          <header className="flex items-center gap-4 border-b border-slate-200/50 px-6 py-4 dark:border-slate-800/50 bg-transparent">
            {icon && (
              <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-indigo-50 dark:bg-indigo-500/10 text-indigo-600 dark:text-indigo-400">
                {icon}
              </div>
            )}
            <div className="min-w-0 flex-1">
              {title && (
                <h2 className="truncate text-[17px] font-bold tracking-tight text-slate-800 dark:text-slate-100">
                  {title}
                </h2>
              )}
              {subtitle && (
                <p className="truncate text-[11px] text-slate-500 dark:text-slate-400">
                  {subtitle}
                </p>
              )}
            </div>
            <button
              ref={closeRef}
              type="button"
              onClick={onClose}
              className="rounded-full p-2 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 dark:hover:bg-slate-800 dark:hover:text-slate-200"
              aria-label={t('Common.Close', { defaultValue: 'Close' })}
            >
              <X size={16} />
            </button>
          </header>
        )}
        <div className={cn('flex-1 overflow-y-auto px-6 py-5', bodyClassName)}>{children}</div>
        {footer && (
          <footer className="flex items-center justify-end gap-3 border-t border-slate-200/50 bg-slate-50/30 px-6 py-4 dark:border-slate-800/50 dark:bg-slate-900/30">
            {footer}
          </footer>
        )}
      </div>
    </div>
  );
};
