import { useEffect, useRef } from 'react';
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
          'relative flex max-h-[92vh] w-full flex-col overflow-hidden rounded-t-2xl border border-slate-200 bg-white shadow-2xl ring-1 ring-black/5 sm:rounded-2xl dark:border-slate-800 dark:bg-slate-950 dark:ring-white/5',
          'animate-zoom-in',
          sizeMap[size],
          className,
        )}
      >
        {(title || icon) && (
          <header className="flex items-start gap-3 border-b border-slate-200/80 bg-slate-50/40 px-4 py-3 dark:border-slate-800/80 dark:bg-slate-900/40">
            {icon && (
              <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 text-white shadow-md shadow-indigo-500/20">
                {icon}
              </div>
            )}
            <div className="min-w-0 flex-1">
              {title && (
                <h2 className="truncate text-sm font-semibold text-slate-900 dark:text-slate-100">
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
              className="rounded-md p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 dark:hover:bg-slate-800 dark:hover:text-slate-200"
              aria-label="Close"
            >
              <X size={14} />
            </button>
          </header>
        )}
        <div className={cn('flex-1 overflow-y-auto p-4', bodyClassName)}>{children}</div>
        {footer && (
          <footer className="flex items-center justify-end gap-2 border-t border-slate-200/80 bg-slate-50/40 px-4 py-2.5 dark:border-slate-800/80 dark:bg-slate-900/40">
            {footer}
          </footer>
        )}
      </div>
    </div>
  );
};
