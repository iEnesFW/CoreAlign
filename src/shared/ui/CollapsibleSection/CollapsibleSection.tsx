import { ChevronDown, ChevronUp } from 'lucide-react';
import { usePersistedState } from '@/shared/hooks/usePersistedState';

export interface CollapsibleSectionProps {
  /** Stable key — the collapsed/expanded state is persisted in localStorage under it. */
  storageKey: string;
  /** Toggle label, e.g. "Özet kartları". */
  label: string;
  defaultCollapsed?: boolean;
  children: React.ReactNode;
}

/**
 * Wraps content (e.g. the stat strip) in a header-toggle whose collapsed state
 * the browser remembers per user. Lets users hide the top summary to give the
 * table + inline detail card more vertical room.
 */
export const CollapsibleSection = ({
  storageKey,
  label,
  defaultCollapsed = false,
  children,
}: CollapsibleSectionProps) => {
  const [collapsed, setCollapsed] = usePersistedState(`collapse:${storageKey}`, defaultCollapsed);

  return (
    <div className="space-y-2">
      <button
        type="button"
        onClick={() => setCollapsed((c) => !c)}
        className="inline-flex items-center gap-1 text-[11px] font-medium text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200"
        aria-expanded={!collapsed}
      >
        {collapsed ? <ChevronDown size={13} /> : <ChevronUp size={13} />}
        {collapsed ? `${label} göster` : `${label} gizle`}
      </button>
      {!collapsed && children}
    </div>
  );
};
