import { ChevronDown, ChevronUp } from 'lucide-react';
import { usePersistedState } from '@/shared/hooks/usePersistedState';

export interface CollapsibleSectionProps {
  storageKey: string;
  label: string;
  defaultCollapsed?: boolean;
  children: React.ReactNode;
}

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
