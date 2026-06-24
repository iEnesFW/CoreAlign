import { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { X } from 'lucide-react';
import { cn } from '@/shared/lib/cn';

interface Props {
  open: boolean;
  title: string;
  subtitle?: string;
  icon?: React.ReactNode;
  onClose: () => void;
  children: React.ReactNode;
  widthClass?: string;
  headerActions?: React.ReactNode;
  statusBadge?: React.ReactNode;
  tone?: 'indigo' | 'emerald' | 'violet' | 'amber' | 'rose' | 'sky' | 'slate';
}

const toneIconBg: Record<NonNullable<Props['tone']>, string> = {
  indigo: 'from-primary-500 to-purple-600',
  emerald: 'from-success-500 to-teal-600',
  violet: 'from-violet-500 to-fuchsia-600',
  amber: 'from-warning-500 to-warning-600',
  rose: 'from-danger-500 to-pink-600',
  sky: 'from-info-500 to-cyan-600',
  slate: 'from-slate-500 to-slate-700',
};

const toneAccent: Record<NonNullable<Props['tone']>, string> = {
  indigo: 'from-primary-500/15',
  emerald: 'from-success-500/15',
  violet: 'from-violet-500/15',
  amber: 'from-warning-500/15',
  rose: 'from-danger-500/15',
  sky: 'from-info-500/15',
  slate: 'from-slate-500/10',
};

export const DetailPanel = ({
  open,
  title,
  subtitle,
  icon,
  onClose,
  children,
  widthClass = 'w-full sm:max-w-md md:max-w-lg lg:max-w-xl xl:max-w-2xl',
  headerActions,
  statusBadge,
  tone = 'indigo',
}: Props) => {
  const { t } = useTranslation();
  const closeButtonRef = useRef<HTMLButtonElement>(null);
  const previousActiveRef = useRef<HTMLElement | null>(null);
  const asideRef = useRef<HTMLElement>(null);

  useEffect(() => {
    if (!open) return;

    previousActiveRef.current = document.activeElement as HTMLElement | null;
    requestAnimationFrame(() => closeButtonRef.current?.focus());

    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
        return;
      }
      if (e.key !== 'Tab') return;
      const root = asideRef.current;
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
    return () => {
      document.removeEventListener('keydown', handler);
      previousActiveRef.current?.focus?.();
    };
  }, [open, onClose]);

  return (
    <>
      <div
        className={cn(
          'fixed inset-0 z-40 bg-slate-950/40 backdrop-blur-sm transition-opacity duration-200',
          open ? 'pointer-events-auto opacity-100' : 'pointer-events-none opacity-0',
        )}
        onClick={onClose}
        role="presentation"
      />
      <aside
        ref={asideRef}
        className={cn(
          'fixed inset-y-0 right-0 z-50 flex h-full flex-col border-l border-slate-200 bg-white shadow-2xl transition-transform duration-300 ease-[cubic-bezier(0.22,1,0.36,1)] dark:border-slate-800 dark:bg-slate-950',
          widthClass,
          open ? 'translate-x-0' : 'translate-x-full',
        )}
        aria-hidden={!open}
        aria-modal={open}
        role="dialog"
        aria-label={title}
      >
        <header className="relative overflow-hidden border-b border-slate-200 px-4 py-3 dark:border-slate-800">
          <div
            className={cn(
              'pointer-events-none absolute -top-16 -right-16 h-40 w-40 rounded-full bg-gradient-to-br to-transparent blur-3xl',
              toneAccent[tone],
            )}
          />
          <div className="relative flex items-start justify-between gap-3">
            <div className="flex min-w-0 items-center gap-3">
              {icon && (
                <div
                  className={cn(
                    'flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br text-white shadow-md shadow-primary-500/20 ring-1 ring-white/20',
                    toneIconBg[tone],
                  )}
                >
                  {icon}
                </div>
              )}
              <div className="min-w-0">
                <h2 className="flex items-center gap-2 text-base font-semibold text-slate-900 dark:text-slate-100">
                  <span className="truncate">{title}</span>
                  {statusBadge}
                </h2>
                {subtitle && (
                  <p className="truncate text-xs text-slate-500 dark:text-slate-400">{subtitle}</p>
                )}
              </div>
            </div>
            <div className="flex shrink-0 items-center gap-1">
              {headerActions}
              <button
                ref={closeButtonRef}
                type="button"
                onClick={onClose}
                className="rounded-md p-1.5 text-slate-500 transition-colors hover:bg-slate-100 hover:text-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:hover:bg-slate-800 dark:hover:text-slate-200"
                aria-label={t('Common.Close', { defaultValue: 'Close' })}
              >
                <X size={16} />
              </button>
            </div>
          </div>
        </header>
        <div className="flex-1 overflow-y-auto">{children}</div>
      </aside>
    </>
  );
};

interface PanelTabsProps<T extends string> {
  tabs: { id: T; label: string; icon?: React.ReactNode; count?: number }[];
  active: T;
  onSelect: (tab: T) => void;
}

export function PanelTabs<T extends string>({ tabs, active, onSelect }: PanelTabsProps<T>) {
  return (
    <div
      role="tablist"
      className="sticky top-0 z-10 flex gap-0.5 overflow-x-auto border-b border-slate-200 bg-white/95 px-2 backdrop-blur dark:border-slate-800 dark:bg-slate-950/95"
    >
      {tabs.map((tab) => {
        const isActive = active === tab.id;
        return (
          <button
            key={tab.id}
            type="button"
            role="tab"
            aria-selected={isActive}
            onClick={() => onSelect(tab.id)}
            className={cn(
              '-mb-px flex shrink-0 items-center gap-1.5 border-b-2 px-3 py-2 text-xs font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500/40',
              isActive
                ? 'border-primary-500 text-primary-600 dark:text-primary-300'
                : 'border-transparent text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200',
            )}
          >
            {tab.icon}
            {tab.label}
            {tab.count !== undefined && tab.count > 0 && (
              <span
                className={cn(
                  'ml-0.5 rounded-full px-1.5 py-px text-[9px] font-semibold tabular-nums',
                  isActive
                    ? 'bg-primary-100 text-primary-700 dark:bg-primary-500/20 dark:text-primary-300'
                    : 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400',
                )}
              >
                {tab.count}
              </span>
            )}
          </button>
        );
      })}
    </div>
  );
}
