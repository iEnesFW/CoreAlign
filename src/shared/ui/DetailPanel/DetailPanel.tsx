import { useEffect, useRef } from 'react';
import { X } from 'lucide-react';

interface Props {
  open: boolean;
  title: string;
  subtitle?: string;
  onClose: () => void;
  children: React.ReactNode;
  widthClass?: string;
}

export const DetailPanel = ({
  open,
  title,
  subtitle,
  onClose,
  children,
  widthClass = 'w-full sm:max-w-md md:max-w-lg lg:max-w-xl xl:max-w-2xl',
}: Props) => {
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
        className={`fixed inset-0 z-40 bg-black/30 transition-opacity duration-200 ${
          open ? 'pointer-events-auto opacity-100' : 'pointer-events-none opacity-0'
        }`}
        onClick={onClose}
        role="presentation"
      />
      <aside
        ref={asideRef}
        className={`fixed inset-y-0 right-0 z-50 flex h-full flex-col border-l border-slate-200 bg-white shadow-2xl transition-transform duration-200 dark:border-slate-800 dark:bg-slate-900 ${widthClass} ${
          open ? 'translate-x-0' : 'translate-x-full'
        }`}
        aria-hidden={!open}
        aria-modal={open}
        role="dialog"
        aria-label={title}
      >
        <header className="flex items-start justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
          <div className="min-w-0">
            <h2 className="truncate text-base font-semibold text-slate-900 dark:text-slate-100">
              {title}
            </h2>
            {subtitle && (
              <p className="truncate text-xs text-slate-500 dark:text-slate-400">{subtitle}</p>
            )}
          </div>
          <button
            ref={closeButtonRef}
            type="button"
            onClick={onClose}
            className="rounded p-1.5 text-slate-500 hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:hover:bg-slate-800"
            aria-label="Close"
          >
            <X size={16} />
          </button>
        </header>
        <div className="flex-1 overflow-y-auto">{children}</div>
      </aside>
    </>
  );
};

interface PanelTabsProps<T extends string> {
  tabs: { id: T; label: string; icon?: React.ReactNode }[];
  active: T;
  onSelect: (tab: T) => void;
}

export function PanelTabs<T extends string>({ tabs, active, onSelect }: PanelTabsProps<T>) {
  return (
    <div
      role="tablist"
      className="sticky top-0 z-10 flex gap-1 overflow-x-auto border-b border-slate-200 bg-white px-2 dark:border-slate-800 dark:bg-slate-900"
    >
      {tabs.map((tab) => (
        <button
          key={tab.id}
          type="button"
          role="tab"
          aria-selected={active === tab.id}
          onClick={() => onSelect(tab.id)}
          className={`-mb-px flex shrink-0 items-center gap-1.5 border-b-2 px-3 py-2 text-xs font-medium transition focus:outline-none focus:ring-2 focus:ring-indigo-500 ${
            active === tab.id
              ? 'border-indigo-500 text-indigo-600 dark:text-indigo-400'
              : 'border-transparent text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200'
          }`}
        >
          {tab.icon}
          {tab.label}
        </button>
      ))}
    </div>
  );
}
